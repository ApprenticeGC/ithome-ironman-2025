using System.Collections.Concurrent;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Handles plugin failure recovery and restoration operations.
/// Provides automatic recovery mechanisms with exponential backoff and manual recovery operations.
/// </summary>
public class PluginRecoveryService : IPluginRecoveryService, IDisposable
{
    private readonly ILogger<PluginRecoveryService> _logger;
    private readonly IPluginStateTracker _stateTracker;
    private readonly ConcurrentDictionary<IPlugin, PluginRecoveryInfo> _recoveryInfo = new();
    private readonly object _lockObject = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginRecoveryService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="stateTracker">Plugin state tracker.</param>
    public PluginRecoveryService(ILogger<PluginRecoveryService> logger, IPluginStateTracker stateTracker)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        
        // Subscribe to unhealthy plugin notifications for automatic recovery
        _stateTracker.PluginUnhealthy += OnPluginUnhealthy;
        
        _logger.LogInformation("PluginRecoveryService initialized with max auto recovery attempts: {MaxAttempts}", MaxAutoRecoveryAttempts);
    }

    /// <inheritdoc />
    public int MaxAutoRecoveryAttempts { get; } = 3;

    /// <inheritdoc />
    public async Task<bool> RecoverPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        lock (_lockObject)
        {
            if (!_recoveryInfo.TryGetValue(plugin, out var info))
            {
                info = new PluginRecoveryInfo();
                _recoveryInfo[plugin] = info;
            }

            info.AttemptCount++;
            info.LastAttempt = DateTimeOffset.UtcNow;
        }

        var currentState = _stateTracker.GetPluginState(plugin);
        var attemptNumber = _recoveryInfo[plugin].AttemptCount;
        
        _logger.LogInformation("Starting recovery attempt #{AttemptNumber} for plugin {PluginId} (current state: {CurrentState})", 
            attemptNumber, plugin.Metadata.Id, currentState);

        try
        {
            // Set recovery state
            _stateTracker.SetPluginState(plugin, PluginState.Recovering);

            // Recovery strategy based on current state
            bool success = currentState switch
            {
                PluginState.Failed => await RecoverFromFailedStateAsync(plugin, cancellationToken),
                PluginState.Running when !plugin.IsRunning => await RecoverFromRunningMismatchAsync(plugin, cancellationToken),
                PluginState.Stopped when plugin.IsRunning => await RecoverFromStoppedMismatchAsync(plugin, cancellationToken),
                _ => await PerformGenericRecoveryAsync(plugin, cancellationToken)
            };

            if (success)
            {
                _logger.LogInformation("Recovery successful for plugin {PluginId} after {AttemptNumber} attempts", 
                    plugin.Metadata.Id, attemptNumber);
                
                // Reset recovery info on successful recovery
                lock (_lockObject)
                {
                    if (_recoveryInfo.TryGetValue(plugin, out var recoveryInfo))
                    {
                        recoveryInfo.AttemptCount = 0;
                        recoveryInfo.LastSuccessfulRecovery = DateTimeOffset.UtcNow;
                    }
                }
            }
            else
            {
                _logger.LogWarning("Recovery failed for plugin {PluginId} (attempt #{AttemptNumber})", 
                    plugin.Metadata.Id, attemptNumber);
                
                _stateTracker.SetPluginState(plugin, PluginState.Failed, 
                    new InvalidOperationException($"Recovery attempt #{attemptNumber} failed"));
            }

            // Fire recovery completed event
            FireRecoveryCompletedEvent(plugin, success, attemptNumber, null);
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery attempt #{AttemptNumber} failed for plugin {PluginId}", 
                attemptNumber, plugin.Metadata.Id);
            
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            
            // Fire recovery completed event
            FireRecoveryCompletedEvent(plugin, false, attemptNumber, ex);
            
            return false;
        }
    }

    /// <inheritdoc />
    public Task<bool> CreateCheckpointAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            var currentState = _stateTracker.GetPluginState(plugin);
            
            if (!_recoveryInfo.TryGetValue(plugin, out var info))
            {
                info = new PluginRecoveryInfo();
                _recoveryInfo[plugin] = info;
            }

            info.CheckpointState = currentState;
            info.CheckpointTimestamp = DateTimeOffset.UtcNow;
            info.HasCheckpoint = true;

            _logger.LogDebug("Created checkpoint for plugin {PluginId} at state {State}", 
                plugin.Metadata.Id, currentState);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkpoint for plugin {PluginId}", plugin.Metadata.Id);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestoreFromCheckpointAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (!_recoveryInfo.TryGetValue(plugin, out var info) || !info.HasCheckpoint)
        {
            _logger.LogWarning("No checkpoint available for plugin {PluginId}", plugin.Metadata.Id);
            return false;
        }

        try
        {
            var targetState = info.CheckpointState ?? PluginState.Stopped;
            
            _logger.LogInformation("Restoring plugin {PluginId} to checkpoint state {TargetState}", 
                plugin.Metadata.Id, targetState);

            // Stop plugin if running
            if (plugin.IsRunning)
            {
                _stateTracker.SetPluginState(plugin, PluginState.Stopping);
                await plugin.StopAsync(cancellationToken);
                _stateTracker.SetPluginState(plugin, PluginState.Stopped);
            }

            // Restore to checkpoint state
            if (targetState == PluginState.Running)
            {
                _stateTracker.SetPluginState(plugin, PluginState.Starting);
                await plugin.StartAsync(cancellationToken);
                _stateTracker.SetPluginState(plugin, PluginState.Running);
            }

            _logger.LogInformation("Successfully restored plugin {PluginId} to checkpoint state {TargetState}", 
                plugin.Metadata.Id, targetState);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore plugin {PluginId} from checkpoint", plugin.Metadata.Id);
            return false;
        }
    }

    /// <inheritdoc />
    public bool IsAutoRecoveryEnabled(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        
        return _recoveryInfo.TryGetValue(plugin, out var info) && info.AutoRecoveryEnabled;
    }

    /// <inheritdoc />
    public void SetAutoRecoveryEnabled(IPlugin plugin, bool enabled)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (!_recoveryInfo.TryGetValue(plugin, out var info))
        {
            info = new PluginRecoveryInfo();
            _recoveryInfo[plugin] = info;
        }

        info.AutoRecoveryEnabled = enabled;
        
        _logger.LogInformation("Auto recovery {Status} for plugin {PluginId}", 
            enabled ? "enabled" : "disabled", plugin.Metadata.Id);
    }

    /// <inheritdoc />
    public int GetRecoveryAttemptCount(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        
        return _recoveryInfo.TryGetValue(plugin, out var info) ? info.AttemptCount : 0;
    }

    /// <inheritdoc />
    public void ResetRecoveryAttemptCount(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (_recoveryInfo.TryGetValue(plugin, out var info))
        {
            info.AttemptCount = 0;
            _logger.LogDebug("Reset recovery attempt count for plugin {PluginId}", plugin.Metadata.Id);
        }
    }

    /// <inheritdoc />
    public event EventHandler<PluginRecoveryEventArgs>? RecoveryCompleted;

    private async void OnPluginUnhealthy(object? sender, PluginHealthResult healthResult)
    {
        if (_disposed || !IsAutoRecoveryEnabled(healthResult.Plugin))
            return;

        var attemptCount = GetRecoveryAttemptCount(healthResult.Plugin);
        
        if (attemptCount >= MaxAutoRecoveryAttempts)
        {
            _logger.LogWarning("Plugin {PluginId} has exceeded max auto recovery attempts ({MaxAttempts}), skipping auto recovery", 
                healthResult.Plugin.Metadata.Id, MaxAutoRecoveryAttempts);
            return;
        }

        _logger.LogInformation("Starting automatic recovery for unhealthy plugin {PluginId}", healthResult.Plugin.Metadata.Id);
        
        try
        {
            await RecoverPluginAsync(healthResult.Plugin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automatic recovery for plugin {PluginId}", healthResult.Plugin.Metadata.Id);
        }
    }

    private async Task<bool> RecoverFromFailedStateAsync(IPlugin plugin, CancellationToken cancellationToken)
    {
        // Try to restart the plugin
        try
        {
            if (plugin.IsRunning)
            {
                _stateTracker.SetPluginState(plugin, PluginState.Stopping);
                await plugin.StopAsync(cancellationToken);
                _stateTracker.SetPluginState(plugin, PluginState.Stopped);
            }

            _stateTracker.SetPluginState(plugin, PluginState.Starting);
            await plugin.StartAsync(cancellationToken);
            _stateTracker.SetPluginState(plugin, PluginState.Running);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> RecoverFromRunningMismatchAsync(IPlugin plugin, CancellationToken cancellationToken)
    {
        // Plugin should be running but isn't - start it
        try
        {
            _stateTracker.SetPluginState(plugin, PluginState.Starting);
            await plugin.StartAsync(cancellationToken);
            _stateTracker.SetPluginState(plugin, PluginState.Running);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> RecoverFromStoppedMismatchAsync(IPlugin plugin, CancellationToken cancellationToken)
    {
        // Plugin should be stopped but is running - stop it
        try
        {
            _stateTracker.SetPluginState(plugin, PluginState.Stopping);
            await plugin.StopAsync(cancellationToken);
            _stateTracker.SetPluginState(plugin, PluginState.Stopped);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> PerformGenericRecoveryAsync(IPlugin plugin, CancellationToken cancellationToken)
    {
        // Generic recovery: stop and start the plugin
        try
        {
            if (plugin.IsRunning)
            {
                _stateTracker.SetPluginState(plugin, PluginState.Stopping);
                await plugin.StopAsync(cancellationToken);
                _stateTracker.SetPluginState(plugin, PluginState.Stopped);
            }

            _stateTracker.SetPluginState(plugin, PluginState.Starting);
            await plugin.StartAsync(cancellationToken);
            _stateTracker.SetPluginState(plugin, PluginState.Running);
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void FireRecoveryCompletedEvent(IPlugin plugin, bool wasSuccessful, int attemptNumber, Exception? exception)
    {
        try
        {
            var eventArgs = new PluginRecoveryEventArgs(plugin, wasSuccessful, attemptNumber, exception);
            RecoveryCompleted?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing RecoveryCompleted event for plugin {PluginId}", plugin.Metadata.Id);
        }
    }

    /// <summary>
    /// Releases resources used by the PluginRecoveryService.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _stateTracker.PluginUnhealthy -= OnPluginUnhealthy;
            _recoveryInfo.Clear();
            _disposed = true;
            
            _logger.LogInformation("PluginRecoveryService disposed");
        }
    }

    private class PluginRecoveryInfo
    {
        public int AttemptCount { get; set; }
        public DateTimeOffset LastAttempt { get; set; }
        public DateTimeOffset? LastSuccessfulRecovery { get; set; }
        public bool AutoRecoveryEnabled { get; set; } = true; // Default to enabled
        public bool HasCheckpoint { get; set; }
        public PluginState? CheckpointState { get; set; }
        public DateTimeOffset? CheckpointTimestamp { get; set; }
    }
}
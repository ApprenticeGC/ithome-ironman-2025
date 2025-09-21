using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Handles plugin failure recovery and restoration operations.
/// Provides automatic recovery mechanisms and manual recovery operations.
/// </summary>
public class PluginRecoveryService : IPluginRecoveryService, IDisposable
{
    private readonly ILogger<PluginRecoveryService> _logger;
    private readonly ConcurrentDictionary<IPlugin, PluginRecoveryState> _recoveryStates = new();
    private readonly ConcurrentDictionary<IPlugin, PluginCheckpoint> _checkpoints = new();
    private bool _disposed;

    public PluginRecoveryService(ILogger<PluginRecoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the maximum number of automatic recovery attempts allowed.
    /// </summary>
    public int MaxAutoRecoveryAttempts { get; } = 3;

    /// <summary>
    /// Event fired when a plugin recovery operation completes.
    /// </summary>
    public event EventHandler<PluginRecoveryEventArgs>? RecoveryCompleted;

    /// <summary>
    /// Attempts to recover a failed plugin.
    /// </summary>
    /// <param name="plugin">The failed plugin to recover.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if recovery was successful, false otherwise.</returns>
    public async Task<bool> RecoverPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var recoveryState = _recoveryStates.GetOrAdd(plugin, _ => new PluginRecoveryState());
        var attemptNumber = Interlocked.Increment(ref recoveryState.AttemptCount);

        _logger.LogInformation("Starting recovery attempt {AttemptNumber} for plugin {PluginName}",
            attemptNumber, plugin.Metadata.Name);

        try
        {
            // Try to restore from checkpoint first
            if (_checkpoints.TryGetValue(plugin, out var checkpoint))
            {
                if (await RestoreFromCheckpointAsync(plugin, checkpoint, cancellationToken))
                {
                    _logger.LogInformation("Successfully recovered plugin {PluginName} from checkpoint",
                        plugin.Metadata.Name);
                    
                    OnRecoveryCompleted(plugin, true, attemptNumber);
                    return true;
                }
            }

            // If checkpoint restore failed, try basic recovery
            if (await BasicRecoveryAsync(plugin, cancellationToken))
            {
                _logger.LogInformation("Successfully recovered plugin {PluginName} using basic recovery",
                    plugin.Metadata.Name);
                
                OnRecoveryCompleted(plugin, true, attemptNumber);
                return true;
            }

            _logger.LogWarning("Recovery attempt {AttemptNumber} failed for plugin {PluginName}",
                attemptNumber, plugin.Metadata.Name);
            
            OnRecoveryCompleted(plugin, false, attemptNumber);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recovery attempt {AttemptNumber} failed with exception for plugin {PluginName}",
                attemptNumber, plugin.Metadata.Name);
            
            OnRecoveryCompleted(plugin, false, attemptNumber, ex);
            return false;
        }
    }

    /// <summary>
    /// Creates a checkpoint of the plugin's current state for recovery purposes.
    /// </summary>
    /// <param name="plugin">The plugin to checkpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if checkpoint was created successfully.</returns>
    public async Task<bool> CreateCheckpointAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            var checkpoint = new PluginCheckpoint
            {
                Plugin = plugin,
                CreatedAt = DateTimeOffset.UtcNow,
                IsRunning = plugin.IsRunning,
                Context = plugin.Context
            };

            _checkpoints.AddOrUpdate(plugin, checkpoint, (_, _) => checkpoint);

            _logger.LogDebug("Created checkpoint for plugin {PluginName}", plugin.Metadata.Name);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create checkpoint for plugin {PluginName}", plugin.Metadata.Name);
            return false;
        }
    }

    /// <summary>
    /// Restores a plugin to its last known good checkpoint.
    /// </summary>
    /// <param name="plugin">The plugin to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if restoration was successful.</returns>
    public async Task<bool> RestoreFromCheckpointAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (!_checkpoints.TryGetValue(plugin, out var checkpoint))
        {
            _logger.LogWarning("No checkpoint available for plugin {PluginName}", plugin.Metadata.Name);
            return false;
        }

        return await RestoreFromCheckpointAsync(plugin, checkpoint, cancellationToken);
    }

    /// <summary>
    /// Gets whether automatic recovery is enabled for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>True if automatic recovery is enabled.</returns>
    public bool IsAutoRecoveryEnabled(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var recoveryState = _recoveryStates.GetOrAdd(plugin, _ => new PluginRecoveryState());
        return recoveryState.AutoRecoveryEnabled;
    }

    /// <summary>
    /// Enables or disables automatic recovery for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to configure.</param>
    /// <param name="enabled">Whether to enable automatic recovery.</param>
    public void SetAutoRecoveryEnabled(IPlugin plugin, bool enabled)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var recoveryState = _recoveryStates.GetOrAdd(plugin, _ => new PluginRecoveryState());
        recoveryState.AutoRecoveryEnabled = enabled;

        _logger.LogDebug("Auto-recovery {Status} for plugin {PluginName}",
            enabled ? "enabled" : "disabled", plugin.Metadata.Name);
    }

    /// <summary>
    /// Gets the number of recovery attempts made for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>Number of recovery attempts.</returns>
    public int GetRecoveryAttemptCount(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return _recoveryStates.TryGetValue(plugin, out var state) ? state.AttemptCount : 0;
    }

    /// <summary>
    /// Resets the recovery attempt count for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to reset.</param>
    public void ResetRecoveryAttemptCount(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (_recoveryStates.TryGetValue(plugin, out var state))
        {
            Interlocked.Exchange(ref state.AttemptCount, 0);
            _logger.LogDebug("Reset recovery attempt count for plugin {PluginName}", plugin.Metadata.Name);
        }
    }

    private async Task<bool> RestoreFromCheckpointAsync(IPlugin plugin, PluginCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Restoring plugin {PluginName} from checkpoint created at {CheckpointTime}",
                plugin.Metadata.Name, checkpoint.CreatedAt);

            // Stop the plugin if it's running
            if (plugin.IsRunning)
            {
                await plugin.StopAsync(cancellationToken);
            }

            // Restore context if available
            if (checkpoint.Context != null)
            {
                plugin.Context = checkpoint.Context;
            }

            // If the checkpoint was for a running plugin, restart it
            if (checkpoint.IsRunning)
            {
                await plugin.InitializeAsync(cancellationToken);
                await plugin.StartAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore plugin {PluginName} from checkpoint", plugin.Metadata.Name);
            return false;
        }
    }

    private async Task<bool> BasicRecoveryAsync(IPlugin plugin, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Attempting basic recovery for plugin {PluginName}", plugin.Metadata.Name);

            // Stop the plugin
            if (plugin.IsRunning)
            {
                await plugin.StopAsync(cancellationToken);
            }

            // Wait a brief moment
            await Task.Delay(1000, cancellationToken);

            // Reinitialize and restart
            await plugin.InitializeAsync(cancellationToken);
            await plugin.StartAsync(cancellationToken);

            return plugin.IsRunning;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Basic recovery failed for plugin {PluginName}", plugin.Metadata.Name);
            return false;
        }
    }

    private void OnRecoveryCompleted(IPlugin plugin, bool wasSuccessful, int attemptNumber, Exception? exception = null)
    {
        try
        {
            RecoveryCompleted?.Invoke(this, new PluginRecoveryEventArgs(plugin, wasSuccessful, attemptNumber, exception));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking RecoveryCompleted event for plugin {PluginName}", plugin.Metadata.Name);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _recoveryStates.Clear();
        _checkpoints.Clear();

        _logger.LogDebug("PluginRecoveryService disposed");
    }

    private class PluginRecoveryState
    {
        public int AttemptCount;
        public bool AutoRecoveryEnabled = true;
    }

    private class PluginCheckpoint
    {
        public required IPlugin Plugin { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required bool IsRunning { get; init; }
        public IPluginContext? Context { get; init; }
    }
}
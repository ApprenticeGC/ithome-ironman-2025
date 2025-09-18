using Microsoft.Extensions.Logging;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Options for configuring plugin recovery behavior.
/// </summary>
public record PluginRecoveryOptions(
    int MaxRecoveryAttempts = 3,
    TimeSpan RecoveryDelay = default,
    bool EnableAutomaticRecovery = true,
    bool CreateCheckpoints = true
)
{
    public PluginRecoveryOptions() : this(3, TimeSpan.FromSeconds(5), true, true) { }
}

/// <summary>
/// Strategies for plugin recovery.
/// </summary>
public enum RecoveryStrategy
{
    /// <summary>
    /// Restart the plugin in place.
    /// </summary>
    Restart,

    /// <summary>
    /// Reload the plugin from disk.
    /// </summary>
    Reload,

    /// <summary>
    /// Restore from a previous checkpoint.
    /// </summary>
    RestoreCheckpoint,

    /// <summary>
    /// Disable the plugin to prevent further failures.
    /// </summary>
    Disable
}

/// <summary>
/// Represents a plugin checkpoint for recovery purposes.
/// </summary>
public record PluginCheckpoint(
    string PluginId,
    DateTime CreatedAt,
    PluginState State,
    byte[] StateData
);

/// <summary>
/// Result of a plugin recovery operation.
/// </summary>
public record RecoveryResult(
    bool Success,
    RecoveryStrategy Strategy,
    string? Message = null,
    Exception? Exception = null
);

/// <summary>
/// Service for handling plugin failures and recovery operations.
/// </summary>
public class PluginRecoveryService : IDisposable
{
    private readonly IPluginLifecycleManager _lifecycleManager;
    private readonly PluginStateTracker _stateTracker;
    private readonly ILogger<PluginRecoveryService>? _logger;
    private readonly PluginRecoveryOptions _options;
    private readonly Dictionary<string, List<PluginCheckpoint>> _checkpoints = new();
    private readonly Dictionary<string, int> _recoveryAttempts = new();
    private bool _disposed;

    /// <summary>
    /// Event raised when a recovery operation is attempted.
    /// </summary>
    public event EventHandler<RecoveryAttemptEventArgs>? RecoveryAttempted;

    /// <summary>
    /// Event raised when a recovery operation completes.
    /// </summary>
    public event EventHandler<RecoveryCompletedEventArgs>? RecoveryCompleted;

    public PluginRecoveryService(
        IPluginLifecycleManager lifecycleManager,
        PluginStateTracker stateTracker,
        PluginRecoveryOptions? options = null,
        ILogger<PluginRecoveryService>? logger = null)
    {
        _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        _stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        _options = options ?? new PluginRecoveryOptions();
        _logger = logger;

        // Subscribe to plugin failure events
        if (_options.EnableAutomaticRecovery)
        {
            _stateTracker.PluginFailureDetected += OnPluginFailureDetected;
        }
    }

    /// <summary>
    /// Creates a checkpoint for a plugin's current state.
    /// </summary>
    public Task<bool> CreateCheckpointAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (!_options.CreateCheckpoints)
            return Task.FromResult(false);

        try
        {
            var plugin = _stateTracker.GetPluginState(pluginId);
            if (plugin == null)
            {
                _logger?.LogWarning("Cannot create checkpoint for unknown plugin {PluginId}", pluginId);
                return Task.FromResult(false);
            }

            // In a real implementation, this would serialize the plugin's actual state
            var stateData = System.Text.Encoding.UTF8.GetBytes($"checkpoint_{DateTime.UtcNow:yyyyMMddHHmmss}");
            
            var checkpoint = new PluginCheckpoint(
                pluginId,
                DateTime.UtcNow,
                plugin.State,
                stateData
            );

            if (!_checkpoints.ContainsKey(pluginId))
                _checkpoints[pluginId] = new List<PluginCheckpoint>();

            _checkpoints[pluginId].Add(checkpoint);

            // Keep only the last 5 checkpoints
            if (_checkpoints[pluginId].Count > 5)
                _checkpoints[pluginId].RemoveAt(0);

            _logger?.LogInformation("Created checkpoint for plugin {PluginId}", pluginId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create checkpoint for plugin {PluginId}", pluginId);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Attempts to recover a failed plugin.
    /// </summary>
    public async Task<RecoveryResult> RecoverPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var attemptCount = _recoveryAttempts.GetValueOrDefault(pluginId, 0) + 1;
        _recoveryAttempts[pluginId] = attemptCount;

        var strategy = DetermineRecoveryStrategy(pluginId, attemptCount);
        
        RecoveryAttempted?.Invoke(this, new RecoveryAttemptEventArgs(
            pluginId, strategy, attemptCount, DateTime.UtcNow));

        _logger?.LogInformation("Attempting recovery for plugin {PluginId} using strategy {Strategy} (attempt {Attempt})",
            pluginId, strategy, attemptCount);

        try
        {
            var result = strategy switch
            {
                RecoveryStrategy.Restart => await RestartPluginAsync(pluginId, cancellationToken),
                RecoveryStrategy.Reload => await ReloadPluginAsync(pluginId, cancellationToken),
                RecoveryStrategy.RestoreCheckpoint => await RestoreCheckpointAsync(pluginId, cancellationToken),
                RecoveryStrategy.Disable => await DisablePluginAsync(pluginId, cancellationToken),
                _ => new RecoveryResult(false, strategy, "Unknown recovery strategy")
            };

            if (result.Success)
            {
                _recoveryAttempts.Remove(pluginId);
                _logger?.LogInformation("Successfully recovered plugin {PluginId} using strategy {Strategy}",
                    pluginId, strategy);
            }
            else
            {
                _logger?.LogWarning("Recovery attempt {Attempt} failed for plugin {PluginId}: {Message}",
                    attemptCount, pluginId, result.Message);
            }

            RecoveryCompleted?.Invoke(this, new RecoveryCompletedEventArgs(
                pluginId, strategy, result.Success, attemptCount, DateTime.UtcNow, result.Message));

            return result;
        }
        catch (Exception ex)
        {
            var result = new RecoveryResult(false, strategy, ex.Message, ex);
            
            _logger?.LogError(ex, "Recovery attempt {Attempt} failed for plugin {PluginId} with exception",
                attemptCount, pluginId);

            RecoveryCompleted?.Invoke(this, new RecoveryCompletedEventArgs(
                pluginId, strategy, false, attemptCount, DateTime.UtcNow, ex.Message));

            return result;
        }
    }

    private RecoveryStrategy DetermineRecoveryStrategy(string pluginId, int attemptCount)
    {
        // Strategy selection based on attempt count and available options
        return attemptCount switch
        {
            1 => RecoveryStrategy.Restart,
            2 => _checkpoints.ContainsKey(pluginId) && _checkpoints[pluginId].Any() 
                ? RecoveryStrategy.RestoreCheckpoint 
                : RecoveryStrategy.Reload,
            3 => RecoveryStrategy.Reload,
            _ => RecoveryStrategy.Disable
        };
    }

    private async Task<RecoveryResult> RestartPluginAsync(string pluginId, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycleManager.StopPluginAsync(pluginId, cancellationToken);
            await Task.Delay(_options.RecoveryDelay, cancellationToken);
            var success = await _lifecycleManager.StartPluginAsync(pluginId, cancellationToken);
            
            return new RecoveryResult(success, RecoveryStrategy.Restart, 
                success ? "Plugin restarted successfully" : "Failed to restart plugin");
        }
        catch (Exception ex)
        {
            return new RecoveryResult(false, RecoveryStrategy.Restart, ex.Message, ex);
        }
    }

    private async Task<RecoveryResult> ReloadPluginAsync(string pluginId, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _lifecycleManager.ReloadPluginAsync(pluginId, cancellationToken);
            
            return new RecoveryResult(success, RecoveryStrategy.Reload,
                success ? "Plugin reloaded successfully" : "Failed to reload plugin");
        }
        catch (Exception ex)
        {
            return new RecoveryResult(false, RecoveryStrategy.Reload, ex.Message, ex);
        }
    }

    private async Task<RecoveryResult> RestoreCheckpointAsync(string pluginId, CancellationToken cancellationToken)
    {
        try
        {
            if (!_checkpoints.TryGetValue(pluginId, out var checkpoints) || !checkpoints.Any())
            {
                return new RecoveryResult(false, RecoveryStrategy.RestoreCheckpoint, "No checkpoints available");
            }

            var latestCheckpoint = checkpoints.Last();
            
            // In a real implementation, this would restore the plugin's state from the checkpoint
            await _lifecycleManager.StopPluginAsync(pluginId, cancellationToken);
            await Task.Delay(_options.RecoveryDelay, cancellationToken);
            var success = await _lifecycleManager.StartPluginAsync(pluginId, cancellationToken);

            return new RecoveryResult(success, RecoveryStrategy.RestoreCheckpoint,
                success ? $"Plugin restored from checkpoint created at {latestCheckpoint.CreatedAt}"
                        : "Failed to restore plugin from checkpoint");
        }
        catch (Exception ex)
        {
            return new RecoveryResult(false, RecoveryStrategy.RestoreCheckpoint, ex.Message, ex);
        }
    }

    private async Task<RecoveryResult> DisablePluginAsync(string pluginId, CancellationToken cancellationToken)
    {
        try
        {
            await _lifecycleManager.StopPluginAsync(pluginId, cancellationToken);
            _stateTracker.UpdatePluginState(pluginId, PluginState.Failed);
            
            return new RecoveryResult(true, RecoveryStrategy.Disable, "Plugin disabled due to repeated failures");
        }
        catch (Exception ex)
        {
            return new RecoveryResult(false, RecoveryStrategy.Disable, ex.Message, ex);
        }
    }

    private async void OnPluginFailureDetected(object? sender, PluginFailureEventArgs e)
    {
        if (_disposed) return;

        var attemptCount = _recoveryAttempts.GetValueOrDefault(e.PluginId, 0);
        
        if (attemptCount < _options.MaxRecoveryAttempts)
        {
            _logger?.LogInformation("Automatic recovery triggered for plugin {PluginId} (failure count: {FailureCount})",
                e.PluginId, e.FailureCount);

            await RecoverPluginAsync(e.PluginId);
        }
        else
        {
            _logger?.LogWarning("Plugin {PluginId} has exceeded maximum recovery attempts ({MaxAttempts}), disabling automatic recovery",
                e.PluginId, _options.MaxRecoveryAttempts);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        if (_options.EnableAutomaticRecovery)
        {
            _stateTracker.PluginFailureDetected -= OnPluginFailureDetected;
        }
        
        _checkpoints.Clear();
        _recoveryAttempts.Clear();
        
        _logger?.LogInformation("Plugin recovery service disposed");
    }
}

/// <summary>
/// Event arguments for recovery attempt events.
/// </summary>
public record RecoveryAttemptEventArgs(
    string PluginId,
    RecoveryStrategy Strategy,
    int AttemptNumber,
    DateTime Timestamp
);

/// <summary>
/// Event arguments for recovery completed events.
/// </summary>
public record RecoveryCompletedEventArgs(
    string PluginId,
    RecoveryStrategy Strategy,
    bool Success,
    int AttemptNumber,
    DateTime Timestamp,
    string? Message = null
);
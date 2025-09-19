using GameConsole.Plugins.Core;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Handles plugin failure recovery and restoration operations.
/// Provides automatic recovery mechanisms and manual recovery operations.
/// </summary>
public interface IPluginRecoveryService
{
    /// <summary>
    /// Attempts to recover a failed plugin.
    /// </summary>
    /// <param name="plugin">The failed plugin to recover.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if recovery was successful, false otherwise.</returns>
    Task<bool> RecoverPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a checkpoint of the plugin's current state for recovery purposes.
    /// </summary>
    /// <param name="plugin">The plugin to checkpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if checkpoint was created successfully.</returns>
    Task<bool> CreateCheckpointAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a plugin to its last known good checkpoint.
    /// </summary>
    /// <param name="plugin">The plugin to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if restoration was successful.</returns>
    Task<bool> RestoreFromCheckpointAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether automatic recovery is enabled for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>True if automatic recovery is enabled.</returns>
    bool IsAutoRecoveryEnabled(IPlugin plugin);

    /// <summary>
    /// Enables or disables automatic recovery for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to configure.</param>
    /// <param name="enabled">Whether to enable automatic recovery.</param>
    void SetAutoRecoveryEnabled(IPlugin plugin, bool enabled);

    /// <summary>
    /// Gets the number of recovery attempts made for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>Number of recovery attempts.</returns>
    int GetRecoveryAttemptCount(IPlugin plugin);

    /// <summary>
    /// Resets the recovery attempt count for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to reset.</param>
    void ResetRecoveryAttemptCount(IPlugin plugin);

    /// <summary>
    /// Gets the maximum number of automatic recovery attempts allowed.
    /// </summary>
    int MaxAutoRecoveryAttempts { get; }

    /// <summary>
    /// Event fired when a plugin recovery operation completes.
    /// </summary>
    event EventHandler<PluginRecoveryEventArgs> RecoveryCompleted;
}

/// <summary>
/// Event arguments for plugin recovery operations.
/// </summary>
public class PluginRecoveryEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginRecoveryEventArgs"/> class.
    /// </summary>
    /// <param name="plugin">The plugin that was recovered.</param>
    /// <param name="wasSuccessful">Whether the recovery was successful.</param>
    /// <param name="attemptNumber">The recovery attempt number.</param>
    /// <param name="exception">Optional exception if recovery failed.</param>
    public PluginRecoveryEventArgs(IPlugin plugin, bool wasSuccessful, int attemptNumber, Exception? exception = null)
    {
        Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        WasSuccessful = wasSuccessful;
        AttemptNumber = attemptNumber;
        Exception = exception;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the plugin that was recovered.
    /// </summary>
    public IPlugin Plugin { get; }

    /// <summary>
    /// Gets whether the recovery was successful.
    /// </summary>
    public bool WasSuccessful { get; }

    /// <summary>
    /// Gets the recovery attempt number.
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    /// Gets the exception if recovery failed.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the timestamp when recovery completed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
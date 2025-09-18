namespace GameConsole.Plugins.Core;

/// <summary>
/// Provides information about plugin lifecycle events.
/// Used to notify subscribers about plugin state changes during loading, initialization, starting, stopping, and unloading.
/// </summary>
public class PluginLifecycleEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLifecycleEventArgs"/> class.
    /// </summary>
    /// <param name="plugin">The plugin instance involved in the lifecycle event.</param>
    /// <param name="phase">The lifecycle phase (e.g., "Configuring", "Initializing", "Starting", etc.).</param>
    /// <param name="exception">Optional exception if the lifecycle operation failed.</param>
    public PluginLifecycleEventArgs(IPlugin plugin, string phase, Exception? exception = null)
    {
        Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        Phase = phase ?? throw new ArgumentNullException(nameof(phase));
        Exception = exception;
    }

    /// <summary>
    /// Gets the plugin instance involved in the lifecycle event.
    /// </summary>
    public IPlugin Plugin { get; }

    /// <summary>
    /// Gets the lifecycle phase that triggered this event.
    /// Common phases include: "Configuring", "Configured", "Initializing", "Initialized", 
    /// "Starting", "Started", "Stopping", "Stopped", "Unloading", "Unloaded".
    /// </summary>
    public string Phase { get; }

    /// <summary>
    /// Gets the exception that occurred during the lifecycle operation, if any.
    /// This will be null for successful operations.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the lifecycle operation should be canceled.
    /// This can be set by event handlers to prevent the operation from proceeding.
    /// Not all lifecycle phases support cancellation.
    /// </summary>
    public bool Cancel { get; set; }
}
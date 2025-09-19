using GameConsole.Plugins.Core;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Event arguments for plugin state change notifications.
/// </summary>
public class PluginStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="plugin">The plugin that changed state.</param>
    /// <param name="previousState">The previous state of the plugin.</param>
    /// <param name="newState">The new state of the plugin.</param>
    /// <param name="exception">Optional exception if the state change was due to an error.</param>
    public PluginStateChangedEventArgs(IPlugin plugin, PluginState previousState, PluginState newState, Exception? exception = null)
    {
        Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        PreviousState = previousState;
        NewState = newState;
        Exception = exception;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the plugin that changed state.
    /// </summary>
    public IPlugin Plugin { get; }

    /// <summary>
    /// Gets the previous state of the plugin.
    /// </summary>
    public PluginState PreviousState { get; }

    /// <summary>
    /// Gets the new state of the plugin.
    /// </summary>
    public PluginState NewState { get; }

    /// <summary>
    /// Gets the exception that caused the state change, if any.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the timestamp when the state change occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
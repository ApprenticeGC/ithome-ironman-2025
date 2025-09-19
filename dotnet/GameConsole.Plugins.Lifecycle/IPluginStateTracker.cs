using GameConsole.Plugins.Core;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Tracks plugin states and provides health monitoring capabilities.
/// Monitors plugin health through watchdog timers and periodic checks.
/// </summary>
public interface IPluginStateTracker
{
    /// <summary>
    /// Gets the current state of the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>The current plugin state, or null if not tracked.</returns>
    PluginState? GetPluginState(IPlugin plugin);

    /// <summary>
    /// Sets the state of a plugin and records the transition.
    /// </summary>
    /// <param name="plugin">The plugin whose state to set.</param>
    /// <param name="newState">The new state.</param>
    /// <param name="exception">Optional exception associated with the state change.</param>
    void SetPluginState(IPlugin plugin, PluginState newState, Exception? exception = null);

    /// <summary>
    /// Starts tracking a plugin's state and health.
    /// </summary>
    /// <param name="plugin">The plugin to start tracking.</param>
    /// <param name="initialState">The initial state of the plugin.</param>
    void StartTracking(IPlugin plugin, PluginState initialState = PluginState.Discovered);

    /// <summary>
    /// Stops tracking a plugin's state and health.
    /// </summary>
    /// <param name="plugin">The plugin to stop tracking.</param>
    void StopTracking(IPlugin plugin);

    /// <summary>
    /// Performs a health check on the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<PluginHealthResult> PerformHealthCheckAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently tracked plugins.
    /// </summary>
    IReadOnlyCollection<IPlugin> TrackedPlugins { get; }

    /// <summary>
    /// Gets plugins in the specified state.
    /// </summary>
    /// <param name="state">The state to filter by.</param>
    /// <returns>Collection of plugins in the specified state.</returns>
    IReadOnlyCollection<IPlugin> GetPluginsInState(PluginState state);

    /// <summary>
    /// Gets plugins that are currently unhealthy.
    /// </summary>
    IReadOnlyCollection<IPlugin> UnhealthyPlugins { get; }

    /// <summary>
    /// Event fired when a plugin's state changes.
    /// </summary>
    event EventHandler<PluginStateChangedEventArgs> StateChanged;

    /// <summary>
    /// Event fired when a plugin becomes unhealthy.
    /// </summary>
    event EventHandler<PluginHealthResult> PluginUnhealthy;
}
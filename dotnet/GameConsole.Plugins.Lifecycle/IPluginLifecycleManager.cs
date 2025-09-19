using GameConsole.Core.Abstractions;
using GameConsole.Plugins.Core;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Manages the complete lifecycle of plugins including state transitions, health monitoring,
/// recovery operations, and updates. Provides comprehensive plugin management capabilities
/// with dependency-aware operations and graceful shutdown support.
/// </summary>
public interface IPluginLifecycleManager : IService
{
    /// <summary>
    /// Gets all plugins currently managed by this lifecycle manager.
    /// </summary>
    IReadOnlyCollection<IPlugin> ManagedPlugins { get; }

    /// <summary>
    /// Loads a plugin from the specified path and adds it to lifecycle management.
    /// </summary>
    /// <param name="pluginPath">Path to the plugin assembly.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded plugin instance.</returns>
    Task<IPlugin> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a plugin and removes it from lifecycle management.
    /// Performs graceful shutdown and cleanup operations.
    /// </summary>
    /// <param name="plugin">The plugin to unload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully unloaded, false otherwise.</returns>
    Task<bool> UnloadPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>The current plugin state.</returns>
    PluginState GetPluginState(IPlugin plugin);

    /// <summary>
    /// Starts a plugin, transitioning it through necessary states to Running.
    /// Handles dependency resolution and proper state transitions.
    /// </summary>
    /// <param name="plugin">The plugin to start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if started successfully, false otherwise.</returns>
    Task<bool> StartPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a plugin gracefully, transitioning it to Stopped state.
    /// </summary>
    /// <param name="plugin">The plugin to stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if stopped successfully, false otherwise.</returns>
    Task<bool> StopPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts a plugin by stopping and starting it.
    /// Useful for recovery scenarios or configuration changes.
    /// </summary>
    /// <param name="plugin">The plugin to restart.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if restarted successfully, false otherwise.</returns>
    Task<bool> RestartPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates recovery for a failed plugin.
    /// Attempts to restore the plugin to a functional state.
    /// </summary>
    /// <param name="plugin">The plugin to recover.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if recovery was successful, false otherwise.</returns>
    Task<bool> RecoverPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a plugin to a newer version.
    /// Handles hot-swapping and rollback if the update fails.
    /// </summary>
    /// <param name="plugin">The plugin to update.</param>
    /// <param name="newVersionPath">Path to the new plugin version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if updated successfully, false if rolled back.</returns>
    Task<bool> UpdatePluginAsync(IPlugin plugin, string newVersionPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<PluginHealthResult> CheckPluginHealthAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets plugins that depend on the specified plugin.
    /// Useful for determining impact of plugin operations.
    /// </summary>
    /// <param name="plugin">The plugin to check dependents for.</param>
    /// <returns>Collection of plugins that depend on the specified plugin.</returns>
    IReadOnlyCollection<IPlugin> GetPluginDependents(IPlugin plugin);

    /// <summary>
    /// Gets plugins that the specified plugin depends on.
    /// </summary>
    /// <param name="plugin">The plugin to check dependencies for.</param>
    /// <returns>Collection of plugins that the specified plugin depends on.</returns>
    IReadOnlyCollection<IPlugin> GetPluginDependencies(IPlugin plugin);

    /// <summary>
    /// Performs graceful shutdown of all managed plugins.
    /// Stops plugins in dependency-safe order and performs cleanup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the shutdown operation.</returns>
    Task GracefulShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when a plugin state changes.
    /// </summary>
    event EventHandler<PluginStateChangedEventArgs> PluginStateChanged;
}
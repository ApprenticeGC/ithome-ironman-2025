using GameConsole.Core.Abstractions;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Interface for managing plugin lifecycle operations.
/// </summary>
public interface IPluginLifecycleManager : IService
{
    /// <summary>
    /// Event raised when a plugin state changes.
    /// </summary>
    event EventHandler<PluginLifecycleEventArgs>? PluginStateChanged;

    /// <summary>
    /// Loads a plugin from the specified path.
    /// </summary>
    /// <param name="pluginPath">Path to the plugin assembly.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the load operation.</returns>
    Task<PluginLoadResult> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a plugin with the specified ID.
    /// </summary>
    /// <param name="pluginId">ID of the plugin to unload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the plugin was successfully unloaded.</returns>
    Task<bool> UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads a plugin with the specified ID.
    /// </summary>
    /// <param name="pluginId">ID of the plugin to reload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the plugin was successfully reloaded.</returns>
    Task<bool> ReloadPluginAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a loaded plugin.
    /// </summary>
    /// <param name="pluginId">ID of the plugin to start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the plugin was successfully started.</returns>
    Task<bool> StartPluginAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a running plugin.
    /// </summary>
    /// <param name="pluginId">ID of the plugin to stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the plugin was successfully stopped.</returns>
    Task<bool> StopPluginAsync(string pluginId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current state of a plugin.
    /// </summary>
    /// <param name="pluginId">ID of the plugin.</param>
    /// <returns>Current plugin state.</returns>
    PluginState GetPluginState(string pluginId);

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    /// <returns>Collection of loaded plugins.</returns>
    IEnumerable<LoadedPlugin> GetLoadedPlugins();

    /// <summary>
    /// Gets plugin information by ID.
    /// </summary>
    /// <param name="pluginId">ID of the plugin.</param>
    /// <returns>Plugin metadata if found.</returns>
    PluginMetadata? GetPluginInfo(string pluginId);

    /// <summary>
    /// Validates a plugin before loading.
    /// </summary>
    /// <param name="pluginPath">Path to the plugin assembly.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<ValidationResult> ValidatePluginAsync(string pluginPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the health of a plugin.
    /// </summary>
    /// <param name="pluginId">ID of the plugin.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    Task<HealthCheckResult> CheckPluginHealthAsync(string pluginId, CancellationToken cancellationToken = default);
}
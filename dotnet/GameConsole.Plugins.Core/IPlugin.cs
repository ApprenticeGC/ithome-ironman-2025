using GameConsole.Core.Abstractions;

namespace GameConsole.Plugins.Core;

/// <summary>
/// Defines the base interface for all plugins in the GameConsole system.
/// Extends the base <see cref="IService"/> interface with plugin-specific functionality
/// including metadata access, context management, and enhanced lifecycle operations.
/// </summary>
public interface IPlugin : IService
{
    /// <summary>
    /// Gets the metadata information for this plugin.
    /// This includes identity, version, dependencies, and other plugin-specific information.
    /// </summary>
    IPluginMetadata Metadata { get; }

    /// <summary>
    /// Gets or sets the runtime context for this plugin.
    /// The context provides access to services, configuration, and runtime environment.
    /// This is typically set by the plugin loader before configuration.
    /// </summary>
    IPluginContext? Context { get; set; }

    /// <summary>
    /// Configures the plugin with its runtime context.
    /// This method is called before <see cref="IService.InitializeAsync"/> to provide
    /// the plugin with access to its runtime environment and dependencies.
    /// </summary>
    /// <param name="context">The runtime context for the plugin.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration operation.</returns>
    Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether this plugin can be safely unloaded.
    /// This method is called before attempting to unload the plugin to ensure
    /// it's in a state where unloading won't cause issues.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>
    /// A task that returns true if the plugin can be safely unloaded, false otherwise.
    /// </returns>
    Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs plugin-specific cleanup before unloading.
    /// This method is called after <see cref="IService.StopAsync"/> and before
    /// <see cref="IAsyncDisposable.DisposeAsync"/> during the unload process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cleanup operation.</returns>
    Task PrepareUnloadAsync(CancellationToken cancellationToken = default);
}
using Microsoft.Extensions.Configuration;

namespace GameConsole.Plugins.Core;

/// <summary>
/// Provides runtime context and environment access for plugins.
/// This interface gives plugins access to the host application's services, configuration, and runtime environment.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Gets the service provider for dependency injection.
    /// Plugins can use this to resolve services from the host application.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets the configuration system for accessing application and plugin-specific settings.
    /// </summary>
    IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the directory path where the plugin is located.
    /// This can be used for loading plugin-specific resources, configuration files, or assets.
    /// </summary>
    string PluginDirectory { get; }

    /// <summary>
    /// Gets a cancellation token that is signaled when the application is shutting down.
    /// Plugins should monitor this token for graceful shutdown scenarios.
    /// </summary>
    CancellationToken ShutdownToken { get; }

    /// <summary>
    /// Gets additional properties and context data specific to this plugin instance.
    /// This can include runtime configuration, feature flags, or other contextual information.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}
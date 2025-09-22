using Microsoft.Extensions.Configuration;
using GameConsole.Configuration.Core.Models;

namespace GameConsole.Configuration.Core;

/// <summary>
/// Provides configuration from multiple sources with support for hot-reload and priority ordering.
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Gets the name of this configuration provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the priority of this provider (higher values take precedence).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this provider supports hot-reload.
    /// </summary>
    bool SupportsHotReload { get; }

    /// <summary>
    /// Loads configuration data into the provided builder.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="context">The configuration context.</param>
    /// <returns>A task representing the async load operation.</returns>
    Task LoadAsync(IConfigurationBuilder builder, ConfigurationContext context);

    /// <summary>
    /// Determines if this provider can handle the given context.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    /// <returns>True if the provider can handle the context.</returns>
    bool CanLoad(ConfigurationContext context);

    /// <summary>
    /// Occurs when the provider detects configuration changes (if hot-reload is supported).
    /// </summary>
    event EventHandler<ConfigurationProviderChangeEventArgs>? Changed;
}

/// <summary>
/// Event arguments for configuration provider change notifications.
/// </summary>
public sealed class ConfigurationProviderChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the name of the provider that changed.
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Gets additional details about the change.
    /// </summary>
    public string? ChangeDescription { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationProviderChangeEventArgs"/> class.
    /// </summary>
    public ConfigurationProviderChangeEventArgs(string providerName, string? changeDescription = null)
    {
        ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        ChangeDescription = changeDescription;
    }
}
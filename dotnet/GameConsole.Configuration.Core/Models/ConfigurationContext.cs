namespace GameConsole.Configuration.Core.Models;

/// <summary>
/// Provides contextual information for configuration resolution and validation.
/// </summary>
public sealed class ConfigurationContext
{
    /// <summary>
    /// Gets or sets the current environment name (e.g., "Development", "Production").
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Gets or sets the application mode (e.g., "Game", "Editor").
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Gets or sets the plugin identifier for plugin-specific configuration.
    /// </summary>
    public string? PluginId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier for user-specific configuration.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets additional properties that can be used by configuration providers.
    /// </summary>
    public Dictionary<string, object> Properties { get; } = new();

    /// <summary>
    /// Gets or sets the configuration scope for this context.
    /// </summary>
    public ConfigurationScope Scope { get; set; } = ConfigurationScope.Global;
}
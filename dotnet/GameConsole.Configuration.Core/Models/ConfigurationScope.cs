namespace GameConsole.Configuration.Core.Models;

/// <summary>
/// Defines the scope of configuration settings within the GameConsole system.
/// Used to organize and prioritize configuration from different sources and contexts.
/// </summary>
public enum ConfigurationScope
{
    /// <summary>
    /// Global application-wide configuration that applies to all components.
    /// </summary>
    Global,

    /// <summary>
    /// Configuration specific to the current execution mode (Game or Editor mode).
    /// </summary>
    Mode,

    /// <summary>
    /// Plugin-specific configuration settings that only apply to individual plugins.
    /// </summary>
    Plugin,

    /// <summary>
    /// User-specific preferences and customizations.
    /// </summary>
    User,

    /// <summary>
    /// Environment-specific overrides (Development, Staging, Production).
    /// </summary>
    Environment
}
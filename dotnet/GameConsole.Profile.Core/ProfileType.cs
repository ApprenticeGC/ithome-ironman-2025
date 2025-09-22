namespace GameConsole.Profile.Core;

/// <summary>
/// Defines the types of profiles supported by the system.
/// </summary>
public enum ProfileType
{
    /// <summary>
    /// User-defined custom profile with specific configurations.
    /// </summary>
    Custom = 0,

    /// <summary>
    /// Profile that mimics Unity engine behavior patterns.
    /// </summary>
    Unity = 1,

    /// <summary>
    /// Profile that mimics Godot engine behavior patterns.
    /// </summary>
    Godot = 2,

    /// <summary>
    /// Minimal profile with essential services only.
    /// </summary>
    Minimal = 3,

    /// <summary>
    /// Development-optimized profile with debugging capabilities.
    /// </summary>
    Development = 4,

    /// <summary>
    /// Default system profile (existing behavior).
    /// </summary>
    Default = 5
}
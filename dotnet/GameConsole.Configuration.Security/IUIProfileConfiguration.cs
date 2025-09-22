using GameConsole.Core.Abstractions;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Defines the configuration interface for UI profiles in the GameConsole system.
/// UI profiles control how the user interface behaves and which providers/systems are used.
/// </summary>
public interface IUIProfileConfiguration
{
    /// <summary>
    /// Gets the unique identifier for this profile configuration.
    /// </summary>
    string ProfileId { get; }

    /// <summary>
    /// Gets the human-readable name of this profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this profile provides.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the UI mode that this profile implements.
    /// </summary>
    UIMode Mode { get; }

    /// <summary>
    /// Gets the configuration settings for this profile.
    /// </summary>
    IReadOnlyDictionary<string, object> Settings { get; }

    /// <summary>
    /// Gets the provider types that should be used with this profile.
    /// Maps from capability interface type to concrete provider type.
    /// </summary>
    IReadOnlyDictionary<Type, Type> ProviderMappings { get; }
}

/// <summary>
/// Defines the supported UI modes in the GameConsole system.
/// </summary>
public enum UIMode
{
    /// <summary>
    /// Text-based User Interface mode (default, TUI-first).
    /// Optimized for console/terminal interactions.
    /// </summary>
    TUI = 0,

    /// <summary>
    /// Unity engine simulation mode.
    /// Simulates Unity-like behaviors and interfaces.
    /// </summary>
    Unity = 1,

    /// <summary>
    /// Godot engine simulation mode.
    /// Simulates Godot-like behaviors and interfaces.
    /// </summary>
    Godot = 2,

    /// <summary>
    /// Custom mode for user-defined profiles.
    /// </summary>
    Custom = 99
}
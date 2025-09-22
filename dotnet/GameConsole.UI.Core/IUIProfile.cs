namespace GameConsole.UI.Core;

/// <summary>
/// Represents a UI profile that defines the behavior and configuration for different UI modes.
/// Profiles enable simulation of Unity/Godot behaviors by configuring different UI providers and settings.
/// </summary>
public interface IUIProfile
{
    /// <summary>
    /// Gets the unique identifier for this UI profile.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of this UI profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this UI profile provides.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the configuration settings for this UI profile.
    /// </summary>
    UIProfileSettings Settings { get; }

    /// <summary>
    /// Gets a value indicating whether this profile is currently active.
    /// </summary>
    bool IsActive { get; }
}
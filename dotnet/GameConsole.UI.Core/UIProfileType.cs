namespace GameConsole.UI.Core;

/// <summary>
/// Defines the types of UI profiles available in the GameConsole system.
/// Supports different UI behavior patterns for various game engine styles.
/// </summary>
public enum UIProfileType
{
    /// <summary>
    /// Unity-style UI profile with Unity-specific behaviors and conventions.
    /// </summary>
    Unity,

    /// <summary>
    /// Godot-style UI profile with Godot-specific behaviors and conventions.
    /// </summary>
    Godot,

    /// <summary>
    /// Custom Terminal User Interface (TUI) profile optimized for console-based UI.
    /// This is the default profile type following the TUI-first architecture.
    /// </summary>
    CustomTUI,

    /// <summary>
    /// Default profile type that provides basic UI functionality.
    /// </summary>
    Default
}
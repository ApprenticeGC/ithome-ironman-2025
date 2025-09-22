namespace GameConsole.UI.Profiles;

/// <summary>
/// Defines the different UI modes that can be configured through profiles.
/// Each mode represents a different approach to user interface behavior and rendering.
/// </summary>
public enum UIMode
{
    /// <summary>
    /// Text-based User Interface mode - the default and primary mode.
    /// Provides console-based interactions suitable for terminal environments.
    /// </summary>
    TUI = 0,

    /// <summary>
    /// Unity-style UI mode simulation.
    /// Configures providers and systems to behave similar to Unity's component-based approach.
    /// </summary>
    UnityStyle = 1,

    /// <summary>
    /// Godot-style UI mode simulation.
    /// Configures providers and systems to behave similar to Godot's scene-based approach.
    /// </summary>
    GodotStyle = 2,

    /// <summary>
    /// Custom UI mode for specialized configurations.
    /// Allows for completely custom provider and system configurations.
    /// </summary>
    Custom = 99
}
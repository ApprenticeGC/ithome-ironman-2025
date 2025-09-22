namespace GameConsole.UI.Core
{
    /// <summary>
    /// Defines the types of UI profiles supported by the system.
    /// Each profile type represents a different approach to user interface behavior.
    /// </summary>
    public enum ProfileType
{
    /// <summary>
    /// Text User Interface (TUI) profile - the default console-based interface.
    /// Optimized for keyboard navigation and text-based interaction.
    /// </summary>
    TUI = 0,

    /// <summary>
    /// Unity-like profile that simulates Unity editor and runtime behaviors.
    /// Includes immediate mode GUI patterns and component-based UI architecture.
    /// </summary>
    UnityLike = 1,

    /// <summary>
    /// Godot-like profile that simulates Godot engine UI behaviors.
    /// Includes node-based UI hierarchy and signal-based event handling.
    /// </summary>
    GodotLike = 2,

    /// <summary>
    /// Custom profile type for user-defined configurations.
    /// Allows for completely customized provider and system configurations.
    /// </summary>
    Custom = 99
    }
}
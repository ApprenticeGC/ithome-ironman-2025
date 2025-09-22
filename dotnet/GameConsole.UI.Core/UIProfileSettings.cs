namespace GameConsole.UI.Core;

/// <summary>
/// Configuration settings for a UI profile.
/// Contains all the settings needed to configure the UI behavior for different engine simulation modes.
/// </summary>
public class UIProfileSettings
{
    /// <summary>
    /// Gets or sets the UI rendering mode (e.g., "Unity", "Godot", "Custom").
    /// </summary>
    public string RenderingMode { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the input handling mode for this profile.
    /// </summary>
    public string InputMode { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the graphics backend to use for this profile.
    /// </summary>
    public string GraphicsBackend { get; set; } = "Default";

    /// <summary>
    /// Gets or sets whether TUI (Text User Interface) mode is enabled.
    /// </summary>
    public bool TuiMode { get; set; } = true;

    /// <summary>
    /// Gets or sets additional custom properties for this profile.
    /// </summary>
    public Dictionary<string, object> CustomProperties { get; set; } = new();

    /// <summary>
    /// Gets or sets the priority of this profile when multiple profiles are available.
    /// Higher values indicate higher priority.
    /// </summary>
    public int Priority { get; set; } = 0;
}
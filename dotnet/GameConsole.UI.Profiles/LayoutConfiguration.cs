namespace GameConsole.UI.Profiles;

/// <summary>
/// Defines the layout configuration for a UI profile.
/// </summary>
public class LayoutConfiguration
{
    /// <summary>
    /// Gets or sets the default width for UI panels.
    /// </summary>
    public int DefaultPanelWidth { get; set; } = 80;
    
    /// <summary>
    /// Gets or sets the default height for UI panels.
    /// </summary>
    public int DefaultPanelHeight { get; set; } = 24;
    
    /// <summary>
    /// Gets or sets the main panel configurations.
    /// </summary>
    public IReadOnlyList<PanelConfiguration> Panels { get; set; } = Array.Empty<PanelConfiguration>();
    
    /// <summary>
    /// Gets or sets whether the layout supports dynamic resizing.
    /// </summary>
    public bool SupportsDynamicResize { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the minimum console width required for this layout.
    /// </summary>
    public int MinimumConsoleWidth { get; set; } = 80;
    
    /// <summary>
    /// Gets or sets the minimum console height required for this layout.
    /// </summary>
    public int MinimumConsoleHeight { get; set; } = 24;
}

/// <summary>
/// Configuration for a specific UI panel within a layout.
/// </summary>
public class PanelConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for this panel.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display name of this panel.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the panel position (top, bottom, left, right, center).
    /// </summary>
    public string Position { get; set; } = "center";
    
    /// <summary>
    /// Gets or sets the panel width (in characters or percentage).
    /// </summary>
    public int Width { get; set; } = 80;
    
    /// <summary>
    /// Gets or sets the panel height (in characters or percentage).
    /// </summary>
    public int Height { get; set; } = 24;
    
    /// <summary>
    /// Gets or sets whether this panel is visible by default.
    /// </summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether this panel can be resized by the user.
    /// </summary>
    public bool IsResizable { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the panel's Z-order (higher values appear on top).
    /// </summary>
    public int ZOrder { get; set; } = 0;
}
namespace GameConsole.UI.Profiles;

/// <summary>
/// Defines the layout configuration for a UI profile.
/// Controls how UI elements are arranged and displayed.
/// </summary>
public sealed class LayoutConfiguration
{
    /// <summary>
    /// Gets or sets the main layout template to use.
    /// </summary>
    public string LayoutTemplate { get; set; } = "Default";

    /// <summary>
    /// Gets or sets the configuration for window panels and regions.
    /// </summary>
    public List<PanelConfiguration> Panels { get; set; } = new List<PanelConfiguration>();

    /// <summary>
    /// Gets or sets the default panel that should have focus when the profile loads.
    /// </summary>
    public string DefaultFocusPanel { get; set; } = "Main";

    /// <summary>
    /// Gets or sets whether panels can be resized by the user.
    /// </summary>
    public bool AllowPanelResize { get; set; } = true;

    /// <summary>
    /// Gets or sets whether panels can be moved or reordered by the user.
    /// </summary>
    public bool AllowPanelReorder { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum width for resizable panels.
    /// </summary>
    public int MinPanelWidth { get; set; } = 200;

    /// <summary>
    /// Gets or sets the minimum height for resizable panels.
    /// </summary>
    public int MinPanelHeight { get; set; } = 100;
}

/// <summary>
/// Configuration for a specific UI panel within the layout.
/// </summary>
public sealed class PanelConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for the panel.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display title for the panel.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of content this panel displays.
    /// </summary>
    public string ContentType { get; set; } = "General";

    /// <summary>
    /// Gets or sets the initial width of the panel (in pixels or percentage).
    /// </summary>
    public string Width { get; set; } = "auto";

    /// <summary>
    /// Gets or sets the initial height of the panel (in pixels or percentage).
    /// </summary>
    public string Height { get; set; } = "auto";

    /// <summary>
    /// Gets or sets the position where this panel should be docked.
    /// </summary>
    public string DockPosition { get; set; } = "Center";

    /// <summary>
    /// Gets or sets whether this panel is visible by default.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this panel can be closed by the user.
    /// </summary>
    public bool CanClose { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority for panel ordering (higher values appear first).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets additional properties specific to the panel type.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
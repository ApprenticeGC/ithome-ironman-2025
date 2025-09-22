namespace GameConsole.UI.Profiles;

/// <summary>
/// Configuration for the status bar within a UI profile.
/// </summary>
public sealed class StatusBarConfiguration
{
    /// <summary>
    /// Gets or sets whether the status bar is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the position of the status bar.
    /// </summary>
    public string Position { get; set; } = "Bottom";

    /// <summary>
    /// Gets or sets the status bar segments from left to right.
    /// </summary>
    public List<StatusBarSegment> Segments { get; set; } = new List<StatusBarSegment>();

    /// <summary>
    /// Gets or sets the default text to show when no specific status is displayed.
    /// </summary>
    public string DefaultText { get; set; } = "Ready";

    /// <summary>
    /// Gets or sets the auto-hide delay in milliseconds (0 = never hide).
    /// </summary>
    public int AutoHideDelayMs { get; set; } = 0;
}

/// <summary>
/// Represents a segment of the status bar showing specific information.
/// </summary>
public sealed class StatusBarSegment
{
    /// <summary>
    /// Gets or sets the unique identifier for this segment.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of information this segment displays.
    /// </summary>
    public string Type { get; set; } = "Text";

    /// <summary>
    /// Gets or sets the width of this segment (in pixels or percentage).
    /// </summary>
    public string Width { get; set; } = "auto";

    /// <summary>
    /// Gets or sets the alignment of content within the segment.
    /// </summary>
    public string Alignment { get; set; } = "Left";

    /// <summary>
    /// Gets or sets whether this segment is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority for segment ordering (higher values appear first).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets the format string for displaying the segment content.
    /// </summary>
    public string Format { get; set; } = "{0}";

    /// <summary>
    /// Gets or sets additional properties specific to the segment type.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Configuration for toolbars within a UI profile.
/// </summary>
public sealed class ToolbarConfiguration
{
    /// <summary>
    /// Gets or sets the available toolbars in the profile.
    /// </summary>
    public List<ToolbarDefinition> Toolbars { get; set; } = new List<ToolbarDefinition>();

    /// <summary>
    /// Gets or sets whether toolbars can be customized by the user.
    /// </summary>
    public bool AllowUserCustomization { get; set; } = true;

    /// <summary>
    /// Gets or sets whether toolbars can be moved to different positions.
    /// </summary>
    public bool AllowDocking { get; set; } = true;

    /// <summary>
    /// Gets or sets the default toolbar that should be visible.
    /// </summary>
    public string DefaultToolbar { get; set; } = "Main";
}

/// <summary>
/// Defines a toolbar within the UI profile.
/// </summary>
public sealed class ToolbarDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the toolbar.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the toolbar.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position where the toolbar should be docked.
    /// </summary>
    public string Position { get; set; } = "Top";

    /// <summary>
    /// Gets or sets whether the toolbar is visible by default.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the items in the toolbar from left to right.
    /// </summary>
    public List<ToolbarItemDefinition> Items { get; set; } = new List<ToolbarItemDefinition>();

    /// <summary>
    /// Gets or sets the priority for toolbar ordering.
    /// </summary>
    public int Priority { get; set; } = 0;
}

/// <summary>
/// Defines an item within a toolbar.
/// </summary>
public sealed class ToolbarItemDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the toolbar item.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of toolbar item (Button, Separator, Dropdown, etc.).
    /// </summary>
    public string Type { get; set; } = "Button";

    /// <summary>
    /// Gets or sets the command to execute when the item is activated.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon identifier for the toolbar item.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tooltip text for the toolbar item.
    /// </summary>
    public string Tooltip { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display text for the toolbar item (if applicable).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this toolbar item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this toolbar item is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority for item ordering within the toolbar.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets additional properties specific to the item type.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
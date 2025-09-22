namespace GameConsole.UI.Profiles;

/// <summary>
/// Configuration for commands available in a UI profile.
/// </summary>
public class CommandSet
{
    /// <summary>
    /// Gets or sets the primary commands for this profile.
    /// </summary>
    public IReadOnlyList<string> PrimaryCommands { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the secondary commands for this profile.
    /// </summary>
    public IReadOnlyList<string> SecondaryCommands { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the commands that should be hidden in this profile.
    /// </summary>
    public IReadOnlyList<string> HiddenCommands { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets custom command aliases for this profile.
    /// </summary>
    public IReadOnlyDictionary<string, string> CommandAliases { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Configuration for UI layout in a profile.
/// </summary>
public class LayoutConfiguration
{
    /// <summary>
    /// Gets or sets the preferred layout orientation.
    /// </summary>
    public LayoutOrientation Orientation { get; set; } = LayoutOrientation.Horizontal;

    /// <summary>
    /// Gets or sets the panel visibility configuration.
    /// </summary>
    public IReadOnlyDictionary<string, bool> PanelVisibility { get; set; } = new Dictionary<string, bool>();

    /// <summary>
    /// Gets or sets the panel size ratios.
    /// </summary>
    public IReadOnlyDictionary<string, float> PanelSizes { get; set; } = new Dictionary<string, float>();
}

/// <summary>
/// Layout orientation options.
/// </summary>
public enum LayoutOrientation
{
    Horizontal,
    Vertical,
    Grid
}

/// <summary>
/// Configuration for key bindings in a profile.
/// </summary>
public class KeyBindingSet
{
    /// <summary>
    /// Gets or sets the key bindings for commands.
    /// </summary>
    public IReadOnlyDictionary<string, string> Bindings { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the disabled key bindings for this profile.
    /// </summary>
    public IReadOnlyList<string> DisabledBindings { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Configuration for menus in a profile.
/// </summary>
public class MenuConfiguration
{
    /// <summary>
    /// Gets or sets the menu items to display.
    /// </summary>
    public IReadOnlyList<string> MenuItems { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the context menu items.
    /// </summary>
    public IReadOnlyList<string> ContextMenuItems { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the hidden menu items.
    /// </summary>
    public IReadOnlyList<string> HiddenMenuItems { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Configuration for the status bar in a profile.
/// </summary>
public class StatusBarConfiguration
{
    /// <summary>
    /// Gets or sets whether the status bar is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the status bar items to display.
    /// </summary>
    public IReadOnlyList<string> Items { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the refresh interval for status items in milliseconds.
    /// </summary>
    public int RefreshInterval { get; set; } = 1000;
}

/// <summary>
/// Configuration for toolbars in a profile.
/// </summary>
public class ToolbarConfiguration
{
    /// <summary>
    /// Gets or sets whether the toolbar is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the toolbar buttons to display.
    /// </summary>
    public IReadOnlyList<string> Buttons { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the toolbar position.
    /// </summary>
    public ToolbarPosition Position { get; set; } = ToolbarPosition.Top;
}

/// <summary>
/// Toolbar position options.
/// </summary>
public enum ToolbarPosition
{
    Top,
    Bottom,
    Left,
    Right
}
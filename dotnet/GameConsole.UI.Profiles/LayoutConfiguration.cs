namespace GameConsole.UI.Profiles;

/// <summary>
/// Configuration for UI layout including panels, windows, and interface organization.
/// </summary>
public class LayoutConfiguration
{
    /// <summary>
    /// UI panels configuration.
    /// </summary>
    public IReadOnlyList<PanelConfiguration> Panels { get; init; } = Array.Empty<PanelConfiguration>();

    /// <summary>
    /// Global UI theme settings.
    /// </summary>
    public ThemeConfiguration Theme { get; init; } = new();

    /// <summary>
    /// Window management settings.
    /// </summary>
    public WindowConfiguration Window { get; init; } = new();

    /// <summary>
    /// Navigation and menu configuration.
    /// </summary>
    public NavigationConfiguration Navigation { get; init; } = new();

    /// <summary>
    /// Validates the layout configuration for consistency.
    /// </summary>
    /// <returns>True if the configuration is valid.</returns>
    public bool IsValid()
    {
        // Check for duplicate panel names
        var panelNames = Panels.Select(p => p.Name).ToList();
        return panelNames.Count == panelNames.Distinct().Count();
    }
}

/// <summary>
/// Configuration for a UI panel.
/// </summary>
public class PanelConfiguration
{
    /// <summary>
    /// Unique name of the panel.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Type of panel (e.g., "Console", "Inspector", "Hierarchy").
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Position of the panel in the layout.
    /// </summary>
    public PanelPosition Position { get; init; } = PanelPosition.Left;

    /// <summary>
    /// Size allocation for the panel (percentage or pixels).
    /// </summary>
    public string Size { get; init; } = "25%";

    /// <summary>
    /// Whether the panel is visible by default.
    /// </summary>
    public bool IsVisible { get; init; } = true;

    /// <summary>
    /// Whether the panel can be resized by the user.
    /// </summary>
    public bool IsResizable { get; init; } = true;

    /// <summary>
    /// Whether the panel can be closed/hidden by the user.
    /// </summary>
    public bool IsClosable { get; init; } = true;
}

/// <summary>
/// Possible positions for UI panels.
/// </summary>
public enum PanelPosition
{
    Left,
    Right,
    Top,
    Bottom,
    Center,
    Floating
}

/// <summary>
/// Theme configuration for UI appearance.
/// </summary>
public class ThemeConfiguration
{
    /// <summary>
    /// Color scheme name.
    /// </summary>
    public string ColorScheme { get; init; } = "Dark";

    /// <summary>
    /// Font family for UI text.
    /// </summary>
    public string FontFamily { get; init; } = "Consolas";

    /// <summary>
    /// Base font size.
    /// </summary>
    public int FontSize { get; init; } = 12;

    /// <summary>
    /// UI scaling factor.
    /// </summary>
    public float Scale { get; init; } = 1.0f;
}

/// <summary>
/// Window configuration settings.
/// </summary>
public class WindowConfiguration
{
    /// <summary>
    /// Default window width.
    /// </summary>
    public int Width { get; init; } = 1200;

    /// <summary>
    /// Default window height.
    /// </summary>
    public int Height { get; init; } = 800;

    /// <summary>
    /// Whether the window can be resized.
    /// </summary>
    public bool IsResizable { get; init; } = true;

    /// <summary>
    /// Whether the window should start maximized.
    /// </summary>
    public bool StartMaximized { get; init; } = false;
}

/// <summary>
/// Navigation and menu configuration.
/// </summary>
public class NavigationConfiguration
{
    /// <summary>
    /// Whether to show the main menu bar.
    /// </summary>
    public bool ShowMenuBar { get; init; } = true;

    /// <summary>
    /// Whether to show the toolbar.
    /// </summary>
    public bool ShowToolbar { get; init; } = true;

    /// <summary>
    /// Whether to show the status bar.
    /// </summary>
    public bool ShowStatusBar { get; init; } = true;

    /// <summary>
    /// Navigation shortcuts configuration.
    /// </summary>
    public IReadOnlyDictionary<string, string> Shortcuts { get; init; } = 
        new Dictionary<string, string>().AsReadOnly();
}
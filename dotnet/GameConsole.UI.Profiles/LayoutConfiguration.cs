namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents the layout configuration for a UI profile, defining how UI elements are arranged and displayed.
/// </summary>
public sealed class LayoutConfiguration
{
    /// <summary>
    /// Gets or sets the main window configuration.
    /// </summary>
    public WindowConfiguration MainWindow { get; set; } = new();

    /// <summary>
    /// Gets or sets the panel configurations for different UI regions.
    /// </summary>
    public Dictionary<string, PanelConfiguration> Panels { get; set; } = new();

    /// <summary>
    /// Gets or sets the menu bar configuration.
    /// </summary>
    public MenuConfiguration MenuBar { get; set; } = new();

    /// <summary>
    /// Gets or sets the status bar configuration.
    /// </summary>
    public StatusBarConfiguration StatusBar { get; set; } = new();

    /// <summary>
    /// Gets or sets the toolbar configuration.
    /// </summary>
    public ToolbarConfiguration Toolbar { get; set; } = new();

    /// <summary>
    /// Gets or sets keyboard shortcut configurations.
    /// </summary>
    public KeyBindingSet KeyBindings { get; set; } = new();
}

/// <summary>
/// Configuration for the main application window.
/// </summary>
public sealed class WindowConfiguration
{
    /// <summary>
    /// Gets or sets the default window title.
    /// </summary>
    public string Title { get; set; } = "GameConsole";

    /// <summary>
    /// Gets or sets the default window width.
    /// </summary>
    public int Width { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the default window height.
    /// </summary>
    public int Height { get; set; } = 768;

    /// <summary>
    /// Gets or sets whether the window is resizable.
    /// </summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Gets or sets the window theme.
    /// </summary>
    public string Theme { get; set; } = "Default";
}

/// <summary>
/// Configuration for UI panels within the layout.
/// </summary>
public sealed class PanelConfiguration
{
    /// <summary>
    /// Gets or sets the panel name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the panel is visible by default.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the panel position within its container.
    /// </summary>
    public string Position { get; set; } = "Center";

    /// <summary>
    /// Gets or sets the panel size.
    /// </summary>
    public SizeConfiguration Size { get; set; } = new();
}

/// <summary>
/// Configuration for menu bars.
/// </summary>
public sealed class MenuConfiguration
{
    /// <summary>
    /// Gets or sets whether the menu bar is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the menu items.
    /// </summary>
    public List<MenuItemConfiguration> Items { get; set; } = new();
}

/// <summary>
/// Configuration for status bars.
/// </summary>
public sealed class StatusBarConfiguration
{
    /// <summary>
    /// Gets or sets whether the status bar is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the status bar height.
    /// </summary>
    public int Height { get; set; } = 24;

    /// <summary>
    /// Gets or sets the status bar sections.
    /// </summary>
    public List<StatusSection> Sections { get; set; } = new();
}

/// <summary>
/// Configuration for toolbars.
/// </summary>
public sealed class ToolbarConfiguration
{
    /// <summary>
    /// Gets or sets whether the toolbar is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the toolbar position.
    /// </summary>
    public string Position { get; set; } = "Top";

    /// <summary>
    /// Gets or sets the toolbar buttons.
    /// </summary>
    public List<ToolbarButton> Buttons { get; set; } = new();
}

/// <summary>
/// Configuration for keyboard shortcuts.
/// </summary>
public sealed class KeyBindingSet
{
    private readonly Dictionary<string, string> _bindings = new();

    /// <summary>
    /// Gets all available key bindings.
    /// </summary>
    public IReadOnlyDictionary<string, string> Bindings => _bindings;

    /// <summary>
    /// Adds or updates a key binding.
    /// </summary>
    /// <param name="key">The key combination.</param>
    /// <param name="command">The command to execute.</param>
    public void AddBinding(string key, string command)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(command);
        
        _bindings[key] = command;
    }

    /// <summary>
    /// Removes a key binding.
    /// </summary>
    /// <param name="key">The key combination to remove.</param>
    /// <returns>True if the binding was removed; otherwise, false.</returns>
    public bool RemoveBinding(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _bindings.Remove(key);
    }
}

/// <summary>
/// Represents size configuration for UI elements.
/// </summary>
public sealed class SizeConfiguration
{
    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public int Width { get; set; } = 200;

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public int Height { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether the size is flexible.
    /// </summary>
    public bool Flexible { get; set; } = true;
}

/// <summary>
/// Represents a menu item configuration.
/// </summary>
public sealed class MenuItemConfiguration
{
    /// <summary>
    /// Gets or sets the menu item text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command to execute when clicked.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the menu item is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Represents a status bar section.
/// </summary>
public sealed class StatusSection
{
    /// <summary>
    /// Gets or sets the section name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section width.
    /// </summary>
    public int Width { get; set; } = 100;
}

/// <summary>
/// Represents a toolbar button.
/// </summary>
public sealed class ToolbarButton
{
    /// <summary>
    /// Gets or sets the button text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the button tooltip.
    /// </summary>
    public string Tooltip { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command to execute when clicked.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the button is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
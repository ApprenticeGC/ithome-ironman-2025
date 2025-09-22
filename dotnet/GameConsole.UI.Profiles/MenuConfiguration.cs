namespace GameConsole.UI.Profiles;

/// <summary>
/// Defines key binding configurations for a UI profile.
/// Maps keyboard shortcuts to commands and actions.
/// </summary>
public sealed class KeyBindingSet
{
    /// <summary>
    /// Gets or sets the global key bindings that work across all contexts.
    /// </summary>
    public Dictionary<string, string> GlobalBindings { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets context-specific key bindings organized by context name.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> ContextualBindings { get; set; } = new Dictionary<string, Dictionary<string, string>>();

    /// <summary>
    /// Gets or sets key bindings that override system defaults.
    /// </summary>
    public Dictionary<string, string> SystemOverrides { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets whether key bindings can be customized by the user.
    /// </summary>
    public bool AllowUserCustomization { get; set; } = true;
}

/// <summary>
/// Configuration for menu systems within a UI profile.
/// </summary>
public sealed class MenuConfiguration
{
    /// <summary>
    /// Gets or sets the configuration for the main menu bar.
    /// </summary>
    public MenuBarConfiguration MainMenu { get; set; } = new MenuBarConfiguration();

    /// <summary>
    /// Gets or sets context menu configurations for different UI elements.
    /// </summary>
    public Dictionary<string, ContextMenuConfiguration> ContextMenus { get; set; } = new Dictionary<string, ContextMenuConfiguration>();

    /// <summary>
    /// Gets or sets whether menus should show keyboard shortcuts.
    /// </summary>
    public bool ShowKeyboardShortcuts { get; set; } = true;

    /// <summary>
    /// Gets or sets whether menus should show icons for commands.
    /// </summary>
    public bool ShowIcons { get; set; } = true;
}

/// <summary>
/// Configuration for the main menu bar.
/// </summary>
public sealed class MenuBarConfiguration
{
    /// <summary>
    /// Gets or sets the menu items in order of appearance.
    /// </summary>
    public List<MenuItemConfiguration> Items { get; set; } = new List<MenuItemConfiguration>();

    /// <summary>
    /// Gets or sets whether the menu bar is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the position of the menu bar.
    /// </summary>
    public string Position { get; set; } = "Top";
}

/// <summary>
/// Configuration for context menus that appear on right-click or similar actions.
/// </summary>
public sealed class ContextMenuConfiguration
{
    /// <summary>
    /// Gets or sets the menu items for this context menu.
    /// </summary>
    public List<MenuItemConfiguration> Items { get; set; } = new List<MenuItemConfiguration>();

    /// <summary>
    /// Gets or sets the trigger condition for showing this context menu.
    /// </summary>
    public string Trigger { get; set; } = "RightClick";
}

/// <summary>
/// Configuration for individual menu items.
/// </summary>
public sealed class MenuItemConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for the menu item.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display text for the menu item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command to execute when the item is selected.
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the icon identifier for the menu item.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the keyboard shortcut text to display.
    /// </summary>
    public string ShortcutText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this menu item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this menu item is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this menu item is a separator.
    /// </summary>
    public bool IsSeparator { get; set; } = false;

    /// <summary>
    /// Gets or sets submenu items if this is a parent menu.
    /// </summary>
    public List<MenuItemConfiguration> SubItems { get; set; } = new List<MenuItemConfiguration>();

    /// <summary>
    /// Gets or sets the priority for menu item ordering (higher values appear first).
    /// </summary>
    public int Priority { get; set; } = 0;
}
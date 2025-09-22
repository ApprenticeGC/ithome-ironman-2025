namespace GameConsole.UI.Profiles;

/// <summary>
/// Configuration for menu systems in a UI profile.
/// </summary>
public class MenuConfiguration
{
    /// <summary>
    /// Gets or sets the main menu items.
    /// </summary>
    public IReadOnlyList<MenuItem> MainMenu { get; set; } = Array.Empty<MenuItem>();
    
    /// <summary>
    /// Gets or sets the context menu items.
    /// </summary>
    public IReadOnlyList<MenuItem> ContextMenu { get; set; } = Array.Empty<MenuItem>();
    
    /// <summary>
    /// Gets or sets whether menu accelerator keys are shown.
    /// </summary>
    public bool ShowAcceleratorKeys { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the menu separator character.
    /// </summary>
    public string SeparatorChar { get; set; } = "-";
}

/// <summary>
/// Represents a menu item.
/// </summary>
public class MenuItem
{
    /// <summary>
    /// Gets or sets the menu item ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the command ID to execute when selected.
    /// </summary>
    public string? CommandId { get; set; }
    
    /// <summary>
    /// Gets or sets the keyboard shortcut for this menu item.
    /// </summary>
    public string? ShortcutKey { get; set; }
    
    /// <summary>
    /// Gets or sets whether this is a separator item.
    /// </summary>
    public bool IsSeparator { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether this menu item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets child menu items (for submenus).
    /// </summary>
    public IReadOnlyList<MenuItem> Children { get; set; } = Array.Empty<MenuItem>();
}
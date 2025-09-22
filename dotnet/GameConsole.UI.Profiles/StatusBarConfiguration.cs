namespace GameConsole.UI.Profiles;

/// <summary>
/// Configuration for the status bar in a UI profile.
/// </summary>
public class StatusBarConfiguration
{
    /// <summary>
    /// Gets or sets the status bar items.
    /// </summary>
    public IReadOnlyList<StatusBarItem> Items { get; set; } = Array.Empty<StatusBarItem>();
    
    /// <summary>
    /// Gets or sets whether the status bar is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the status bar position (top or bottom).
    /// </summary>
    public string Position { get; set; } = "bottom";
    
    /// <summary>
    /// Gets or sets the status bar height in characters.
    /// </summary>
    public int Height { get; set; } = 1;
}

/// <summary>
/// Represents an item in the status bar.
/// </summary>
public class StatusBarItem
{
    /// <summary>
    /// Gets or sets the item ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the item alignment (left, center, right).
    /// </summary>
    public string Alignment { get; set; } = "left";
    
    /// <summary>
    /// Gets or sets the priority order (higher values appear first).
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets whether this item is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;
}

/// <summary>
/// Configuration for the toolbar in a UI profile.
/// </summary>
public class ToolbarConfiguration
{
    /// <summary>
    /// Gets or sets the toolbar items.
    /// </summary>
    public IReadOnlyList<ToolbarItem> Items { get; set; } = Array.Empty<ToolbarItem>();
    
    /// <summary>
    /// Gets or sets whether the toolbar is visible.
    /// </summary>
    public bool IsVisible { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the toolbar position (top, bottom).
    /// </summary>
    public string Position { get; set; } = "top";
}

/// <summary>
/// Represents an item in the toolbar.
/// </summary>
public class ToolbarItem
{
    /// <summary>
    /// Gets or sets the item ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display text.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the command ID to execute when clicked.
    /// </summary>
    public string? CommandId { get; set; }
    
    /// <summary>
    /// Gets or sets the tooltip text.
    /// </summary>
    public string? Tooltip { get; set; }
    
    /// <summary>
    /// Gets or sets whether this is a separator item.
    /// </summary>
    public bool IsSeparator { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether this item is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the item priority (higher values appear first).
    /// </summary>
    public int Priority { get; set; } = 0;
}
namespace GameConsole.UI.Core;

/// <summary>
/// Common UI types and enums used across the UI system.
/// </summary>

/// <summary>
/// Represents a position in the console coordinate system.
/// </summary>
public record struct ConsolePosition(int X, int Y);

/// <summary>
/// Represents dimensions (width, height) for UI components.
/// </summary>
public record struct ComponentSize(int Width, int Height);

/// <summary>
/// Represents the bounds of a UI component.
/// </summary>
public record struct ComponentBounds(ConsolePosition Position, ComponentSize Size)
{
    /// <summary>
    /// Gets the right edge of the bounds.
    /// </summary>
    public int Right => Position.X + Size.Width;
    
    /// <summary>
    /// Gets the bottom edge of the bounds.
    /// </summary>
    public int Bottom => Position.Y + Size.Height;
    
    /// <summary>
    /// Checks if a point is within these bounds.
    /// </summary>
    /// <param name="point">Point to check.</param>
    /// <returns>True if point is within bounds.</returns>
    public bool Contains(ConsolePosition point) =>
        point.X >= Position.X && point.X < Right &&
        point.Y >= Position.Y && point.Y < Bottom;
}

/// <summary>
/// Defines the visual style properties for UI components.
/// </summary>
public record struct ComponentStyle(
    ConsoleColor? Foreground = null,
    ConsoleColor? Background = null,
    BorderStyle Border = BorderStyle.None,
    TextAlignment Alignment = TextAlignment.Left,
    bool Bold = false,
    bool Underline = false);

/// <summary>
/// Represents padding/margin values for UI components.
/// </summary>
public record struct Spacing(int Left, int Top, int Right, int Bottom)
{
    /// <summary>
    /// Creates uniform spacing.
    /// </summary>
    /// <param name="all">Value for all sides.</param>
    public Spacing(int all) : this(all, all, all, all) { }
    
    /// <summary>
    /// Creates horizontal and vertical spacing.
    /// </summary>
    /// <param name="horizontal">Left and right spacing.</param>
    /// <param name="vertical">Top and bottom spacing.</param>
    public Spacing(int horizontal, int vertical) : this(horizontal, vertical, horizontal, vertical) { }
    
    /// <summary>
    /// Gets the total horizontal spacing.
    /// </summary>
    public int Horizontal => Left + Right;
    
    /// <summary>
    /// Gets the total vertical spacing.
    /// </summary>
    public int Vertical => Top + Bottom;
}

/// <summary>
/// Defines how borders are rendered around components.
/// </summary>
public enum BorderStyle
{
    None,
    Single,
    Double,
    Rounded,
    Thick,
    Dashed,
    Dotted
}

/// <summary>
/// Defines text alignment options.
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right,
    Justify
}

/// <summary>
/// Defines vertical alignment options.
/// </summary>
public enum VerticalAlignment
{
    Top,
    Middle,
    Bottom,
    Stretch
}

/// <summary>
/// Defines horizontal alignment options.
/// </summary>
public enum HorizontalAlignment
{
    Left,
    Center,
    Right,
    Stretch
}

/// <summary>
/// Defines layout direction options.
/// </summary>
public enum LayoutDirection
{
    Horizontal,
    Vertical
}

/// <summary>
/// Defines component visibility states.
/// </summary>
public enum Visibility
{
    Visible,
    Hidden,
    Collapsed
}

/// <summary>
/// Defines component states for styling and interaction.
/// </summary>
public enum ComponentState
{
    Normal,
    Focused,
    Pressed,
    Hovered,
    Disabled,
    Selected
}

/// <summary>
/// Dialog result values.
/// </summary>
public enum DialogResult
{
    None,
    OK,
    Cancel,
    Yes,
    No,
    Retry,
    Ignore,
    Abort
}

/// <summary>
/// Represents the result of a menu item selection.
/// </summary>
public record struct MenuItemResult(
    string ItemId,
    string DisplayText,
    object? Value = null,
    bool WasCancelled = false);

/// <summary>
/// Represents rendering context information.
/// </summary>
public record struct RenderContext(
    ComponentBounds ViewportBounds,
    DateTime RenderTime,
    bool ForceRedraw = false,
    int FrameNumber = 0);

/// <summary>
/// Represents component configuration for creation.
/// </summary>
public record ComponentConfiguration(
    string Id,
    string? ParentId = null,
    ComponentBounds? Bounds = null,
    ComponentStyle? Style = null,
    Visibility Visibility = Visibility.Visible,
    bool CanFocus = false,
    Dictionary<string, object>? Properties = null);

/// <summary>
/// Event arguments for component state changes.
/// </summary>
public class ComponentStateChangedEventArgs : EventArgs
{
    public string ComponentId { get; }
    public ComponentState OldState { get; }
    public ComponentState NewState { get; }
    public DateTime ChangeTime { get; }
    
    public ComponentStateChangedEventArgs(string componentId, ComponentState oldState, ComponentState newState)
    {
        ComponentId = componentId;
        OldState = oldState;
        NewState = newState;
        ChangeTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for layout invalidation.
/// </summary>
public class LayoutInvalidatedEventArgs : EventArgs
{
    public string? ComponentId { get; }
    public string Reason { get; }
    public DateTime InvalidationTime { get; }
    
    public LayoutInvalidatedEventArgs(string reason, string? componentId = null)
    {
        ComponentId = componentId;
        Reason = reason;
        InvalidationTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a UI theme with styling information.
/// </summary>
public record UITheme(
    string Name,
    string DisplayName,
    Dictionary<ComponentState, ComponentStyle> DefaultStyles,
    Dictionary<string, ComponentStyle> ComponentStyles,
    ConsoleColor DefaultForeground = ConsoleColor.Gray,
    ConsoleColor DefaultBackground = ConsoleColor.Black,
    string? Description = null);
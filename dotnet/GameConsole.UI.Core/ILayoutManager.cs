namespace GameConsole.UI.Core;

/// <summary>
/// Interface for layout management.
/// </summary>
public interface ILayoutManager
{
    /// <summary>
    /// Arrange components within a container using the specified layout.
    /// </summary>
    Task ArrangeAsync(IUIComponent container, IReadOnlyList<IUIComponent> components, LayoutType layout, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Calculate the preferred size for a container with given components and layout.
    /// </summary>
    Size CalculatePreferredSize(IReadOnlyList<IUIComponent> components, LayoutType layout, Size availableSize);
}

/// <summary>
/// Types of layout arrangements.
/// </summary>
public enum LayoutType
{
    /// <summary>
    /// Components are positioned manually at specific coordinates.
    /// </summary>
    None,
    
    /// <summary>
    /// Components are arranged vertically in a stack.
    /// </summary>
    VerticalStack,
    
    /// <summary>
    /// Components are arranged horizontally in a row.
    /// </summary>
    HorizontalStack,
    
    /// <summary>
    /// Components are arranged in a grid pattern.
    /// </summary>
    Grid,
    
    /// <summary>
    /// Components dock to edges of the container.
    /// </summary>
    Dock
}

/// <summary>
/// Layout parameters for components.
/// </summary>
public class LayoutParams
{
    /// <summary>
    /// Margins around the component.
    /// </summary>
    public Thickness Margin { get; set; } = Thickness.Empty;
    
    /// <summary>
    /// Padding inside the component.
    /// </summary>
    public Thickness Padding { get; set; } = Thickness.Empty;
    
    /// <summary>
    /// Horizontal alignment within parent container.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment { get; set; } = HorizontalAlignment.Left;
    
    /// <summary>
    /// Vertical alignment within parent container.
    /// </summary>
    public VerticalAlignment VerticalAlignment { get; set; } = VerticalAlignment.Top;
    
    /// <summary>
    /// Dock position for dock layout.
    /// </summary>
    public DockPosition Dock { get; set; } = DockPosition.None;
    
    /// <summary>
    /// Grid row for grid layout.
    /// </summary>
    public int GridRow { get; set; } = 0;
    
    /// <summary>
    /// Grid column for grid layout.
    /// </summary>
    public int GridColumn { get; set; } = 0;
    
    /// <summary>
    /// Number of rows this component spans in grid layout.
    /// </summary>
    public int GridRowSpan { get; set; } = 1;
    
    /// <summary>
    /// Number of columns this component spans in grid layout.
    /// </summary>
    public int GridColumnSpan { get; set; } = 1;
}

/// <summary>
/// Represents thickness values for margins, padding, borders, etc.
/// </summary>
public readonly struct Thickness : IEquatable<Thickness>
{
    public int Left { get; }
    public int Top { get; }
    public int Right { get; }
    public int Bottom { get; }
    
    public Thickness(int uniformSize)
        : this(uniformSize, uniformSize, uniformSize, uniformSize) { }
    
    public Thickness(int horizontal, int vertical)
        : this(horizontal, vertical, horizontal, vertical) { }
    
    public Thickness(int left, int top, int right, int bottom)
    {
        Left = Math.Max(0, left);
        Top = Math.Max(0, top);
        Right = Math.Max(0, right);
        Bottom = Math.Max(0, bottom);
    }
    
    public static readonly Thickness Empty = new(0);
    
    public int Horizontal => Left + Right;
    public int Vertical => Top + Bottom;
    
    public bool Equals(Thickness other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
    public override bool Equals(object? obj) => obj is Thickness other && Equals(other);
    public override int GetHashCode() => ((Left * 397) ^ Top) * 397 ^ (Right * 397) ^ Bottom;
    public override string ToString() => $"Thickness({Left}, {Top}, {Right}, {Bottom})";
    
    public static bool operator ==(Thickness left, Thickness right) => left.Equals(right);
    public static bool operator !=(Thickness left, Thickness right) => !left.Equals(right);
}

public enum HorizontalAlignment
{
    Left,
    Center, 
    Right,
    Stretch
}

public enum VerticalAlignment
{
    Top,
    Center,
    Bottom,
    Stretch
}

public enum DockPosition
{
    None,
    Left,
    Top,
    Right,
    Bottom,
    Fill
}
namespace GameConsole.UI.Core;

/// <summary>
/// Interface for UI layout management.
/// </summary>
public interface ILayout
{
    /// <summary>
    /// Arrange child components within the given bounds.
    /// </summary>
    /// <param name="container">Container holding the child components.</param>
    /// <param name="availableBounds">Available space for layout.</param>
    void ArrangeChildren(ILayoutContainer container, Rectangle availableBounds);
    
    /// <summary>
    /// Calculate the desired size for the container based on its children.
    /// </summary>
    /// <param name="container">Container to calculate size for.</param>
    /// <param name="availableSize">Available size constraints.</param>
    /// <returns>Desired size for the container.</returns>
    Size MeasureContainer(ILayoutContainer container, Size availableSize);
}

/// <summary>
/// Simple absolute layout - children positioned using explicit coordinates.
/// </summary>
public interface IAbsoluteLayout : ILayout
{
    // Inherits base layout functionality
}

/// <summary>
/// Stack layout - arranges children in a vertical or horizontal stack.
/// </summary>
public interface IStackLayout : ILayout
{
    /// <summary>
    /// Direction to stack child components.
    /// </summary>
    StackDirection Direction { get; set; }
    
    /// <summary>
    /// Spacing between child components.
    /// </summary>
    int Spacing { get; set; }
    
    /// <summary>
    /// Alignment of children within the stack.
    /// </summary>
    StackAlignment Alignment { get; set; }
}

/// <summary>
/// Grid layout - arranges children in a grid with rows and columns.
/// </summary>
public interface IGridLayout : ILayout
{
    /// <summary>
    /// Number of columns in the grid.
    /// </summary>
    int Columns { get; set; }
    
    /// <summary>
    /// Number of rows in the grid.
    /// </summary>
    int Rows { get; set; }
    
    /// <summary>
    /// Horizontal spacing between grid cells.
    /// </summary>
    int HorizontalSpacing { get; set; }
    
    /// <summary>
    /// Vertical spacing between grid cells.
    /// </summary>
    int VerticalSpacing { get; set; }
}

/// <summary>
/// Stack direction options.
/// </summary>
public enum StackDirection
{
    Vertical,
    Horizontal
}

/// <summary>
/// Alignment options for stack layout.
/// </summary>
public enum StackAlignment
{
    Start,
    Center,
    End,
    Fill
}

/// <summary>
/// Interface for components that can be positioned within a grid.
/// </summary>
public interface IGridPositioned
{
    /// <summary>
    /// Grid row this component occupies.
    /// </summary>
    int GridRow { get; set; }
    
    /// <summary>
    /// Grid column this component occupies.
    /// </summary>
    int GridColumn { get; set; }
    
    /// <summary>
    /// Number of rows this component spans.
    /// </summary>
    int RowSpan { get; set; }
    
    /// <summary>
    /// Number of columns this component spans.
    /// </summary>
    int ColumnSpan { get; set; }
}
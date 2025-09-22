namespace GameConsole.UI.Console;

/// <summary>
/// Represents layout alignment options.
/// </summary>
public enum LayoutAlignment
{
    /// <summary>Start/Left alignment.</summary>
    Start,
    /// <summary>Center alignment.</summary>
    Center,
    /// <summary>End/Right alignment.</summary>
    End,
    /// <summary>Stretch to fill available space.</summary>
    Stretch
}

/// <summary>
/// Represents layout direction options.
/// </summary>
public enum LayoutDirection
{
    /// <summary>Horizontal layout (left to right).</summary>
    Horizontal,
    /// <summary>Vertical layout (top to bottom).</summary>
    Vertical
}

/// <summary>
/// Represents margin/padding values.
/// </summary>
/// <param name="Left">Left margin/padding.</param>
/// <param name="Top">Top margin/padding.</param>
/// <param name="Right">Right margin/padding.</param>
/// <param name="Bottom">Bottom margin/padding.</param>
public readonly record struct LayoutSpacing(int Left, int Top, int Right, int Bottom)
{
    /// <summary>
    /// Creates uniform spacing.
    /// </summary>
    /// <param name="value">The spacing value for all sides.</param>
    public LayoutSpacing(int value) : this(value, value, value, value) { }
    
    /// <summary>
    /// Creates horizontal and vertical spacing.
    /// </summary>
    /// <param name="horizontal">The horizontal spacing (left and right).</param>
    /// <param name="vertical">The vertical spacing (top and bottom).</param>
    public LayoutSpacing(int horizontal, int vertical) : this(horizontal, vertical, horizontal, vertical) { }
    
    /// <summary>
    /// Gets the total horizontal spacing.
    /// </summary>
    public int TotalHorizontal => Left + Right;
    
    /// <summary>
    /// Gets the total vertical spacing.
    /// </summary>
    public int TotalVertical => Top + Bottom;
    
    /// <summary>
    /// Gets empty spacing (no spacing on any side).
    /// </summary>
    public static readonly LayoutSpacing None = new(0);
}

/// <summary>
/// Base interface for layout elements.
/// </summary>
public interface ILayout
{
    /// <summary>
    /// Gets the desired size of the element for the given available size.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size.</returns>
    ConsoleSize GetDesiredSize(ConsoleSize availableSize);
    
    /// <summary>
    /// Arranges the element within the specified rectangle.
    /// </summary>
    /// <param name="arrangeRect">The rectangle to arrange within.</param>
    /// <returns>The actual rectangle used by the element.</returns>
    ConsoleRect Arrange(ConsoleRect arrangeRect);
    
    /// <summary>
    /// Renders the element to the specified buffer.
    /// </summary>
    /// <param name="buffer">The console buffer to render to.</param>
    /// <param name="bounds">The bounds to render within.</param>
    void Render(ConsoleBuffer buffer, ConsoleRect bounds);
}

/// <summary>
/// Interface for layout containers that can hold child elements.
/// </summary>
public interface ILayoutContainer : ILayout
{
    /// <summary>
    /// Gets the child elements.
    /// </summary>
    IReadOnlyList<ILayout> Children { get; }
    
    /// <summary>
    /// Adds a child element.
    /// </summary>
    /// <param name="child">The child element to add.</param>
    void AddChild(ILayout child);
    
    /// <summary>
    /// Removes a child element.
    /// </summary>
    /// <param name="child">The child element to remove.</param>
    /// <returns>True if the child was found and removed; otherwise, false.</returns>
    bool RemoveChild(ILayout child);
    
    /// <summary>
    /// Clears all child elements.
    /// </summary>
    void ClearChildren();
}

/// <summary>
/// Manages console layout and provides text positioning services.
/// </summary>
public class ConsoleLayoutManager
{
    private readonly TerminalInfo _terminalInfo;
    private readonly List<ILayoutContainer> _containers = [];
    
    /// <summary>
    /// Initializes a new console layout manager.
    /// </summary>
    /// <param name="terminalInfo">Terminal information.</param>
    public ConsoleLayoutManager(TerminalInfo terminalInfo)
    {
        _terminalInfo = terminalInfo ?? throw new ArgumentNullException(nameof(terminalInfo));
    }
    
    /// <summary>
    /// Gets the available layout size.
    /// </summary>
    public ConsoleSize AvailableSize => _terminalInfo.Size;
    
    /// <summary>
    /// Creates a horizontal layout container.
    /// </summary>
    /// <param name="spacing">Spacing between child elements.</param>
    /// <param name="padding">Internal padding.</param>
    /// <returns>A new horizontal layout container.</returns>
    public ILayoutContainer CreateHorizontalContainer(int spacing = 0, LayoutSpacing padding = default)
        => new StackLayout(LayoutDirection.Horizontal, spacing, padding);
    
    /// <summary>
    /// Creates a vertical layout container.
    /// </summary>
    /// <param name="spacing">Spacing between child elements.</param>
    /// <param name="padding">Internal padding.</param>
    /// <returns>A new vertical layout container.</returns>
    public ILayoutContainer CreateVerticalContainer(int spacing = 0, LayoutSpacing padding = default)
        => new StackLayout(LayoutDirection.Vertical, spacing, padding);
    
    /// <summary>
    /// Creates a grid layout container.
    /// </summary>
    /// <param name="columns">Number of columns.</param>
    /// <param name="rows">Number of rows.</param>
    /// <param name="spacing">Spacing between cells.</param>
    /// <param name="padding">Internal padding.</param>
    /// <returns>A new grid layout container.</returns>
    public ILayoutContainer CreateGridContainer(int columns, int rows, int spacing = 0, LayoutSpacing padding = default)
        => new GridLayout(columns, rows, spacing, padding);
    
    /// <summary>
    /// Creates a text element with the specified content and formatting.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="alignment">Text alignment.</param>
    /// <param name="foregroundColor">Foreground color ANSI sequence.</param>
    /// <param name="backgroundColor">Background color ANSI sequence.</param>
    /// <param name="style">Style ANSI sequence.</param>
    /// <returns>A new text layout element.</returns>
    public ILayout CreateTextElement(string text, LayoutAlignment alignment = LayoutAlignment.Start,
                                   string foregroundColor = "", string backgroundColor = "", string style = "")
        => new TextElement(text, alignment, foregroundColor, backgroundColor, style);
    
    /// <summary>
    /// Creates a border element that wraps another element.
    /// </summary>
    /// <param name="child">The child element to wrap.</param>
    /// <param name="title">Optional border title.</param>
    /// <param name="foregroundColor">Border foreground color ANSI sequence.</param>
    /// <param name="backgroundColor">Border background color ANSI sequence.</param>
    /// <param name="style">Border style ANSI sequence.</param>
    /// <returns>A new border layout element.</returns>
    public ILayout CreateBorderElement(ILayout child, string title = "", 
                                     string foregroundColor = "", string backgroundColor = "", string style = "")
        => new BorderElement(child, title, foregroundColor, backgroundColor, style);
    
    /// <summary>
    /// Calculates the optimal layout for the given container within the available space.
    /// </summary>
    /// <param name="container">The container to layout.</param>
    /// <returns>The calculated layout rectangle.</returns>
    public ConsoleRect CalculateLayout(ILayoutContainer container)
    {
        var availableSize = AvailableSize;
        var desiredSize = container.GetDesiredSize(availableSize);
        var arrangeRect = new ConsoleRect(0, 0, 
            Math.Min(desiredSize.Width, availableSize.Width),
            Math.Min(desiredSize.Height, availableSize.Height));
        
        return container.Arrange(arrangeRect);
    }
    
    /// <summary>
    /// Renders the specified container to a console buffer.
    /// </summary>
    /// <param name="container">The container to render.</param>
    /// <returns>A console buffer containing the rendered layout.</returns>
    public ConsoleBuffer RenderToBuffer(ILayoutContainer container)
    {
        var layoutRect = CalculateLayout(container);
        var buffer = new ConsoleBuffer(Math.Max(1, layoutRect.Width), Math.Max(1, layoutRect.Height));
        container.Render(buffer, new ConsoleRect(0, 0, buffer.Width, buffer.Height));
        return buffer;
    }
    
    /// <summary>
    /// Centers a rectangle within the available space.
    /// </summary>
    /// <param name="size">The size to center.</param>
    /// <returns>A centered rectangle.</returns>
    public ConsoleRect CenterRect(ConsoleSize size)
    {
        var availableSize = AvailableSize;
        var x = Math.Max(0, (availableSize.Width - size.Width) / 2);
        var y = Math.Max(0, (availableSize.Height - size.Height) / 2);
        return new ConsoleRect(x, y, Math.Min(size.Width, availableSize.Width), 
                              Math.Min(size.Height, availableSize.Height));
    }
    
    /// <summary>
    /// Updates the terminal size information.
    /// </summary>
    public void UpdateTerminalSize()
    {
        _terminalInfo.UpdateSize();
    }
}
namespace GameConsole.UI.Console;

/// <summary>
/// A text element that displays formatted text.
/// </summary>
public class TextElement : ILayout
{
    private readonly string _text;
    private readonly LayoutAlignment _alignment;
    private readonly string _foregroundColor;
    private readonly string _backgroundColor;
    private readonly string _style;
    
    /// <summary>
    /// Initializes a new text element.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="alignment">Text alignment.</param>
    /// <param name="foregroundColor">Foreground color ANSI sequence.</param>
    /// <param name="backgroundColor">Background color ANSI sequence.</param>
    /// <param name="style">Style ANSI sequence.</param>
    public TextElement(string text, LayoutAlignment alignment = LayoutAlignment.Start,
                      string foregroundColor = "", string backgroundColor = "", string style = "")
    {
        _text = text ?? string.Empty;
        _alignment = alignment;
        _foregroundColor = foregroundColor;
        _backgroundColor = backgroundColor;
        _style = style;
    }
    
    /// <summary>
    /// Gets the text content.
    /// </summary>
    public string Text => _text;
    
    /// <inheritdoc />
    public ConsoleSize GetDesiredSize(ConsoleSize availableSize)
    {
        if (string.IsNullOrEmpty(_text))
            return new ConsoleSize(0, 1);
        
        var lines = _text.Split('\n');
        var maxWidth = lines.Max(line => line.Length);
        return new ConsoleSize(Math.Min(maxWidth, availableSize.Width), 
                              Math.Min(lines.Length, availableSize.Height));
    }
    
    /// <inheritdoc />
    public ConsoleRect Arrange(ConsoleRect arrangeRect)
    {
        var desiredSize = GetDesiredSize(arrangeRect.Size);
        return new ConsoleRect(arrangeRect.X, arrangeRect.Y, 
                              Math.Min(desiredSize.Width, arrangeRect.Width),
                              Math.Min(desiredSize.Height, arrangeRect.Height));
    }
    
    /// <inheritdoc />
    public void Render(ConsoleBuffer buffer, ConsoleRect bounds)
    {
        if (string.IsNullOrEmpty(_text) || bounds.IsEmpty)
            return;
        
        var lines = _text.Split('\n');
        for (var i = 0; i < Math.Min(lines.Length, bounds.Height); i++)
        {
            var line = lines[i];
            if (string.IsNullOrEmpty(line)) continue;
            
            var x = bounds.X;
            var availableWidth = bounds.Width;
            
            // Apply alignment
            if (_alignment == LayoutAlignment.Center && line.Length < availableWidth)
                x += (availableWidth - line.Length) / 2;
            else if (_alignment == LayoutAlignment.End && line.Length < availableWidth)
                x += availableWidth - line.Length;
            
            // Clip text to fit
            var renderText = line.Length > availableWidth ? line[..availableWidth] : line;
            
            buffer.SetText(x, bounds.Y + i, renderText, _foregroundColor, _backgroundColor, _style);
        }
    }
}

/// <summary>
/// A border element that wraps another element with a border.
/// </summary>
public class BorderElement : ILayout
{
    private readonly ILayout _child;
    private readonly string _title;
    private readonly string _foregroundColor;
    private readonly string _backgroundColor;
    private readonly string _style;
    
    /// <summary>
    /// Initializes a new border element.
    /// </summary>
    /// <param name="child">The child element to wrap.</param>
    /// <param name="title">Optional border title.</param>
    /// <param name="foregroundColor">Border foreground color ANSI sequence.</param>
    /// <param name="backgroundColor">Border background color ANSI sequence.</param>
    /// <param name="style">Border style ANSI sequence.</param>
    public BorderElement(ILayout child, string title = "", 
                        string foregroundColor = "", string backgroundColor = "", string style = "")
    {
        _child = child ?? throw new ArgumentNullException(nameof(child));
        _title = title ?? string.Empty;
        _foregroundColor = foregroundColor;
        _backgroundColor = backgroundColor;
        _style = style;
    }
    
    /// <inheritdoc />
    public ConsoleSize GetDesiredSize(ConsoleSize availableSize)
    {
        // Reserve space for border
        var childAvailable = new ConsoleSize(
            Math.Max(0, availableSize.Width - 2),
            Math.Max(0, availableSize.Height - 2));
        
        var childDesired = _child.GetDesiredSize(childAvailable);
        
        // Add border space
        return new ConsoleSize(childDesired.Width + 2, childDesired.Height + 2);
    }
    
    /// <inheritdoc />
    public ConsoleRect Arrange(ConsoleRect arrangeRect)
    {
        var desiredSize = GetDesiredSize(arrangeRect.Size);
        var actualWidth = Math.Min(desiredSize.Width, arrangeRect.Width);
        var actualHeight = Math.Min(desiredSize.Height, arrangeRect.Height);
        
        // Arrange child within border
        if (actualWidth > 2 && actualHeight > 2)
        {
            var childRect = new ConsoleRect(arrangeRect.X + 1, arrangeRect.Y + 1, 
                                          actualWidth - 2, actualHeight - 2);
            _child.Arrange(childRect);
        }
        
        return new ConsoleRect(arrangeRect.X, arrangeRect.Y, actualWidth, actualHeight);
    }
    
    /// <inheritdoc />
    public void Render(ConsoleBuffer buffer, ConsoleRect bounds)
    {
        if (bounds.Width < 2 || bounds.Height < 2)
            return;
        
        // Draw border
        buffer.DrawBorder(bounds, _foregroundColor, _backgroundColor, _style);
        
        // Draw title if provided
        if (!string.IsNullOrEmpty(_title) && bounds.Width > 4)
        {
            var titleText = _title.Length > bounds.Width - 4 ? _title[..(bounds.Width - 4)] : _title;
            buffer.SetText(bounds.X + 2, bounds.Y, titleText, _foregroundColor, _backgroundColor, _style);
        }
        
        // Render child within border
        if (bounds.Width > 2 && bounds.Height > 2)
        {
            var childBounds = new ConsoleRect(bounds.X + 1, bounds.Y + 1, 
                                            bounds.Width - 2, bounds.Height - 2);
            _child.Render(buffer, childBounds);
        }
    }
}

/// <summary>
/// A stack layout container that arranges children horizontally or vertically.
/// </summary>
public class StackLayout : ILayoutContainer
{
    private readonly List<ILayout> _children = [];
    private readonly LayoutDirection _direction;
    private readonly int _spacing;
    private readonly LayoutSpacing _padding;
    
    /// <summary>
    /// Initializes a new stack layout.
    /// </summary>
    /// <param name="direction">Layout direction.</param>
    /// <param name="spacing">Spacing between children.</param>
    /// <param name="padding">Internal padding.</param>
    public StackLayout(LayoutDirection direction, int spacing = 0, LayoutSpacing padding = default)
    {
        _direction = direction;
        _spacing = Math.Max(0, spacing);
        _padding = padding;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<ILayout> Children => _children.AsReadOnly();
    
    /// <inheritdoc />
    public void AddChild(ILayout child)
    {
        if (child != null)
            _children.Add(child);
    }
    
    /// <inheritdoc />
    public bool RemoveChild(ILayout child) => _children.Remove(child);
    
    /// <inheritdoc />
    public void ClearChildren() => _children.Clear();
    
    /// <inheritdoc />
    public ConsoleSize GetDesiredSize(ConsoleSize availableSize)
    {
        if (_children.Count == 0)
            return new ConsoleSize(_padding.TotalHorizontal, _padding.TotalVertical);
        
        var contentSize = new ConsoleSize(
            Math.Max(0, availableSize.Width - _padding.TotalHorizontal),
            Math.Max(0, availableSize.Height - _padding.TotalVertical));
        
        if (_direction == LayoutDirection.Horizontal)
        {
            var totalWidth = 0;
            var maxHeight = 0;
            
            foreach (var child in _children)
            {
                var childSize = child.GetDesiredSize(contentSize);
                totalWidth += childSize.Width;
                maxHeight = Math.Max(maxHeight, childSize.Height);
            }
            
            totalWidth += (_children.Count - 1) * _spacing;
            return new ConsoleSize(totalWidth + _padding.TotalHorizontal, 
                                  maxHeight + _padding.TotalVertical);
        }
        else
        {
            var maxWidth = 0;
            var totalHeight = 0;
            
            foreach (var child in _children)
            {
                var childSize = child.GetDesiredSize(contentSize);
                maxWidth = Math.Max(maxWidth, childSize.Width);
                totalHeight += childSize.Height;
            }
            
            totalHeight += (_children.Count - 1) * _spacing;
            return new ConsoleSize(maxWidth + _padding.TotalHorizontal,
                                  totalHeight + _padding.TotalVertical);
        }
    }
    
    /// <inheritdoc />
    public ConsoleRect Arrange(ConsoleRect arrangeRect)
    {
        if (_children.Count == 0)
            return arrangeRect;
        
        var contentRect = new ConsoleRect(
            arrangeRect.X + _padding.Left,
            arrangeRect.Y + _padding.Top,
            Math.Max(0, arrangeRect.Width - _padding.TotalHorizontal),
            Math.Max(0, arrangeRect.Height - _padding.TotalVertical));
        
        if (_direction == LayoutDirection.Horizontal)
        {
            var x = contentRect.X;
            foreach (var child in _children)
            {
                var childSize = child.GetDesiredSize(contentRect.Size);
                var childRect = new ConsoleRect(x, contentRect.Y, 
                    Math.Min(childSize.Width, contentRect.Right - x), contentRect.Height);
                child.Arrange(childRect);
                x += childRect.Width + _spacing;
            }
        }
        else
        {
            var y = contentRect.Y;
            foreach (var child in _children)
            {
                var childSize = child.GetDesiredSize(contentRect.Size);
                var childRect = new ConsoleRect(contentRect.X, y, contentRect.Width,
                    Math.Min(childSize.Height, contentRect.Bottom - y));
                child.Arrange(childRect);
                y += childRect.Height + _spacing;
            }
        }
        
        return arrangeRect;
    }
    
    /// <inheritdoc />
    public void Render(ConsoleBuffer buffer, ConsoleRect bounds)
    {
        var contentRect = new ConsoleRect(
            bounds.X + _padding.Left,
            bounds.Y + _padding.Top,
            Math.Max(0, bounds.Width - _padding.TotalHorizontal),
            Math.Max(0, bounds.Height - _padding.TotalVertical));
        
        if (_direction == LayoutDirection.Horizontal)
        {
            var x = contentRect.X;
            foreach (var child in _children)
            {
                if (x >= contentRect.Right) break;
                
                var childSize = child.GetDesiredSize(contentRect.Size);
                var childBounds = new ConsoleRect(x, contentRect.Y, 
                    Math.Min(childSize.Width, contentRect.Right - x), contentRect.Height);
                child.Render(buffer, childBounds);
                x += childBounds.Width + _spacing;
            }
        }
        else
        {
            var y = contentRect.Y;
            foreach (var child in _children)
            {
                if (y >= contentRect.Bottom) break;
                
                var childSize = child.GetDesiredSize(contentRect.Size);
                var childBounds = new ConsoleRect(contentRect.X, y, contentRect.Width,
                    Math.Min(childSize.Height, contentRect.Bottom - y));
                child.Render(buffer, childBounds);
                y += childBounds.Height + _spacing;
            }
        }
    }
}

/// <summary>
/// A grid layout container that arranges children in a grid pattern.
/// </summary>
public class GridLayout : ILayoutContainer
{
    private readonly List<ILayout> _children = [];
    private readonly int _columns;
    private readonly int _rows;
    private readonly int _spacing;
    private readonly LayoutSpacing _padding;
    
    /// <summary>
    /// Initializes a new grid layout.
    /// </summary>
    /// <param name="columns">Number of columns.</param>
    /// <param name="rows">Number of rows.</param>
    /// <param name="spacing">Spacing between cells.</param>
    /// <param name="padding">Internal padding.</param>
    public GridLayout(int columns, int rows, int spacing = 0, LayoutSpacing padding = default)
    {
        _columns = Math.Max(1, columns);
        _rows = Math.Max(1, rows);
        _spacing = Math.Max(0, spacing);
        _padding = padding;
    }
    
    /// <inheritdoc />
    public IReadOnlyList<ILayout> Children => _children.AsReadOnly();
    
    /// <inheritdoc />
    public void AddChild(ILayout child)
    {
        if (child != null && _children.Count < _columns * _rows)
            _children.Add(child);
    }
    
    /// <inheritdoc />
    public bool RemoveChild(ILayout child) => _children.Remove(child);
    
    /// <inheritdoc />
    public void ClearChildren() => _children.Clear();
    
    /// <inheritdoc />
    public ConsoleSize GetDesiredSize(ConsoleSize availableSize)
    {
        if (_children.Count == 0)
            return new ConsoleSize(_padding.TotalHorizontal, _padding.TotalVertical);
        
        var contentSize = new ConsoleSize(
            Math.Max(0, availableSize.Width - _padding.TotalHorizontal),
            Math.Max(0, availableSize.Height - _padding.TotalVertical));
        
        var cellWidth = (contentSize.Width - (_columns - 1) * _spacing) / _columns;
        var cellHeight = (contentSize.Height - (_rows - 1) * _spacing) / _rows;
        var cellSize = new ConsoleSize(Math.Max(0, cellWidth), Math.Max(0, cellHeight));
        
        var maxChildWidth = 0;
        var maxChildHeight = 0;
        
        foreach (var child in _children)
        {
            var childSize = child.GetDesiredSize(cellSize);
            maxChildWidth = Math.Max(maxChildWidth, childSize.Width);
            maxChildHeight = Math.Max(maxChildHeight, childSize.Height);
        }
        
        var totalWidth = _columns * maxChildWidth + (_columns - 1) * _spacing + _padding.TotalHorizontal;
        var totalHeight = _rows * maxChildHeight + (_rows - 1) * _spacing + _padding.TotalVertical;
        
        return new ConsoleSize(Math.Min(totalWidth, availableSize.Width),
                              Math.Min(totalHeight, availableSize.Height));
    }
    
    /// <inheritdoc />
    public ConsoleRect Arrange(ConsoleRect arrangeRect)
    {
        if (_children.Count == 0)
            return arrangeRect;
        
        var contentRect = new ConsoleRect(
            arrangeRect.X + _padding.Left,
            arrangeRect.Y + _padding.Top,
            Math.Max(0, arrangeRect.Width - _padding.TotalHorizontal),
            Math.Max(0, arrangeRect.Height - _padding.TotalVertical));
        
        var cellWidth = (contentRect.Width - (_columns - 1) * _spacing) / _columns;
        var cellHeight = (contentRect.Height - (_rows - 1) * _spacing) / _rows;
        
        for (var i = 0; i < _children.Count; i++)
        {
            var row = i / _columns;
            var col = i % _columns;
            
            if (row >= _rows) break;
            
            var x = contentRect.X + col * (cellWidth + _spacing);
            var y = contentRect.Y + row * (cellHeight + _spacing);
            
            var cellRect = new ConsoleRect(x, y, cellWidth, cellHeight);
            _children[i].Arrange(cellRect);
        }
        
        return arrangeRect;
    }
    
    /// <inheritdoc />
    public void Render(ConsoleBuffer buffer, ConsoleRect bounds)
    {
        var contentRect = new ConsoleRect(
            bounds.X + _padding.Left,
            bounds.Y + _padding.Top,
            Math.Max(0, bounds.Width - _padding.TotalHorizontal),
            Math.Max(0, bounds.Height - _padding.TotalVertical));
        
        var cellWidth = (contentRect.Width - (_columns - 1) * _spacing) / _columns;
        var cellHeight = (contentRect.Height - (_rows - 1) * _spacing) / _rows;
        
        for (var i = 0; i < _children.Count; i++)
        {
            var row = i / _columns;
            var col = i % _columns;
            
            if (row >= _rows) break;
            
            var x = contentRect.X + col * (cellWidth + _spacing);
            var y = contentRect.Y + row * (cellHeight + _spacing);
            
            var cellBounds = new ConsoleRect(x, y, cellWidth, cellHeight);
            _children[i].Render(buffer, cellBounds);
        }
    }
}
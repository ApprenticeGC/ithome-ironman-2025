namespace GameConsole.UI.Console;

/// <summary>
/// Represents a single character cell in the console with formatting information.
/// </summary>
public readonly record struct ConsoleCell(char Character, string ForegroundColor, string BackgroundColor, string Style)
{
    /// <summary>
    /// Gets an empty cell (space character with default formatting).
    /// </summary>
    public static readonly ConsoleCell Empty = new(' ', string.Empty, string.Empty, string.Empty);
    
    /// <summary>
    /// Creates a cell with just a character and default formatting.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>A console cell with the specified character.</returns>
    public static ConsoleCell Create(char character) => new(character, string.Empty, string.Empty, string.Empty);
    
    /// <summary>
    /// Creates a cell with character and foreground color.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <param name="foregroundColor">The foreground color ANSI sequence.</param>
    /// <returns>A console cell with the specified character and foreground color.</returns>
    public static ConsoleCell Create(char character, string foregroundColor) 
        => new(character, foregroundColor, string.Empty, string.Empty);
    
    /// <summary>
    /// Gets whether this cell is empty.
    /// </summary>
    public bool IsEmpty => Character == ' ' && string.IsNullOrEmpty(ForegroundColor) && 
                          string.IsNullOrEmpty(BackgroundColor) && string.IsNullOrEmpty(Style);
}

/// <summary>
/// Virtual console buffer for complex text layout and rendering.
/// Provides a high-level interface for text manipulation with ANSI formatting.
/// </summary>
public class ConsoleBuffer
{
    private readonly ConsoleCell[,] _buffer;
    private readonly int _width;
    private readonly int _height;
    
    /// <summary>
    /// Initializes a new console buffer.
    /// </summary>
    /// <param name="width">Width in characters.</param>
    /// <param name="height">Height in rows.</param>
    public ConsoleBuffer(int width, int height)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        
        _width = width;
        _height = height;
        _buffer = new ConsoleCell[height, width];
        Clear();
    }
    
    /// <summary>
    /// Gets the width of the buffer.
    /// </summary>
    public int Width => _width;
    
    /// <summary>
    /// Gets the height of the buffer.
    /// </summary>
    public int Height => _height;
    
    /// <summary>
    /// Gets the size of the buffer.
    /// </summary>
    public ConsoleSize Size => new(_width, _height);
    
    /// <summary>
    /// Gets or sets a cell at the specified position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <returns>The console cell at the specified position.</returns>
    public ConsoleCell this[int x, int y]
    {
        get => IsValidPosition(x, y) ? _buffer[y, x] : ConsoleCell.Empty;
        set { if (IsValidPosition(x, y)) _buffer[y, x] = value; }
    }
    
    /// <summary>
    /// Gets or sets a cell at the specified point.
    /// </summary>
    /// <param name="point">The position.</param>
    /// <returns>The console cell at the specified position.</returns>
    public ConsoleCell this[ConsolePoint point]
    {
        get => this[point.X, point.Y];
        set => this[point.X, point.Y] = value;
    }
    
    /// <summary>
    /// Clears the entire buffer.
    /// </summary>
    public void Clear()
    {
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                _buffer[y, x] = ConsoleCell.Empty;
            }
        }
    }
    
    /// <summary>
    /// Clears a rectangular region of the buffer.
    /// </summary>
    /// <param name="rect">The rectangle to clear.</param>
    public void Clear(ConsoleRect rect)
    {
        var clippedRect = ClipRect(rect);
        for (var y = clippedRect.Top; y < clippedRect.Bottom; y++)
        {
            for (var x = clippedRect.Left; x < clippedRect.Right; x++)
            {
                _buffer[y, x] = ConsoleCell.Empty;
            }
        }
    }
    
    /// <summary>
    /// Sets text at the specified position.
    /// </summary>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="text">The text to set.</param>
    /// <param name="foregroundColor">Optional foreground color ANSI sequence.</param>
    /// <param name="backgroundColor">Optional background color ANSI sequence.</param>
    /// <param name="style">Optional style ANSI sequence.</param>
    public void SetText(int x, int y, string text, string foregroundColor = "", 
                        string backgroundColor = "", string style = "")
    {
        if (string.IsNullOrEmpty(text) || !IsValidPosition(x, y)) return;
        
        for (var i = 0; i < text.Length && x + i < _width; i++)
        {
            _buffer[y, x + i] = new ConsoleCell(text[i], foregroundColor, backgroundColor, style);
        }
    }
    
    /// <summary>
    /// Sets text at the specified point.
    /// </summary>
    /// <param name="point">The position.</param>
    /// <param name="text">The text to set.</param>
    /// <param name="foregroundColor">Optional foreground color ANSI sequence.</param>
    /// <param name="backgroundColor">Optional background color ANSI sequence.</param>
    /// <param name="style">Optional style ANSI sequence.</param>
    public void SetText(ConsolePoint point, string text, string foregroundColor = "", 
                        string backgroundColor = "", string style = "")
        => SetText(point.X, point.Y, text, foregroundColor, backgroundColor, style);
    
    /// <summary>
    /// Fills a rectangular region with a character and formatting.
    /// </summary>
    /// <param name="rect">The rectangle to fill.</param>
    /// <param name="character">The character to fill with.</param>
    /// <param name="foregroundColor">Optional foreground color ANSI sequence.</param>
    /// <param name="backgroundColor">Optional background color ANSI sequence.</param>
    /// <param name="style">Optional style ANSI sequence.</param>
    public void FillRect(ConsoleRect rect, char character, string foregroundColor = "", 
                         string backgroundColor = "", string style = "")
    {
        var clippedRect = ClipRect(rect);
        var cell = new ConsoleCell(character, foregroundColor, backgroundColor, style);
        
        for (var y = clippedRect.Top; y < clippedRect.Bottom; y++)
        {
            for (var x = clippedRect.Left; x < clippedRect.Right; x++)
            {
                _buffer[y, x] = cell;
            }
        }
    }
    
    /// <summary>
    /// Draws a border around a rectangular region.
    /// </summary>
    /// <param name="rect">The rectangle to border.</param>
    /// <param name="foregroundColor">Optional foreground color ANSI sequence.</param>
    /// <param name="backgroundColor">Optional background color ANSI sequence.</param>
    /// <param name="style">Optional style ANSI sequence.</param>
    public void DrawBorder(ConsoleRect rect, string foregroundColor = "", 
                          string backgroundColor = "", string style = "")
    {
        if (rect.Width < 2 || rect.Height < 2) return;
        
        var clippedRect = ClipRect(rect);
        if (clippedRect.IsEmpty) return;
        
        // Top and bottom borders
        for (var x = clippedRect.Left + 1; x < clippedRect.Right - 1; x++)
        {
            if (clippedRect.Top < _height)
                _buffer[clippedRect.Top, x] = new ConsoleCell('─', foregroundColor, backgroundColor, style);
            if (clippedRect.Bottom - 1 >= 0 && clippedRect.Bottom - 1 < _height)
                _buffer[clippedRect.Bottom - 1, x] = new ConsoleCell('─', foregroundColor, backgroundColor, style);
        }
        
        // Left and right borders
        for (var y = clippedRect.Top + 1; y < clippedRect.Bottom - 1; y++)
        {
            if (clippedRect.Left < _width)
                _buffer[y, clippedRect.Left] = new ConsoleCell('│', foregroundColor, backgroundColor, style);
            if (clippedRect.Right - 1 >= 0 && clippedRect.Right - 1 < _width)
                _buffer[y, clippedRect.Right - 1] = new ConsoleCell('│', foregroundColor, backgroundColor, style);
        }
        
        // Corners
        if (IsValidPosition(clippedRect.Left, clippedRect.Top))
            _buffer[clippedRect.Top, clippedRect.Left] = new ConsoleCell('┌', foregroundColor, backgroundColor, style);
        if (IsValidPosition(clippedRect.Right - 1, clippedRect.Top))
            _buffer[clippedRect.Top, clippedRect.Right - 1] = new ConsoleCell('┐', foregroundColor, backgroundColor, style);
        if (IsValidPosition(clippedRect.Left, clippedRect.Bottom - 1))
            _buffer[clippedRect.Bottom - 1, clippedRect.Left] = new ConsoleCell('└', foregroundColor, backgroundColor, style);
        if (IsValidPosition(clippedRect.Right - 1, clippedRect.Bottom - 1))
            _buffer[clippedRect.Bottom - 1, clippedRect.Right - 1] = new ConsoleCell('┘', foregroundColor, backgroundColor, style);
    }
    
    /// <summary>
    /// Renders the buffer to a string with ANSI escape sequences.
    /// </summary>
    /// <returns>The rendered buffer as a string.</returns>
    public string Render()
    {
        var sb = new System.Text.StringBuilder();
        var lastForegroundColor = string.Empty;
        var lastBackgroundColor = string.Empty;
        var lastStyle = string.Empty;
        
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                var cell = _buffer[y, x];
                
                // Apply formatting changes only when needed
                if (cell.Style != lastStyle)
                {
                    sb.Append(ANSIEscapeSequences.Reset);
                    if (!string.IsNullOrEmpty(cell.Style))
                        sb.Append(cell.Style);
                    lastStyle = cell.Style;
                    lastForegroundColor = string.Empty; // Reset tracking
                    lastBackgroundColor = string.Empty;
                }
                
                if (cell.ForegroundColor != lastForegroundColor)
                {
                    if (!string.IsNullOrEmpty(cell.ForegroundColor))
                        sb.Append(cell.ForegroundColor);
                    lastForegroundColor = cell.ForegroundColor;
                }
                
                if (cell.BackgroundColor != lastBackgroundColor)
                {
                    if (!string.IsNullOrEmpty(cell.BackgroundColor))
                        sb.Append(cell.BackgroundColor);
                    lastBackgroundColor = cell.BackgroundColor;
                }
                
                sb.Append(cell.Character);
            }
            
            if (y < _height - 1)
                sb.AppendLine();
        }
        
        sb.Append(ANSIEscapeSequences.Reset);
        return sb.ToString();
    }
    
    /// <summary>
    /// Copies a rectangular region from another buffer.
    /// </summary>
    /// <param name="source">The source buffer.</param>
    /// <param name="sourceRect">The source rectangle.</param>
    /// <param name="destPoint">The destination point.</param>
    public void CopyFrom(ConsoleBuffer source, ConsoleRect sourceRect, ConsolePoint destPoint)
    {
        var clippedSourceRect = source.ClipRect(sourceRect);
        var destRect = new ConsoleRect(destPoint, clippedSourceRect.Size);
        var clippedDestRect = ClipRect(destRect);
        
        var copyWidth = Math.Min(clippedSourceRect.Width, clippedDestRect.Width);
        var copyHeight = Math.Min(clippedSourceRect.Height, clippedDestRect.Height);
        
        for (var y = 0; y < copyHeight; y++)
        {
            for (var x = 0; x < copyWidth; x++)
            {
                var sourceCell = source[clippedSourceRect.X + x, clippedSourceRect.Y + y];
                this[clippedDestRect.X + x, clippedDestRect.Y + y] = sourceCell;
            }
        }
    }
    
    private bool IsValidPosition(int x, int y) => x >= 0 && x < _width && y >= 0 && y < _height;
    
    private ConsoleRect ClipRect(ConsoleRect rect)
    {
        var left = Math.Max(0, rect.Left);
        var top = Math.Max(0, rect.Top);
        var right = Math.Min(_width, rect.Right);
        var bottom = Math.Min(_height, rect.Bottom);
        
        return new ConsoleRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }
}
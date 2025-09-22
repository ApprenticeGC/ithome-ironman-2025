namespace GameConsole.UI.Console;

/// <summary>
/// Virtual console buffer for text-based rendering with ANSI support.
/// </summary>
public class ConsoleBuffer : IConsoleBuffer
{
    private readonly ConsoleCell[,] _buffer;
    private readonly object _bufferLock = new();

    /// <inheritdoc/>
    public int Width { get; private set; }

    /// <inheritdoc/>
    public int Height { get; private set; }

    /// <summary>
    /// Initializes a new console buffer with the specified dimensions.
    /// </summary>
    /// <param name="width">Buffer width in characters.</param>
    /// <param name="height">Buffer height in characters.</param>
    public ConsoleBuffer(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
        _buffer = new ConsoleCell[Height, Width];
        Clear();
    }

    /// <inheritdoc/>
    public void WriteAt(int x, int y, string text, ConsoleTextStyle style = default)
    {
        if (string.IsNullOrEmpty(text) || !IsValidPosition(x, y))
            return;

        lock (_bufferLock)
        {
            for (int i = 0; i < text.Length && x + i < Width; i++)
            {
                _buffer[y, x + i] = new ConsoleCell(text[i], style);
            }
        }
    }

    /// <inheritdoc/>
    public void WriteCharAt(int x, int y, char character, ConsoleTextStyle style = default)
    {
        if (!IsValidPosition(x, y))
            return;

        lock (_bufferLock)
        {
            _buffer[y, x] = new ConsoleCell(character, style);
        }
    }

    /// <inheritdoc/>
    public void FillArea(int x, int y, int width, int height, char character = ' ', ConsoleTextStyle style = default)
    {
        var bounds = ClipBounds(x, y, width, height);
        if (!bounds.HasValue)
            return;

        var (clipX, clipY, clipWidth, clipHeight) = bounds.Value;

        lock (_bufferLock)
        {
            for (int row = clipY; row < clipY + clipHeight; row++)
            {
                for (int col = clipX; col < clipX + clipWidth; col++)
                {
                    _buffer[row, col] = new ConsoleCell(character, style);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void DrawBorder(int x, int y, int width, int height, ConsoleBorderStyle borderStyle = ConsoleBorderStyle.Single, ConsoleTextStyle style = default)
    {
        if (width < 2 || height < 2 || borderStyle == ConsoleBorderStyle.None)
            return;

        var characters = GetBorderCharacters(borderStyle);

        // Draw corners
        WriteCharAt(x, y, characters.TopLeft, style);
        WriteCharAt(x + width - 1, y, characters.TopRight, style);
        WriteCharAt(x, y + height - 1, characters.BottomLeft, style);
        WriteCharAt(x + width - 1, y + height - 1, characters.BottomRight, style);

        // Draw horizontal lines
        for (int col = x + 1; col < x + width - 1; col++)
        {
            WriteCharAt(col, y, characters.Horizontal, style);
            WriteCharAt(col, y + height - 1, characters.Horizontal, style);
        }

        // Draw vertical lines
        for (int row = y + 1; row < y + height - 1; row++)
        {
            WriteCharAt(x, row, characters.Vertical, style);
            WriteCharAt(x + width - 1, row, characters.Vertical, style);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_bufferLock)
        {
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    _buffer[row, col] = new ConsoleCell(' ', ConsoleTextStyle.Default);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void ClearArea(int x, int y, int width, int height)
    {
        FillArea(x, y, width, height, ' ', ConsoleTextStyle.Default);
    }

    /// <inheritdoc/>
    public char GetCharAt(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return ' ';

        lock (_bufferLock)
        {
            return _buffer[y, x].Character;
        }
    }

    /// <inheritdoc/>
    public ConsoleTextStyle GetStyleAt(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return ConsoleTextStyle.Default;

        lock (_bufferLock)
        {
            return _buffer[y, x].Style;
        }
    }

    /// <inheritdoc/>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            lock (_bufferLock)
            {
                // Move cursor to top-left
                System.Console.SetCursorPosition(0, 0);

                var previousStyle = ConsoleTextStyle.Default;
                var output = new System.Text.StringBuilder();

                for (int row = 0; row < Height; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        var cell = _buffer[row, col];

                        // Apply style changes if needed
                        if (!cell.Style.Equals(previousStyle))
                        {
                            output.Append(GetAnsiStyleCodes(cell.Style, previousStyle));
                            previousStyle = cell.Style;
                        }

                        output.Append(cell.Character);
                    }

                    // Move to next line if not the last row
                    if (row < Height - 1)
                    {
                        output.AppendLine();
                    }
                }

                // Reset to default style at the end
                if (!previousStyle.Equals(ConsoleTextStyle.Default))
                {
                    output.Append("\u001b[0m"); // Reset all formatting
                }

                System.Console.Write(output.ToString());
                System.Console.SetCursorPosition(0, Height);
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public void Resize(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);

        // Note: This creates a new buffer, losing existing content.
        // For a production implementation, you'd want to preserve content where possible.
        lock (_bufferLock)
        {
            // Create new buffer and copy what we can from the old one
            var newBuffer = new ConsoleCell[Height, Width];
            
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (row < _buffer.GetLength(0) && col < _buffer.GetLength(1))
                    {
                        newBuffer[row, col] = _buffer[row, col];
                    }
                    else
                    {
                        newBuffer[row, col] = new ConsoleCell(' ', ConsoleTextStyle.Default);
                    }
                }
            }

            // Replace the buffer reference
            Array.Clear(_buffer, 0, _buffer.Length);
            Array.Copy(newBuffer, _buffer, Math.Min(_buffer.Length, newBuffer.Length));
        }
    }

    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    private (int x, int y, int width, int height)? ClipBounds(int x, int y, int width, int height)
    {
        if (x >= Width || y >= Height || x + width <= 0 || y + height <= 0)
            return null;

        var clipX = Math.Max(0, x);
        var clipY = Math.Max(0, y);
        var clipWidth = Math.Min(Width - clipX, x + width - clipX);
        var clipHeight = Math.Min(Height - clipY, y + height - clipY);

        return (clipX, clipY, clipWidth, clipHeight);
    }

    private static BorderCharacters GetBorderCharacters(ConsoleBorderStyle style)
    {
        return style switch
        {
            ConsoleBorderStyle.Single => new BorderCharacters('┌', '┐', '└', '┘', '─', '│'),
            ConsoleBorderStyle.Double => new BorderCharacters('╔', '╗', '╚', '╝', '═', '║'),
            ConsoleBorderStyle.Rounded => new BorderCharacters('╭', '╮', '╰', '╯', '─', '│'),
            ConsoleBorderStyle.Thick => new BorderCharacters('┏', '┓', '┗', '┛', '━', '┃'),
            _ => new BorderCharacters('+', '+', '+', '+', '-', '|')
        };
    }

    private static string GetAnsiStyleCodes(ConsoleTextStyle newStyle, ConsoleTextStyle previousStyle)
    {
        var codes = new List<string>();

        // Reset if needed
        if (newStyle.ForegroundColor != previousStyle.ForegroundColor ||
            newStyle.BackgroundColor != previousStyle.BackgroundColor ||
            (!newStyle.IsBold && previousStyle.IsBold) ||
            (!newStyle.IsUnderlined && previousStyle.IsUnderlined) ||
            (!newStyle.IsItalic && previousStyle.IsItalic) ||
            (!newStyle.IsBlinking && previousStyle.IsBlinking))
        {
            codes.Add("0"); // Reset all
        }

        // Apply formatting
        if (newStyle.IsBold) codes.Add("1");
        if (newStyle.IsItalic) codes.Add("3");
        if (newStyle.IsUnderlined) codes.Add("4");
        if (newStyle.IsBlinking) codes.Add("5");

        // Apply foreground color
        if (newStyle.ForegroundColor != ConsoleColorType.Default)
        {
            codes.Add($"3{GetColorCode(newStyle.ForegroundColor)}");
        }

        // Apply background color
        if (newStyle.BackgroundColor != ConsoleColorType.Default)
        {
            codes.Add($"4{GetColorCode(newStyle.BackgroundColor)}");
        }

        return codes.Count > 0 ? $"\u001b[{string.Join(";", codes)}m" : "";
    }

    private static int GetColorCode(ConsoleColorType color)
    {
        return color switch
        {
            ConsoleColorType.Black => 0,
            ConsoleColorType.DarkRed => 1,
            ConsoleColorType.DarkGreen => 2,
            ConsoleColorType.DarkYellow => 3,
            ConsoleColorType.DarkBlue => 4,
            ConsoleColorType.DarkMagenta => 5,
            ConsoleColorType.DarkCyan => 6,
            ConsoleColorType.Gray => 7,
            ConsoleColorType.DarkGray => 0, // Bright black
            ConsoleColorType.Red => 1, // With bright flag
            ConsoleColorType.Green => 2, // With bright flag
            ConsoleColorType.Yellow => 3, // With bright flag
            ConsoleColorType.Blue => 4, // With bright flag
            ConsoleColorType.Magenta => 5, // With bright flag
            ConsoleColorType.Cyan => 6, // With bright flag
            ConsoleColorType.White => 7, // With bright flag
            _ => 7 // Default to white
        };
    }

    /// <summary>
    /// Represents a single cell in the console buffer.
    /// </summary>
    private readonly struct ConsoleCell
    {
        public char Character { get; }
        public ConsoleTextStyle Style { get; }

        public ConsoleCell(char character, ConsoleTextStyle style)
        {
            Character = character;
            Style = style;
        }
    }

    /// <summary>
    /// Border characters for different border styles.
    /// </summary>
    private readonly struct BorderCharacters
    {
        public char TopLeft { get; }
        public char TopRight { get; }
        public char BottomLeft { get; }
        public char BottomRight { get; }
        public char Horizontal { get; }
        public char Vertical { get; }

        public BorderCharacters(char topLeft, char topRight, char bottomLeft, char bottomRight, char horizontal, char vertical)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
            Horizontal = horizontal;
            Vertical = vertical;
        }
    }
}
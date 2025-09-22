using System.Text;

namespace GameConsole.UI.Console;

/// <summary>
/// Implementation of console UI framework with ANSI escape code support.
/// </summary>
public class ConsoleUIFramework : IConsoleUIFramework
{
    private const string ANSI_CLEAR_SCREEN = "\x1b[2J\x1b[H";
    private const string ANSI_RESET = "\x1b[0m";
    private const string ANSI_HIDE_CURSOR = "\x1b[?25l";
    private const string ANSI_SHOW_CURSOR = "\x1b[?25h";
    
    private bool _cursorVisible = true;
    
    public int Width => System.Console.WindowWidth;
    public int Height => System.Console.WindowHeight;
    
    public bool SupportsColor => !System.Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") == null;
    public bool SupportsUnicode => System.Console.OutputEncoding.EncodingName.Contains("UTF");
    
    public bool CursorVisible
    {
        get => _cursorVisible;
        set
        {
            if (_cursorVisible != value)
            {
                _cursorVisible = value;
                System.Console.Write(value ? ANSI_SHOW_CURSOR : ANSI_HIDE_CURSOR);
            }
        }
    }
    
    public void WriteAt(int x, int y, string text, ConsoleColor? foreground = null, ConsoleColor? background = null, TextStyle style = TextStyle.None)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return;
            
        SetCursor(x, y);
        var formattedText = FormatText(text, foreground, background, style);
        System.Console.Write(formattedText);
    }
    
    public void Clear()
    {
        if (SupportsColor)
        {
            System.Console.Write(ANSI_CLEAR_SCREEN);
        }
        else
        {
            System.Console.Clear();
        }
    }
    
    public void ClearArea(int x, int y, int width, int height)
    {
        var spaces = new string(' ', Math.Min(width, Width - x));
        for (int row = y; row < y + height && row < Height; row++)
        {
            if (x < Width)
            {
                WriteAt(x, row, spaces);
            }
        }
    }
    
    public void SetCursor(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < Width && y < Height)
        {
            System.Console.SetCursorPosition(x, y);
        }
    }
    
    public void DrawBox(int x, int y, int width, int height, BoxStyle style = BoxStyle.Single, ConsoleColor? foreground = null, ConsoleColor? background = null)
    {
        if (width < 2 || height < 2) return;
        
        var chars = GetBoxChars(style);
        
        // Draw corners and edges
        WriteAt(x, y, chars.TopLeft.ToString(), foreground, background);
        WriteAt(x + width - 1, y, chars.TopRight.ToString(), foreground, background);
        WriteAt(x, y + height - 1, chars.BottomLeft.ToString(), foreground, background);
        WriteAt(x + width - 1, y + height - 1, chars.BottomRight.ToString(), foreground, background);
        
        // Draw horizontal lines
        var horizontalLine = new string(chars.Horizontal, width - 2);
        WriteAt(x + 1, y, horizontalLine, foreground, background);
        WriteAt(x + 1, y + height - 1, horizontalLine, foreground, background);
        
        // Draw vertical lines
        for (int row = y + 1; row < y + height - 1; row++)
        {
            WriteAt(x, row, chars.Vertical.ToString(), foreground, background);
            WriteAt(x + width - 1, row, chars.Vertical.ToString(), foreground, background);
        }
    }
    
    public string FormatText(string text, ConsoleColor? foreground = null, ConsoleColor? background = null, TextStyle style = TextStyle.None)
    {
        if (!SupportsColor && foreground == null && background == null && style == TextStyle.None)
            return text;
            
        var sb = new StringBuilder();
        
        // Apply text styles
        if (style.HasFlag(TextStyle.Bold))
            sb.Append("\x1b[1m");
        if (style.HasFlag(TextStyle.Dim))
            sb.Append("\x1b[2m");
        if (style.HasFlag(TextStyle.Italic))
            sb.Append("\x1b[3m");
        if (style.HasFlag(TextStyle.Underline))
            sb.Append("\x1b[4m");
        if (style.HasFlag(TextStyle.Strikethrough))
            sb.Append("\x1b[9m");
        
        // Apply colors
        if (foreground.HasValue)
            sb.Append(GetAnsiColorCode(foreground.Value, false));
        if (background.HasValue)
            sb.Append(GetAnsiColorCode(background.Value, true));
            
        sb.Append(text);
        
        // Reset formatting
        if (SupportsColor && (foreground.HasValue || background.HasValue || style != TextStyle.None))
            sb.Append(ANSI_RESET);
            
        return sb.ToString();
    }
    
    private static BoxChars GetBoxChars(BoxStyle style)
    {
        return style switch
        {
            BoxStyle.Single => new BoxChars('┌', '┐', '└', '┘', '─', '│'),
            BoxStyle.Double => new BoxChars('╔', '╗', '╚', '╝', '═', '║'),
            BoxStyle.Thick => new BoxChars('┏', '┓', '┗', '┛', '━', '┃'),
            BoxStyle.Rounded => new BoxChars('╭', '╮', '╰', '╯', '─', '│'),
            _ => new BoxChars('+', '+', '+', '+', '-', '|')
        };
    }
    
    private static string GetAnsiColorCode(ConsoleColor color, bool background)
    {
        int colorCode = color switch
        {
            ConsoleColor.Black => 0,
            ConsoleColor.DarkRed => 1,
            ConsoleColor.DarkGreen => 2,
            ConsoleColor.DarkYellow => 3,
            ConsoleColor.DarkBlue => 4,
            ConsoleColor.DarkMagenta => 5,
            ConsoleColor.DarkCyan => 6,
            ConsoleColor.Gray => 7,
            ConsoleColor.DarkGray => 8,
            ConsoleColor.Red => 9,
            ConsoleColor.Green => 10,
            ConsoleColor.Yellow => 11,
            ConsoleColor.Blue => 12,
            ConsoleColor.Magenta => 13,
            ConsoleColor.Cyan => 14,
            ConsoleColor.White => 15,
            _ => 7
        };
        
        int baseCode = background ? 40 : 30;
        return colorCode < 8 ? $"\x1b[{baseCode + colorCode}m" : $"\x1b[{baseCode + 60 + (colorCode - 8)}m";
    }
    
    private readonly record struct BoxChars(char TopLeft, char TopRight, char BottomLeft, char BottomRight, char Horizontal, char Vertical);
}
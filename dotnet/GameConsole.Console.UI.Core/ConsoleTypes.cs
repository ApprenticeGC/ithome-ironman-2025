namespace GameConsole.Console.UI.Core;

/// <summary>
/// Core types for Console UI positioning and sizing.
/// </summary>

/// <summary>
/// Represents a position in 2D console coordinates.
/// </summary>
public record struct ConsolePosition(int X, int Y)
{
    public static ConsolePosition Zero => new(0, 0);
    
    public static ConsolePosition operator +(ConsolePosition a, ConsolePosition b) =>
        new(a.X + b.X, a.Y + b.Y);
        
    public static ConsolePosition operator -(ConsolePosition a, ConsolePosition b) =>
        new(a.X - b.X, a.Y - b.Y);
}

/// <summary>
/// Represents dimensions in 2D console coordinates.
/// </summary>
public record struct ConsoleSize(int Width, int Height)
{
    public static ConsoleSize Zero => new(0, 0);
    
    public bool IsEmpty => Width <= 0 || Height <= 0;
    public int Area => Width * Height;
}

/// <summary>
/// Represents a rectangular region in console coordinates.
/// </summary>
public record struct ConsoleBounds(ConsolePosition Position, ConsoleSize Size)
{
    public int Left => Position.X;
    public int Top => Position.Y;
    public int Right => Position.X + Size.Width - 1;
    public int Bottom => Position.Y + Size.Height - 1;
    public int Width => Size.Width;
    public int Height => Size.Height;
    
    public static ConsoleBounds Empty => new(ConsolePosition.Zero, ConsoleSize.Zero);
    
    public bool Contains(ConsolePosition point) =>
        point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;
        
    public bool Intersects(ConsoleBounds other) =>
        !(other.Left > Right || other.Right < Left || other.Top > Bottom || other.Bottom < Top);
}

/// <summary>
/// Console color definitions for UI elements.
/// </summary>
public enum ConsoleColor
{
    Black = 0,
    DarkBlue = 1,
    DarkGreen = 2,
    DarkCyan = 3,
    DarkRed = 4,
    DarkMagenta = 5,
    DarkYellow = 6,
    Gray = 7,
    DarkGray = 8,
    Blue = 9,
    Green = 10,
    Cyan = 11,
    Red = 12,
    Magenta = 13,
    Yellow = 14,
    White = 15
}

/// <summary>
/// Represents console text styling.
/// </summary>
public record struct ConsoleStyle(
    ConsoleColor Foreground = ConsoleColor.White,
    ConsoleColor Background = ConsoleColor.Black,
    bool Bold = false,
    bool Underline = false,
    bool Reversed = false)
{
    public static ConsoleStyle Default => new();
}

/// <summary>
/// Represents a character with styling for console rendering.
/// </summary>
public record struct StyledChar(char Character, ConsoleStyle Style)
{
    public static StyledChar Empty => new(' ', ConsoleStyle.Default);
}
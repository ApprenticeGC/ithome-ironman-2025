namespace GameConsole.UI.Core;

/// <summary>
/// Common UI types used across the console UI system.
/// </summary>

/// <summary>
/// Represents a 2D position in console coordinates.
/// </summary>
public record struct Position(int X, int Y)
{
    public static readonly Position Zero = new(0, 0);
    
    public static Position operator +(Position a, Position b) => new(a.X + b.X, a.Y + b.Y);
    public static Position operator -(Position a, Position b) => new(a.X - b.X, a.Y - b.Y);
}

/// <summary>
/// Represents 2D dimensions for UI elements.
/// </summary>
public record struct Size(int Width, int Height)
{
    public static readonly Size Zero = new(0, 0);
    
    public bool IsEmpty => Width <= 0 || Height <= 0;
}

/// <summary>
/// Represents a rectangular bounds for UI layout.
/// </summary>
public record struct UIBounds(Position Position, Size Size)
{
    public int Left => Position.X;
    public int Top => Position.Y;
    public int Right => Position.X + Size.Width - 1;
    public int Bottom => Position.Y + Size.Height - 1;
    
    public bool Contains(Position point) =>
        point.X >= Left && point.X <= Right &&
        point.Y >= Top && point.Y <= Bottom;
}

/// <summary>
/// Console color representation with foreground and background.
/// </summary>
public record struct ConsoleColor(
    System.ConsoleColor Foreground = System.ConsoleColor.White,
    System.ConsoleColor Background = System.ConsoleColor.Black);

/// <summary>
/// Text alignment options for UI elements.
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Vertical alignment options for UI elements.
/// </summary>
public enum VerticalAlignment
{
    Top,
    Center,
    Bottom
}

/// <summary>
/// UI component visibility state.
/// </summary>
public enum Visibility
{
    Visible,
    Hidden,
    Collapsed
}
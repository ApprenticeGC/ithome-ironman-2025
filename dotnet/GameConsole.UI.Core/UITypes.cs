namespace GameConsole.UI.Core;

/// <summary>
/// Common UI types used across the console UI system.
/// </summary>

/// <summary>
/// Represents a position in console coordinates.
/// </summary>
public record struct Position(int X, int Y);

/// <summary>
/// Represents size dimensions.
/// </summary>
public record struct Size(int Width, int Height);

/// <summary>
/// Represents a rectangular area with position and size.
/// </summary>
public record struct Rectangle(Position Position, Size Size)
{
    public int X => Position.X;
    public int Y => Position.Y;
    public int Width => Size.Width;
    public int Height => Size.Height;
    public int Right => X + Width;
    public int Bottom => Y + Height;
}

/// <summary>
/// Represents a text block component for displaying text.
/// </summary>
public record struct TextBlock(
    string Id,
    string Text,
    Rectangle Bounds,
    ConsoleColor ForegroundColor = ConsoleColor.White,
    ConsoleColor BackgroundColor = ConsoleColor.Black,
    TextAlignment Alignment = TextAlignment.Left,
    bool IsVisible = true);

/// <summary>
/// Represents a text input component for user text entry.
/// </summary>
public record struct TextInput(
    string Id,
    string Text,
    Rectangle Bounds,
    string Placeholder = "",
    int MaxLength = 0,
    ConsoleColor ForegroundColor = ConsoleColor.White,
    ConsoleColor BackgroundColor = ConsoleColor.Black,
    bool IsEnabled = true,
    bool IsVisible = true,
    bool IsFocused = false);

/// <summary>
/// Represents a button component for user interaction.
/// </summary>
public record struct Button(
    string Id,
    string Text,
    Rectangle Bounds,
    ConsoleColor ForegroundColor = ConsoleColor.White,
    ConsoleColor BackgroundColor = ConsoleColor.DarkBlue,
    ConsoleColor HoverColor = ConsoleColor.Blue,
    bool IsEnabled = true,
    bool IsVisible = true,
    bool IsPressed = false,
    bool IsHovered = false);

/// <summary>
/// Represents a panel component for grouping other components.
/// </summary>
public record struct Panel(
    string Id,
    Rectangle Bounds,
    string Title = "",
    ConsoleColor BorderColor = ConsoleColor.Gray,
    ConsoleColor BackgroundColor = ConsoleColor.Black,
    BorderStyle BorderStyle = BorderStyle.Single,
    bool IsVisible = true);

/// <summary>
/// Represents a menu component for navigation options.
/// </summary>
public record struct Menu(
    string Id,
    Rectangle Bounds,
    IReadOnlyList<MenuItem> Items,
    int SelectedIndex = 0,
    ConsoleColor ForegroundColor = ConsoleColor.White,
    ConsoleColor BackgroundColor = ConsoleColor.Black,
    ConsoleColor SelectedColor = ConsoleColor.Yellow,
    bool IsVisible = true);

/// <summary>
/// Represents a menu item within a menu.
/// </summary>
public record struct MenuItem(
    string Id,
    string Text,
    string Description = "",
    bool IsEnabled = true,
    bool IsSeparator = false);

/// <summary>
/// Text alignment options for text components.
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Border styles for panel components.
/// </summary>
public enum BorderStyle
{
    None,
    Single,
    Double,
    Rounded
}

/// <summary>
/// Console colors for UI components.
/// </summary>
public enum ConsoleColor
{
    Black,
    DarkBlue,
    DarkGreen,
    DarkCyan,
    DarkRed,
    DarkMagenta,
    DarkYellow,
    Gray,
    DarkGray,
    Blue,
    Green,
    Cyan,
    Red,
    Magenta,
    Yellow,
    White
}
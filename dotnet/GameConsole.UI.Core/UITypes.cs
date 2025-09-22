namespace GameConsole.UI.Core;

/// <summary>
/// Represents a position in the UI coordinate system.
/// </summary>
public class Position : IEquatable<Position>
{
    /// <summary>
    /// Initializes a new instance of the Position class.
    /// </summary>
    /// <param name="x">The X coordinate (column).</param>
    /// <param name="y">The Y coordinate (row).</param>
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Gets the X coordinate (column).
    /// </summary>
    public int X { get; }
    
    /// <summary>
    /// Gets the Y coordinate (row).
    /// </summary>
    public int Y { get; }
    
    /// <summary>
    /// Gets the origin position (0, 0).
    /// </summary>
    public static Position Origin => new Position(0, 0);
    
    /// <summary>
    /// Adds an offset to the current position.
    /// </summary>
    /// <param name="offset">The offset to add.</param>
    /// <returns>A new position with the offset applied.</returns>
    public Position Add(Position offset) => new Position(X + offset.X, Y + offset.Y);

    /// <inheritdoc />
    public bool Equals(Position? other) => 
        other != null && X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override bool Equals(object? obj) => 
        obj is Position position && Equals(position);

    /// <inheritdoc />
    public override int GetHashCode() => (X << 16) | Y;
}

/// <summary>
/// Represents a size with width and height dimensions.
/// </summary>
public class Size : IEquatable<Size>
{
    /// <summary>
    /// Initializes a new instance of the Size class.
    /// </summary>
    /// <param name="width">The width dimension.</param>
    /// <param name="height">The height dimension.</param>
    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets the width dimension.
    /// </summary>
    public int Width { get; }
    
    /// <summary>
    /// Gets the height dimension.
    /// </summary>
    public int Height { get; }
    
    /// <summary>
    /// Gets an empty size (0, 0).
    /// </summary>
    public static Size Empty => new Size(0, 0);
    
    /// <summary>
    /// Gets the total area (width * height).
    /// </summary>
    public int Area => Width * Height;

    /// <inheritdoc />
    public bool Equals(Size? other) => 
        other != null && Width == other.Width && Height == other.Height;

    /// <inheritdoc />
    public override bool Equals(object? obj) => 
        obj is Size size && Equals(size);

    /// <inheritdoc />
    public override int GetHashCode() => (Width << 16) | Height;
}

/// <summary>
/// Represents a rectangular area with position and size.
/// </summary>
public class Rectangle : IEquatable<Rectangle>
{
    /// <summary>
    /// Initializes a new instance of the Rectangle class.
    /// </summary>
    /// <param name="position">The top-left position of the rectangle.</param>
    /// <param name="size">The size of the rectangle.</param>
    public Rectangle(Position position, Size size)
    {
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Size = size ?? throw new ArgumentNullException(nameof(size));
    }

    /// <summary>
    /// Gets the top-left position of the rectangle.
    /// </summary>
    public Position Position { get; }
    
    /// <summary>
    /// Gets the size of the rectangle.
    /// </summary>
    public Size Size { get; }
    
    /// <summary>
    /// Gets the left edge X coordinate.
    /// </summary>
    public int Left => Position.X;
    
    /// <summary>
    /// Gets the top edge Y coordinate.
    /// </summary>
    public int Top => Position.Y;
    
    /// <summary>
    /// Gets the right edge X coordinate.
    /// </summary>
    public int Right => Position.X + Size.Width - 1;
    
    /// <summary>
    /// Gets the bottom edge Y coordinate.
    /// </summary>
    public int Bottom => Position.Y + Size.Height - 1;
    
    /// <summary>
    /// Checks if the rectangle contains the specified position.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>True if the position is within the rectangle bounds.</returns>
    public bool Contains(Position position) => 
        position.X >= Left && position.X <= Right && 
        position.Y >= Top && position.Y <= Bottom;

    /// <inheritdoc />
    public bool Equals(Rectangle? other) => 
        other != null && Position.Equals(other.Position) && Size.Equals(other.Size);

    /// <inheritdoc />
    public override bool Equals(object? obj) => 
        obj is Rectangle rectangle && Equals(rectangle);

    /// <inheritdoc />
    public override int GetHashCode() => Position.GetHashCode() ^ Size.GetHashCode();
}

/// <summary>
/// Represents text styling information.
/// </summary>
public class TextStyle : IEquatable<TextStyle>
{
    /// <summary>
    /// Initializes a new instance of the TextStyle class.
    /// </summary>
    /// <param name="foregroundColor">Optional foreground color.</param>
    /// <param name="backgroundColor">Optional background color.</param>
    /// <param name="attributes">Text attributes (bold, underline, etc.).</param>
    public TextStyle(ConsoleColor? foregroundColor = null, ConsoleColor? backgroundColor = null, 
        TextAttributes attributes = TextAttributes.None)
    {
        ForegroundColor = foregroundColor;
        BackgroundColor = backgroundColor;
        Attributes = attributes;
    }

    /// <summary>
    /// Gets the optional foreground color.
    /// </summary>
    public ConsoleColor? ForegroundColor { get; }
    
    /// <summary>
    /// Gets the optional background color.
    /// </summary>
    public ConsoleColor? BackgroundColor { get; }
    
    /// <summary>
    /// Gets the text attributes (bold, underline, etc.).
    /// </summary>
    public TextAttributes Attributes { get; }

    /// <inheritdoc />
    public bool Equals(TextStyle? other) => 
        other != null && ForegroundColor == other.ForegroundColor && 
        BackgroundColor == other.BackgroundColor && Attributes == other.Attributes;

    /// <inheritdoc />
    public override bool Equals(object? obj) => 
        obj is TextStyle textStyle && Equals(textStyle);

    /// <inheritdoc />
    public override int GetHashCode() => 
        (ForegroundColor?.GetHashCode() ?? 0) ^ 
        (BackgroundColor?.GetHashCode() ?? 0) ^ 
        Attributes.GetHashCode();
}

/// <summary>
/// Text attribute flags for styling.
/// </summary>
[Flags]
public enum TextAttributes
{
    /// <summary>
    /// No attributes applied.
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Bold text.
    /// </summary>
    Bold = 1,
    
    /// <summary>
    /// Underlined text.
    /// </summary>
    Underline = 2,
    
    /// <summary>
    /// Reversed colors (foreground and background swapped).
    /// </summary>
    Reverse = 4,
    
    /// <summary>
    /// Blinking text (may not be supported on all terminals).
    /// </summary>
    Blink = 8
}
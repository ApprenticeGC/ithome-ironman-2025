namespace GameConsole.UI.Console;

/// <summary>
/// Represents a point in the console coordinate system.
/// </summary>
/// <param name="X">X coordinate (column).</param>
/// <param name="Y">Y coordinate (row).</param>
public readonly record struct ConsolePoint(int X, int Y)
{
    /// <summary>
    /// Gets the origin point (0, 0).
    /// </summary>
    public static readonly ConsolePoint Origin = new(0, 0);
    
    /// <summary>
    /// Adds two console points.
    /// </summary>
    public static ConsolePoint operator +(ConsolePoint left, ConsolePoint right)
        => new(left.X + right.X, left.Y + right.Y);
    
    /// <summary>
    /// Subtracts two console points.
    /// </summary>
    public static ConsolePoint operator -(ConsolePoint left, ConsolePoint right)
        => new(left.X - right.X, left.Y - right.Y);
}

/// <summary>
/// Represents a size in the console coordinate system.
/// </summary>
/// <param name="Width">Width in characters.</param>
/// <param name="Height">Height in rows.</param>
public readonly record struct ConsoleSize(int Width, int Height)
{
    /// <summary>
    /// Gets an empty size (0, 0).
    /// </summary>
    public static readonly ConsoleSize Empty = new(0, 0);
    
    /// <summary>
    /// Gets whether this size is empty.
    /// </summary>
    public bool IsEmpty => Width == 0 && Height == 0;
}

/// <summary>
/// Represents a rectangle in the console coordinate system.
/// </summary>
/// <param name="X">X coordinate of the left edge.</param>
/// <param name="Y">Y coordinate of the top edge.</param>
/// <param name="Width">Width in characters.</param>
/// <param name="Height">Height in rows.</param>
public readonly record struct ConsoleRect(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Initializes a new rectangle with position and size.
    /// </summary>
    /// <param name="position">Position of the rectangle.</param>
    /// <param name="size">Size of the rectangle.</param>
    public ConsoleRect(ConsolePoint position, ConsoleSize size) 
        : this(position.X, position.Y, size.Width, size.Height)
    {
    }
    
    /// <summary>
    /// Gets an empty rectangle.
    /// </summary>
    public static readonly ConsoleRect Empty = new(0, 0, 0, 0);
    
    /// <summary>
    /// Gets the position of the rectangle.
    /// </summary>
    public ConsolePoint Position => new(X, Y);
    
    /// <summary>
    /// Gets the size of the rectangle.
    /// </summary>
    public ConsoleSize Size => new(Width, Height);
    
    /// <summary>
    /// Gets the left edge X coordinate.
    /// </summary>
    public int Left => X;
    
    /// <summary>
    /// Gets the top edge Y coordinate.
    /// </summary>
    public int Top => Y;
    
    /// <summary>
    /// Gets the right edge X coordinate (exclusive).
    /// </summary>
    public int Right => X + Width;
    
    /// <summary>
    /// Gets the bottom edge Y coordinate (exclusive).
    /// </summary>
    public int Bottom => Y + Height;
    
    /// <summary>
    /// Gets the center point of the rectangle.
    /// </summary>
    public ConsolePoint Center => new(X + Width / 2, Y + Height / 2);
    
    /// <summary>
    /// Gets whether this rectangle is empty.
    /// </summary>
    public bool IsEmpty => Width == 0 || Height == 0;
    
    /// <summary>
    /// Determines whether the specified point is contained within this rectangle.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is contained within this rectangle; otherwise, false.</returns>
    public bool Contains(ConsolePoint point)
        => point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;
    
    /// <summary>
    /// Determines whether the specified rectangle is entirely contained within this rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to test.</param>
    /// <returns>True if the rectangle is contained within this rectangle; otherwise, false.</returns>
    public bool Contains(ConsoleRect rect)
        => rect.X >= X && rect.Right <= Right && rect.Y >= Y && rect.Bottom <= Bottom;
    
    /// <summary>
    /// Determines whether this rectangle intersects with another rectangle.
    /// </summary>
    /// <param name="rect">The rectangle to test.</param>
    /// <returns>True if the rectangles intersect; otherwise, false.</returns>
    public bool IntersectsWith(ConsoleRect rect)
        => rect.X < Right && X < rect.Right && rect.Y < Bottom && Y < rect.Bottom;
    
    /// <summary>
    /// Returns the intersection of two rectangles.
    /// </summary>
    /// <param name="rect">The rectangle to intersect with.</param>
    /// <returns>The intersection rectangle, or Empty if no intersection.</returns>
    public ConsoleRect Intersect(ConsoleRect rect)
    {
        var x = Math.Max(X, rect.X);
        var y = Math.Max(Y, rect.Y);
        var width = Math.Max(0, Math.Min(Right, rect.Right) - x);
        var height = Math.Max(0, Math.Min(Bottom, rect.Bottom) - y);
        return new ConsoleRect(x, y, width, height);
    }
    
    /// <summary>
    /// Returns a rectangle that represents the union of two rectangles.
    /// </summary>
    /// <param name="rect">The rectangle to union with.</param>
    /// <returns>The union rectangle.</returns>
    public ConsoleRect Union(ConsoleRect rect)
    {
        var x = Math.Min(X, rect.X);
        var y = Math.Min(Y, rect.Y);
        var width = Math.Max(Right, rect.Right) - x;
        var height = Math.Max(Bottom, rect.Bottom) - y;
        return new ConsoleRect(x, y, width, height);
    }
    
    /// <summary>
    /// Returns a rectangle that is inflated by the specified amount.
    /// </summary>
    /// <param name="width">The amount to inflate the width.</param>
    /// <param name="height">The amount to inflate the height.</param>
    /// <returns>The inflated rectangle.</returns>
    public ConsoleRect Inflate(int width, int height)
        => new(X - width, Y - height, Width + 2 * width, Height + 2 * height);
    
    /// <summary>
    /// Returns a rectangle that is offset by the specified amount.
    /// </summary>
    /// <param name="x">The amount to offset horizontally.</param>
    /// <param name="y">The amount to offset vertically.</param>
    /// <returns>The offset rectangle.</returns>
    public ConsoleRect Offset(int x, int y)
        => new(X + x, Y + y, Width, Height);
    
    /// <summary>
    /// Returns a rectangle that is offset by the specified point.
    /// </summary>
    /// <param name="point">The point to offset by.</param>
    /// <returns>The offset rectangle.</returns>
    public ConsoleRect Offset(ConsolePoint point)
        => new(X + point.X, Y + point.Y, Width, Height);
}
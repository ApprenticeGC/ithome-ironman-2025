namespace GameConsole.UI.Core;

/// <summary>
/// Represents a 2D position with X and Y coordinates.
/// </summary>
public readonly struct Position : IEquatable<Position>
{
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    public int X { get; }
    
    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    public int Y { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Position"/> struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    /// <summary>
    /// Gets a position at the origin (0, 0).
    /// </summary>
    public static Position Zero => new(0, 0);
    
    /// <summary>
    /// Adds two positions.
    /// </summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns>The sum of the two positions.</returns>
    public static Position operator +(Position left, Position right) =>
        new(left.X + right.X, left.Y + right.Y);
    
    /// <summary>
    /// Subtracts two positions.
    /// </summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns>The difference of the two positions.</returns>
    public static Position operator -(Position left, Position right) =>
        new(left.X - right.X, left.Y - right.Y);
    
    /// <inheritdoc />
    public bool Equals(Position other) => X == other.X && Y == other.Y;
    
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Position other && Equals(other);
    
    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);
    
    /// <summary>
    /// Determines whether two positions are equal.
    /// </summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns>true if the positions are equal; otherwise, false.</returns>
    public static bool operator ==(Position left, Position right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two positions are not equal.
    /// </summary>
    /// <param name="left">The first position.</param>
    /// <param name="right">The second position.</param>
    /// <returns>true if the positions are not equal; otherwise, false.</returns>
    public static bool operator !=(Position left, Position right) => !left.Equals(right);
    
    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y})";
}

/// <summary>
/// Represents a 2D size with width and height.
/// </summary>
public readonly struct Size : IEquatable<Size>
{
    /// <summary>
    /// Gets the width.
    /// </summary>
    public int Width { get; }
    
    /// <summary>
    /// Gets the height.
    /// </summary>
    public int Height { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Size"/> struct.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }
    
    /// <summary>
    /// Gets an empty size (0, 0).
    /// </summary>
    public static Size Empty => new(0, 0);
    
    /// <inheritdoc />
    public bool Equals(Size other) => Width == other.Width && Height == other.Height;
    
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Size other && Equals(other);
    
    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Width, Height);
    
    /// <summary>
    /// Determines whether two sizes are equal.
    /// </summary>
    /// <param name="left">The first size.</param>
    /// <param name="right">The second size.</param>
    /// <returns>true if the sizes are equal; otherwise, false.</returns>
    public static bool operator ==(Size left, Size right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two sizes are not equal.
    /// </summary>
    /// <param name="left">The first size.</param>
    /// <param name="right">The second size.</param>
    /// <returns>true if the sizes are not equal; otherwise, false.</returns>
    public static bool operator !=(Size left, Size right) => !left.Equals(right);
    
    /// <inheritdoc />
    public override string ToString() => $"{Width}x{Height}";
}

/// <summary>
/// Represents a rectangle with position and size.
/// </summary>
public readonly struct Rectangle : IEquatable<Rectangle>
{
    /// <summary>
    /// Gets the position of the rectangle.
    /// </summary>
    public Position Position { get; }
    
    /// <summary>
    /// Gets the size of the rectangle.
    /// </summary>
    public Size Size { get; }
    
    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    public int X => Position.X;
    
    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    public int Y => Position.Y;
    
    /// <summary>
    /// Gets the width.
    /// </summary>
    public int Width => Size.Width;
    
    /// <summary>
    /// Gets the height.
    /// </summary>
    public int Height => Size.Height;
    
    /// <summary>
    /// Gets the right edge of the rectangle.
    /// </summary>
    public int Right => X + Width;
    
    /// <summary>
    /// Gets the bottom edge of the rectangle.
    /// </summary>
    public int Bottom => Y + Height;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Rectangle"/> struct.
    /// </summary>
    /// <param name="position">The position.</param>
    /// <param name="size">The size.</param>
    public Rectangle(Position position, Size size)
    {
        Position = position;
        Size = size;
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="Rectangle"/> struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Rectangle(int x, int y, int width, int height) : this(new Position(x, y), new Size(width, height))
    {
    }
    
    /// <summary>
    /// Gets an empty rectangle.
    /// </summary>
    public static Rectangle Empty => new(Position.Zero, Size.Empty);
    
    /// <summary>
    /// Determines whether the rectangle contains the specified point.
    /// </summary>
    /// <param name="point">The point to check.</param>
    /// <returns>true if the rectangle contains the point; otherwise, false.</returns>
    public bool Contains(Position point) =>
        point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;
    
    /// <inheritdoc />
    public bool Equals(Rectangle other) => Position.Equals(other.Position) && Size.Equals(other.Size);
    
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Rectangle other && Equals(other);
    
    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Position, Size);
    
    /// <summary>
    /// Determines whether two rectangles are equal.
    /// </summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns>true if the rectangles are equal; otherwise, false.</returns>
    public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);
    
    /// <summary>
    /// Determines whether two rectangles are not equal.
    /// </summary>
    /// <param name="left">The first rectangle.</param>
    /// <param name="right">The second rectangle.</param>
    /// <returns>true if the rectangles are not equal; otherwise, false.</returns>
    public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);
    
    /// <inheritdoc />
    public override string ToString() => $"Rectangle({Position}, {Size})";
}
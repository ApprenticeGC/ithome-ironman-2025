namespace GameConsole.UI.Core;

/// <summary>
/// Represents a 2D point with integer coordinates.
/// </summary>
public readonly struct Point : IEquatable<Point>
{
    public int X { get; }
    public int Y { get; }
    
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    public static readonly Point Empty = new(0, 0);
    
    public bool Equals(Point other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is Point other && Equals(other);
    public override int GetHashCode() => (X * 397) ^ Y;
    public override string ToString() => $"Point({X}, {Y})";
    
    public static bool operator ==(Point left, Point right) => left.Equals(right);
    public static bool operator !=(Point left, Point right) => !left.Equals(right);
    
    public static Point operator +(Point point, Size size) => new(point.X + size.Width, point.Y + size.Height);
    public static Point operator -(Point point, Size size) => new(point.X - size.Width, point.Y - size.Height);
}

/// <summary>
/// Represents a 2D size with integer dimensions.
/// </summary>
public readonly struct Size : IEquatable<Size>
{
    public int Width { get; }
    public int Height { get; }
    
    public Size(int width, int height)
    {
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
    }
    
    public static readonly Size Empty = new(0, 0);
    
    public bool IsEmpty => Width == 0 || Height == 0;
    
    public bool Equals(Size other) => Width == other.Width && Height == other.Height;
    public override bool Equals(object? obj) => obj is Size other && Equals(other);
    public override int GetHashCode() => (Width * 397) ^ Height;
    public override string ToString() => $"Size({Width}, {Height})";
    
    public static bool operator ==(Size left, Size right) => left.Equals(right);
    public static bool operator !=(Size left, Size right) => !left.Equals(right);
}

/// <summary>
/// Represents a rectangle with integer coordinates and dimensions.
/// </summary>
public readonly struct Rectangle : IEquatable<Rectangle>
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
    
    public Rectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
    }
    
    public Rectangle(Point location, Size size)
        : this(location.X, location.Y, size.Width, size.Height) { }
    
    public static readonly Rectangle Empty = new(0, 0, 0, 0);
    
    public Point Location => new(X, Y);
    public Size Size => new(Width, Height);
    public int Left => X;
    public int Top => Y;
    public int Right => X + Width - 1;
    public int Bottom => Y + Height - 1;
    public bool IsEmpty => Width == 0 || Height == 0;
    
    public bool Contains(Point point) => 
        point.X >= X && point.X < X + Width && 
        point.Y >= Y && point.Y < Y + Height;
        
    public bool Contains(Rectangle rectangle) =>
        X <= rectangle.X && 
        Y <= rectangle.Y &&
        X + Width >= rectangle.X + rectangle.Width &&
        Y + Height >= rectangle.Y + rectangle.Height;
    
    public bool Intersects(Rectangle rectangle) =>
        rectangle.X < X + Width &&
        X < rectangle.X + rectangle.Width &&
        rectangle.Y < Y + Height &&
        Y < rectangle.Y + rectangle.Height;
    
    public Rectangle Inflate(int width, int height) =>
        new(X - width, Y - height, Width + 2 * width, Height + 2 * height);
        
    public Rectangle Offset(int x, int y) =>
        new(X + x, Y + y, Width, Height);
    
    public bool Equals(Rectangle other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
    public override bool Equals(object? obj) => obj is Rectangle other && Equals(other);
    public override int GetHashCode() => ((X * 397) ^ Y) * 397 ^ (Width * 397) ^ Height;
    public override string ToString() => $"Rectangle({X}, {Y}, {Width}, {Height})";
    
    public static bool operator ==(Rectangle left, Rectangle right) => left.Equals(right);
    public static bool operator !=(Rectangle left, Rectangle right) => !left.Equals(right);
}
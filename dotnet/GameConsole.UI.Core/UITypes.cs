namespace GameConsole.UI.Core;

/// <summary>
/// Represents a rectangular bounds in UI coordinates.
/// </summary>
public struct UIRect
{
    /// <summary>
    /// X coordinate of the left edge.
    /// </summary>
    public int X { get; set; }
    
    /// <summary>
    /// Y coordinate of the top edge.
    /// </summary>
    public int Y { get; set; }
    
    /// <summary>
    /// Width of the rectangle.
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Height of the rectangle.
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Initializes a new UIRect with the specified coordinates and size.
    /// </summary>
    public UIRect(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
    
    /// <summary>
    /// Gets the right edge coordinate.
    /// </summary>
    public int Right => X + Width;
    
    /// <summary>
    /// Gets the bottom edge coordinate.
    /// </summary>
    public int Bottom => Y + Height;
    
    /// <summary>
    /// Gets the center point of the rectangle.
    /// </summary>
    public UIPoint Center => new(X + Width / 2, Y + Height / 2);
    
    /// <summary>
    /// Checks if a point is contained within this rectangle.
    /// </summary>
    public bool Contains(UIPoint point) => 
        point.X >= X && point.X < Right && point.Y >= Y && point.Y < Bottom;
    
    /// <summary>
    /// Checks if another rectangle intersects with this one.
    /// </summary>
    public bool Intersects(UIRect other) => 
        !(other.X >= Right || other.Right <= X || other.Y >= Bottom || other.Bottom <= Y);
}

/// <summary>
/// Represents a point in UI coordinates.
/// </summary>
public struct UIPoint
{
    /// <summary>
    /// X coordinate.
    /// </summary>
    public int X { get; set; }
    
    /// <summary>
    /// Y coordinate.
    /// </summary>
    public int Y { get; set; }
    
    /// <summary>
    /// Initializes a new UIPoint with the specified coordinates.
    /// </summary>
    public UIPoint(int x, int y)
    {
        X = x;
        Y = y;
    }
    
    /// <summary>
    /// Zero point (0, 0).
    /// </summary>
    public static UIPoint Zero => new(0, 0);
}

/// <summary>
/// Represents a size in UI coordinates.
/// </summary>
public struct UISize
{
    /// <summary>
    /// Width dimension.
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Height dimension.
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Initializes a new UISize with the specified dimensions.
    /// </summary>
    public UISize(int width, int height)
    {
        Width = width;
        Height = height;
    }
    
    /// <summary>
    /// Zero size (0, 0).
    /// </summary>
    public static UISize Zero => new(0, 0);
    
    /// <summary>
    /// Calculates the area of this size.
    /// </summary>
    public int Area => Width * Height;
}

/// <summary>
/// Represents a color in the UI system.
/// </summary>
public struct UIColor
{
    /// <summary>
    /// Red component (0-255).
    /// </summary>
    public byte R { get; set; }
    
    /// <summary>
    /// Green component (0-255).
    /// </summary>
    public byte G { get; set; }
    
    /// <summary>
    /// Blue component (0-255).
    /// </summary>
    public byte B { get; set; }
    
    /// <summary>
    /// Alpha component (0-255).
    /// </summary>
    public byte A { get; set; }
    
    /// <summary>
    /// Initializes a new UIColor with the specified RGBA components.
    /// </summary>
    public UIColor(byte r, byte g, byte b, byte a = 255)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    /// <summary>
    /// Common colors for console UI.
    /// </summary>
    public static class Colors
    {
        public static UIColor Black => new(0, 0, 0);
        public static UIColor White => new(255, 255, 255);
        public static UIColor Red => new(255, 0, 0);
        public static UIColor Green => new(0, 255, 0);
        public static UIColor Blue => new(0, 0, 255);
        public static UIColor Yellow => new(255, 255, 0);
        public static UIColor Cyan => new(0, 255, 255);
        public static UIColor Magenta => new(255, 0, 255);
        public static UIColor Gray => new(128, 128, 128);
        public static UIColor DarkGray => new(64, 64, 64);
        public static UIColor LightGray => new(192, 192, 192);
    }
}

/// <summary>
/// Text alignment options for UI components.
/// </summary>
public enum UITextAlignment
{
    Left,
    Center,
    Right,
    Justify
}

/// <summary>
/// Layout strategies for arranging UI components.
/// </summary>
public enum LayoutStrategy
{
    None,
    Vertical,
    Horizontal,
    Grid,
    Flow
}
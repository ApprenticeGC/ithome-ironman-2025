namespace GameConsole.UI.Core;

/// <summary>
/// Represents a 2D position in UI coordinate space.
/// </summary>
public struct UIPosition
{
    public int X { get; set; }
    public int Y { get; set; }

    public UIPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static UIPosition Zero => new(0, 0);
}

/// <summary>
/// Represents UI dimensions (width and height).
/// </summary>
public struct UISize
{
    public int Width { get; set; }
    public int Height { get; set; }

    public UISize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public static UISize Empty => new(0, 0);
}

/// <summary>
/// Represents a rectangular area in UI coordinate space.
/// </summary>
public struct UIRect
{
    public UIPosition Position { get; set; }
    public UISize Size { get; set; }

    public UIRect(UIPosition position, UISize size)
    {
        Position = position;
        Size = size;
    }

    public UIRect(int x, int y, int width, int height)
    {
        Position = new UIPosition(x, y);
        Size = new UISize(width, height);
    }

    public int Left => Position.X;
    public int Top => Position.Y;
    public int Right => Position.X + Size.Width;
    public int Bottom => Position.Y + Size.Height;

    public static UIRect Empty => new(0, 0, 0, 0);
}

/// <summary>
/// Console color enumeration for UI theming.
/// </summary>
public enum UIColor
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
/// UI element alignment options.
/// </summary>
public enum UIAlignment
{
    Left,
    Center,
    Right,
    Top,
    Middle,
    Bottom
}

/// <summary>
/// UI element state for visual feedback.
/// </summary>
public enum UIState
{
    Normal,
    Focused,
    Pressed,
    Disabled,
    Hovered
}
namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents a position in the console UI.
/// </summary>
public readonly struct Position
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
    /// Initializes a new instance of the Position struct.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"({X}, {Y})";
}

/// <summary>
/// Represents a size in the console UI.
/// </summary>
public readonly struct Size
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
    /// Initializes a new instance of the Size struct.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public override string ToString() => $"{Width}x{Height}";
}
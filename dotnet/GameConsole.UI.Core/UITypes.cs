using System;
using System.Numerics;

namespace GameConsole.UI.Core;

/// <summary>
/// Common UI types used across the UI system.
/// </summary>

/// <summary>
/// Represents a 2D position in screen space.
/// </summary>
public record struct Position(int X, int Y)
{
    public static Position Zero => new(0, 0);
}

/// <summary>
/// Represents 2D dimensions.
/// </summary>
public record struct Size(int Width, int Height)
{
    public static Size Zero => new(0, 0);
}

/// <summary>
/// Represents a rectangular area in screen space.
/// </summary>
public record struct Rectangle(Position Position, Size Size)
{
    public int X => Position.X;
    public int Y => Position.Y;
    public int Width => Size.Width;
    public int Height => Size.Height;
    
    public int Right => X + Width;
    public int Bottom => Y + Height;
    
    public static Rectangle Empty => new(Position.Zero, Size.Zero);
}

/// <summary>
/// Represents padding around UI elements.
/// </summary>
public record struct Padding(int Left, int Top, int Right, int Bottom)
{
    public static Padding Zero => new(0, 0, 0, 0);
    public static Padding All(int value) => new(value, value, value, value);
}

/// <summary>
/// Represents margin around UI elements.
/// </summary>
public record struct Margin(int Left, int Top, int Right, int Bottom)
{
    public static Margin Zero => new(0, 0, 0, 0);
    public static Margin All(int value) => new(value, value, value, value);
}

/// <summary>
/// UI color representation using RGBA values.
/// </summary>
public record struct UIColor(byte R, byte G, byte B, byte A = 255)
{
    public static UIColor White => new(255, 255, 255);
    public static UIColor Black => new(0, 0, 0);
    public static UIColor Transparent => new(0, 0, 0, 0);
    public static UIColor Red => new(255, 0, 0);
    public static UIColor Green => new(0, 255, 0);
    public static UIColor Blue => new(0, 0, 255);
    
    /// <summary>
    /// Convert to Vector4 for graphics operations.
    /// </summary>
    public Vector4 ToVector4() => new(R / 255f, G / 255f, B / 255f, A / 255f);
}

/// <summary>
/// Text alignment options.
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Vertical alignment options.
/// </summary>
public enum VerticalAlignment
{
    Top,
    Middle,
    Bottom
}

/// <summary>
/// UI component state.
/// </summary>
public enum UIState
{
    Normal,
    Hover,
    Active,
    Disabled
}

/// <summary>
/// Layout anchor modes.
/// </summary>
public enum AnchorMode
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}
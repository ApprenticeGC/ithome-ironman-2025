using System.Drawing;

namespace GameConsole.UI.Console;

/// <summary>
/// Core interface for console UI framework providing text-based rendering capabilities.
/// </summary>
public interface IConsoleUIFramework
{
    /// <summary>
    /// Gets the current console width in characters.
    /// </summary>
    int Width { get; }
    
    /// <summary>
    /// Gets the current console height in characters.
    /// </summary>
    int Height { get; }
    
    /// <summary>
    /// Gets a value indicating whether the console supports colors.
    /// </summary>
    bool SupportsColor { get; }
    
    /// <summary>
    /// Gets a value indicating whether the console supports Unicode characters.
    /// </summary>
    bool SupportsUnicode { get; }
    
    /// <summary>
    /// Writes text to the console at the specified position with formatting.
    /// </summary>
    /// <param name="x">X coordinate (column).</param>
    /// <param name="y">Y coordinate (row).</param>
    /// <param name="text">Text to write.</param>
    /// <param name="foreground">Foreground color.</param>
    /// <param name="background">Background color.</param>
    /// <param name="style">Text style options.</param>
    void WriteAt(int x, int y, string text, ConsoleColor? foreground = null, ConsoleColor? background = null, TextStyle style = TextStyle.None);
    
    /// <summary>
    /// Clears the entire console screen.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Clears a rectangular area of the console.
    /// </summary>
    /// <param name="x">Starting X coordinate.</param>
    /// <param name="y">Starting Y coordinate.</param>
    /// <param name="width">Width of the area to clear.</param>
    /// <param name="height">Height of the area to clear.</param>
    void ClearArea(int x, int y, int width, int height);
    
    /// <summary>
    /// Sets the cursor position.
    /// </summary>
    /// <param name="x">X coordinate (column).</param>
    /// <param name="y">Y coordinate (row).</param>
    void SetCursor(int x, int y);
    
    /// <summary>
    /// Gets or sets cursor visibility.
    /// </summary>
    bool CursorVisible { get; set; }
    
    /// <summary>
    /// Draws a box with the specified dimensions and style.
    /// </summary>
    /// <param name="x">Starting X coordinate.</param>
    /// <param name="y">Starting Y coordinate.</param>
    /// <param name="width">Width of the box.</param>
    /// <param name="height">Height of the box.</param>
    /// <param name="style">Box drawing style.</param>
    /// <param name="foreground">Foreground color.</param>
    /// <param name="background">Background color.</param>
    void DrawBox(int x, int y, int width, int height, BoxStyle style = BoxStyle.Single, ConsoleColor? foreground = null, ConsoleColor? background = null);
    
    /// <summary>
    /// Applies ANSI formatting to text.
    /// </summary>
    /// <param name="text">Text to format.</param>
    /// <param name="foreground">Foreground color.</param>
    /// <param name="background">Background color.</param>
    /// <param name="style">Text style.</param>
    /// <returns>Formatted text with ANSI codes.</returns>
    string FormatText(string text, ConsoleColor? foreground = null, ConsoleColor? background = null, TextStyle style = TextStyle.None);
}

/// <summary>
/// Text style options for console output.
/// </summary>
[Flags]
public enum TextStyle
{
    None = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Strikethrough = 8,
    Dim = 16
}

/// <summary>
/// Box drawing styles for console UI elements.
/// </summary>
public enum BoxStyle
{
    None,
    Single,
    Double,
    Thick,
    Rounded
}
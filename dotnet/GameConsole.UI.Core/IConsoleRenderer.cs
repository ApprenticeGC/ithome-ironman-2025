using System.Reactive;

namespace GameConsole.UI.Core;

/// <summary>
/// Interface for rendering UI components to the console.
/// </summary>
public interface IConsoleRenderer
{
    /// <summary>
    /// Clear the console screen.
    /// </summary>
    Task ClearAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set the cursor position.
    /// </summary>
    Task SetCursorPositionAsync(int left, int top, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Write text at the current cursor position.
    /// </summary>
    Task WriteTextAsync(string text, ConsoleColor? foreground = null, ConsoleColor? background = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Write text at a specific position.
    /// </summary>
    Task WriteTextAtAsync(int left, int top, string text, ConsoleColor? foreground = null, ConsoleColor? background = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Draw a border around a rectangular area.
    /// </summary>
    Task DrawBorderAsync(Rectangle bounds, BorderStyle style = BorderStyle.Single, ConsoleColor? foreground = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Fill a rectangular area with a character.
    /// </summary>
    Task FillRectangleAsync(Rectangle bounds, char fillChar = ' ', ConsoleColor? foreground = null, ConsoleColor? background = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the current console size.
    /// </summary>
    Size GetConsoleSize();
    
    /// <summary>
    /// Observable that fires when console size changes.
    /// </summary>
    IObservable<Size> ConsoleSizeChanged { get; }
}

/// <summary>
/// Border styles for drawing borders.
/// </summary>
public enum BorderStyle
{
    None,
    Single,
    Double,
    Rounded
}
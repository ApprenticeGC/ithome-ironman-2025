using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Core UI rendering service interface for console-based UI rendering.
/// Provides console output operations and ANSI escape code support.
/// </summary>
public interface IUIRenderer : ICapabilityProvider
{
    /// <summary>
    /// Renders a UI component to the console.
    /// </summary>
    /// <param name="component">The component to render.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RenderComponentAsync(IUIComponent component, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears a rectangular area of the console.
    /// </summary>
    /// <param name="bounds">The area to clear.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearAreaAsync(UIBounds bounds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the console cursor position.
    /// </summary>
    /// <param name="position">The position to set the cursor to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetCursorPositionAsync(Position position, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Renders text at a specific position with color.
    /// </summary>
    /// <param name="position">Position to render the text.</param>
    /// <param name="text">Text to render.</param>
    /// <param name="colors">Foreground and background colors.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RenderTextAsync(Position position, string text, ConsoleColor colors, CancellationToken cancellationToken = default);
}
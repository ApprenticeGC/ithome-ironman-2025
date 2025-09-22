using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Core UI service interface for text-based user interface rendering and input handling.
/// Provides essential TUI capabilities including text rendering, layout management, and component handling.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    /// <summary>
    /// Renders text at the specified position with optional styling.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="position">Position where to render the text.</param>
    /// <param name="style">Optional text styling information.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async render operation.</returns>
    Task RenderTextAsync(string text, Position position, TextStyle? style = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the entire UI surface.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async clear operation.</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears a specific rectangular area.
    /// </summary>
    /// <param name="bounds">The rectangular area to clear.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async clear operation.</returns>
    Task ClearAreaAsync(Rectangle bounds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current size of the UI surface.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current size of the UI surface.</returns>
    Task<Size> GetSurfaceSizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the cursor position.
    /// </summary>
    /// <param name="position">Position to set the cursor to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetCursorPositionAsync(Position position, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the UI surface size changes.
    /// </summary>
    event EventHandler<SizeChangedEventArgs>? SizeChanged;
}

/// <summary>
/// Capability interface for advanced text rendering features.
/// Provides enhanced text formatting, colors, and styling capabilities.
/// </summary>
public interface IAdvancedTextRenderingCapability : ICapabilityProvider
{
    /// <summary>
    /// Renders text with advanced formatting including colors and attributes.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="position">Position where to render the text.</param>
    /// <param name="foregroundColor">Text color.</param>
    /// <param name="backgroundColor">Background color.</param>
    /// <param name="attributes">Text attributes (bold, underline, etc.).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async render operation.</returns>
    Task RenderColoredTextAsync(string text, Position position, ConsoleColor? foregroundColor = null, 
        ConsoleColor? backgroundColor = null, TextAttributes attributes = TextAttributes.None, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for UI component management.
/// Allows services to manage and render complex UI components and layouts.
/// </summary>
public interface IComponentManagementCapability : ICapabilityProvider
{
    /// <summary>
    /// Adds a UI component to the managed component collection.
    /// </summary>
    /// <param name="component">The component to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async add operation.</returns>
    Task AddComponentAsync(IUIComponent component, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a UI component from the managed collection.
    /// </summary>
    /// <param name="componentId">ID of the component to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async remove operation.</returns>
    Task RemoveComponentAsync(string componentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders all managed components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async render operation.</returns>
    Task RenderAllComponentsAsync(CancellationToken cancellationToken = default);
}
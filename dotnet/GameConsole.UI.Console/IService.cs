using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Console;

/// <summary>
/// Console UI service interface for text-based user interface rendering and interaction.
/// Provides rich console components with colors, formatting, and keyboard navigation.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    /// <summary>
    /// Gets the console framework for managing UI components.
    /// </summary>
    IConsoleUIFramework Framework { get; }

    /// <summary>
    /// Gets the input manager for handling console keyboard interactions.
    /// </summary>
    IConsoleInputManager InputManager { get; }

    /// <summary>
    /// Gets the layout manager for positioning and organizing UI elements.
    /// </summary>
    IConsoleLayoutManager LayoutManager { get; }

    /// <summary>
    /// Renders all UI components to the console.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async render operation.</returns>
    Task RenderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the console display.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async clear operation.</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current console dimensions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Console dimensions (width, height).</returns>
    Task<(int Width, int Height)> GetConsoleDimensionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the console cursor position.
    /// </summary>
    /// <param name="x">X coordinate (column).</param>
    /// <param name="y">Y coordinate (row).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetCursorPositionAsync(int x, int y, CancellationToken cancellationToken = default);
}
using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Console;

/// <summary>
/// Core UI service interface for console-based user interface operations.
/// Provides unified interface for text-based UI rendering and interaction.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    /// <summary>
    /// Gets the console UI framework instance for rendering operations.
    /// </summary>
    IConsoleUIFramework Framework { get; }
    
    /// <summary>
    /// Gets the input manager for handling keyboard interactions.
    /// </summary>
    IConsoleInputManager InputManager { get; }
    
    /// <summary>
    /// Gets the layout manager for positioning UI components.
    /// </summary>
    IConsoleLayoutManager LayoutManager { get; }
    
    /// <summary>
    /// Event fired when the console screen is resized.
    /// </summary>
    event EventHandler<ConsoleResizeEventArgs>? ScreenResize;
    
    /// <summary>
    /// Renders the current UI state to the console.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task RenderAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears the console screen.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for console resize events.
/// </summary>
public class ConsoleResizeEventArgs : EventArgs
{
    public int Width { get; }
    public int Height { get; }
    
    public ConsoleResizeEventArgs(int width, int height)
    {
        Width = width;
        Height = height;
    }
}
using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Core UI service interface for console-based user interface operations.
/// Provides unified interface for TUI rendering and component management.
/// </summary>
public interface IUIService : IService
{
    #region Rendering Operations
    
    /// <summary>
    /// Begins a new UI render frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BeginFrameAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ends the current UI render frame and presents to console.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EndFrameAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears the current console screen.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);
    
    #endregion

    #region Component Management
    
    /// <summary>
    /// Gets the window manager capability.
    /// </summary>
    IWindowManagerCapability? WindowManager { get; }
    
    /// <summary>
    /// Gets the renderer capability.
    /// </summary>
    IRendererCapability? Renderer { get; }
    
    /// <summary>
    /// Gets the theme manager capability.
    /// </summary>
    IThemeManagerCapability? ThemeManager { get; }

    #endregion
    
    #region Console State
    
    /// <summary>
    /// Sets the console size.
    /// </summary>
    /// <param name="width">Console width in characters.</param>
    /// <param name="height">Console height in characters.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetConsoleSizeAsync(int width, int height, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current console dimensions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The console dimensions as (width, height).</returns>
    Task<(int width, int height)> GetConsoleSizeAsync(CancellationToken cancellationToken = default);
    
    #endregion
}

/// <summary>
/// Capability interface for window management operations.
/// </summary>
public interface IWindowManagerCapability : ICapabilityProvider
{
    /// <summary>
    /// Creates a new window.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="width">Window width.</param>
    /// <param name="height">Window height.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Created window instance.</returns>
    Task<IWindow> CreateWindowAsync(string title, int x, int y, int width, int height, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a window by ID.
    /// </summary>
    /// <param name="windowId">Window identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Window instance or null if not found.</returns>
    Task<IWindow?> GetWindowAsync(string windowId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Destroys a window.
    /// </summary>
    /// <param name="windowId">Window identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DestroyWindowAsync(string windowId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for rendering operations.
/// </summary>
public interface IRendererCapability : ICapabilityProvider
{
    /// <summary>
    /// Renders text at the specified position.
    /// </summary>
    /// <param name="text">Text to render.</param>
    /// <param name="x">X position.</param>
    /// <param name="y">Y position.</param>
    /// <param name="style">Text style.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RenderTextAsync(string text, int x, int y, TextStyle style, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Renders a component to the specified render target.
    /// </summary>
    /// <param name="component">Component to render.</param>
    /// <param name="target">Render target.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RenderComponentAsync(IUIComponent component, IRenderTarget target, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for theme management operations.
/// </summary>
public interface IThemeManagerCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets the current theme.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current theme.</returns>
    Task<ITheme> GetCurrentThemeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the current theme.
    /// </summary>
    /// <param name="theme">Theme to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetCurrentThemeAsync(ITheme theme, CancellationToken cancellationToken = default);
}
namespace GameConsole.UI.Core;

/// <summary>
/// Interface for managing UI windows and components.
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// Create a new window.
    /// </summary>
    Task<IWindow> CreateWindowAsync(string title, Rectangle bounds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all open windows.
    /// </summary>
    IReadOnlyList<IWindow> GetWindows();
    
    /// <summary>
    /// Close a window.
    /// </summary>
    Task CloseWindowAsync(IWindow window, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set the focused window.
    /// </summary>
    Task SetFocusAsync(IWindow window, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the currently focused window.
    /// </summary>
    IWindow? GetFocusedWindow();
    
    /// <summary>
    /// Observable that fires when window focus changes.
    /// </summary>
    IObservable<IWindow?> FocusChanged { get; }
}
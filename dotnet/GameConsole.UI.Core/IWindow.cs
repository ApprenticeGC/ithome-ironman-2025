namespace GameConsole.UI.Core;

/// <summary>
/// Interface for UI windows.
/// </summary>
public interface IWindow : IUIComponent
{
    /// <summary>
    /// Window title displayed in the title bar.
    /// </summary>
    string Title { get; set; }
    
    /// <summary>
    /// Whether the window has focus.
    /// </summary>
    new bool HasFocus { get; }
    
    /// <summary>
    /// Whether the window can be moved.
    /// </summary>
    bool CanMove { get; set; }
    
    /// <summary>
    /// Whether the window can be resized.
    /// </summary>
    bool CanResize { get; set; }
    
    /// <summary>
    /// Client area bounds (interior area excluding border/title).
    /// </summary>
    Rectangle ClientBounds { get; }
    
    /// <summary>
    /// Add a component to this window.
    /// </summary>
    Task AddComponentAsync(IUIComponent component, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Remove a component from this window.
    /// </summary>
    Task RemoveComponentAsync(IUIComponent component, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all components in this window.
    /// </summary>
    IReadOnlyList<IUIComponent> GetComponents();
    
    /// <summary>
    /// Close this window.
    /// </summary>
    Task CloseAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Observable that fires when the window is closed.
    /// </summary>
    IObservable<IWindow> Closed { get; }
}

/// <summary>
/// Base interface for all UI components.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Unique identifier for this component.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Component bounds within its parent container.
    /// </summary>
    Rectangle Bounds { get; set; }
    
    /// <summary>
    /// Whether the component is visible.
    /// </summary>
    bool Visible { get; set; }
    
    /// <summary>
    /// Whether the component can receive focus.
    /// </summary>
    bool CanFocus { get; set; }
    
    /// <summary>
    /// Whether the component currently has focus.
    /// </summary>
    bool HasFocus { get; }
    
    /// <summary>
    /// Parent component (null for root components).
    /// </summary>
    IUIComponent? Parent { get; set; }
    
    /// <summary>
    /// Render this component.
    /// </summary>
    Task RenderAsync(IConsoleRenderer renderer, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Handle a UI event.
    /// </summary>
    Task<bool> HandleEventAsync(UIEvent uiEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Observable that fires when the component bounds change.
    /// </summary>
    IObservable<Rectangle> BoundsChanged { get; }
    
    /// <summary>
    /// Observable that fires when the component visibility changes.
    /// </summary>
    IObservable<bool> VisibilityChanged { get; }
}
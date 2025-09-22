using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Main UI management service interface.
/// Provides unified interface for UI rendering and event handling.
/// </summary>
public interface IUIService : IService
{
    #region Component Management
    
    /// <summary>
    /// Root UI container for all components.
    /// </summary>
    ILayoutContainer RootContainer { get; }
    
    /// <summary>
    /// Add a component to the UI system.
    /// </summary>
    /// <param name="component">Component to add.</param>
    /// <param name="parent">Parent container, or null for root.</param>
    void AddComponent(IUIComponent component, ILayoutContainer? parent = null);
    
    /// <summary>
    /// Remove a component from the UI system.
    /// </summary>
    /// <param name="componentId">ID of component to remove.</param>
    /// <returns>True if component was removed.</returns>
    bool RemoveComponent(string componentId);
    
    /// <summary>
    /// Find a component by ID.
    /// </summary>
    /// <param name="componentId">ID of component to find.</param>
    /// <returns>Component or null if not found.</returns>
    IUIComponent? FindComponent(string componentId);
    
    #endregion
    
    #region Focus Management
    
    /// <summary>
    /// Currently focused component.
    /// </summary>
    IUIComponent? FocusedComponent { get; }
    
    /// <summary>
    /// Set focus to a specific component.
    /// </summary>
    /// <param name="component">Component to focus, or null to clear focus.</param>
    /// <returns>True if focus was set successfully.</returns>
    bool SetFocus(IUIComponent? component);
    
    /// <summary>
    /// Move focus to the next focusable component.
    /// </summary>
    /// <returns>True if focus moved to a new component.</returns>
    bool FocusNext();
    
    /// <summary>
    /// Move focus to the previous focusable component.
    /// </summary>
    /// <returns>True if focus moved to a new component.</returns>
    bool FocusPrevious();
    
    #endregion
    
    #region Event System
    
    /// <summary>
    /// Process a UI event through the component hierarchy.
    /// </summary>
    /// <param name="uiEvent">Event to process.</param>
    /// <returns>True if event was handled by any component.</returns>
    bool ProcessEvent(UIEvent uiEvent);
    
    /// <summary>
    /// Event fired when any UI component handles an event.
    /// </summary>
    event EventHandler<UIEventProcessedEventArgs>? EventProcessed;
    
    #endregion
    
    #region Rendering
    
    /// <summary>
    /// Render the UI system to the graphics context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async rendering operation.</returns>
    Task RenderAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update the UI system state.
    /// </summary>
    /// <param name="deltaTime">Time since last update in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default);
    
    #endregion
    
    #region Integration Capabilities
    
    /// <summary>
    /// Gets the graphics integration capability for rendering UI components.
    /// </summary>
    IUIGraphicsCapability? GraphicsIntegration { get; }
    
    /// <summary>
    /// Gets the input integration capability for handling user input.
    /// </summary>
    IUIInputCapability? InputIntegration { get; }
    
    #endregion
}

/// <summary>
/// Event arguments for UI event processing.
/// </summary>
public class UIEventProcessedEventArgs : EventArgs
{
    public UIEvent Event { get; init; }
    public IUIComponent? HandlerComponent { get; init; }
    public bool WasHandled { get; init; }
    
    public UIEventProcessedEventArgs(UIEvent uiEvent, IUIComponent? handlerComponent, bool wasHandled)
    {
        Event = uiEvent;
        HandlerComponent = handlerComponent;
        WasHandled = wasHandled;
    }
}

/// <summary>
/// Capability interface for graphics integration with UI system.
/// </summary>
public interface IUIGraphicsCapability : ICapabilityProvider
{
    /// <summary>
    /// Render a UI component to the graphics context.
    /// </summary>
    /// <param name="component">Component to render.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async rendering operation.</returns>
    Task RenderComponentAsync(IUIComponent component, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Set the UI rendering viewport.
    /// </summary>
    /// <param name="bounds">Viewport bounds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetViewportAsync(Rectangle bounds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Begin UI rendering frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BeginUIFrameAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// End UI rendering frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EndUIFrameAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for input integration with UI system.
/// </summary>
public interface IUIInputCapability : ICapabilityProvider
{
    /// <summary>
    /// Convert mouse input to UI event.
    /// </summary>
    /// <param name="mousePosition">Mouse position.</param>
    /// <param name="button">Mouse button.</param>
    /// <param name="isPressed">Whether button is pressed or released.</param>
    /// <returns>UI event or null if not applicable.</returns>
    UIEvent? ConvertMouseInput(Position mousePosition, MouseButton button, bool isPressed);
    
    /// <summary>
    /// Convert keyboard input to UI event.
    /// </summary>
    /// <param name="keyCode">Key code.</param>
    /// <param name="modifiers">Key modifiers.</param>
    /// <param name="isPressed">Whether key is pressed or released.</param>
    /// <returns>UI event or null if not applicable.</returns>
    UIEvent? ConvertKeyboardInput(KeyCode keyCode, KeyModifiers modifiers, bool isPressed);
    
    /// <summary>
    /// Convert text input to UI event.
    /// </summary>
    /// <param name="character">Input character.</param>
    /// <param name="targetComponent">Target component for text input.</param>
    /// <returns>UI event or null if not applicable.</returns>
    UIEvent? ConvertTextInput(char character, IUIComponent? targetComponent);
    
    /// <summary>
    /// Get current mouse position in UI coordinates.
    /// </summary>
    /// <returns>Current mouse position.</returns>
    Position GetMousePosition();
}
using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Interface for UI framework abstraction supporting Console, Web, and Desktop frameworks.
/// Provides framework-agnostic UI operations using the adapter pattern.
/// </summary>
public interface IUIFramework : IService
{
    /// <summary>
    /// Gets the type of UI framework this instance represents.
    /// </summary>
    UIFrameworkType FrameworkType { get; }
    
    /// <summary>
    /// Gets the capabilities supported by this UI framework.
    /// </summary>
    UICapabilities SupportedCapabilities { get; }
    
    /// <summary>
    /// Event raised when the framework capabilities change.
    /// </summary>
    event EventHandler<UICapabilitiesChangedEventArgs>? CapabilitiesChanged;
    
    /// <summary>
    /// Initializes the UI framework with the specified context.
    /// </summary>
    /// <param name="context">The UI context for framework initialization.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeFrameworkAsync(UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a component factory for this framework.
    /// </summary>
    /// <param name="context">The UI context for component creation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a component factory.</returns>
    Task<IUIComponentFactory> CreateComponentFactoryAsync(UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Renders the specified component using this framework.
    /// </summary>
    /// <param name="component">The component to render.</param>
    /// <param name="context">The UI context for rendering.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async rendering operation.</returns>
    Task RenderComponentAsync(IUIComponent component, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Handles events from UI components in a framework-specific manner.
    /// </summary>
    /// <param name="eventArgs">The event arguments from the component.</param>
    /// <param name="context">The UI context for event handling.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async event handling operation.</returns>
    Task HandleComponentEventAsync(UIComponentEventArgs eventArgs, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the framework's supported capabilities.
    /// </summary>
    /// <param name="capabilities">The new capabilities to support.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateCapabilitiesAsync(UICapabilities capabilities, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Applies styling or theming to the framework.
    /// </summary>
    /// <param name="styleData">Framework-specific styling data.</param>
    /// <param name="context">The UI context for styling.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async styling operation.</returns>
    Task ApplyStyleAsync(object styleData, UIContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for UI capabilities changes.
/// </summary>
public class UICapabilitiesChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous capabilities.
    /// </summary>
    public UICapabilities PreviousCapabilities { get; }
    
    /// <summary>
    /// Gets the new capabilities.
    /// </summary>
    public UICapabilities NewCapabilities { get; }
    
    /// <summary>
    /// Initializes a new instance of the UICapabilitiesChangedEventArgs class.
    /// </summary>
    /// <param name="previousCapabilities">The previous capabilities.</param>
    /// <param name="newCapabilities">The new capabilities.</param>
    public UICapabilitiesChangedEventArgs(UICapabilities previousCapabilities, UICapabilities newCapabilities)
    {
        PreviousCapabilities = previousCapabilities;
        NewCapabilities = newCapabilities;
    }
}
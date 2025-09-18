namespace GameConsole.UI.Core;

/// <summary>
/// Interface for cross-framework UI components that can work across Console, Web, and Desktop frameworks.
/// Provides component lifecycle management, event handling, and data binding capabilities.
/// </summary>
public interface IUIComponent
{
    /// <summary>
    /// Gets the unique identifier for this component.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets the type of this component.
    /// </summary>
    string ComponentType { get; }
    
    /// <summary>
    /// Gets or sets the data bound to this component.
    /// </summary>
    object? Data { get; set; }
    
    /// <summary>
    /// Gets the child components of this component.
    /// </summary>
    IReadOnlyList<IUIComponent> Children { get; }
    
    /// <summary>
    /// Gets the properties of this component.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
    
    /// <summary>
    /// Gets a value indicating whether this component is currently visible.
    /// </summary>
    bool IsVisible { get; }
    
    /// <summary>
    /// Gets a value indicating whether this component is currently enabled.
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Event raised when a component event occurs (user interaction, state change, etc.).
    /// </summary>
    event EventHandler<UIComponentEventArgs>? ComponentEvent;
    
    /// <summary>
    /// Event raised when the component's data changes.
    /// </summary>
    event EventHandler<UIDataChangedEventArgs>? DataChanged;
    
    /// <summary>
    /// Initializes the component with the specified context.
    /// </summary>
    /// <param name="context">The UI context for component initialization.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Renders the component using the specified context.
    /// </summary>
    /// <param name="context">The UI context for rendering.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async rendering operation that returns framework-specific render data.</returns>
    Task<object> RenderAsync(UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the component with new data.
    /// </summary>
    /// <param name="data">The new data to bind to the component.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateAsync(object? data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets a property on the component.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetPropertyAsync(string key, object value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a property from the component.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <param name="defaultValue">The default value if the property is not found.</param>
    /// <returns>The property value or the default value.</returns>
    T GetProperty<T>(string key, T defaultValue = default!);
    
    /// <summary>
    /// Adds a child component to this component.
    /// </summary>
    /// <param name="child">The child component to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddChildAsync(IUIComponent child, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a child component from this component.
    /// </summary>
    /// <param name="child">The child component to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the child was removed.</returns>
    Task<bool> RemoveChildAsync(IUIComponent child, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the visibility of the component.
    /// </summary>
    /// <param name="visible">True to make the component visible; false to hide it.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetVisibilityAsync(bool visible, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the enabled state of the component.
    /// </summary>
    /// <param name="enabled">True to enable the component; false to disable it.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Disposes of the component and releases any resources.
    /// </summary>
    /// <returns>A task representing the async disposal operation.</returns>
    ValueTask DisposeAsync();
}

/// <summary>
/// Event arguments for UI component events.
/// </summary>
public class UIComponentEventArgs : EventArgs
{
    /// <summary>
    /// Gets the component that raised the event.
    /// </summary>
    public IUIComponent Component { get; }
    
    /// <summary>
    /// Gets the type of event that occurred.
    /// </summary>
    public string EventType { get; }
    
    /// <summary>
    /// Gets the event data.
    /// </summary>
    public object? EventData { get; }
    
    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
    
    /// <summary>
    /// Initializes a new instance of the UIComponentEventArgs class.
    /// </summary>
    /// <param name="component">The component that raised the event.</param>
    /// <param name="eventType">The type of event that occurred.</param>
    /// <param name="eventData">The event data.</param>
    public UIComponentEventArgs(IUIComponent component, string eventType, object? eventData = null)
    {
        Component = component ?? throw new ArgumentNullException(nameof(component));
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        EventData = eventData;
        Timestamp = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Event arguments for UI data changes.
/// </summary>
public class UIDataChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous data value.
    /// </summary>
    public object? PreviousData { get; }
    
    /// <summary>
    /// Gets the new data value.
    /// </summary>
    public object? NewData { get; }
    
    /// <summary>
    /// Gets the timestamp when the data changed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
    
    /// <summary>
    /// Initializes a new instance of the UIDataChangedEventArgs class.
    /// </summary>
    /// <param name="previousData">The previous data value.</param>
    /// <param name="newData">The new data value.</param>
    public UIDataChangedEventArgs(object? previousData, object? newData)
    {
        PreviousData = previousData;
        NewData = newData;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
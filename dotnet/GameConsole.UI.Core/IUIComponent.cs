namespace GameConsole.UI.Core;

/// <summary>
/// Represents the result of a UI rendering operation.
/// </summary>
public record RenderResult(
    bool Success,
    string? ErrorMessage = null,
    Dictionary<string, object>? Metadata = null
);

/// <summary>
/// Interface for rendering UI components across different frameworks.
/// </summary>
public interface IUIRenderer
{
    /// <summary>
    /// Renders a component within the specified context.
    /// </summary>
    Task<RenderResult> RenderAsync(IUIComponent component, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates a previously rendered component.
    /// </summary>
    Task<RenderResult> UpdateAsync(IUIComponent component, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears or removes a component from the rendered output.
    /// </summary>
    Task ClearAsync(string componentId, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Framework type this renderer supports.
    /// </summary>
    FrameworkType SupportedFramework { get; }
    
    /// <summary>
    /// Capabilities supported by this renderer.
    /// </summary>
    UICapabilities SupportedCapabilities { get; }
}

/// <summary>
/// Base interface for all UI components that work across different frameworks.
/// Provides common functionality for rendering, state management, and event handling.
/// </summary>
public interface IUIComponent : IAsyncDisposable
{
    /// <summary>
    /// Unique identifier for this component instance.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Component type classification.
    /// </summary>
    ComponentType ComponentType { get; }
    
    /// <summary>
    /// Current state of the component.
    /// </summary>
    Dictionary<string, object> State { get; }
    
    /// <summary>
    /// Style context for rendering.
    /// </summary>
    StyleContext Style { get; set; }
    
    /// <summary>
    /// Indicates if the component is currently visible.
    /// </summary>
    bool IsVisible { get; set; }
    
    /// <summary>
    /// Indicates if the component is enabled for interaction.
    /// </summary>
    bool IsEnabled { get; set; }
    
    /// <summary>
    /// Parent component, if any.
    /// </summary>
    IUIComponent? Parent { get; set; }
    
    /// <summary>
    /// Child components, if this component can contain others.
    /// </summary>
    IReadOnlyList<IUIComponent> Children { get; }
    
    /// <summary>
    /// Renders the component using the specified renderer and context.
    /// </summary>
    Task<RenderResult> RenderAsync(IUIRenderer renderer, UIContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the component's state and triggers re-rendering if necessary.
    /// </summary>
    Task UpdateStateAsync(Dictionary<string, object> newState, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a state value by key.
    /// </summary>
    T? GetState<T>(string key, T? defaultValue = default);
    
    /// <summary>
    /// Sets a state value and triggers update if changed.
    /// </summary>
    Task SetStateAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a child component.
    /// </summary>
    Task AddChildAsync(IUIComponent child, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a child component.
    /// </summary>
    Task RemoveChildAsync(IUIComponent child, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Handles input events directed at this component.
    /// </summary>
    Task HandleInputAsync(Input.Core.InputEvent inputEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event fired when the component's state changes.
    /// </summary>
    event Func<UIDataBindingEvent, Task>? StateChanged;
    
    /// <summary>
    /// Event fired when the component receives focus.
    /// </summary>
    event Func<UIFocusEvent, Task>? FocusReceived;
    
    /// <summary>
    /// Event fired when the component loses focus.
    /// </summary>
    event Func<UIFocusEvent, Task>? FocusLost;
    
    /// <summary>
    /// Event fired when the component is interacted with.
    /// </summary>
    event Func<UIInteractionEvent, Task>? Interacted;
    
    /// <summary>
    /// Validates the component's current state.
    /// </summary>
    Task<UIValidationEvent> ValidateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base abstract implementation of IUIComponent providing common functionality.
/// </summary>
public abstract class UIComponentBase : IUIComponent
{
    private readonly List<IUIComponent> _children = new();
    private Dictionary<string, object> _state = new();
    
    protected UIComponentBase(string id, ComponentType componentType)
    {
        Id = id;
        ComponentType = componentType;
        Style = StyleContext.Empty;
    }
    
    /// <inheritdoc />
    public string Id { get; }
    
    /// <inheritdoc />
    public ComponentType ComponentType { get; }
    
    /// <inheritdoc />
    public Dictionary<string, object> State => new(_state);
    
    /// <inheritdoc />
    public StyleContext Style { get; set; }
    
    /// <inheritdoc />
    public bool IsVisible { get; set; } = true;
    
    /// <inheritdoc />
    public bool IsEnabled { get; set; } = true;
    
    /// <inheritdoc />
    public IUIComponent? Parent { get; set; }
    
    /// <inheritdoc />
    public IReadOnlyList<IUIComponent> Children => _children.AsReadOnly();
    
    /// <inheritdoc />
    public event Func<UIDataBindingEvent, Task>? StateChanged;
    
    /// <inheritdoc />
    public event Func<UIFocusEvent, Task>? FocusReceived;
    
    /// <inheritdoc />
    public event Func<UIFocusEvent, Task>? FocusLost;
    
    /// <inheritdoc />
    public event Func<UIInteractionEvent, Task>? Interacted;
    
    /// <inheritdoc />
    public abstract Task<RenderResult> RenderAsync(IUIRenderer renderer, UIContext context, CancellationToken cancellationToken = default);
    
    /// <inheritdoc />
    public virtual async Task UpdateStateAsync(Dictionary<string, object> newState, CancellationToken cancellationToken = default)
    {
        var oldState = new Dictionary<string, object>(_state);
        _state = new Dictionary<string, object>(newState);
        
        // Fire state change events for modified properties
        foreach (var kvp in newState)
        {
            if (!oldState.TryGetValue(kvp.Key, out var oldValue) || !Equals(oldValue, kvp.Value))
            {
                var stateEvent = new UIDataBindingEvent(Id, kvp.Key, oldValue, kvp.Value, DateTime.UtcNow);
                if (StateChanged != null)
                {
                    await StateChanged(stateEvent);
                }
            }
        }
    }
    
    /// <inheritdoc />
    public T? GetState<T>(string key, T? defaultValue = default)
    {
        if (_state.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    /// <inheritdoc />
    public async Task SetStateAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        if (!_state.TryGetValue(key, out var oldValue) || !Equals(oldValue, value))
        {
            _state[key] = value!;
            var stateEvent = new UIDataBindingEvent(Id, key, oldValue, value, DateTime.UtcNow);
            if (StateChanged != null)
            {
                await StateChanged(stateEvent);
            }
        }
    }
    
    /// <inheritdoc />
    public virtual async Task AddChildAsync(IUIComponent child, CancellationToken cancellationToken = default)
    {
        if (child.Parent != null)
        {
            await child.Parent.RemoveChildAsync(child, cancellationToken);
        }
        
        child.Parent = this;
        _children.Add(child);
        
        var lifecycleEvent = new UILifecycleEvent(child.Id, "mounted", DateTime.UtcNow);
        // Publish lifecycle event if needed
    }
    
    /// <inheritdoc />
    public virtual async Task RemoveChildAsync(IUIComponent child, CancellationToken cancellationToken = default)
    {
        if (_children.Remove(child))
        {
            child.Parent = null;
            var lifecycleEvent = new UILifecycleEvent(child.Id, "unmounted", DateTime.UtcNow);
            // Publish lifecycle event if needed
        }
        await Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public virtual async Task HandleInputAsync(Input.Core.InputEvent inputEvent, CancellationToken cancellationToken = default)
    {
        // Default implementation - can be overridden by specific components
        if (Interacted != null)
        {
            var interactionEvent = new UIInteractionEvent(Id, "input", DateTime.UtcNow, inputEvent);
            await Interacted(interactionEvent);
        }
    }
    
    /// <summary>
    /// Raises the FocusReceived event.
    /// </summary>
    protected async Task OnFocusReceivedAsync(UIFocusEvent focusEvent)
    {
        if (FocusReceived != null)
        {
            await FocusReceived(focusEvent);
        }
    }
    
    /// <summary>
    /// Raises the FocusLost event.
    /// </summary>
    protected async Task OnFocusLostAsync(UIFocusEvent focusEvent)
    {
        if (FocusLost != null)
        {
            await FocusLost(focusEvent);
        }
    }
    
    /// <inheritdoc />
    public virtual Task<UIValidationEvent> ValidateAsync(CancellationToken cancellationToken = default)
    {
        // Default implementation - always valid unless overridden
        var validationEvent = new UIValidationEvent(Id, true, null, DateTime.UtcNow);
        return Task.FromResult(validationEvent);
    }
    
    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        // Dispose of all children
        foreach (var child in _children)
        {
            await child.DisposeAsync();
        }
        _children.Clear();
        
        // Clear event handlers
        StateChanged = null;
        FocusReceived = null;
        FocusLost = null;
        Interacted = null;
        
        GC.SuppressFinalize(this);
    }
}
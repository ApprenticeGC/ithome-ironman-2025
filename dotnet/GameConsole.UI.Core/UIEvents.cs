namespace GameConsole.UI.Core;

/// <summary>
/// Base class for all UI events that occur within the framework.
/// </summary>
public abstract record UIEvent(
    string EventType,
    DateTime Timestamp,
    string? ComponentId = null,
    Dictionary<string, object>? Data = null
)
{
    /// <summary>
    /// Gets event data of a specific type.
    /// </summary>
    public T? GetData<T>(string key, T? defaultValue = default)
    {
        if (Data?.TryGetValue(key, out var value) == true && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }
    
    /// <summary>
    /// Creates a new event with additional data.
    /// </summary>
    public UIEvent WithData(string key, object value)
    {
        var newData = Data is not null ? new Dictionary<string, object>(Data) : new Dictionary<string, object>();
        newData[key] = value;
        return this with { Data = newData };
    }
}

/// <summary>
/// Represents an interaction event from a UI component.
/// </summary>
public record UIInteractionEvent(
    string ComponentId,
    string ActionType,
    DateTime Timestamp,
    object? ActionData = null,
    Dictionary<string, object>? Data = null
) : UIEvent("interaction", Timestamp, ComponentId, Data);

/// <summary>
/// Represents a data binding event for component state changes.
/// </summary>
public record UIDataBindingEvent(
    string ComponentId,
    string PropertyName,
    object? OldValue,
    object? NewValue,
    DateTime Timestamp,
    Dictionary<string, object>? Data = null
) : UIEvent("dataBinding", Timestamp, ComponentId, Data);

/// <summary>
/// Represents a focus change event in the UI.
/// </summary>
public record UIFocusEvent(
    string? PreviousComponentId,
    string? CurrentComponentId,
    DateTime Timestamp,
    Dictionary<string, object>? Data = null
) : UIEvent("focus", Timestamp, CurrentComponentId, Data);

/// <summary>
/// Represents a validation event for form components.
/// </summary>
public record UIValidationEvent(
    string ComponentId,
    bool IsValid,
    string[]? ValidationErrors,
    DateTime Timestamp,
    Dictionary<string, object>? Data = null
) : UIEvent("validation", Timestamp, ComponentId, Data);

/// <summary>
/// Represents a lifecycle event for UI components.
/// </summary>
public record UILifecycleEvent(
    string ComponentId,
    string LifecycleStage, // created, mounted, updated, unmounted, destroyed
    DateTime Timestamp,
    Dictionary<string, object>? Data = null
) : UIEvent("lifecycle", Timestamp, ComponentId, Data);

/// <summary>
/// Event handler delegate for UI events.
/// </summary>
public delegate Task UIEventHandler<in T>(T eventArgs) where T : UIEvent;

/// <summary>
/// Interface for dispatching UI events across the framework.
/// </summary>
public interface IUIEventDispatcher
{
    /// <summary>
    /// Publishes a UI event to all registered handlers.
    /// </summary>
    Task PublishAsync<T>(T eventArgs, CancellationToken cancellationToken = default) where T : UIEvent;
    
    /// <summary>
    /// Subscribes to UI events of a specific type.
    /// </summary>
    IDisposable Subscribe<T>(UIEventHandler<T> handler) where T : UIEvent;
    
    /// <summary>
    /// Subscribes to all UI events.
    /// </summary>
    IDisposable SubscribeAll(UIEventHandler<UIEvent> handler);
    
    /// <summary>
    /// Observable stream of all UI events.
    /// </summary>
    IObservable<UIEvent> Events { get; }
}

/// <summary>
/// Interface for managing UI event subscriptions and cleanup.
/// </summary>
public interface IUIEventSubscription : IDisposable
{
    /// <summary>
    /// Indicates if the subscription is still active.
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// The event type being subscribed to.
    /// </summary>
    string EventType { get; }
}
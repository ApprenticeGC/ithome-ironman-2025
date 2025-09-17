namespace TestLib.CQRS;

/// <summary>
/// Interface for the event bus in the CQRS system.
/// Handles publishing and subscribing to events.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;

    /// <summary>
    /// Subscribes to events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>
    /// <param name="handler">The handler function for the event.</param>
    void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IEvent;

    /// <summary>
    /// Unsubscribes from events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to unsubscribe from.</typeparam>
    /// <param name="handler">The handler function to remove.</param>
    void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IEvent;
}
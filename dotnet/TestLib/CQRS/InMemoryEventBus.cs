using System.Collections.Concurrent;

namespace TestLib.CQRS;

/// <summary>
/// In-memory implementation of the event bus.
/// Provides simple event publishing and subscription capabilities.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _handlers = new();

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        if (!_handlers.TryGetValue(eventType, out var handlers))
        {
            return;
        }

        var tasks = handlers.Cast<Func<TEvent, CancellationToken, Task>>()
                           .Select(handler => handler(@event, cancellationToken));

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Subscribes to events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to.</typeparam>
    /// <param name="handler">The handler function for the event.</param>
    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        _handlers.AddOrUpdate(
            eventType,
            new ConcurrentBag<object> { handler },
            (key, existing) =>
            {
                existing.Add(handler);
                return existing;
            });
    }

    /// <summary>
    /// Unsubscribes from events of the specified type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to unsubscribe from.</typeparam>
    /// <param name="handler">The handler function to remove.</param>
    public void Unsubscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            // Note: ConcurrentBag doesn't support removal, so we create a new bag without the handler
            var remainingHandlers = handlers.Where(h => !ReferenceEquals(h, handler)).ToArray();
            _handlers.TryUpdate(eventType, new ConcurrentBag<object>(remainingHandlers), handlers);
        }
    }

    /// <summary>
    /// Gets the count of handlers for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event.</typeparam>
    /// <returns>The number of handlers for the event type.</returns>
    public int GetHandlerCount<TEvent>() where TEvent : IEvent
    {
        var eventType = typeof(TEvent);
        return _handlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
    }

    /// <summary>
    /// Clears all handlers for all event types.
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
    }
}
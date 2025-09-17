namespace TestLib.CQRS;

/// <summary>
/// Abstract base class for events in the CQRS system.
/// </summary>
public abstract class Event : IEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}
namespace TestLib.CQRS;

/// <summary>
/// Interface for events in the CQRS system.
/// Events represent something that has happened in the system.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Unique identifier for the event.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Timestamp when the event occurred.
    /// </summary>
    DateTime Timestamp { get; }
}
namespace GameConsole.Core.Abstractions;

/// <summary>
/// Base interface for all actor messages.
/// Provides message identification and correlation support.
/// </summary>
public interface IActorMessage
{
    /// <summary>
    /// Unique identifier for this message instance.
    /// </summary>
    Guid MessageId { get; }

    /// <summary>
    /// Optional correlation ID to link related messages.
    /// Used for request/response patterns and message chains.
    /// </summary>
    Guid? CorrelationId { get; }

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }
}

/// <summary>
/// Base implementation of IActorMessage with common functionality.
/// </summary>
public abstract class ActorMessage : IActorMessage
{
    /// <inheritdoc/>
    public Guid MessageId { get; }

    /// <inheritdoc/>
    public Guid? CorrelationId { get; protected set; }

    /// <inheritdoc/>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Initializes a new message with unique ID and timestamp.
    /// </summary>
    protected ActorMessage()
    {
        MessageId = Guid.NewGuid();
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Initializes a new message as a response to another message.
    /// Sets CorrelationId to the original message's MessageId.
    /// </summary>
    /// <param name="originalMessage">The message this is responding to.</param>
    protected ActorMessage(IActorMessage originalMessage) : this()
    {
        CorrelationId = originalMessage.MessageId;
    }

    /// <summary>
    /// Initializes a new message with a specific correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to set.</param>
    protected ActorMessage(Guid correlationId) : this()
    {
        CorrelationId = correlationId;
    }
}
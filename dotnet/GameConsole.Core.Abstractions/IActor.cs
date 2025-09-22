namespace GameConsole.Core.Abstractions;

/// <summary>
/// Result of an actor message handling operation.
/// </summary>
public enum ActorMessageHandleResult
{
    /// <summary>
    /// Message was handled successfully.
    /// </summary>
    Handled,
    
    /// <summary>
    /// Message was not handled (no appropriate handler).
    /// </summary>
    NotHandled,
    
    /// <summary>
    /// Message handling failed due to an error.
    /// </summary>
    Failed,
    
    /// <summary>
    /// Message was deferred for later processing.
    /// </summary>
    Deferred
}

/// <summary>
/// Defines the interface for actors in the GameConsole system.
/// Actors are autonomous entities that process messages asynchronously and maintain internal state.
/// Extends IService to integrate with the existing service lifecycle.
/// </summary>
public interface IActor : IService
{
    /// <summary>
    /// Unique identifier for this actor instance.
    /// </summary>
    ActorId Id { get; }

    /// <summary>
    /// Gets the type name of this actor for identification and clustering.
    /// </summary>
    string ActorType { get; }

    /// <summary>
    /// Sends a message to this actor for asynchronous processing.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes when the message is queued for processing.</returns>
    Task SendMessageAsync(IActorMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request message and waits for a response.
    /// </summary>
    /// <typeparam name="TResponse">Expected response type.</typeparam>
    /// <param name="request">The request message to send.</param>
    /// <param name="timeout">Timeout for waiting for response.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task that completes with the response message, or null if timeout occurs.</returns>
    Task<TResponse?> SendRequestAsync<TResponse>(IActorMessage request, TimeSpan timeout, CancellationToken cancellationToken = default)
        where TResponse : class, IActorMessage;

    /// <summary>
    /// Event fired when the actor processes a message.
    /// Can be used for monitoring, logging, and debugging.
    /// </summary>
    event EventHandler<ActorMessageEventArgs>? MessageProcessed;
}

/// <summary>
/// Event arguments for actor message processing events.
/// </summary>
public class ActorMessageEventArgs : EventArgs
{
    /// <summary>
    /// The message that was processed.
    /// </summary>
    public IActorMessage Message { get; }

    /// <summary>
    /// Result of the message processing.
    /// </summary>
    public ActorMessageHandleResult Result { get; }

    /// <summary>
    /// Time taken to process the message.
    /// </summary>
    public TimeSpan ProcessingTime { get; }

    /// <summary>
    /// Exception that occurred during processing (if Result is Failed).
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Initializes new event arguments.
    /// </summary>
    public ActorMessageEventArgs(IActorMessage message, ActorMessageHandleResult result, TimeSpan processingTime, Exception? exception = null)
    {
        Message = message;
        Result = result;
        ProcessingTime = processingTime;
        Exception = exception;
    }
}
namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Represents a reference to an actor instance, providing location-transparent communication.
/// </summary>
public interface IActorRef
{
    /// <summary>
    /// Gets the unique path identifier for this actor.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Gets the name of this actor.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sends a message to the actor asynchronously.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="sender">Optional sender actor reference.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async send operation.</returns>
    Task TellAsync(object message, IActorRef? sender = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the actor and waits for a response.
    /// </summary>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="timeout">Maximum time to wait for response.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async ask operation.</returns>
    Task<TResponse> AskAsync<TResponse>(object message, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether this actor reference is still valid.
    /// </summary>
    bool IsValid { get; }
}
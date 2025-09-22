namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Provides context and runtime information for actor operations.
/// </summary>
public interface IActorContext
{
    /// <summary>
    /// Gets the current actor's reference.
    /// </summary>
    IActorRef Self { get; }

    /// <summary>
    /// Gets the sender of the current message being processed.
    /// </summary>
    IActorRef? Sender { get; }

    /// <summary>
    /// Gets the actor system that owns this actor.
    /// </summary>
    IActorSystem System { get; }

    /// <summary>
    /// Creates a child actor.
    /// </summary>
    /// <typeparam name="T">The actor type to create.</typeparam>
    /// <param name="name">Optional name for the child actor.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the child actor reference.</returns>
    Task<IActorRef> ActorOfAsync<T>(string? name = null, CancellationToken cancellationToken = default) where T : IActor, new();

    /// <summary>
    /// Stops a child actor.
    /// </summary>
    /// <param name="child">The child actor to stop.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    Task StopAsync(IActorRef child, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the current actor.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    Task StopSelfAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches another actor for termination.
    /// </summary>
    /// <param name="actor">The actor to watch.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async watch operation.</returns>
    Task WatchAsync(IActorRef actor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops watching an actor for termination.
    /// </summary>
    /// <param name="actor">The actor to unwatch.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unwatch operation.</returns>
    Task UnwatchAsync(IActorRef actor, CancellationToken cancellationToken = default);
}
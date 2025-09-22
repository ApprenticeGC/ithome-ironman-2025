using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Defines the actor system service interface, managing actor lifecycle and clustering.
/// </summary>
public interface IActorSystem : IService, ICapabilityProvider
{
    /// <summary>
    /// Creates a new top-level actor of the specified type.
    /// </summary>
    /// <typeparam name="T">The actor type to create.</typeparam>
    /// <param name="name">Optional name for the actor.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the actor reference.</returns>
    Task<IActorRef> ActorOfAsync<T>(string? name = null, CancellationToken cancellationToken = default) where T : IActor, new();

    /// <summary>
    /// Selects an actor by path.
    /// </summary>
    /// <param name="path">The actor path to select.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the actor reference, or null if not found.</returns>
    Task<IActorRef?> ActorSelectionAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the cluster manager for this actor system.
    /// </summary>
    IClusterManager? Cluster { get; }

    /// <summary>
    /// Gets the dead letter office for undeliverable messages.
    /// </summary>
    IActorRef DeadLetters { get; }

    /// <summary>
    /// Terminates the actor system gracefully.
    /// </summary>
    /// <param name="timeout">Maximum time to wait for termination.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async termination operation.</returns>
    Task TerminateAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}
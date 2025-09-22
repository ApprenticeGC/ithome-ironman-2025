using System.Numerics;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for managing a cluster of actors that can be processed as a group.
/// Clusters provide efficient batch operations and coordinated behavior for multiple actors.
/// </summary>
public interface IActorCluster
{
    /// <summary>
    /// Gets the unique identifier for this cluster.
    /// </summary>
    string ClusterId { get; }
    
    /// <summary>
    /// Gets the collection of actors currently in this cluster.
    /// This is a read-only collection to prevent external modification.
    /// </summary>
    IReadOnlyCollection<IActor> Actors { get; }
    
    /// <summary>
    /// Gets the calculated center position of all actors in this cluster.
    /// This is computed based on the average position of all active actors.
    /// </summary>
    Vector3 ClusterCenter { get; }
    
    /// <summary>
    /// Gets or sets a value indicating whether this cluster is currently active.
    /// Inactive clusters may be skipped during processing.
    /// </summary>
    bool IsActive { get; set; }
    
    /// <summary>
    /// Adds an actor to this cluster asynchronously.
    /// </summary>
    /// <param name="actor">The actor to add to the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async add operation.</returns>
    Task AddActorAsync(IActor actor, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes an actor from this cluster by its identifier.
    /// </summary>
    /// <param name="actorId">The identifier of the actor to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async remove operation that returns true if the actor was found and removed.</returns>
    Task<bool> RemoveActorAsync(string actorId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates all actors in this cluster and recalculates cluster properties.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster update operation.</returns>
    Task UpdateClusterAsync(float deltaTime, CancellationToken cancellationToken = default);
}
using System.Collections.Concurrent;
using System.Numerics;
using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Implementation of an actor cluster that manages a collection of actors
/// and provides batch processing capabilities.
/// </summary>
public class ActorCluster : IActorCluster
{
    private readonly ConcurrentDictionary<string, IActor> _actors = new();
    private System.Numerics.Vector3 _clusterCenter = System.Numerics.Vector3.Zero;
    private readonly object _centerLock = new();

    /// <summary>
    /// Gets the unique identifier for this cluster.
    /// </summary>
    public string ClusterId { get; }

    /// <summary>
    /// Gets the collection of actors currently in this cluster.
    /// </summary>
    public IReadOnlyCollection<IActor> Actors => _actors.Values.ToList().AsReadOnly();

    /// <summary>
    /// Gets the calculated center position of all actors in this cluster.
    /// </summary>
    public System.Numerics.Vector3 ClusterCenter 
    { 
        get 
        { 
            lock (_centerLock) 
            { 
                return _clusterCenter; 
            } 
        } 
    }

    /// <summary>
    /// Gets or sets a value indicating whether this cluster is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the ActorCluster class.
    /// </summary>
    /// <param name="clusterId">The unique identifier for this cluster.</param>
    public ActorCluster(string clusterId)
    {
        ClusterId = clusterId ?? throw new ArgumentNullException(nameof(clusterId));
    }

    /// <summary>
    /// Adds an actor to this cluster asynchronously.
    /// </summary>
    /// <param name="actor">The actor to add to the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async add operation.</returns>
    public Task AddActorAsync(IActor actor, CancellationToken cancellationToken = default)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));

        _actors.TryAdd(actor.Id, actor);
        RecalculateClusterCenter();
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes an actor from this cluster by its identifier.
    /// </summary>
    /// <param name="actorId">The identifier of the actor to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async remove operation that returns true if the actor was found and removed.</returns>
    public Task<bool> RemoveActorAsync(string actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(actorId))
            return Task.FromResult(false);

        var removed = _actors.TryRemove(actorId, out _);
        if (removed)
        {
            RecalculateClusterCenter();
        }
        
        return Task.FromResult(removed);
    }

    /// <summary>
    /// Updates all actors in this cluster and recalculates cluster properties.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster update operation.</returns>
    public async Task UpdateClusterAsync(float deltaTime, CancellationToken cancellationToken = default)
    {
        if (!IsActive || _actors.IsEmpty)
            return;

        // Update all actors in parallel for better performance
        var updateTasks = _actors.Values
            .Where(actor => actor.IsActive)
            .Select(actor => actor.UpdateAsync(deltaTime, cancellationToken));

        await Task.WhenAll(updateTasks);
        
        // Recalculate cluster center after all actors have been updated
        RecalculateClusterCenter();
    }

    /// <summary>
    /// Recalculates the cluster center based on the current positions of all active actors.
    /// </summary>
    private void RecalculateClusterCenter()
    {
        lock (_centerLock)
        {
            var activeActors = _actors.Values.Where(a => a.IsActive).ToList();
            
            if (!activeActors.Any())
            {
                _clusterCenter = Vector3.Zero;
                return;
            }

            var sum = activeActors.Aggregate(Vector3.Zero, (current, actor) => current + actor.Position);
            _clusterCenter = sum / activeActors.Count;
        }
    }
}
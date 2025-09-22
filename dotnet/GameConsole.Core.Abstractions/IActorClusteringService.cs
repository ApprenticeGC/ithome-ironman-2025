namespace GameConsole.Core.Abstractions;

/// <summary>
/// Tier 1 contract: Service interface for managing actor clusters in the GameConsole system.
/// Provides cluster lifecycle management and batch processing capabilities for AI agents.
/// </summary>
public interface IActorClusteringService : IService, ICapabilityProvider
{
    /// <summary>
    /// Creates a new actor cluster with the specified identifier.
    /// </summary>
    /// <param name="clusterId">The unique identifier for the new cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created cluster, or null if creation failed.</returns>
    Task<IActorCluster?> CreateClusterAsync(string clusterId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Destroys an existing actor cluster and removes all its actors.
    /// </summary>
    /// <param name="clusterId">The identifier of the cluster to destroy.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the cluster was found and destroyed.</returns>
    Task<bool> DestroyClusterAsync(string clusterId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an existing cluster by its identifier.
    /// </summary>
    /// <param name="clusterId">The identifier of the cluster to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the cluster, or null if not found.</returns>
    Task<IActorCluster?> GetClusterAsync(string clusterId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all currently managed clusters.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns all clusters.</returns>
    Task<IEnumerable<IActorCluster>> GetAllClustersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates all clusters and their actors. This is the main processing loop
    /// that should be called each frame to update cluster state.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update, in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async batch update operation.</returns>
    Task UpdateAllClustersAsync(float deltaTime, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the total number of actors across all clusters.
    /// Useful for performance monitoring and resource management.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the total actor count.</returns>
    Task<int> GetTotalActorCountAsync(CancellationToken cancellationToken = default);
}
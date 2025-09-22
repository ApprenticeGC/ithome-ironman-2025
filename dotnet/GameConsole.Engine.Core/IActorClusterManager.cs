using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Cluster formation strategies for organizing actors.
/// </summary>
public enum ClusterFormationStrategy
{
    /// <summary>
    /// Clusters are formed manually by explicit assignment.
    /// </summary>
    Manual,
    
    /// <summary>
    /// Clusters are formed based on geographical proximity.
    /// </summary>
    Geographic,
    
    /// <summary>
    /// Clusters are formed based on functional similarity.
    /// </summary>
    Functional,
    
    /// <summary>
    /// Clusters are formed based on workload balancing.
    /// </summary>
    LoadBalanced,
    
    /// <summary>
    /// Clusters are formed adaptively based on performance metrics.
    /// </summary>
    Adaptive
}

/// <summary>
/// Cluster state enumeration.
/// </summary>
public enum ClusterState
{
    /// <summary>
    /// Cluster is being formed.
    /// </summary>
    Forming,
    
    /// <summary>
    /// Cluster is active and processing messages.
    /// </summary>
    Active,
    
    /// <summary>
    /// Cluster is being reorganized.
    /// </summary>
    Reorganizing,
    
    /// <summary>
    /// Cluster is being dissolved.
    /// </summary>
    Dissolving,
    
    /// <summary>
    /// Cluster has been dissolved.
    /// </summary>
    Dissolved,
    
    /// <summary>
    /// Cluster has encountered an error.
    /// </summary>
    Faulted
}

/// <summary>
/// Represents cluster performance and health metrics.
/// </summary>
public class ClusterMetrics
{
    /// <summary>
    /// Number of active actors in the cluster.
    /// </summary>
    public int ActiveActorCount { get; set; }
    
    /// <summary>
    /// Average message processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTime { get; set; }
    
    /// <summary>
    /// Messages processed per second across the cluster.
    /// </summary>
    public double MessagesPerSecond { get; set; }
    
    /// <summary>
    /// Cluster resource utilization percentage (0.0 to 1.0).
    /// </summary>
    public float ResourceUtilization { get; set; }
    
    /// <summary>
    /// Cluster health score (0.0 to 1.0).
    /// </summary>
    public float HealthScore { get; set; }
    
    /// <summary>
    /// Number of failed actors in the cluster.
    /// </summary>
    public int FailedActorCount { get; set; }
    
    /// <summary>
    /// Last update timestamp for these metrics.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// Initializes a new instance of the ClusterMetrics class.
    /// </summary>
    public ClusterMetrics()
    {
        LastUpdated = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Configuration for cluster behavior.
/// </summary>
public class ClusterConfiguration
{
    /// <summary>
    /// Maximum number of actors allowed in this cluster.
    /// </summary>
    public int MaxActorCount { get; set; } = 100;
    
    /// <summary>
    /// Minimum number of actors required for cluster stability.
    /// </summary>
    public int MinActorCount { get; set; } = 1;
    
    /// <summary>
    /// Strategy used for forming this cluster.
    /// </summary>
    public ClusterFormationStrategy FormationStrategy { get; set; } = ClusterFormationStrategy.Manual;
    
    /// <summary>
    /// Whether load balancing is enabled for this cluster.
    /// </summary>
    public bool LoadBalancingEnabled { get; set; } = true;
    
    /// <summary>
    /// Whether automatic rebalancing is enabled.
    /// </summary>
    public bool AutoRebalancingEnabled { get; set; } = false;
    
    /// <summary>
    /// Interval in seconds for metrics collection.
    /// </summary>
    public int MetricsCollectionInterval { get; set; } = 30;
    
    /// <summary>
    /// Custom configuration properties.
    /// </summary>
    public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
}

/// <summary>
/// Arguments for cluster events.
/// </summary>
public class ClusterEventArgs : EventArgs
{
    /// <summary>
    /// The cluster ID.
    /// </summary>
    public string ClusterId { get; }
    
    /// <summary>
    /// The new cluster state.
    /// </summary>
    public ClusterState State { get; }
    
    /// <summary>
    /// Optional additional event data.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new instance of the ClusterEventArgs class.
    /// </summary>
    /// <param name="clusterId">The cluster ID.</param>
    /// <param name="state">The new cluster state.</param>
    /// <param name="data">Optional additional event data.</param>
    public ClusterEventArgs(string clusterId, ClusterState state, object? data = null)
    {
        ClusterId = clusterId ?? throw new ArgumentNullException(nameof(clusterId));
        State = state;
        Data = data;
    }
}

/// <summary>
/// Tier 2: Actor Cluster Manager service interface for managing and coordinating clusters of actors.
/// Provides clustering capabilities including formation, load balancing, failover, and performance
/// optimization for distributed AI agent systems within the game environment.
/// </summary>
public interface IActorClusterManager : IService
{
    /// <summary>
    /// Event raised when a cluster's state changes.
    /// </summary>
    event EventHandler<ClusterEventArgs>? ClusterStateChanged;

    /// <summary>
    /// Gets all active cluster IDs.
    /// </summary>
    IReadOnlyCollection<string> ActiveClusters { get; }

    /// <summary>
    /// Creates a new actor cluster with the specified configuration.
    /// </summary>
    /// <param name="clusterId">Unique identifier for the new cluster.</param>
    /// <param name="configuration">Configuration settings for the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster creation operation.</returns>
    Task CreateClusterAsync(string clusterId, ClusterConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dissolves an existing actor cluster.
    /// </summary>
    /// <param name="clusterId">The ID of the cluster to dissolve.</param>
    /// <param name="graceful">Whether to gracefully stop all actors before dissolving.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster dissolution operation.</returns>
    Task DissolveClusterAsync(string clusterId, bool graceful = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an actor to a cluster.
    /// </summary>
    /// <param name="actorId">The ID of the actor to add.</param>
    /// <param name="clusterId">The ID of the target cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async actor addition operation.</returns>
    Task AddActorToClusterAsync(string actorId, string clusterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an actor from its current cluster.
    /// </summary>
    /// <param name="actorId">The ID of the actor to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async actor removal operation.</returns>
    Task RemoveActorFromClusterAsync(string actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all actor IDs in a specific cluster.
    /// </summary>
    /// <param name="clusterId">The ID of the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns actor IDs.</returns>
    Task<IEnumerable<string>> GetClusterActorsAsync(string clusterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the cluster ID that contains the specified actor.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the cluster ID, or null if not clustered.</returns>
    Task<string?> GetActorClusterAsync(string actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for a specific cluster.
    /// </summary>
    /// <param name="clusterId">The ID of the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns cluster metrics.</returns>
    Task<ClusterMetrics> GetClusterMetricsAsync(string clusterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebalances actors across all clusters to optimize performance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async rebalancing operation.</returns>
    Task RebalanceClustersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Routes a message to the best actor in a cluster based on load balancing.
    /// </summary>
    /// <param name="clusterId">The ID of the target cluster.</param>
    /// <param name="message">The message to route.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async message routing operation.</returns>
    Task RouteMessageToClusterAsync(string clusterId, ActorMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a message to all actors in a cluster.
    /// </summary>
    /// <param name="clusterId">The ID of the target cluster.</param>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async broadcast operation.</returns>
    Task BroadcastToClusterAsync(string clusterId, ActorMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an actor with the cluster manager for tracking and management.
    /// </summary>
    /// <param name="actor">The actor to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterActorAsync(IActor actor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an actor from the cluster manager.
    /// </summary>
    /// <param name="actorId">The ID of the actor to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unregistration operation.</returns>
    Task UnregisterActorAsync(string actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current configuration of a cluster.
    /// </summary>
    /// <param name="clusterId">The ID of the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the cluster configuration.</returns>
    Task<ClusterConfiguration?> GetClusterConfigurationAsync(string clusterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the configuration of an existing cluster.
    /// </summary>
    /// <param name="clusterId">The ID of the cluster.</param>
    /// <param name="configuration">The new configuration settings.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration update operation.</returns>
    Task UpdateClusterConfigurationAsync(string clusterId, ClusterConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs health checks on all clusters and reports any issues.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async health check operation that returns a health report.</returns>
    Task<IDictionary<string, ClusterMetrics>> PerformHealthCheckAsync(CancellationToken cancellationToken = default);
}
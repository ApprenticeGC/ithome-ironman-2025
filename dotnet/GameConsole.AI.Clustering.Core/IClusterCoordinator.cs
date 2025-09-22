using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Clustering.Core;

/// <summary>
/// Interface for the AI agent actor clustering coordinator service.
/// Manages cluster membership, leader election, and coordination of AI agents.
/// </summary>
public interface IClusterCoordinator : IService
{
    /// <summary>
    /// Gets the current cluster configuration.
    /// </summary>
    ClusterConfiguration Configuration { get; }
    
    /// <summary>
    /// Gets the current state of the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the current cluster state.</returns>
    Task<ClusterState> GetClusterStateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Joins an existing cluster or forms a new one if none exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the join operation.</returns>
    Task JoinClusterAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Leaves the cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the leave operation.</returns>
    Task LeaveClusterAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event raised when cluster membership changes.
    /// </summary>
    event EventHandler<ClusterMembershipChangedEventArgs>? ClusterMembershipChanged;
}
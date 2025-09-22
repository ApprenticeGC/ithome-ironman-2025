using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Clustering.Core;

/// <summary>
/// Capability interface for leader election and coordination within the AI agent cluster.
/// Enables services to participate in leader election and coordinate distributed operations.
/// </summary>
public interface IClusterLeadershipCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets the current cluster leader.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the current leader, or null if no leader is elected.</returns>
    Task<ClusterMember?> GetLeaderAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the current node is the cluster leader.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the current node is the leader.</returns>
    Task<bool> IsLeaderAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers for leader election change notifications.
    /// </summary>
    /// <param name="callback">Callback to invoke when leadership changes.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the registration operation.</returns>
    Task RegisterLeadershipCallbackAsync(Func<ClusterMember?, ClusterMember?, Task> callback, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Attempts to coordinate a distributed operation across the cluster.
    /// Only succeeds if the current node is the leader.
    /// </summary>
    /// <param name="operation">The operation to coordinate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if coordination was successful.</returns>
    Task<bool> CoordinateOperationAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}
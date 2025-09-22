using Akka.Actor;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Clustering;

/// <summary>
/// Manages AI agent cluster coordination and membership.
/// Handles cluster formation, node discovery, and cluster-wide configuration.
/// </summary>
public interface IAIClusterManager : IService
{
    /// <summary>
    /// Gets the current cluster state information.
    /// </summary>
    Task<ClusterState> GetClusterStateAsync();
    
    /// <summary>
    /// Joins a node to the cluster.
    /// </summary>
    /// <param name="nodeAddress">Address of the node to join</param>
    Task JoinClusterAsync(Address nodeAddress);
    
    /// <summary>
    /// Leaves the cluster gracefully.
    /// </summary>
    Task LeaveClusterAsync();
    
    /// <summary>
    /// Gets the actor system for this cluster manager.
    /// </summary>
    ActorSystem ActorSystem { get; }
}

/// <summary>
/// Represents the current state of the AI cluster.
/// </summary>
public record ClusterState(
    IReadOnlyList<Address> Members,
    Address? Leader,
    bool IsHealthy,
    int TotalNodes
);
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Service responsible for coordinating AI agents across the cluster.
/// Manages cluster formation, membership, and distributed coordination.
/// </summary>
public interface IAIClusterCoordinatorService : IService
{
    /// <summary>
    /// Registers an AI agent with the cluster coordinator.
    /// </summary>
    /// <param name="agent">The AI agent to register.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the registration operation.</returns>
    Task RegisterAgentAsync(IClusterableAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the cluster coordinator.
    /// </summary>
    /// <param name="agentId">ID of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the unregistration operation.</returns>
    Task UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered AI agents in the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Collection of registered agents.</returns>
    Task<IEnumerable<IClusterableAIAgent>> GetRegisteredAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AI agents by their cluster role.
    /// </summary>
    /// <param name="role">The role to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Collection of agents with the specified role.</returns>
    Task<IEnumerable<IClusterableAIAgent>> GetAgentsByRoleAsync(string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates cluster formation with the specified configuration.
    /// </summary>
    /// <param name="config">Configuration for the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the cluster formation operation.</returns>
    Task InitiateClusterAsync(AIClusterConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the shutdown operation.</returns>
    Task ShutdownClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health status of the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Cluster health information.</returns>
    Task<ClusterHealthInfo> GetClusterHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the cluster topology changes (members join/leave).
    /// </summary>
    event EventHandler<ClusterTopologyChangedEventArgs>? TopologyChanged;
}

/// <summary>
/// Represents the health status of the AI agent cluster.
/// </summary>
public class ClusterHealthInfo
{
    /// <summary>
    /// Gets the total number of cluster members.
    /// </summary>
    public int TotalMembers { get; }

    /// <summary>
    /// Gets the number of active cluster members.
    /// </summary>
    public int ActiveMembers { get; }

    /// <summary>
    /// Gets the number of unreachable cluster members.
    /// </summary>
    public int UnreachableMembers { get; }

    /// <summary>
    /// Gets whether the cluster is healthy.
    /// </summary>
    public bool IsHealthy { get; }

    /// <summary>
    /// Gets the cluster leader information, if any.
    /// </summary>
    public ClusterMemberInfo? Leader { get; }

    /// <summary>
    /// Gets when the health status was last updated.
    /// </summary>
    public DateTime LastUpdated { get; }

    public ClusterHealthInfo(int totalMembers, int activeMembers, int unreachableMembers, 
        bool isHealthy, ClusterMemberInfo? leader, DateTime lastUpdated)
    {
        TotalMembers = totalMembers;
        ActiveMembers = activeMembers;
        UnreachableMembers = unreachableMembers;
        IsHealthy = isHealthy;
        Leader = leader;
        LastUpdated = lastUpdated;
    }
}

/// <summary>
/// Event arguments for cluster topology changes.
/// </summary>
public class ClusterTopologyChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of topology change that occurred.
    /// </summary>
    public ClusterTopologyChangeType ChangeType { get; }

    /// <summary>
    /// Gets the member that was affected by the change.
    /// </summary>
    public ClusterMemberInfo AffectedMember { get; }

    /// <summary>
    /// Gets the current cluster health after the change.
    /// </summary>
    public ClusterHealthInfo CurrentHealth { get; }

    public ClusterTopologyChangedEventArgs(ClusterTopologyChangeType changeType, 
        ClusterMemberInfo affectedMember, ClusterHealthInfo currentHealth)
    {
        ChangeType = changeType;
        AffectedMember = affectedMember;
        CurrentHealth = currentHealth;
    }
}

/// <summary>
/// Types of cluster topology changes.
/// </summary>
public enum ClusterTopologyChangeType
{
    /// <summary>A new member joined the cluster.</summary>
    MemberJoined,
    
    /// <summary>A member left the cluster gracefully.</summary>
    MemberLeft,
    
    /// <summary>A member was removed due to failure.</summary>
    MemberRemoved,
    
    /// <summary>A member became unreachable.</summary>
    MemberUnreachable,
    
    /// <summary>A previously unreachable member became reachable again.</summary>
    MemberReachable,
    
    /// <summary>Leadership changed in the cluster.</summary>
    LeadershipChanged
}
using GameConsole.Core.Abstractions;
using Akka.Actor;

namespace GameConsole.AI.Core;

/// <summary>
/// Represents an AI agent that can participate in actor clustering.
/// Provides capabilities for distributed coordination and cluster membership.
/// </summary>
public interface IClusterableAIAgent : ICapabilityProvider
{
    /// <summary>
    /// Gets the unique identifier for this AI agent within the cluster.
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Gets the agent's role within the cluster (e.g., "coordinator", "worker", "analyzer").
    /// </summary>
    string ClusterRole { get; }

    /// <summary>
    /// Gets the current cluster membership status of this agent.
    /// </summary>
    ClusterMembershipStatus Status { get; }

    /// <summary>
    /// Event raised when the agent's cluster membership status changes.
    /// </summary>
    event EventHandler<ClusterMembershipChangedEventArgs>? MembershipChanged;

    /// <summary>
    /// Joins the AI agent cluster with the specified configuration.
    /// </summary>
    /// <param name="clusterConfig">Configuration for cluster membership.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the join operation.</returns>
    Task JoinClusterAsync(AIClusterConfiguration clusterConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves the AI agent cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the leave operation.</returns>
    Task LeaveClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to another AI agent in the cluster.
    /// </summary>
    /// <param name="targetAgentId">ID of the target AI agent.</param>
    /// <param name="message">Message to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the message sending operation.</returns>
    Task SendMessageAsync(string targetAgentId, object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a message to all AI agents in the cluster with the specified role.
    /// </summary>
    /// <param name="role">Target role to broadcast to.</param>
    /// <param name="message">Message to broadcast.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the broadcast operation.</returns>
    Task BroadcastToRoleAsync(string role, object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all active AI agents in the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Collection of active cluster members.</returns>
    Task<IEnumerable<ClusterMemberInfo>> GetClusterMembersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the cluster membership status of an AI agent.
/// </summary>
public enum ClusterMembershipStatus
{
    /// <summary>The agent is not connected to the cluster.</summary>
    Disconnected,
    
    /// <summary>The agent is attempting to join the cluster.</summary>
    Joining,
    
    /// <summary>The agent is an active member of the cluster.</summary>
    Active,
    
    /// <summary>The agent is leaving the cluster.</summary>
    Leaving,
    
    /// <summary>The agent has been removed from the cluster due to failure.</summary>
    Removed
}

/// <summary>
/// Configuration for AI agent cluster membership.
/// </summary>
public class AIClusterConfiguration
{
    /// <summary>
    /// Gets or sets the cluster seed nodes for initial connection.
    /// </summary>
    public IEnumerable<string> SeedNodes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the port for cluster communication.
    /// </summary>
    public int Port { get; set; } = 8080;

    /// <summary>
    /// Gets or sets the hostname for this node.
    /// </summary>
    public string Hostname { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the cluster name.
    /// </summary>
    public string ClusterName { get; set; } = "ai-agents";

    /// <summary>
    /// Gets or sets the heartbeat interval for cluster health monitoring.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Event arguments for cluster membership changes.
/// </summary>
public class ClusterMembershipChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous membership status.
    /// </summary>
    public ClusterMembershipStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the current membership status.
    /// </summary>
    public ClusterMembershipStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the timestamp when the change occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    public ClusterMembershipChangedEventArgs(ClusterMembershipStatus previousStatus, ClusterMembershipStatus currentStatus)
    {
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Information about a cluster member.
/// </summary>
public class ClusterMemberInfo
{
    /// <summary>
    /// Gets the unique identifier of the cluster member.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the role of the cluster member.
    /// </summary>
    public string Role { get; }

    /// <summary>
    /// Gets the address of the cluster member.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Gets the current status of the cluster member.
    /// </summary>
    public ClusterMembershipStatus Status { get; }

    /// <summary>
    /// Gets when the member joined the cluster.
    /// </summary>
    public DateTime JoinedAt { get; }

    public ClusterMemberInfo(string agentId, string role, string address, ClusterMembershipStatus status, DateTime joinedAt)
    {
        AgentId = agentId;
        Role = role;
        Address = address;
        Status = status;
        JoinedAt = joinedAt;
    }
}
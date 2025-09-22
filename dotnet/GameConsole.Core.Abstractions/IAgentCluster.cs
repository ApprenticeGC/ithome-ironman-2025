using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Cluster health status indicators.
/// </summary>
public enum ClusterHealth
{
    /// <summary>
    /// Cluster is healthy and fully operational.
    /// </summary>
    Healthy,
    
    /// <summary>
    /// Cluster is degraded but still functional.
    /// </summary>
    Degraded,
    
    /// <summary>
    /// Cluster is unhealthy and may have limited functionality.
    /// </summary>
    Unhealthy,
    
    /// <summary>
    /// Cluster is offline and not functional.
    /// </summary>
    Offline
}

/// <summary>
/// Arguments for cluster-related events.
/// </summary>
public class ClusterEventArgs : EventArgs
{
    /// <summary>
    /// The identifier of the cluster.
    /// </summary>
    public string ClusterId { get; }
    
    /// <summary>
    /// The current health status of the cluster.
    /// </summary>
    public ClusterHealth Health { get; }
    
    /// <summary>
    /// Optional additional data about the cluster event.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new instance of the ClusterEventArgs class.
    /// </summary>
    /// <param name="clusterId">The identifier of the cluster.</param>
    /// <param name="health">The current health status of the cluster.</param>
    /// <param name="data">Optional additional data about the cluster event.</param>
    public ClusterEventArgs(string clusterId, ClusterHealth health, object? data = null)
    {
        ClusterId = clusterId ?? throw new ArgumentNullException(nameof(clusterId));
        Health = health;
        Data = data;
    }
}

/// <summary>
/// Tier 1: Core abstraction for AI agent clustering.
/// Defines the contract for managing distributed groups of AI agents,
/// handling cluster membership, load balancing, and fault tolerance.
/// </summary>
public interface IAgentCluster : IService
{
    /// <summary>
    /// Event raised when an agent joins the cluster.
    /// </summary>
    event EventHandler<AgentEventArgs>? AgentJoined;
    
    /// <summary>
    /// Event raised when an agent leaves the cluster.
    /// </summary>
    event EventHandler<AgentEventArgs>? AgentLeft;
    
    /// <summary>
    /// Event raised when the cluster's health status changes.
    /// </summary>
    event EventHandler<ClusterEventArgs>? HealthChanged;
    
    /// <summary>
    /// Event raised when cluster membership changes.
    /// </summary>
    event EventHandler<ClusterEventArgs>? MembershipChanged;

    /// <summary>
    /// Gets the unique identifier for this cluster.
    /// </summary>
    string ClusterId { get; }
    
    /// <summary>
    /// Gets the current health status of the cluster.
    /// </summary>
    ClusterHealth Health { get; }
    
    /// <summary>
    /// Gets the total number of active agents in the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the agent count.</returns>
    Task<int> GetAgentCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all agent identifiers currently in the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns agent identifiers.</returns>
    Task<IEnumerable<string>> GetAgentIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agent identifiers of a specific type in the cluster.
    /// </summary>
    /// <param name="agentType">The type of agents to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns matching agent identifiers.</returns>
    Task<IEnumerable<string>> GetAgentsByTypeAsync(string agentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an agent to the cluster.
    /// </summary>
    /// <param name="agent">The agent to add to the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async agent addition operation.</returns>
    Task AddAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an agent from the cluster.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async agent removal operation.</returns>
    Task RemoveAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Distributes a task to the best available agent in the cluster.
    /// </summary>
    /// <param name="task">The task to distribute.</param>
    /// <param name="agentType">Optional agent type filter for task assignment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the task result and executing agent ID.</returns>
    Task<(object? result, string agentId)> DistributeTaskAsync(object task, string? agentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a message to all agents in the cluster.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="agentType">Optional agent type filter for message targeting.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async broadcast operation.</returns>
    Task BroadcastMessageAsync(object message, string? agentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an agent exists in the cluster.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the agent exists.</returns>
    Task<bool> HasAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about an agent in the cluster.
    /// </summary>
    /// <param name="agentId">The identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the agent instance, or null if not found.</returns>
    Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);
}
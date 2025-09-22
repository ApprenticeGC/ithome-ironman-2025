using GameConsole.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameConsole.Engine.Core;

/// <summary>
/// Load balancing strategies for task distribution.
/// </summary>
public enum LoadBalancingStrategy
{
    /// <summary>
    /// Distribute tasks in round-robin fashion.
    /// </summary>
    RoundRobin,
    
    /// <summary>
    /// Distribute tasks based on agent load.
    /// </summary>
    LoadBased,
    
    /// <summary>
    /// Distribute tasks randomly.
    /// </summary>
    Random,
    
    /// <summary>
    /// Distribute tasks to the least recently used agent.
    /// </summary>
    LeastRecentlyUsed
}

/// <summary>
/// Configuration options for agent management.
/// </summary>
public class AgentManagementConfiguration
{
    /// <summary>
    /// Maximum number of agents allowed per cluster.
    /// </summary>
    public int MaxAgentsPerCluster { get; set; } = 100;
    
    /// <summary>
    /// Default load balancing strategy.
    /// </summary>
    public LoadBalancingStrategy DefaultLoadBalancingStrategy { get; set; } = LoadBalancingStrategy.RoundRobin;
    
    /// <summary>
    /// Timeout for agent operations in milliseconds.
    /// </summary>
    public int AgentOperationTimeoutMs { get; set; } = 30000;
    
    /// <summary>
    /// Interval for health checks in milliseconds.
    /// </summary>
    public int HealthCheckIntervalMs { get; set; } = 10000;
    
    /// <summary>
    /// Whether to automatically restart failed agents.
    /// </summary>
    public bool AutoRestartFailedAgents { get; set; } = true;
}

/// <summary>
/// Tier 2: Agent management service interface for coordinating AI agent lifecycle
/// and cluster operations. Acts as a mechanical proxy that delegates to the
/// underlying actor system while providing game-specific orchestration.
/// </summary>
public interface IAgentManager : IService
{
    /// <summary>
    /// Event raised when agent management configuration changes.
    /// </summary>
    event EventHandler<EventArgs>? ConfigurationChanged;
    
    /// <summary>
    /// Event raised when an agent operation completes.
    /// </summary>
    event EventHandler<AgentEventArgs>? OperationCompleted;
    
    /// <summary>
    /// Event raised when cluster topology changes.
    /// </summary>
    event EventHandler<ClusterEventArgs>? TopologyChanged;

    /// <summary>
    /// Gets the current agent management configuration.
    /// </summary>
    AgentManagementConfiguration Configuration { get; }

    /// <summary>
    /// Sets the underlying actor system for agent management.
    /// </summary>
    /// <param name="actorSystem">The actor system to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetActorSystemAsync(IActorSystem actorSystem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the agent management configuration.
    /// </summary>
    /// <param name="configuration">The new configuration to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration update operation.</returns>
    Task UpdateConfigurationAsync(AgentManagementConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and starts a new agent cluster with the specified configuration.
    /// </summary>
    /// <param name="clusterId">The unique identifier for the cluster.</param>
    /// <param name="loadBalancingStrategy">The load balancing strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster creation operation.</returns>
    Task CreateClusterAsync(string clusterId, LoadBalancingStrategy loadBalancingStrategy = LoadBalancingStrategy.RoundRobin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys an agent cluster and stops all its agents.
    /// </summary>
    /// <param name="clusterId">The identifier of the cluster to destroy.</param>
    /// <param name="graceful">Whether to perform graceful shutdown of agents.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster destruction operation.</returns>
    Task DestroyClusterAsync(string clusterId, bool graceful = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Spawns a new agent in the specified cluster.
    /// </summary>
    /// <param name="clusterId">The identifier of the target cluster.</param>
    /// <param name="agentType">The type of agent to spawn.</param>
    /// <param name="agentId">Optional specific identifier for the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async agent spawning operation that returns the agent ID.</returns>
    Task<string> SpawnAgentAsync(string clusterId, string agentType, string? agentId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Terminates an agent and removes it from its cluster.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to terminate.</param>
    /// <param name="graceful">Whether to perform graceful shutdown.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async agent termination operation.</returns>
    Task TerminateAgentAsync(string agentId, bool graceful = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a task for processing by any available agent in the specified cluster.
    /// </summary>
    /// <param name="clusterId">The identifier of the target cluster.</param>
    /// <param name="task">The task to process.</param>
    /// <param name="agentType">Optional agent type filter.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the processing result and agent ID.</returns>
    Task<(object? result, string agentId)> SubmitTaskAsync(string clusterId, object task, string? agentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of all clusters managed by this service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns cluster status information.</returns>
    Task<Dictionary<string, ClusterHealth>> GetClusterStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed metrics about agent performance and cluster health.
    /// </summary>
    /// <param name="clusterId">Optional cluster ID to filter metrics. If null, returns system-wide metrics.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns performance metrics.</returns>
    Task<Dictionary<string, object>> GetMetricsAsync(string? clusterId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs health checks on all managed clusters and agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async health check operation.</returns>
    Task PerformHealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebalances agents across clusters based on current load.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async rebalancing operation.</returns>
    Task RebalanceAsync(CancellationToken cancellationToken = default);
}
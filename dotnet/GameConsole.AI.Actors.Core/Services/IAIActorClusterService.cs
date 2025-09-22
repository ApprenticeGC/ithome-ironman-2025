using Akka.Actor;
using GameConsole.Core.Abstractions;
using GameConsole.AI.Actors.Core.Messages;

namespace GameConsole.AI.Actors.Core.Services;

/// <summary>
/// Service interface for managing clustered AI agents.
/// Provides capabilities for distributed agent management, load balancing, and fault tolerance.
/// </summary>
public interface IAIActorClusterService : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the Akka.NET actor system used for clustering.
    /// </summary>
    ActorSystem ActorSystem { get; }
    
    /// <summary>
    /// Gets information about the current cluster state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns cluster state information.</returns>
    Task<ClusterStateResponse> GetClusterStateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts an AI agent with the specified configuration.
    /// The agent will be deployed to an appropriate cluster node based on load balancing.
    /// </summary>
    /// <param name="agentType">The type of AI agent to start.</param>
    /// <param name="config">Configuration parameters for the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns agent startup information.</returns>
    Task<AgentStarted> StartAgentAsync(string agentType, AgentConfig config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops a specific AI agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to stop.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns agent stop information.</returns>
    Task<AgentStopped> StopAgentAsync(string agentId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a processing request to an AI agent.
    /// The request will be routed to an appropriate agent instance based on load balancing.
    /// </summary>
    /// <param name="request">The processing request to send.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the processing response.</returns>
    Task<ProcessResponse> ProcessRequestAsync(ProcessRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Triggers rebalancing of agents across cluster nodes.
    /// This redistributes agents to optimize load distribution and resource utilization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns rebalancing results.</returns>
    Task<RebalanceCompleted> RebalanceAgentsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets health information for all registered AI backends.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns backend health information.</returns>
    Task<IEnumerable<BackendHealthResponse>> GetBackendHealthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a list of all active AI agents in the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns active agent information.</returns>
    Task<IEnumerable<AgentInfo>> GetActiveAgentsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about an active AI agent in the cluster.
/// </summary>
/// <param name="AgentId">The unique identifier of the agent.</param>
/// <param name="AgentType">The type of AI agent.</param>
/// <param name="NodeAddress">The cluster node address where the agent is running.</param>
/// <param name="Status">The current status of the agent.</param>
/// <param name="LastActivity">The timestamp of the last activity.</param>
/// <param name="ActiveRequests">The number of currently active requests.</param>
public record AgentInfo(
    string AgentId,
    string AgentType,
    string NodeAddress,
    string Status,
    DateTime LastActivity,
    int ActiveRequests);
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Interface for AI agent clustering services.
    /// Manages the clustering of AI agents across multiple nodes for scalability and fault tolerance.
    /// </summary>
    public interface IAIAgentCluster : IService
    {
        /// <summary>
        /// Gets information about the current cluster state.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>A task representing the async operation that returns cluster information.</returns>
        Task<ClusterInfo> GetClusterInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers an AI agent with the cluster.
        /// </summary>
        /// <param name="agent">The AI agent to register.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>A task representing the async operation.</returns>
        Task RegisterAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unregisters an AI agent from the cluster.
        /// </summary>
        /// <param name="agentId">The ID of the agent to unregister.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>A task representing the async operation.</returns>
        Task UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Routes a message to an appropriate AI agent in the cluster.
        /// </summary>
        /// <param name="message">The message to route.</param>
        /// <param name="routingStrategy">Optional routing strategy to use.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>A task representing the async operation that returns the agent's response.</returns>
        Task<AIAgentResponse> RouteMessageAsync(
            AIAgentMessage message, 
            string? routingStrategy = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all active agents in the cluster.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>A task representing the async operation that returns information about active agents.</returns>
        Task<IEnumerable<AgentInfo>> GetActiveAgentsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Event raised when the cluster membership changes.
        /// </summary>
        event EventHandler<ClusterMembershipChangedEventArgs>? ClusterMembershipChanged;
    }
}
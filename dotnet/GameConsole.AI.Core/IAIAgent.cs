using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Base interface for AI agents in the GameConsole system.
    /// Defines the fundamental operations that all AI agents must support.
    /// </summary>
    public interface IAIAgent
    {
        /// <summary>
        /// Unique identifier for this AI agent.
        /// </summary>
        string AgentId { get; }

        /// <summary>
        /// Human-readable name for this AI agent.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Current state of the AI agent.
        /// </summary>
        AIAgentState State { get; }

        /// <summary>
        /// Processes a message or command for this AI agent.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>The agent's response to the message.</returns>
        Task<AIAgentResponse> ProcessMessageAsync(AIAgentMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current status and metadata of the AI agent.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>Current agent status information.</returns>
        Task<AIAgentStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Capability interface for AI agents that support clustering operations.
    /// </summary>
    public interface IAIClusteringCapability : ICapabilityProvider
    {
        /// <summary>
        /// Joins this agent to a cluster.
        /// </summary>
        /// <param name="clusterAddress">Address of the cluster to join.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>Task representing the join operation.</returns>
        Task JoinClusterAsync(string clusterAddress, CancellationToken cancellationToken = default);

        /// <summary>
        /// Leaves the current cluster.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>Task representing the leave operation.</returns>
        Task LeaveClusterAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about the current cluster this agent belongs to.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>Current cluster information, or null if not in a cluster.</returns>
        Task<AIClusterInfo?> GetClusterInfoAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Discovers other AI agents in the cluster.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>List of discovered agents in the cluster.</returns>
        Task<IEnumerable<AIAgentInfo>> DiscoverClusterAgentsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Capability interface for AI agents that support distributed coordination.
    /// </summary>
    public interface IAICoordinationCapability : ICapabilityProvider
    {
        /// <summary>
        /// Coordinates with other agents to execute a distributed task.
        /// </summary>
        /// <param name="task">The distributed task to coordinate.</param>
        /// <param name="participatingAgents">List of agents to coordinate with.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>Result of the coordinated task execution.</returns>
        Task<AICoordinationResult> CoordinateTaskAsync(AIDistributedTask task, IEnumerable<string> participatingAgents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Elects a leader among a group of agents for task coordination.
        /// </summary>
        /// <param name="candidateAgents">Agents eligible for leadership.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>The elected leader agent ID.</returns>
        Task<string> ElectLeaderAsync(IEnumerable<string> candidateAgents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Synchronizes state across multiple agents in the cluster.
        /// </summary>
        /// <param name="stateData">State data to synchronize.</param>
        /// <param name="targetAgents">Agents to synchronize with.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>Task representing the synchronization operation.</returns>
        Task SynchronizeStateAsync(AIAgentState stateData, IEnumerable<string> targetAgents, CancellationToken cancellationToken = default);
    }
}
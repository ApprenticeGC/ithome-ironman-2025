using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Service interface for AI agent orchestration and management.
    /// Provides centralized coordination for multiple AI agents.
    /// </summary>
    public interface IAIOrchestrationService : IService
    {
        /// <summary>
        /// Creates and registers a new AI agent in the orchestration system.
        /// </summary>
        /// <param name="agentType">Type of AI agent to create.</param>
        /// <param name="configuration">Configuration for the new agent.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>The created AI agent.</returns>
        Task<IAIAgent> CreateAgentAsync(string agentType, Dictionary<string, object> configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an AI agent by its ID.
        /// </summary>
        /// <param name="agentId">ID of the agent to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>The AI agent, or null if not found.</returns>
        Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all currently registered AI agents.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>List of all AI agents.</returns>
        Task<IEnumerable<IAIAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an AI agent from the orchestration system.
        /// </summary>
        /// <param name="agentId">ID of the agent to remove.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>True if the agent was removed successfully.</returns>
        Task<bool> RemoveAgentAsync(string agentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Routes a message to the appropriate AI agent(s) for processing.
        /// </summary>
        /// <param name="message">Message to route.</param>
        /// <param name="targetAgentId">Specific agent to route to, or null for automatic routing.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>Response from the target agent(s).</returns>
        Task<IEnumerable<AIAgentResponse>> RouteMessageAsync(AIAgentMessage message, string? targetAgentId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current health status of the orchestration system.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>Health status information.</returns>
        Task<OrchestrationHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Health status information for the AI orchestration system.
    /// </summary>
    public class OrchestrationHealthStatus
    {
        public bool IsHealthy { get; }
        public int TotalAgents { get; }
        public int ActiveAgents { get; }
        public int ErroredAgents { get; }
        public TimeSpan SystemUptime { get; }
        public Dictionary<string, object> SystemMetrics { get; }

        public OrchestrationHealthStatus(bool isHealthy, int totalAgents, int activeAgents, int erroredAgents, TimeSpan systemUptime, Dictionary<string, object> systemMetrics)
        {
            IsHealthy = isHealthy;
            TotalAgents = totalAgents;
            ActiveAgents = activeAgents;
            ErroredAgents = erroredAgents;
            SystemUptime = systemUptime;
            SystemMetrics = systemMetrics;
        }
    }
}
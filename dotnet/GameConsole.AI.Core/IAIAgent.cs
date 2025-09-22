using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Base interface for AI agents in the system.
    /// Represents an individual AI agent that can process messages and execute tasks.
    /// </summary>
    public interface IAIAgent : ICapabilityProvider
    {
        /// <summary>
        /// Gets the unique identifier for this AI agent.
        /// </summary>
        string AgentId { get; }

        /// <summary>
        /// Gets the type/category of this AI agent (e.g., "ChatBot", "TaskExecutor", "Analyzer").
        /// </summary>
        string AgentType { get; }

        /// <summary>
        /// Gets a value indicating whether this agent is currently active and ready to process messages.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Processes a message asynchronously and returns a response.
        /// </summary>
        /// <param name="message">The message to process.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>A task representing the async operation that returns the agent's response.</returns>
        Task<AIAgentResponse> ProcessMessageAsync(AIAgentMessage message, CancellationToken cancellationToken = default);

        /// <summary>
        /// Activates the AI agent, making it ready to process messages.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>A task representing the async operation.</returns>
        Task ActivateAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates the AI agent, stopping message processing.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>A task representing the async operation.</returns>
        Task DeactivateAsync(CancellationToken cancellationToken = default);
    }
}
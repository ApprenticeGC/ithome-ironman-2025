namespace GameConsole.Core.Abstractions;

/// <summary>
/// Base interface for AI agents in the GameConsole system.
/// AI agents are specialized services that provide artificial intelligence capabilities
/// and can be discovered, registered, and managed dynamically.
/// </summary>
public interface IAIAgent : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the unique identifier for this AI agent.
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Gets the type or category of this AI agent (e.g., "ChatBot", "Orchestrator", "Assistant").
    /// </summary>
    string AgentType { get; }

    /// <summary>
    /// Gets the priority level for this agent when multiple agents of the same type are available.
    /// Higher values indicate higher priority.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if this agent can handle a specific request or task.
    /// </summary>
    /// <param name="request">The request or task description.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent can handle the request.</returns>
    Task<bool> CanHandleAsync(object request, CancellationToken cancellationToken = default);
}
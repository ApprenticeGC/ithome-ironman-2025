using GameConsole.Plugins.Core;

namespace GameConsole.AI.Core;

/// <summary>
/// Defines the interface for AI agents in the GameConsole system.
/// Extends the plugin interface with AI-specific capabilities and behaviors.
/// </summary>
public interface IAIAgent : IPlugin
{
    /// <summary>
    /// Gets the AI agent's capabilities and skills.
    /// This provides information about what the agent can do and how it can assist.
    /// </summary>
    IAIAgentCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the current status of the AI agent.
    /// </summary>
    AIAgentStatus Status { get; }

    /// <summary>
    /// Processes a request and returns a response asynchronously.
    /// This is the main interaction point for communicating with the AI agent.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the agent's response.</returns>
    Task<IAIAgentResponse> ProcessRequestAsync(IAIAgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the agent can handle a specific type of request.
    /// </summary>
    /// <param name="requestType">The type of request to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent can handle the request type.</returns>
    Task<bool> CanHandleRequestAsync(Type requestType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the agent's priority for handling a specific request.
    /// Higher values indicate higher priority.
    /// </summary>
    /// <param name="request">The request to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the priority score (0-100).</returns>
    Task<int> GetPriorityAsync(IAIAgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the agent's status changes.
    /// </summary>
    event EventHandler<AIAgentStatusChangedEventArgs>? StatusChanged;
}
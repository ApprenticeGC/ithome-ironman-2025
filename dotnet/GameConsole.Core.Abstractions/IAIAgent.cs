namespace GameConsole.Core.Abstractions;

/// <summary>
/// Base interface for AI agents in the GameConsole system.
/// AI agents are specialized services that provide intelligent behavior and decision-making capabilities.
/// </summary>
public interface IAIAgent : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the unique identifier for this AI agent.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the name of the AI agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the AI agent implementation.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets a description of what this AI agent does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the categories or tags associated with this AI agent.
    /// Used for classification and discovery.
    /// </summary>
    IReadOnlyList<string> Categories { get; }

    /// <summary>
    /// Gets the current status of the AI agent.
    /// </summary>
    AIAgentStatus Status { get; }

    /// <summary>
    /// Executes an action asynchronously using the AI agent.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the response.</returns>
    Task<IAIAgentResponse> ExecuteAsync(IAIAgentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the possible states of an AI agent.
/// </summary>
public enum AIAgentStatus
{
    /// <summary>
    /// The agent is not yet initialized.
    /// </summary>
    NotInitialized,

    /// <summary>
    /// The agent is initializing.
    /// </summary>
    Initializing,

    /// <summary>
    /// The agent is ready to accept requests.
    /// </summary>
    Ready,

    /// <summary>
    /// The agent is currently processing a request.
    /// </summary>
    Processing,

    /// <summary>
    /// The agent encountered an error and is in a faulted state.
    /// </summary>
    Faulted,

    /// <summary>
    /// The agent is being disposed.
    /// </summary>
    Disposing,

    /// <summary>
    /// The agent has been disposed.
    /// </summary>
    Disposed
}
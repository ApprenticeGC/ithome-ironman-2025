namespace GameConsole.Core.Abstractions;

/// <summary>
/// Defines the base interface for AI agents in the GameConsole system.
/// AI agents are specialized services that provide intelligent behavior capabilities
/// and can be discovered and registered within the system.
/// </summary>
public interface IAIAgent : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the unique identifier for this AI agent.
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Gets the display name of the AI agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the AI agent.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets a description of what this AI agent does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the priority of this agent when multiple agents can handle the same capability.
    /// Higher values indicate higher priority.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Processes a request using the AI agent's capabilities.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to process.</typeparam>
    /// <typeparam name="TResponse">The type of response to return.</typeparam>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the response.</returns>
    Task<TResponse> ProcessAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// Checks if the agent can handle a specific request type.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to check.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent can handle the request type.</returns>
    Task<bool> CanHandleAsync<TRequest>(CancellationToken cancellationToken = default)
        where TRequest : class;
}
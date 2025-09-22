namespace GameConsole.Plugins.Core;

/// <summary>
/// Interface for AI agents in the GameConsole system.
/// Extends the base <see cref="IPlugin"/> interface with AI-specific functionality
/// including capability discovery and request handling.
/// </summary>
public interface IAIAgent : IPlugin
{
    /// <summary>
    /// Gets all AI-specific capabilities provided by this agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of AI capabilities this agent supports.</returns>
    Task<IEnumerable<IAIAgentCapability>> GetAICapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether this AI agent can handle a specific type of request.
    /// This allows for dynamic request routing based on agent capabilities and current state.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to check.</typeparam>
    /// <param name="request">The request instance to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent can handle the request, false otherwise.</returns>
    Task<bool> CanHandleRequestAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an AI request and returns the result.
    /// This method should only be called after <see cref="CanHandleRequestAsync{TRequest}"/> returns true.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to process.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the processed response.</returns>
    Task<TResponse> ProcessRequestAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default);
}
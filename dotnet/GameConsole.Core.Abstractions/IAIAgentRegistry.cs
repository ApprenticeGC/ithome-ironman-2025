namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for AI agent registry that manages discovery, registration, and lifecycle of AI agents.
/// Provides centralized access to all available AI agents in the system.
/// </summary>
public interface IAIAgentRegistry : IService
{
    /// <summary>
    /// Registers an AI agent with the registry.
    /// </summary>
    /// <param name="agent">The AI agent to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the registry.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unregistration operation.</returns>
    Task UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of all registered AI agents.</returns>
    Task<IEnumerable<IAIAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an AI agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the AI agent if found, or null if not found.</returns>
    Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents that can handle a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to discover agents for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of agents that provide the specified capability, ordered by priority.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverAgentsByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default)
        where TCapability : class;

    /// <summary>
    /// Discovers AI agents that can handle a specific request type.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to discover agents for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of agents that can handle the specified request type, ordered by priority.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverAgentsByRequestAsync<TRequest>(CancellationToken cancellationToken = default)
        where TRequest : class;

    /// <summary>
    /// Gets the best AI agent for handling a specific capability based on priority and availability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to find the best agent for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the best AI agent for the capability, or null if none are available.</returns>
    Task<IAIAgent?> GetBestAgentForCapabilityAsync<TCapability>(CancellationToken cancellationToken = default)
        where TCapability : class;

    /// <summary>
    /// Gets the best AI agent for handling a specific request type based on priority and availability.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to find the best agent for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the best AI agent for the request type, or null if none are available.</returns>
    Task<IAIAgent?> GetBestAgentForRequestAsync<TRequest>(CancellationToken cancellationToken = default)
        where TRequest : class;

    /// <summary>
    /// Event raised when an AI agent is registered.
    /// </summary>
    event EventHandler<IAIAgent>? AgentRegistered;

    /// <summary>
    /// Event raised when an AI agent is unregistered.
    /// </summary>
    event EventHandler<string>? AgentUnregistered;
}
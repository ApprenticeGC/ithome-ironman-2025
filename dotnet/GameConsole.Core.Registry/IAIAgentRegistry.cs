using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Registry;

/// <summary>
/// Registry for discovering and managing AI agents.
/// Provides methods for agent registration, discovery, and capability-based lookup.
/// </summary>
public interface IAIAgentRegistry
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
    /// <returns>A task that returns true if the agent was successfully unregistered.</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of all registered AI agents.</returns>
    Task<IEnumerable<IAIAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AI agents by type or category.
    /// </summary>
    /// <param name="agentType">The type or category of agents to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of agents of the specified type.</returns>
    Task<IEnumerable<IAIAgent>> GetAgentsByTypeAsync(string agentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an AI agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the agent if found, null otherwise.</returns>
    Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds AI agents that can handle a specific request or task.
    /// </summary>
    /// <param name="request">The request or task description.</param>
    /// <param name="agentType">Optional agent type filter.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of agents that can handle the request, ordered by priority.</returns>
    Task<IEnumerable<IAIAgent>> FindCapableAgentsAsync(object request, string? agentType = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds AI agents that provide a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to search for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of agents that provide the specified capability.</returns>
    Task<IEnumerable<IAIAgent>> FindAgentsByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an AI agent is registered.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent is registered.</returns>
    Task<bool> IsRegisteredAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans an assembly for types decorated with <see cref="AIAgentAttribute"/> and registers them.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="serviceRegistry">The service registry to use for dependency injection.</param>
    /// <param name="agentTypes">Optional agent types to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async scan operation.</returns>
    Task ScanAndRegisterAsync(System.Reflection.Assembly assembly, IServiceRegistry serviceRegistry, string[]? agentTypes = null, CancellationToken cancellationToken = default);
}
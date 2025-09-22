using GameConsole.Plugins.Core;

namespace GameConsole.Core.Registry;

/// <summary>
/// Interface for registering and discovering AI agents in the GameConsole system.
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
    /// <returns>A task that returns true if the agent was unregistered, false if not found.</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers all registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of all registered AI agents.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds AI agents that support a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to search for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns AI agents that support the specified capability.</returns>
    Task<IEnumerable<IAIAgent>> FindAgentsByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default)
        where TCapability : IAIAgentCapability;

    /// <summary>
    /// Finds AI agents that support a capability by name.
    /// </summary>
    /// <param name="capabilityName">The name of the capability to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns AI agents that support the specified capability.</returns>
    Task<IEnumerable<IAIAgent>> FindAgentsByCapabilityNameAsync(string capabilityName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific AI agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the AI agent if found, or null if not found.</returns>
    Task<IAIAgent?> GetAgentByIdAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered AI agents that are currently running.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of running AI agents.</returns>
    Task<IEnumerable<IAIAgent>> GetRunningAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans an assembly for AI agent types decorated with <see cref="AIAgentAttribute"/> and registers them.
    /// </summary>
    /// <param name="assembly">The assembly to scan for AI agents.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the number of AI agents discovered and registered.</returns>
    Task<int> RegisterFromAssemblyAsync(System.Reflection.Assembly assembly, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the count of registered AI agents.</returns>
    Task<int> GetRegisteredAgentCountAsync(CancellationToken cancellationToken = default);
}
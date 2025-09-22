namespace GameConsole.AI.Core;

/// <summary>
/// Defines the interface for discovering and registering AI agents in the GameConsole system.
/// </summary>
public interface IAIAgentRegistry
{
    /// <summary>
    /// Discovers AI agents from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for AI agents.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async discovery operation with the found agents.</returns>
    Task<IReadOnlyList<IAIAgentDescriptor>> DiscoverAgentsAsync(System.Reflection.Assembly assembly, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents from all loaded assemblies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async discovery operation with all found agents.</returns>
    Task<IReadOnlyList<IAIAgentDescriptor>> DiscoverAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an AI agent in the registry.
    /// </summary>
    /// <param name="descriptor">The agent descriptor to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterAgentAsync(IAIAgentDescriptor descriptor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the registry.
    /// </summary>
    /// <param name="agentId">The ID of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unregistration operation with success status.</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered AI agents.
    /// </summary>
    /// <returns>A read-only list of all registered agent descriptors.</returns>
    IReadOnlyList<IAIAgentDescriptor> GetRegisteredAgents();

    /// <summary>
    /// Finds AI agents by their capabilities.
    /// </summary>
    /// <param name="capabilities">The capabilities to search for.</param>
    /// <returns>A read-only list of agent descriptors that provide the specified capabilities.</returns>
    IReadOnlyList<IAIAgentDescriptor> FindAgentsByCapabilities(params string[] capabilities);

    /// <summary>
    /// Finds AI agents by their type.
    /// </summary>
    /// <param name="agentType">The agent type to search for.</param>
    /// <returns>A read-only list of agent descriptors of the specified type.</returns>
    IReadOnlyList<IAIAgentDescriptor> FindAgentsByType(string agentType);

    /// <summary>
    /// Gets a specific AI agent by its ID.
    /// </summary>
    /// <param name="agentId">The ID of the agent to retrieve.</param>
    /// <returns>The agent descriptor if found, null otherwise.</returns>
    IAIAgentDescriptor? GetAgent(string agentId);

    /// <summary>
    /// Checks if an AI agent is registered.
    /// </summary>
    /// <param name="agentId">The ID of the agent to check.</param>
    /// <returns>True if the agent is registered, false otherwise.</returns>
    bool IsAgentRegistered(string agentId);

    /// <summary>
    /// Creates and initializes an instance of an AI agent.
    /// </summary>
    /// <param name="agentId">The ID of the agent to create.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async creation operation with the agent instance.</returns>
    Task<IAIAgent?> CreateAgentAsync(string agentId, CancellationToken cancellationToken = default);
}
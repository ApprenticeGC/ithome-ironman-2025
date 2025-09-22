using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Registry;

/// <summary>
/// Interface for registering and managing AI agents within the GameConsole system.
/// Provides lifecycle management and dependency injection support for AI agents.
/// </summary>
public interface IAIAgentRegistry
{
    /// <summary>
    /// Registers an AI agent descriptor for later instantiation.
    /// </summary>
    /// <param name="descriptor">The AI agent descriptor to register.</param>
    /// <returns>True if the agent was registered successfully, false if already exists.</returns>
    bool RegisterAgent(AIAgentDescriptor descriptor);

    /// <summary>
    /// Registers multiple AI agent descriptors.
    /// </summary>
    /// <param name="descriptors">The AI agent descriptors to register.</param>
    /// <returns>The number of agents successfully registered.</returns>
    int RegisterAgents(IEnumerable<AIAgentDescriptor> descriptors);

    /// <summary>
    /// Creates and configures an AI agent instance from a registered descriptor.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent to create.</param>
    /// <param name="configuration">The configuration for the AI agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The configured AI agent instance, or null if not found.</returns>
    Task<IAIAgent?> CreateAgentAsync(string agentId, IAIAgentConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and configures an AI agent instance that provides a specific capability.
    /// If multiple agents provide the capability, returns the first available one.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability required.</typeparam>
    /// <param name="configuration">The configuration for the AI agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The configured AI agent instance that provides the capability, or null if not found.</returns>
    Task<IAIAgent?> CreateAgentForCapabilityAsync<TCapability>(IAIAgentConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent descriptor.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent to unregister.</param>
    /// <returns>True if the agent was unregistered successfully.</returns>
    bool UnregisterAgent(string agentId);

    /// <summary>
    /// Checks if an AI agent is registered.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent.</param>
    /// <returns>True if the agent is registered.</returns>
    bool IsAgentRegistered(string agentId);

    /// <summary>
    /// Gets all registered AI agent descriptors.
    /// </summary>
    /// <returns>An enumerable of registered AI agent descriptors.</returns>
    IEnumerable<AIAgentDescriptor> GetRegisteredAgents();

    /// <summary>
    /// Gets registered AI agent descriptors that provide a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to search for.</typeparam>
    /// <returns>An enumerable of AI agent descriptors that provide the specified capability.</returns>
    IEnumerable<AIAgentDescriptor> GetAgentsByCapability<TCapability>();

    /// <summary>
    /// Gets a registered AI agent descriptor by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent.</param>
    /// <returns>The AI agent descriptor, or null if not found.</returns>
    AIAgentDescriptor? GetAgentDescriptor(string agentId);

    /// <summary>
    /// Clears all registered AI agent descriptors.
    /// </summary>
    void ClearAgents();
}
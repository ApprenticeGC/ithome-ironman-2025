using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Services;

/// <summary>
/// Core AI service interface for managing AI agents in the GameConsole system.
/// Provides basic agent lifecycle management and serves as the primary entry point for AI functionality.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    /// <summary>
    /// Gets the number of currently registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The total number of registered agents.</returns>
    Task<int> GetAgentCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all registered agent IDs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of agent identifiers.</returns>
    Task<IEnumerable<string>> GetAgentIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an agent with the specified ID is currently registered.
    /// </summary>
    /// <param name="agentId">The agent identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the agent is registered, false otherwise.</returns>
    Task<bool> IsAgentRegisteredAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a specific agent.
    /// </summary>
    /// <param name="agentId">The agent identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Agent metadata if found, null otherwise.</returns>
    Task<Core.AgentMetadata?> GetAgentMetadataAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when an agent is registered with the system.
    /// </summary>
    event EventHandler<AgentRegisteredEventArgs>? AgentRegistered;

    /// <summary>
    /// Event raised when an agent is unregistered from the system.
    /// </summary>
    event EventHandler<AgentUnregisteredEventArgs>? AgentUnregistered;

    /// <summary>
    /// Event raised when an agent's status changes.
    /// </summary>
    event EventHandler<AgentStatusChangedEventArgs>? AgentStatusChanged;

    /// <summary>
    /// Gets the agent discovery capability if available.
    /// </summary>
    IAgentDiscoveryCapability? Discovery { get; }

    /// <summary>
    /// Gets the agent registration capability if available.
    /// </summary>
    IAgentRegistrationCapability? Registration { get; }
}

/// <summary>
/// Capability interface for discovering AI agents based on various criteria.
/// Provides advanced querying and filtering of available agents.
/// </summary>
public interface IAgentDiscoveryCapability : ICapabilityProvider
{
    /// <summary>
    /// Discovers AI agents based on the specified criteria.
    /// </summary>
    /// <param name="criteria">Discovery criteria to filter agents.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A discovery result containing matching agents.</returns>
    Task<Core.AgentDiscoveryResult> DiscoverAgentsAsync(Core.AgentDiscoveryCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers agents that provide a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The capability type to search for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of agents that provide the specified capability.</returns>
    Task<IEnumerable<Core.AgentMetadata>> DiscoverAgentsByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default)
        where TCapability : class;

    /// <summary>
    /// Discovers agents by tags.
    /// </summary>
    /// <param name="tags">Tags to search for (any match).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of agents that have any of the specified tags.</returns>
    Task<IEnumerable<Core.AgentMetadata>> DiscoverAgentsByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available capability types across all registered agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of capability types.</returns>
    Task<IEnumerable<Type>> GetAvailableCapabilityTypesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for registering and unregistering AI agents.
/// Manages the lifecycle of agents in the system.
/// </summary>
public interface IAgentRegistrationCapability : ICapabilityProvider
{
    /// <summary>
    /// Registers an AI agent with the system.
    /// </summary>
    /// <param name="metadata">Metadata describing the agent.</param>
    /// <param name="agentInstance">The actual agent implementation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    Task<bool> RegisterAgentAsync(Core.AgentMetadata metadata, object agentInstance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the system.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if unregistration was successful, false otherwise.</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the metadata for an existing agent.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to update.</param>
    /// <param name="metadata">Updated metadata.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if update was successful, false otherwise.</returns>
    Task<bool> UpdateAgentMetadataAsync(string agentId, Core.AgentMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables or disables an agent.
    /// </summary>
    /// <param name="agentId">The identifier of the agent.</param>
    /// <param name="enabled">True to enable, false to disable.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the status change was successful, false otherwise.</returns>
    Task<bool> SetAgentEnabledAsync(string agentId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the actual agent instance for a registered agent.
    /// </summary>
    /// <typeparam name="TAgent">The expected type of the agent.</typeparam>
    /// <param name="agentId">The identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The agent instance if found and of the correct type, null otherwise.</returns>
    Task<TAgent?> GetAgentInstanceAsync<TAgent>(string agentId, CancellationToken cancellationToken = default)
        where TAgent : class;
}

/// <summary>
/// Event arguments for agent registration events.
/// </summary>
public sealed class AgentRegisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRegisteredEventArgs"/> class.
    /// </summary>
    /// <param name="metadata">Metadata of the registered agent.</param>
    public AgentRegisteredEventArgs(Core.AgentMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <summary>
    /// Gets the metadata of the registered agent.
    /// </summary>
    public Core.AgentMetadata Metadata { get; }
}

/// <summary>
/// Event arguments for agent unregistration events.
/// </summary>
public sealed class AgentUnregisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentUnregisteredEventArgs"/> class.
    /// </summary>
    /// <param name="agentId">The identifier of the unregistered agent.</param>
    /// <param name="metadata">Metadata of the unregistered agent.</param>
    public AgentUnregisteredEventArgs(string agentId, Core.AgentMetadata metadata)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <summary>
    /// Gets the identifier of the unregistered agent.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the metadata of the unregistered agent.
    /// </summary>
    public Core.AgentMetadata Metadata { get; }
}

/// <summary>
/// Event arguments for agent status change events.
/// </summary>
public sealed class AgentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="agentId">The identifier of the agent.</param>
    /// <param name="previousStatus">The previous status of the agent.</param>
    /// <param name="newStatus">The new status of the agent.</param>
    public AgentStatusChangedEventArgs(string agentId, bool previousStatus, bool newStatus)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
    }

    /// <summary>
    /// Gets the identifier of the agent.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the previous enabled status of the agent.
    /// </summary>
    public bool PreviousStatus { get; }

    /// <summary>
    /// Gets the new enabled status of the agent.
    /// </summary>
    public bool NewStatus { get; }
}
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Registry for discovering, registering, and managing AI agents in the GameConsole system.
/// Extends the core service registry pattern with AI-specific functionality.
/// </summary>
public interface IAIAgentRegistry : IService
{
    /// <summary>
    /// Registers an AI agent type for discovery and instantiation.
    /// </summary>
    /// <typeparam name="TAgent">The AI agent type implementing IAIAgent.</typeparam>
    /// <param name="metadata">Optional metadata for the agent type.</param>
    void RegisterAgentType<TAgent>(IAIAgentTypeMetadata? metadata = null) where TAgent : class, IAIAgent;

    /// <summary>
    /// Registers an AI agent type with a factory function.
    /// </summary>
    /// <typeparam name="TAgent">The AI agent type implementing IAIAgent.</typeparam>
    /// <param name="factory">Factory function to create agent instances.</param>
    /// <param name="metadata">Optional metadata for the agent type.</param>
    void RegisterAgentType<TAgent>(Func<IServiceProvider, TAgent> factory, IAIAgentTypeMetadata? metadata = null) where TAgent : class, IAIAgent;

    /// <summary>
    /// Registers an AI agent instance directly.
    /// </summary>
    /// <typeparam name="TAgent">The AI agent type implementing IAIAgent.</typeparam>
    /// <param name="instance">The agent instance to register.</param>
    void RegisterAgentInstance<TAgent>(TAgent instance) where TAgent : class, IAIAgent;

    /// <summary>
    /// Discovers available AI agent types based on capabilities.
    /// </summary>
    /// <param name="requiredCapabilities">Capabilities that discovered agents must support.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns discovered agent types.</returns>
    Task<IReadOnlyList<IAIAgentTypeInfo>> DiscoverAgentTypesAsync(
        IAIAgentCapabilityRequirements? requiredCapabilities = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an instance of a registered AI agent type.
    /// </summary>
    /// <typeparam name="TAgent">The AI agent type to create.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the created agent instance.</returns>
    Task<TAgent> CreateAgentAsync<TAgent>(CancellationToken cancellationToken = default) where TAgent : class, IAIAgent;

    /// <summary>
    /// Creates an instance of a registered AI agent type by name.
    /// </summary>
    /// <param name="agentTypeName">The name of the agent type to create.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the created agent instance.</returns>
    Task<IAIAgent> CreateAgentAsync(string agentTypeName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently active AI agent instances.
    /// </summary>
    /// <returns>A collection of active agent instances.</returns>
    IReadOnlyList<IAIAgent> GetActiveAgents();

    /// <summary>
    /// Gets active AI agents that match the specified capabilities.
    /// </summary>
    /// <param name="requiredCapabilities">Capabilities that agents must support.</param>
    /// <returns>A collection of matching agent instances.</returns>
    IReadOnlyList<IAIAgent> GetActiveAgents(IAIAgentCapabilityRequirements requiredCapabilities);

    /// <summary>
    /// Gets an active AI agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <returns>The agent instance if found, null otherwise.</returns>
    IAIAgent? GetAgent(string agentId);

    /// <summary>
    /// Removes and disposes an AI agent instance.
    /// </summary>
    /// <param name="agent">The agent instance to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async removal operation.</returns>
    Task RemoveAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes and disposes an AI agent instance by ID.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async removal operation.</returns>
    Task RemoveAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans assemblies for AI agent types with the AIAgent attribute and registers them.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for agent types.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async discovery and registration operation.</returns>
    Task DiscoverAndRegisterAsync(IEnumerable<System.Reflection.Assembly> assemblies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a new AI agent is registered.
    /// </summary>
    event EventHandler<AIAgentRegisteredEventArgs>? AgentRegistered;

    /// <summary>
    /// Event raised when an AI agent is removed.
    /// </summary>
    event EventHandler<AIAgentRemovedEventArgs>? AgentRemoved;
}

/// <summary>
/// Metadata for an AI agent type registration.
/// </summary>
public interface IAIAgentTypeMetadata
{
    /// <summary>
    /// Gets the unique name of the agent type.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the agent type.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the description of the agent type.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the categories this agent type belongs to.
    /// </summary>
    IReadOnlyList<string> Categories { get; }

    /// <summary>
    /// Gets additional metadata properties.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}

/// <summary>
/// Information about a discovered AI agent type.
/// </summary>
public interface IAIAgentTypeInfo
{
    /// <summary>
    /// Gets the .NET type of the AI agent.
    /// </summary>
    Type AgentType { get; }

    /// <summary>
    /// Gets the metadata for this agent type.
    /// </summary>
    IAIAgentTypeMetadata Metadata { get; }

    /// <summary>
    /// Gets a preview of the capabilities this agent type provides.
    /// This is determined without creating an instance of the agent.
    /// </summary>
    IAIAgentCapabilities CapabilityPreview { get; }
}

/// <summary>
/// Specifies capability requirements for discovering AI agents.
/// </summary>
public interface IAIAgentCapabilityRequirements
{
    /// <summary>
    /// Gets the required decision types the agent must support.
    /// </summary>
    IReadOnlyList<string> RequiredDecisionTypes { get; }

    /// <summary>
    /// Gets whether the agent must support learning.
    /// </summary>
    bool RequiresLearning { get; }

    /// <summary>
    /// Gets whether the agent must support autonomous mode.
    /// </summary>
    bool RequiresAutonomousMode { get; }

    /// <summary>
    /// Gets the minimum priority level required.
    /// </summary>
    int MinimumPriority { get; }

    /// <summary>
    /// Gets additional capability requirements.
    /// </summary>
    IReadOnlyDictionary<string, object> AdditionalRequirements { get; }
}

/// <summary>
/// Event arguments for AI agent registration events.
/// </summary>
public class AIAgentRegisteredEventArgs : EventArgs
{
    public IAIAgent Agent { get; }
    public IAIAgentTypeMetadata Metadata { get; }

    public AIAgentRegisteredEventArgs(IAIAgent agent, IAIAgentTypeMetadata metadata)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }
}

/// <summary>
/// Event arguments for AI agent removal events.
/// </summary>
public class AIAgentRemovedEventArgs : EventArgs
{
    public string AgentId { get; }
    public string AgentTypeName { get; }

    public AIAgentRemovedEventArgs(string agentId, string agentTypeName)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        AgentTypeName = agentTypeName ?? throw new ArgumentNullException(nameof(agentTypeName));
    }
}
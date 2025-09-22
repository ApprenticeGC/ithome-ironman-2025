namespace GameConsole.Core.Abstractions;

/// <summary>
/// Service interface for discovering available AI agents in the system.
/// Provides methods to find agents by various criteria.
/// </summary>
public interface IAiAgentDiscovery : IService
{
    /// <summary>
    /// Discovers all available AI agents in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of discovered AI agents.</returns>
    Task<IReadOnlyList<IAiAgent>> DiscoverAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents by capability.
    /// </summary>
    /// <param name="capability">The capability to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of AI agents that provide the specified capability.</returns>
    Task<IReadOnlyList<IAiAgent>> DiscoverByCapabilityAsync(string capability, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents by status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of AI agents with the specified status.</returns>
    Task<IReadOnlyList<IAiAgent>> DiscoverByStatusAsync(AiAgentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers an AI agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to find.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The AI agent with the specified ID, or null if not found.</returns>
    Task<IAiAgent?> DiscoverByIdAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a new AI agent is discovered and becomes available.
    /// </summary>
    event EventHandler<AiAgentDiscoveredEventArgs>? AgentDiscovered;

    /// <summary>
    /// Event raised when an AI agent becomes unavailable.
    /// </summary>
    event EventHandler<AiAgentUnavailableEventArgs>? AgentUnavailable;
}

/// <summary>
/// Service interface for registering and managing AI agents in the system.
/// </summary>
public interface IAiAgentRegistry : IService
{
    /// <summary>
    /// Registers an AI agent in the system.
    /// </summary>
    /// <param name="agent">The AI agent to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if registration was successful, false if agent was already registered.</returns>
    Task<bool> RegisterAsync(IAiAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the system.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if unregistration was successful, false if agent was not found.</returns>
    Task<bool> UnregisterAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a registered AI agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The AI agent with the specified ID, or null if not found.</returns>
    Task<IAiAgent?> GetAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of all registered AI agents.</returns>
    Task<IReadOnlyList<IAiAgent>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an AI agent is registered.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the agent is registered, false otherwise.</returns>
    Task<bool> IsRegisteredAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when an AI agent is successfully registered.
    /// </summary>
    event EventHandler<AiAgentRegisteredEventArgs>? AgentRegistered;

    /// <summary>
    /// Event raised when an AI agent is unregistered.
    /// </summary>
    event EventHandler<AiAgentUnregisteredEventArgs>? AgentUnregistered;
}

/// <summary>
/// Event arguments for when an AI agent is discovered.
/// </summary>
public sealed class AiAgentDiscoveredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentDiscoveredEventArgs"/> class.
    /// </summary>
    /// <param name="agent">The discovered AI agent.</param>
    public AiAgentDiscoveredEventArgs(IAiAgent agent)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    /// <summary>
    /// Gets the discovered AI agent.
    /// </summary>
    public IAiAgent Agent { get; }
}

/// <summary>
/// Event arguments for when an AI agent becomes unavailable.
/// </summary>
public sealed class AiAgentUnavailableEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentUnavailableEventArgs"/> class.
    /// </summary>
    /// <param name="agentId">The ID of the unavailable AI agent.</param>
    /// <param name="reason">The reason the agent became unavailable.</param>
    public AiAgentUnavailableEventArgs(string agentId, string? reason = null)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Reason = reason;
    }

    /// <summary>
    /// Gets the ID of the unavailable AI agent.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the reason the agent became unavailable, if known.
    /// </summary>
    public string? Reason { get; }
}

/// <summary>
/// Event arguments for when an AI agent is registered.
/// </summary>
public sealed class AiAgentRegisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentRegisteredEventArgs"/> class.
    /// </summary>
    /// <param name="agent">The registered AI agent.</param>
    public AiAgentRegisteredEventArgs(IAiAgent agent)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    /// <summary>
    /// Gets the registered AI agent.
    /// </summary>
    public IAiAgent Agent { get; }
}

/// <summary>
/// Event arguments for when an AI agent is unregistered.
/// </summary>
public sealed class AiAgentUnregisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentUnregisteredEventArgs"/> class.
    /// </summary>
    /// <param name="agentId">The ID of the unregistered AI agent.</param>
    /// <param name="reason">The reason for unregistration.</param>
    public AiAgentUnregisteredEventArgs(string agentId, string? reason = null)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Reason = reason;
    }

    /// <summary>
    /// Gets the ID of the unregistered AI agent.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the reason for unregistration, if provided.
    /// </summary>
    public string? Reason { get; }
}
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Registry service for managing AI agent registration, lifecycle, and coordination.
/// Provides centralized management of AI agents in the system.
/// </summary>
public interface IAIAgentRegistry : IService
{
    /// <summary>
    /// Registers an AI agent with the registry.
    /// </summary>
    /// <param name="agent">The agent to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the registry.
    /// </summary>
    /// <param name="agentId">The ID of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent was successfully unregistered.</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns all registered agents.</returns>
    Task<IEnumerable<IAIAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific AI agent by ID.
    /// </summary>
    /// <param name="agentId">The ID of the agent to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the agent, or null if not found.</returns>
    Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all agents that are currently available (ready to process requests).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns all available agents.</returns>
    Task<IEnumerable<IAIAgent>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agents filtered by their current status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns agents with the specified status.</returns>
    Task<IEnumerable<IAIAgent>> GetAgentsByStatusAsync(AIAgentStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific agent is registered.
    /// </summary>
    /// <param name="agentId">The ID of the agent to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent is registered.</returns>
    Task<bool> IsAgentRegisteredAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of registered agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the count of registered agents.</returns>
    Task<int> GetAgentCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets registry statistics and health information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns registry statistics.</returns>
    Task<AIAgentRegistryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on all registered agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns health check results.</returns>
    Task<IEnumerable<AIAgentHealthStatus>> PerformHealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when an agent is registered.
    /// </summary>
    event EventHandler<AIAgentRegisteredEventArgs>? AgentRegistered;

    /// <summary>
    /// Event raised when an agent is unregistered.
    /// </summary>
    event EventHandler<AIAgentUnregisteredEventArgs>? AgentUnregistered;

    /// <summary>
    /// Event raised when an agent's status changes.
    /// </summary>
    event EventHandler<AIAgentStatusChangedEventArgs>? AgentStatusChanged;
}

/// <summary>
/// Statistics about the AI agent registry.
/// </summary>
public class AIAgentRegistryStatistics
{
    /// <summary>
    /// Gets or sets the total number of registered agents.
    /// </summary>
    public int TotalAgents { get; set; }

    /// <summary>
    /// Gets or sets the number of agents by status.
    /// </summary>
    public IReadOnlyDictionary<AIAgentStatus, int> AgentsByStatus { get; set; } = new Dictionary<AIAgentStatus, int>();

    /// <summary>
    /// Gets or sets the uptime of the registry.
    /// </summary>
    public TimeSpan RegistryUptime { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when statistics were collected.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Health status information for an AI agent.
/// </summary>
public class AIAgentHealthStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentHealthStatus"/> class.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="isHealthy">Whether the agent is healthy.</param>
    /// <param name="details">Additional health details.</param>
    public AIAgentHealthStatus(string agentId, bool isHealthy, string? details = null)
    {
        AgentId = agentId;
        IsHealthy = isHealthy;
        Details = details;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the agent ID.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets a value indicating whether the agent is healthy.
    /// </summary>
    public bool IsHealthy { get; }

    /// <summary>
    /// Gets additional health details.
    /// </summary>
    public string? Details { get; }

    /// <summary>
    /// Gets the timestamp when the health check was performed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}

/// <summary>
/// Event arguments for agent registration events.
/// </summary>
public class AIAgentRegisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentRegisteredEventArgs"/> class.
    /// </summary>
    /// <param name="agent">The registered agent.</param>
    public AIAgentRegisteredEventArgs(IAIAgent agent)
    {
        Agent = agent;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the registered agent.
    /// </summary>
    public IAIAgent Agent { get; }

    /// <summary>
    /// Gets the timestamp when the agent was registered.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}

/// <summary>
/// Event arguments for agent unregistration events.
/// </summary>
public class AIAgentUnregisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentUnregisteredEventArgs"/> class.
    /// </summary>
    /// <param name="agentId">The ID of the unregistered agent.</param>
    /// <param name="reason">The reason for unregistration.</param>
    public AIAgentUnregisteredEventArgs(string agentId, string? reason = null)
    {
        AgentId = agentId;
        Reason = reason;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the ID of the unregistered agent.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the reason for unregistration.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the timestamp when the agent was unregistered.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
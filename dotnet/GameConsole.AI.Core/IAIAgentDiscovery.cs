using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Service for discovering available AI agents in the system.
/// Implements capability-based discovery and provides agent selection capabilities.
/// </summary>
public interface IAIAgentDiscovery : IService, ICapabilityProvider
{
    /// <summary>
    /// Discovers all available AI agents in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns all discovered AI agents.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents that can handle a specific request type.
    /// </summary>
    /// <param name="requestType">The type of request to find agents for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns agents capable of handling the request type.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverAgentsByRequestTypeAsync(Type requestType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents that have specific capabilities.
    /// </summary>
    /// <param name="capabilities">The capabilities to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns agents with the specified capabilities.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverAgentsByCapabilitiesAsync(IEnumerable<string> capabilities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents in specific skill domains.
    /// </summary>
    /// <param name="skillDomains">The skill domains to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns agents with expertise in the specified domains.</returns>
    Task<IEnumerable<IAIAgent>> DiscoverAgentsBySkillDomainsAsync(IEnumerable<string> skillDomains, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the best AI agent to handle a specific request based on capabilities and priority.
    /// </summary>
    /// <param name="request">The request to find an agent for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the best agent for the request, or null if none found.</returns>
    Task<IAIAgent?> FindBestAgentAsync(IAIAgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ranks available agents for a specific request by their suitability.
    /// </summary>
    /// <param name="request">The request to rank agents for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns agents ranked by suitability (best first).</returns>
    Task<IEnumerable<(IAIAgent Agent, int Score)>> RankAgentsAsync(IAIAgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when new AI agents are discovered.
    /// </summary>
    event EventHandler<AIAgentDiscoveredEventArgs>? AgentDiscovered;

    /// <summary>
    /// Event raised when AI agents become unavailable.
    /// </summary>
    event EventHandler<AIAgentUnavailableEventArgs>? AgentUnavailable;
}

/// <summary>
/// Event arguments for AI agent discovery events.
/// </summary>
public class AIAgentDiscoveredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDiscoveredEventArgs"/> class.
    /// </summary>
    /// <param name="agent">The discovered agent.</param>
    public AIAgentDiscoveredEventArgs(IAIAgent agent)
    {
        Agent = agent;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the discovered AI agent.
    /// </summary>
    public IAIAgent Agent { get; }

    /// <summary>
    /// Gets the timestamp when the agent was discovered.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}

/// <summary>
/// Event arguments for AI agent unavailable events.
/// </summary>
public class AIAgentUnavailableEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentUnavailableEventArgs"/> class.
    /// </summary>
    /// <param name="agent">The unavailable agent.</param>
    /// <param name="reason">The reason why the agent became unavailable.</param>
    public AIAgentUnavailableEventArgs(IAIAgent agent, string? reason = null)
    {
        Agent = agent;
        Reason = reason;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the unavailable AI agent.
    /// </summary>
    public IAIAgent Agent { get; }

    /// <summary>
    /// Gets the reason why the agent became unavailable.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the timestamp when the agent became unavailable.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
using Akka.Actor;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Clustering;

/// <summary>
/// Intelligent message router for AI cluster communication.
/// Routes messages based on agent capabilities, load balancing, and network topology.
/// </summary>
public interface IClusterAIRouter : IService
{
    /// <summary>
    /// Routes an AI request to the most appropriate node in the cluster.
    /// </summary>
    /// <param name="request">The AI request to route</param>
    /// <returns>The response from the AI agent</returns>
    Task<AIResponse> RouteRequestAsync(AIRequest request);
    
    /// <summary>
    /// Gets the current routing table for the cluster.
    /// </summary>
    Task<RoutingTable> GetRoutingTableAsync();
    
    /// <summary>
    /// Updates routing preferences for load balancing.
    /// </summary>
    /// <param name="preferences">New routing preferences</param>
    Task UpdateRoutingPreferencesAsync(RoutingPreferences preferences);
}

/// <summary>
/// Represents an AI request to be routed through the cluster.
/// </summary>
public record AIRequest(
    string RequestId,
    string AgentType,
    IReadOnlyDictionary<string, object> Parameters,
    TimeSpan Timeout,
    Priority Priority = Priority.Normal
);

/// <summary>
/// Represents a response from an AI agent.
/// </summary>
public record AIResponse(
    string RequestId,
    bool Success,
    IReadOnlyDictionary<string, object>? Result,
    string? ErrorMessage,
    TimeSpan ProcessingTime
);

/// <summary>
/// Represents the current routing table for the cluster.
/// </summary>
public record RoutingTable(
    IReadOnlyDictionary<string, IReadOnlyList<Address>> AgentTypeToNodes,
    IReadOnlyDictionary<Address, NodeLoadMetrics> NodeMetrics
);

/// <summary>
/// Configuration for routing preferences.
/// </summary>
public record RoutingPreferences(
    RoutingStrategy Strategy,
    double LoadBalanceThreshold,
    TimeSpan HealthCheckInterval
);

/// <summary>
/// Routing strategies for AI requests.
/// </summary>
public enum RoutingStrategy
{
    RoundRobin,
    LeastLoaded,
    Fastest,
    Closest,
    CapabilityBased
}

/// <summary>
/// Priority levels for AI requests.
/// </summary>
public enum Priority
{
    Low,
    Normal,
    High,
    Critical
}
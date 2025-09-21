using GameConsole.Core.Abstractions;
using GameConsole.AI.Clustering.Models;

namespace GameConsole.AI.Clustering.Interfaces;

/// <summary>
/// Interface for intelligent message routing across the AI cluster.
/// Handles load balancing and capability-based routing decisions.
/// </summary>
public interface IClusterAIRouter : IService
{
    /// <summary>
    /// Routes a message to the most appropriate node based on agent capabilities and load.
    /// </summary>
    /// <param name="request">The routing request containing message and requirements.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the routing decision.</returns>
    Task<RoutingDecision> RouteMessageAsync(RoutingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the best node for a specific agent capability.
    /// </summary>
    /// <param name="agentType">The type of AI agent required.</param>
    /// <param name="operation">The specific operation to perform.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the recommended node, or null if none available.</returns>
    Task<ClusterNode?> GetBestNodeForCapabilityAsync(string agentType, string operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets nodes that can handle the specified capability, ordered by preference.
    /// </summary>
    /// <param name="agentType">The type of AI agent required.</param>
    /// <param name="operation">The specific operation to perform.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the list of capable nodes ordered by preference.</returns>
    Task<IReadOnlyList<ClusterNode>> GetCapableNodesAsync(string agentType, string operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the load balancing weights for cluster nodes.
    /// </summary>
    /// <param name="nodeWeights">Dictionary mapping node IDs to their load balancing weights.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async weight update operation.</returns>
    Task UpdateNodeWeightsAsync(IReadOnlyDictionary<string, double> nodeWeights, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current routing statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the routing statistics.</returns>
    Task<RoutingStatistics> GetRoutingStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures the routing strategy to use.
    /// </summary>
    /// <param name="strategy">The routing strategy configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration operation.</returns>
    Task ConfigureRoutingStrategyAsync(RoutingStrategy strategy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when routing decisions are made (for monitoring and debugging).
    /// </summary>
    event EventHandler<RoutingDecision>? RouteDecided;

    /// <summary>
    /// Event raised when a node becomes unavailable for routing.
    /// </summary>
    event EventHandler<string>? NodeUnavailable;
}

/// <summary>
/// Represents a request for message routing.
/// </summary>
public class RoutingRequest
{
    /// <summary>
    /// Gets the unique identifier for this routing request.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Gets the type of AI agent required to handle this request.
    /// </summary>
    public required string AgentType { get; init; }

    /// <summary>
    /// Gets the specific operation to perform.
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the message payload to be routed.
    /// </summary>
    public required object MessagePayload { get; init; }

    /// <summary>
    /// Gets the priority of this request (higher values = higher priority).
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Gets the maximum acceptable response time in milliseconds.
    /// </summary>
    public int MaxResponseTimeMs { get; init; } = 30000;

    /// <summary>
    /// Gets additional routing preferences and constraints.
    /// </summary>
    public RoutingPreferences Preferences { get; init; } = new();

    /// <summary>
    /// Gets the timestamp when this request was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents routing preferences and constraints.
/// </summary>
public class RoutingPreferences
{
    /// <summary>
    /// Gets preferred node IDs (in order of preference).
    /// </summary>
    public IReadOnlyList<string> PreferredNodes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets excluded node IDs that should not be used.
    /// </summary>
    public IReadOnlyList<string> ExcludedNodes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets whether to prefer nodes with lower latency.
    /// </summary>
    public bool PreferLowLatency { get; init; } = true;

    /// <summary>
    /// Gets whether to prefer nodes with higher available capacity.
    /// </summary>
    public bool PreferHighCapacity { get; init; } = true;

    /// <summary>
    /// Gets the maximum acceptable node utilization percentage.
    /// </summary>
    public double MaxNodeUtilization { get; init; } = 90.0;
}

/// <summary>
/// Represents the result of a routing decision.
/// </summary>
public class RoutingDecision
{
    /// <summary>
    /// Gets the request that was routed.
    /// </summary>
    public required RoutingRequest Request { get; init; }

    /// <summary>
    /// Gets the selected node for handling the request, or null if no suitable node found.
    /// </summary>
    public ClusterNode? SelectedNode { get; init; }

    /// <summary>
    /// Gets the reason for the routing decision.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the confidence score of this routing decision (0-1).
    /// </summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>
    /// Gets alternative nodes that could handle the request (ordered by preference).
    /// </summary>
    public IReadOnlyList<ClusterNode> AlternativeNodes { get; init; } = Array.Empty<ClusterNode>();

    /// <summary>
    /// Gets the timestamp when this routing decision was made.
    /// </summary>
    public DateTime DecidedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents routing statistics for monitoring and optimization.
/// </summary>
public class RoutingStatistics
{
    /// <summary>
    /// Gets the total number of routing requests processed.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Gets the number of successful routing decisions.
    /// </summary>
    public long SuccessfulRoutes { get; init; }

    /// <summary>
    /// Gets the number of failed routing attempts.
    /// </summary>
    public long FailedRoutes { get; init; }

    /// <summary>
    /// Gets the average routing decision time in milliseconds.
    /// </summary>
    public double AverageDecisionTimeMs { get; init; }

    /// <summary>
    /// Gets routing statistics per node.
    /// </summary>
    public IReadOnlyDictionary<string, NodeRoutingStats> PerNodeStats { get; init; } = new Dictionary<string, NodeRoutingStats>();

    /// <summary>
    /// Gets the timestamp when these statistics were captured.
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents routing statistics for a specific node.
/// </summary>
public record NodeRoutingStats
{
    /// <summary>
    /// Gets the number of requests routed to this node.
    /// </summary>
    public long RequestsRouted { get; init; }

    /// <summary>
    /// Gets the success rate for requests routed to this node (0-1).
    /// </summary>
    public double SuccessRate { get; init; }

    /// <summary>
    /// Gets the average response time for this node in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; init; }

    /// <summary>
    /// Gets the current load factor for this node (0-1).
    /// </summary>
    public double LoadFactor { get; init; }
}

/// <summary>
/// Represents a routing strategy configuration.
/// </summary>
public class RoutingStrategy
{
    /// <summary>
    /// Gets the load balancing algorithm to use.
    /// </summary>
    public LoadBalancingAlgorithm Algorithm { get; init; } = LoadBalancingAlgorithm.WeightedRoundRobin;

    /// <summary>
    /// Gets the weight given to latency in routing decisions (0-1).
    /// </summary>
    public double LatencyWeight { get; init; } = 0.3;

    /// <summary>
    /// Gets the weight given to node capacity in routing decisions (0-1).
    /// </summary>
    public double CapacityWeight { get; init; } = 0.4;

    /// <summary>
    /// Gets the weight given to node health in routing decisions (0-1).
    /// </summary>
    public double HealthWeight { get; init; } = 0.3;

    /// <summary>
    /// Gets whether to enable sticky sessions (route related requests to same node).
    /// </summary>
    public bool EnableStickySessions { get; init; } = false;

    /// <summary>
    /// Gets the session affinity timeout in milliseconds.
    /// </summary>
    public int SessionAffinityTimeoutMs { get; init; } = 300000;
}

/// <summary>
/// Represents load balancing algorithms available for routing.
/// </summary>
public enum LoadBalancingAlgorithm
{
    RoundRobin,
    WeightedRoundRobin,
    LeastConnections,
    WeightedLeastConnections,
    CapabilityBased,
    ConsistentHash
}
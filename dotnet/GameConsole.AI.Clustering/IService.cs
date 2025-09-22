using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Core AI clustering service for distributed agent coordination.
/// Manages AI agent clusters with automatic scaling and intelligent routing.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    /// <summary>
    /// Joins the cluster as a node with specified capabilities.
    /// </summary>
    /// <param name="nodeId">Unique identifier for this node.</param>
    /// <param name="capabilities">Capabilities this node can provide.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the node successfully joined the cluster, false otherwise.</returns>
    Task<bool> JoinClusterAsync(string nodeId, string[] capabilities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves the cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async leave operation.</returns>
    Task LeaveClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Routes a message to an appropriate agent in the cluster.
    /// </summary>
    /// <param name="message">Message to route.</param>
    /// <param name="requiredCapabilities">Required capabilities for handling the message.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Response from the target agent.</returns>
    Task<string> RouteMessageAsync(string message, string[] requiredCapabilities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current cluster status information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Cluster status information.</returns>
    Task<ClusterStatus> GetClusterStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for cluster monitoring and metrics.
/// </summary>
public interface IClusterMonitoringCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets health metrics for all nodes in the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Health metrics for each node.</returns>
    Task<NodeHealthMetrics[]> GetNodeHealthMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets load balancing statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Load balancing statistics.</returns>
    Task<LoadBalanceStats> GetLoadBalanceStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event triggered when a node joins the cluster.
    /// </summary>
    event EventHandler<NodeJoinedEventArgs>? NodeJoined;

    /// <summary>
    /// Event triggered when a node leaves the cluster.
    /// </summary>
    event EventHandler<NodeLeftEventArgs>? NodeLeft;
}

/// <summary>
/// Capability interface for advanced routing strategies.
/// </summary>
public interface IAdvancedRoutingCapability : ICapabilityProvider
{
    /// <summary>
    /// Routes message using custom routing strategy.
    /// </summary>
    /// <param name="message">Message to route.</param>
    /// <param name="routingStrategy">Custom routing strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Response from the target agent.</returns>
    Task<string> RouteWithStrategyAsync(string message, RoutingStrategy routingStrategy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcasts a message to all nodes with specified capabilities.
    /// </summary>
    /// <param name="message">Message to broadcast.</param>
    /// <param name="capabilities">Target capabilities for broadcast.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Responses from all target nodes.</returns>
    Task<string[]> BroadcastMessageAsync(string message, string[] capabilities, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the current status of the cluster.
/// </summary>
public class ClusterStatus
{
    public string LeaderNodeId { get; set; } = string.Empty;
    public int ActiveNodes { get; set; }
    public string[] AvailableCapabilities { get; set; } = Array.Empty<string>();
    public DateTime LastUpdated { get; set; }
    public ClusterState State { get; set; }
}

/// <summary>
/// Health metrics for a cluster node.
/// </summary>
public class NodeHealthMetrics
{
    public string NodeId { get; set; } = string.Empty;
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public int ActiveMessages { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public NodeStatus Status { get; set; }
    public DateTime LastHeartbeat { get; set; }
}

/// <summary>
/// Load balancing statistics.
/// </summary>
public class LoadBalanceStats
{
    public int TotalMessages { get; set; }
    public Dictionary<string, int> MessagesPerNode { get; set; } = new();
    public Dictionary<string, TimeSpan> AverageResponseTime { get; set; } = new();
    public double LoadDistributionEfficiency { get; set; }
}

/// <summary>
/// Event arguments for node joined events.
/// </summary>
public class NodeJoinedEventArgs : EventArgs
{
    public string NodeId { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = Array.Empty<string>();
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// Event arguments for node left events.
/// </summary>
public class NodeLeftEventArgs : EventArgs
{
    public string NodeId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime LeftAt { get; set; }
}

/// <summary>
/// Cluster state enumeration.
/// </summary>
public enum ClusterState
{
    Joining,
    Up,
    Leaving,
    Down,
    Unreachable
}

/// <summary>
/// Node status enumeration.
/// </summary>
public enum NodeStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unreachable
}

/// <summary>
/// Routing strategy options.
/// </summary>
public enum RoutingStrategy
{
    RoundRobin,
    LeastLoaded,
    CapabilityBased,
    ConsistentHashing,
    Random
}
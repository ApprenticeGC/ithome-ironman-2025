using Akka.Actor;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Clustering;

/// <summary>
/// Manages individual node operations within the AI cluster.
/// Handles node lifecycle, resource management, and local AI agent coordination.
/// </summary>
public interface IAINodeManager : IService
{
    /// <summary>
    /// Gets the current node information.
    /// </summary>
    Task<NodeInfo> GetNodeInfoAsync();
    
    /// <summary>
    /// Registers an AI agent capability on this node.
    /// </summary>
    /// <param name="agentType">Type of AI agent to register</param>
    /// <param name="capabilities">Agent capabilities</param>
    Task RegisterAgentAsync(string agentType, AgentCapabilities capabilities);
    
    /// <summary>
    /// Unregisters an AI agent from this node.
    /// </summary>
    /// <param name="agentType">Type of AI agent to unregister</param>
    Task UnregisterAgentAsync(string agentType);
    
    /// <summary>
    /// Gets the current node load metrics.
    /// </summary>
    Task<NodeLoadMetrics> GetLoadMetricsAsync();
}

/// <summary>
/// Represents information about a cluster node.
/// </summary>
public record NodeInfo(
    Address Address,
    string NodeId,
    NodeStatus Status,
    IReadOnlyDictionary<string, AgentCapabilities> RegisteredAgents
);

/// <summary>
/// Represents the capabilities of an AI agent.
/// </summary>
public record AgentCapabilities(
    IReadOnlyList<string> SupportedOperations,
    int MaxConcurrentTasks,
    TimeSpan AverageProcessingTime
);

/// <summary>
/// Represents current load metrics for a node.
/// </summary>
public record NodeLoadMetrics(
    double CpuUsage,
    double MemoryUsage,
    int ActiveTasks,
    int QueuedTasks
);

/// <summary>
/// Status of a cluster node.
/// </summary>
public enum NodeStatus
{
    Initializing,
    Ready,
    Busy,
    Degraded,
    Offline
}
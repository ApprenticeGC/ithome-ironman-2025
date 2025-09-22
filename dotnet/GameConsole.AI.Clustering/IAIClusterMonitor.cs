using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Clustering;

/// <summary>
/// Monitors health and performance of the AI cluster.
/// Tracks cluster metrics, detects failures, and triggers recovery actions.
/// </summary>
public interface IAIClusterMonitor : IService
{
    /// <summary>
    /// Gets the current cluster health status.
    /// </summary>
    Task<ClusterHealthStatus> GetClusterHealthAsync();
    
    /// <summary>
    /// Gets detailed performance metrics for the cluster.
    /// </summary>
    Task<ClusterMetrics> GetClusterMetricsAsync();
    
    /// <summary>
    /// Subscribes to cluster health change notifications.
    /// </summary>
    /// <param name="callback">Callback to invoke when health changes</param>
    void SubscribeToHealthChanges(Action<ClusterHealthStatus> callback);
    
    /// <summary>
    /// Unsubscribes from cluster health change notifications.
    /// </summary>
    /// <param name="callback">Callback to remove</param>
    void UnsubscribeFromHealthChanges(Action<ClusterHealthStatus> callback);
}

/// <summary>
/// Represents the overall health status of the AI cluster.
/// </summary>
public record ClusterHealthStatus(
    HealthState State,
    int HealthyNodes,
    int UnhealthyNodes,
    int TotalNodes,
    IReadOnlyList<HealthIssue> Issues,
    DateTime LastUpdated
);

/// <summary>
/// Comprehensive metrics for the AI cluster.
/// </summary>
public record ClusterMetrics(
    double AverageResponseTime,
    int TotalRequestsPerSecond,
    double ClusterUtilization,
    IReadOnlyDictionary<string, int> AgentTypeCounts,
    IReadOnlyList<NodeMetrics> NodeMetrics,
    DateTime CollectedAt
);

/// <summary>
/// Metrics for an individual node in the cluster.
/// </summary>
public record NodeMetrics(
    string NodeId,
    double ResponseTime,
    int RequestsPerSecond,
    double CpuUtilization,
    double MemoryUtilization,
    int ActiveConnections
);

/// <summary>
/// Represents a health issue detected in the cluster.
/// </summary>
public record HealthIssue(
    string NodeId,
    IssueType Type,
    string Description,
    IssueSeverity Severity,
    DateTime DetectedAt
);

/// <summary>
/// Overall health state of the cluster.
/// </summary>
public enum HealthState
{
    Healthy,
    Degraded,
    Critical,
    Offline
}

/// <summary>
/// Types of health issues that can be detected.
/// </summary>
public enum IssueType
{
    NetworkPartition,
    HighLatency,
    ResourceExhaustion,
    NodeUnresponsive,
    ConfigurationError,
    ServiceFailure
}

/// <summary>
/// Severity levels for health issues.
/// </summary>
public enum IssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
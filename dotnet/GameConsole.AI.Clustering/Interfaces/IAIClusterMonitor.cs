using GameConsole.Core.Abstractions;
using GameConsole.AI.Clustering.Models;

namespace GameConsole.AI.Clustering.Interfaces;

/// <summary>
/// Interface for monitoring cluster health and triggering dynamic scaling.
/// Handles health tracking, failure detection, and workload-based scaling decisions.
/// </summary>
public interface IAIClusterMonitor : IService
{
    /// <summary>
    /// Gets the current cluster health status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the current cluster health.</returns>
    Task<ClusterHealth> GetClusterHealthAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a comprehensive health check of the entire cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the health check results.</returns>
    Task<ClusterHealthCheckResult> PerformClusterHealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status of a specific node.
    /// </summary>
    /// <param name="nodeId">The ID of the node to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the node's health status, or null if node not found.</returns>
    Task<NodeHealth?> GetNodeHealthAsync(string nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors workload patterns and returns scaling recommendations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing scaling recommendations.</returns>
    Task<ScalingRecommendation> GetScalingRecommendationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts continuous monitoring of cluster health and workload.
    /// </summary>
    /// <param name="monitoringConfiguration">Configuration for monitoring behavior.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async monitoring start operation.</returns>
    Task StartMonitoringAsync(MonitoringConfiguration monitoringConfiguration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops continuous monitoring of cluster health and workload.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async monitoring stop operation.</returns>
    Task StopMonitoringAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects and reports network partitions in the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing information about detected partitions.</returns>
    Task<IReadOnlyList<NetworkPartition>> DetectNetworkPartitionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets historical cluster metrics for analysis.
    /// </summary>
    /// <param name="fromTime">Start time for metrics collection.</param>
    /// <param name="toTime">End time for metrics collection.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing historical cluster metrics.</returns>
    Task<IReadOnlyList<ClusterMetrics>> GetHistoricalMetricsAsync(DateTime fromTime, DateTime toTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when cluster health status changes.
    /// </summary>
    event EventHandler<ClusterHealth>? ClusterHealthChanged;

    /// <summary>
    /// Event raised when a node health status changes.
    /// </summary>
    event EventHandler<(string NodeId, NodeHealth Health)>? NodeHealthChanged;

    /// <summary>
    /// Event raised when a scaling recommendation is generated.
    /// </summary>
    event EventHandler<ScalingRecommendation>? ScalingRecommended;

    /// <summary>
    /// Event raised when a network partition is detected.
    /// </summary>
    event EventHandler<NetworkPartition>? NetworkPartitionDetected;

    /// <summary>
    /// Event raised when critical alerts are generated.
    /// </summary>
    event EventHandler<ClusterAlert>? CriticalAlertRaised;
}

/// <summary>
/// Represents the result of a comprehensive cluster health check.
/// </summary>
public class ClusterHealthCheckResult
{
    /// <summary>
    /// Gets the overall cluster health status.
    /// </summary>
    public required ClusterHealth OverallHealth { get; init; }

    /// <summary>
    /// Gets detailed health check results for each node.
    /// </summary>
    public IReadOnlyDictionary<string, NodeHealthCheckResult> NodeResults { get; init; } = new Dictionary<string, NodeHealthCheckResult>();

    /// <summary>
    /// Gets cluster-wide connectivity test results.
    /// </summary>
    public IReadOnlyList<ConnectivityTestResult> ConnectivityResults { get; init; } = Array.Empty<ConnectivityTestResult>();

    /// <summary>
    /// Gets performance benchmark results.
    /// </summary>
    public IReadOnlyDictionary<string, double> PerformanceMetrics { get; init; } = new Dictionary<string, double>();

    /// <summary>
    /// Gets the timestamp when this health check was completed.
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a connectivity test result between nodes.
/// </summary>
public class ConnectivityTestResult
{
    /// <summary>
    /// Gets the source node ID.
    /// </summary>
    public required string SourceNodeId { get; init; }

    /// <summary>
    /// Gets the target node ID.
    /// </summary>
    public required string TargetNodeId { get; init; }

    /// <summary>
    /// Gets whether the connectivity test succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the latency in milliseconds (if successful).
    /// </summary>
    public double? LatencyMs { get; init; }

    /// <summary>
    /// Gets the error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the timestamp when this test was performed.
    /// </summary>
    public DateTime TestedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a scaling recommendation based on workload analysis.
/// </summary>
public class ScalingRecommendation
{
    /// <summary>
    /// Gets the type of scaling recommendation.
    /// </summary>
    public required ScalingAction Action { get; init; }

    /// <summary>
    /// Gets the number of nodes to scale by (positive for scale up, negative for scale down).
    /// </summary>
    public int NodeCountChange { get; init; }

    /// <summary>
    /// Gets the reason for this scaling recommendation.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the confidence score of this recommendation (0-1).
    /// </summary>
    public double Confidence { get; init; } = 1.0;

    /// <summary>
    /// Gets the urgency level of this recommendation.
    /// </summary>
    public ScalingUrgency Urgency { get; init; } = ScalingUrgency.Normal;

    /// <summary>
    /// Gets the expected impact of implementing this recommendation.
    /// </summary>
    public required ScalingImpact ExpectedImpact { get; init; }

    /// <summary>
    /// Gets the timestamp when this recommendation was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the type of scaling action recommended.
/// </summary>
public enum ScalingAction
{
    NoAction,
    ScaleUp,
    ScaleDown,
    Rebalance
}

/// <summary>
/// Represents the urgency level of a scaling recommendation.
/// </summary>
public enum ScalingUrgency
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Represents the expected impact of a scaling operation.
/// </summary>
public class ScalingImpact
{
    /// <summary>
    /// Gets the expected change in response time.
    /// </summary>
    public double ResponseTimeChangePercent { get; init; }

    /// <summary>
    /// Gets the expected change in throughput.
    /// </summary>
    public double ThroughputChangePercent { get; init; }

    /// <summary>
    /// Gets the expected change in resource utilization.
    /// </summary>
    public double ResourceUtilizationChangePercent { get; init; }

    /// <summary>
    /// Gets the estimated cost impact of the scaling operation.
    /// </summary>
    public double EstimatedCostChangePercent { get; init; }
}

/// <summary>
/// Represents a detected network partition in the cluster.
/// </summary>
public class NetworkPartition
{
    /// <summary>
    /// Gets the unique identifier for this partition.
    /// </summary>
    public required string PartitionId { get; init; }

    /// <summary>
    /// Gets the nodes that are part of this partition.
    /// </summary>
    public required IReadOnlyList<string> NodeIds { get; init; }

    /// <summary>
    /// Gets whether this is the majority partition.
    /// </summary>
    public bool IsMajorityPartition { get; init; }

    /// <summary>
    /// Gets the timestamp when this partition was first detected.
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the severity of this network partition.
    /// </summary>
    public PartitionSeverity Severity { get; init; } = PartitionSeverity.Warning;

    /// <summary>
    /// Gets additional context about the partition.
    /// </summary>
    public IReadOnlyDictionary<string, string> Context { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents the severity of a network partition.
/// </summary>
public enum PartitionSeverity
{
    Info,
    Warning,
    Critical
}

/// <summary>
/// Represents configuration for cluster monitoring behavior.
/// </summary>
public class MonitoringConfiguration
{
    /// <summary>
    /// Gets the interval between health checks in milliseconds.
    /// </summary>
    public int HealthCheckIntervalMs { get; init; } = 30000;

    /// <summary>
    /// Gets the interval between workload analysis in milliseconds.
    /// </summary>
    public int WorkloadAnalysisIntervalMs { get; init; } = 60000;

    /// <summary>
    /// Gets the interval between network partition detection in milliseconds.
    /// </summary>
    public int PartitionDetectionIntervalMs { get; init; } = 15000;

    /// <summary>
    /// Gets whether to enable automatic scaling recommendations.
    /// </summary>
    public bool EnableAutoScaling { get; init; } = true;

    /// <summary>
    /// Gets the threshold for raising health alerts.
    /// </summary>
    public AlertThreshold AlertThresholds { get; init; } = new();

    /// <summary>
    /// Gets the retention period for historical metrics in hours.
    /// </summary>
    public int MetricsRetentionHours { get; init; } = 24;
}

/// <summary>
/// Represents thresholds for raising monitoring alerts.
/// </summary>
public class AlertThreshold
{
    /// <summary>
    /// Gets the CPU utilization threshold for raising alerts (0-100).
    /// </summary>
    public double CpuUtilizationThreshold { get; init; } = 85.0;

    /// <summary>
    /// Gets the memory utilization threshold for raising alerts (0-100).
    /// </summary>
    public double MemoryUtilizationThreshold { get; init; } = 90.0;

    /// <summary>
    /// Gets the response time threshold for raising alerts in milliseconds.
    /// </summary>
    public double ResponseTimeThresholdMs { get; init; } = 5000.0;

    /// <summary>
    /// Gets the error rate threshold for raising alerts (0-100).
    /// </summary>
    public double ErrorRateThreshold { get; init; } = 5.0;

    /// <summary>
    /// Gets the minimum number of unhealthy nodes before raising a critical alert.
    /// </summary>
    public int MinUnhealthyNodesForCriticalAlert { get; init; } = 1;
}
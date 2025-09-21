namespace GameConsole.AI.Clustering.Models;

/// <summary>
/// Represents the overall health status of the AI cluster.
/// </summary>
public class ClusterHealth
{
    /// <summary>
    /// Gets the overall cluster status.
    /// </summary>
    public required ClusterStatus Status { get; init; }

    /// <summary>
    /// Gets the total number of nodes in the cluster.
    /// </summary>
    public int TotalNodes { get; init; }

    /// <summary>
    /// Gets the number of healthy nodes in the cluster.
    /// </summary>
    public int HealthyNodes { get; init; }

    /// <summary>
    /// Gets the number of degraded nodes in the cluster.
    /// </summary>
    public int DegradedNodes { get; init; }

    /// <summary>
    /// Gets the number of unhealthy nodes in the cluster.
    /// </summary>
    public int UnhealthyNodes { get; init; }

    /// <summary>
    /// Gets the cluster performance metrics.
    /// </summary>
    public required ClusterMetrics Metrics { get; init; }

    /// <summary>
    /// Gets the timestamp when this health status was captured.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets any active alerts or warnings.
    /// </summary>
    public IReadOnlyList<ClusterAlert> Alerts { get; init; } = Array.Empty<ClusterAlert>();
}

/// <summary>
/// Represents the overall status of the cluster.
/// </summary>
public enum ClusterStatus
{
    Unknown,
    Healthy,
    Degraded,
    Critical,
    Offline
}

/// <summary>
/// Represents cluster-wide performance metrics.
/// </summary>
public class ClusterMetrics
{
    /// <summary>
    /// Gets the total number of active AI agents across all nodes.
    /// </summary>
    public int ActiveAgents { get; init; }

    /// <summary>
    /// Gets the current cluster-wide request throughput per second.
    /// </summary>
    public double RequestThroughputPerSecond { get; init; }

    /// <summary>
    /// Gets the average response latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; init; }

    /// <summary>
    /// Gets the cluster CPU utilization percentage (0-100).
    /// </summary>
    public double CpuUtilization { get; init; }

    /// <summary>
    /// Gets the cluster memory utilization percentage (0-100).
    /// </summary>
    public double MemoryUtilization { get; init; }

    /// <summary>
    /// Gets the cluster network utilization percentage (0-100).
    /// </summary>
    public double NetworkUtilization { get; init; }
}

/// <summary>
/// Represents an alert or warning about cluster health.
/// </summary>
public class ClusterAlert
{
    /// <summary>
    /// Gets the severity of this alert.
    /// </summary>
    public required AlertSeverity Severity { get; init; }

    /// <summary>
    /// Gets the alert message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the source component that generated this alert.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the timestamp when this alert was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets additional context data for this alert.
    /// </summary>
    public IReadOnlyDictionary<string, string> Context { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents the severity level of a cluster alert.
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
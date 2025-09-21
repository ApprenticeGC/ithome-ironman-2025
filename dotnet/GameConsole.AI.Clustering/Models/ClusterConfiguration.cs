namespace GameConsole.AI.Clustering.Models;

/// <summary>
/// Represents the configuration settings for the AI cluster.
/// </summary>
public class ClusterConfiguration
{
    /// <summary>
    /// Gets the unique name of the cluster.
    /// </summary>
    public required string ClusterName { get; init; }

    /// <summary>
    /// Gets the seed nodes for cluster formation.
    /// </summary>
    public required IReadOnlyList<string> SeedNodes { get; init; }

    /// <summary>
    /// Gets the minimum number of nodes required for the cluster to be operational.
    /// </summary>
    public int MinimumNodes { get; init; } = 1;

    /// <summary>
    /// Gets the maximum number of nodes allowed in the cluster.
    /// </summary>
    public int MaximumNodes { get; init; } = 100;

    /// <summary>
    /// Gets the health check interval in milliseconds.
    /// </summary>
    public int HealthCheckIntervalMs { get; init; } = 5000;

    /// <summary>
    /// Gets the timeout for node discovery in milliseconds.
    /// </summary>
    public int NodeDiscoveryTimeoutMs { get; init; } = 10000;

    /// <summary>
    /// Gets the auto-scaling configuration.
    /// </summary>
    public required AutoScalingConfiguration AutoScaling { get; init; }

    /// <summary>
    /// Gets the network partition handling strategy.
    /// </summary>
    public PartitionHandlingStrategy PartitionHandling { get; init; } = PartitionHandlingStrategy.KeepMajority;

    /// <summary>
    /// Gets additional cluster properties.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents auto-scaling configuration for the cluster.
/// </summary>
public class AutoScalingConfiguration
{
    /// <summary>
    /// Gets whether auto-scaling is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the target CPU utilization percentage for scaling decisions.
    /// </summary>
    public double TargetCpuUtilization { get; init; } = 70.0;

    /// <summary>
    /// Gets the target memory utilization percentage for scaling decisions.
    /// </summary>
    public double TargetMemoryUtilization { get; init; } = 80.0;

    /// <summary>
    /// Gets the minimum time between scaling operations in milliseconds.
    /// </summary>
    public int CooldownPeriodMs { get; init; } = 60000;

    /// <summary>
    /// Gets the scale-up threshold for request queue length.
    /// </summary>
    public int ScaleUpRequestQueueThreshold { get; init; } = 100;

    /// <summary>
    /// Gets the scale-down threshold for idle time in milliseconds.
    /// </summary>
    public int ScaleDownIdleThresholdMs { get; init; } = 300000;
}

/// <summary>
/// Represents strategies for handling network partitions.
/// </summary>
public enum PartitionHandlingStrategy
{
    /// <summary>
    /// Keep the majority partition active, stop minority partitions.
    /// </summary>
    KeepMajority,

    /// <summary>
    /// Keep the oldest partition active based on join timestamp.
    /// </summary>
    KeepOldest,

    /// <summary>
    /// Allow all partitions to remain active (can cause split-brain).
    /// </summary>
    KeepAll,

    /// <summary>
    /// Stop all partitions and require manual intervention.
    /// </summary>
    StopAll
}
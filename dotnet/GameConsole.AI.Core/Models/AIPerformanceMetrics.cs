namespace GameConsole.AI.Models;

/// <summary>
/// Represents performance metrics for AI operations.
/// </summary>
public class AIPerformanceMetrics
{
    /// <summary>
    /// Gets or sets the total execution time.
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the CPU usage percentage (0-100).
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the GPU usage percentage (0-100).
    /// </summary>
    public double GpuUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the throughput in operations per second.
    /// </summary>
    public double ThroughputOps { get; set; }

    /// <summary>
    /// Gets or sets the number of successful operations.
    /// </summary>
    public long SuccessfulOperations { get; set; }

    /// <summary>
    /// Gets or sets the number of failed operations.
    /// </summary>
    public long FailedOperations { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when these metrics were recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional performance-specific metrics.
    /// </summary>
    public IDictionary<string, double> AdditionalMetrics { get; set; } = new Dictionary<string, double>();
}

/// <summary>
/// Represents estimated performance metrics for AI operations.
/// </summary>
public class AIPerformanceEstimate
{
    /// <summary>
    /// Gets or sets the estimated execution time.
    /// </summary>
    public TimeSpan EstimatedExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets the estimated memory usage in bytes.
    /// </summary>
    public long EstimatedMemoryUsage { get; set; }

    /// <summary>
    /// Gets or sets the estimated CPU usage percentage (0-100).
    /// </summary>
    public double EstimatedCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the estimated GPU usage percentage (0-100).
    /// </summary>
    public double EstimatedGpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the confidence level of this estimate (0-1).
    /// </summary>
    public double ConfidenceLevel { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets additional estimation-specific metrics.
    /// </summary>
    public IDictionary<string, double> AdditionalEstimates { get; set; } = new Dictionary<string, double>();
}
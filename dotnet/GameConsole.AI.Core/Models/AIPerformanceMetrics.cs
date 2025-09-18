namespace GameConsole.AI.Core.Models;

/// <summary>
/// Represents performance metrics for AI agent operations,
/// providing insights into execution performance and resource usage.
/// </summary>
public class AIPerformanceMetrics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIPerformanceMetrics"/> class.
    /// </summary>
    public AIPerformanceMetrics()
    {
        Timestamp = DateTimeOffset.UtcNow;
        CustomMetrics = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the timestamp when these metrics were collected.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets or sets the total number of operations executed.
    /// </summary>
    public long TotalOperations { get; set; }

    /// <summary>
    /// Gets or sets the number of successful operations.
    /// </summary>
    public long SuccessfulOperations { get; set; }

    /// <summary>
    /// Gets or sets the number of failed operations.
    /// </summary>
    public long FailedOperations { get; set; }

    /// <summary>
    /// Gets or sets the average execution time in milliseconds.
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the minimum execution time in milliseconds.
    /// </summary>
    public long MinExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum execution time in milliseconds.
    /// </summary>
    public long MaxExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the current memory usage in megabytes.
    /// </summary>
    public long CurrentMemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage in megabytes.
    /// </summary>
    public long PeakMemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets the current CPU usage percentage.
    /// </summary>
    public double CurrentCpuUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the current GPU usage percentage, if applicable.
    /// </summary>
    public double? CurrentGpuUsagePercent { get; set; }

    /// <summary>
    /// Gets or sets the number of active contexts.
    /// </summary>
    public int ActiveContexts { get; set; }

    /// <summary>
    /// Gets or sets the total number of contexts created.
    /// </summary>
    public long TotalContextsCreated { get; set; }

    /// <summary>
    /// Gets or sets the uptime of the agent in milliseconds.
    /// </summary>
    public long UptimeMs { get; set; }

    /// <summary>
    /// Gets custom metrics specific to the AI implementation.
    /// </summary>
    public IReadOnlyDictionary<string, object> CustomMetrics { get; private set; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations * 100 : 0;

    /// <summary>
    /// Gets the failure rate as a percentage.
    /// </summary>
    public double FailureRate => TotalOperations > 0 ? (double)FailedOperations / TotalOperations * 100 : 0;

    /// <summary>
    /// Sets custom metrics for this performance snapshot.
    /// </summary>
    /// <param name="customMetrics">The custom metrics to set.</param>
    /// <returns>This instance for method chaining.</returns>
    public AIPerformanceMetrics WithCustomMetrics(IReadOnlyDictionary<string, object> customMetrics)
    {
        CustomMetrics = customMetrics ?? throw new ArgumentNullException(nameof(customMetrics));
        return this;
    }

    /// <summary>
    /// Creates a snapshot of the current metrics.
    /// </summary>
    /// <returns>A new instance with the current metric values.</returns>
    public AIPerformanceMetrics CreateSnapshot()
    {
        return new AIPerformanceMetrics
        {
            TotalOperations = TotalOperations,
            SuccessfulOperations = SuccessfulOperations,
            FailedOperations = FailedOperations,
            AverageExecutionTimeMs = AverageExecutionTimeMs,
            MinExecutionTimeMs = MinExecutionTimeMs,
            MaxExecutionTimeMs = MaxExecutionTimeMs,
            CurrentMemoryUsageMB = CurrentMemoryUsageMB,
            PeakMemoryUsageMB = PeakMemoryUsageMB,
            CurrentCpuUsagePercent = CurrentCpuUsagePercent,
            CurrentGpuUsagePercent = CurrentGpuUsagePercent,
            ActiveContexts = ActiveContexts,
            TotalContextsCreated = TotalContextsCreated,
            UptimeMs = UptimeMs
        }.WithCustomMetrics(CustomMetrics);
    }
}
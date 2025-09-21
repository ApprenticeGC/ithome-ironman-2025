namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Represents the current status of a deployment operation.
/// </summary>
public class DeploymentStatus
{
    /// <summary>
    /// Gets or sets the deployment identifier.
    /// </summary>
    public required string DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets the current status value.
    /// </summary>
    public DeploymentStatusValue Status { get; set; }

    /// <summary>
    /// Gets or sets the current stage being executed.
    /// </summary>
    public string? CurrentStage { get; set; }

    /// <summary>
    /// Gets or sets the progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage { get; set; }

    /// <summary>
    /// Gets or sets the deployment start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the last update time.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Gets or sets the estimated completion time.
    /// </summary>
    public DateTime? EstimatedCompletion { get; set; }

    /// <summary>
    /// Gets or sets status information for individual stages.
    /// </summary>
    public List<StageStatusInfo> StageStatuses { get; set; } = new();

    /// <summary>
    /// Gets or sets additional status details.
    /// </summary>
    public string? StatusMessage { get; set; }
}

/// <summary>
/// Status information for a deployment stage.
/// </summary>
public class StageStatusInfo
{
    /// <summary>
    /// Gets or sets the stage identifier.
    /// </summary>
    public required string StageId { get; set; }

    /// <summary>
    /// Gets or sets the stage name.
    /// </summary>
    public required string StageName { get; set; }

    /// <summary>
    /// Gets or sets the current stage status.
    /// </summary>
    public StageStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the stage start time.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the stage completion time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets any status message for the stage.
    /// </summary>
    public string? StatusMessage { get; set; }
}

/// <summary>
/// Metrics and performance data for a deployment.
/// </summary>
public class DeploymentMetrics
{
    /// <summary>
    /// Gets or sets the deployment identifier.
    /// </summary>
    public required string DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets the total deployment duration.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Gets or sets the number of stages executed.
    /// </summary>
    public int StagesExecuted { get; set; }

    /// <summary>
    /// Gets or sets the number of successful stages.
    /// </summary>
    public int SuccessfulStages { get; set; }

    /// <summary>
    /// Gets or sets the number of failed stages.
    /// </summary>
    public int FailedStages { get; set; }

    /// <summary>
    /// Gets or sets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => StagesExecuted > 0 ? (double)SuccessfulStages / StagesExecuted * 100 : 0;

    /// <summary>
    /// Gets or sets resource utilization metrics.
    /// </summary>
    public ResourceMetrics ResourceUsage { get; set; } = new();

    /// <summary>
    /// Gets or sets performance metrics for individual stages.
    /// </summary>
    public List<StageMetrics> StageMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets custom metrics data.
    /// </summary>
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Resource utilization metrics.
/// </summary>
public class ResourceMetrics
{
    /// <summary>
    /// Gets or sets the peak CPU usage percentage.
    /// </summary>
    public double PeakCpuUsage { get; set; }

    /// <summary>
    /// Gets or sets the peak memory usage in MB.
    /// </summary>
    public long PeakMemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets the network data transferred in MB.
    /// </summary>
    public long NetworkTransferMB { get; set; }

    /// <summary>
    /// Gets or sets the disk I/O operations count.
    /// </summary>
    public long DiskIOOperations { get; set; }
}

/// <summary>
/// Performance metrics for a deployment stage.
/// </summary>
public class StageMetrics
{
    /// <summary>
    /// Gets or sets the stage identifier.
    /// </summary>
    public required string StageId { get; set; }

    /// <summary>
    /// Gets or sets the stage execution duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets whether the stage was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts.
    /// </summary>
    public int RetryAttempts { get; set; }

    /// <summary>
    /// Gets or sets stage-specific performance data.
    /// </summary>
    public Dictionary<string, object> PerformanceData { get; set; } = new();
}
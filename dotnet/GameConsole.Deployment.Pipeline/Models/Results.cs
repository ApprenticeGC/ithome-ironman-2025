namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Result of a deployment operation.
/// </summary>
public class DeploymentResult
{
    /// <summary>
    /// Gets or sets the deployment identifier.
    /// </summary>
    public required string DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets whether the deployment was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the final status of the deployment.
    /// </summary>
    public DeploymentStatusValue Status { get; set; }

    /// <summary>
    /// Gets or sets the deployment start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the deployment end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the duration of the deployment.
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;

    /// <summary>
    /// Gets or sets any error message if the deployment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the results of individual stages.
    /// </summary>
    public List<StageResult> StageResults { get; set; } = new();

    /// <summary>
    /// Gets or sets deployment metrics.
    /// </summary>
    public DeploymentMetrics? Metrics { get; set; }

    /// <summary>
    /// Gets or sets additional result data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Result of a deployment stage execution.
/// </summary>
public class StageResult
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
    /// Gets or sets whether the stage was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the final status of the stage.
    /// </summary>
    public StageStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the stage start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the stage end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the duration of the stage execution.
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;

    /// <summary>
    /// Gets or sets any error message if the stage failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the workflow result if applicable.
    /// </summary>
    public WorkflowResult? WorkflowResult { get; set; }

    /// <summary>
    /// Gets or sets health check results.
    /// </summary>
    public List<HealthCheckResult> HealthCheckResults { get; set; } = new();

    /// <summary>
    /// Gets or sets stage-specific output data.
    /// </summary>
    public Dictionary<string, object> OutputData { get; set; } = new();
}

/// <summary>
/// Result of a workflow execution.
/// </summary>
public class WorkflowResult
{
    /// <summary>
    /// Gets or sets the workflow identifier.
    /// </summary>
    public required string WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets whether the workflow was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the workflow status.
    /// </summary>
    public WorkflowStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the workflow start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the workflow end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the workflow execution duration.
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;

    /// <summary>
    /// Gets or sets any error message if the workflow failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the workflow execution log or output.
    /// </summary>
    public string? ExecutionLog { get; set; }

    /// <summary>
    /// Gets or sets workflow-specific output data.
    /// </summary>
    public Dictionary<string, object> OutputData { get; set; } = new();
}

/// <summary>
/// Result of a rollback operation.
/// </summary>
public class RollbackResult
{
    /// <summary>
    /// Gets or sets the rollback identifier.
    /// </summary>
    public required string RollbackId { get; set; }

    /// <summary>
    /// Gets or sets the original deployment identifier that was rolled back.
    /// </summary>
    public required string DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets whether the rollback was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the rollback status.
    /// </summary>
    public RollbackStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the version that was rolled back to.
    /// </summary>
    public string? RolledBackToVersion { get; set; }

    /// <summary>
    /// Gets or sets the rollback start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the rollback end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets or sets the rollback duration.
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;

    /// <summary>
    /// Gets or sets any error message if the rollback failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the reason for the rollback.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets additional rollback data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}
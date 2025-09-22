namespace GameConsole.Deployment.Core;

/// <summary>
/// Represents a deployment stage configuration.
/// </summary>
public class DeploymentStage
{
    /// <summary>
    /// Gets the unique identifier for this stage.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets the name of this deployment stage.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the target environment for this stage.
    /// </summary>
    public DeploymentEnvironment Environment { get; set; }

    /// <summary>
    /// Gets a value indicating whether this stage requires manual approval.
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Gets the order in which this stage should execute.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets the timeout for this stage in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 3600; // 1 hour default

    /// <summary>
    /// Gets additional configuration for this stage.
    /// </summary>
    public IReadOnlyDictionary<string, string> Configuration { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents a deployment stage execution result.
/// </summary>
public class DeploymentStageResult
{
    /// <summary>
    /// Gets the stage that was executed.
    /// </summary>
    public DeploymentStage Stage { get; set; } = null!;

    /// <summary>
    /// Gets the status of the stage execution.
    /// </summary>
    public StageStatus Status { get; set; }

    /// <summary>
    /// Gets the start time of the stage execution.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets the end time of the stage execution.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Gets any error message if the stage failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets additional output from the stage execution.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Gets the duration of the stage execution.
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
}
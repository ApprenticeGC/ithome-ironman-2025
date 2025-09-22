namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Represents the result of a deployment operation.
/// </summary>
public class DeploymentResult
{
    /// <summary>
    /// Gets or sets the deployment context that was executed.
    /// </summary>
    public DeploymentContext Context { get; set; } = new();

    /// <summary>
    /// Gets or sets the final status of the deployment.
    /// </summary>
    public DeploymentStatus Status { get; set; } = DeploymentStatus.NotStarted;

    /// <summary>
    /// Gets or sets the stage where the deployment completed or failed.
    /// </summary>
    public DeploymentStage CompletedStage { get; set; }

    /// <summary>
    /// Gets or sets the error message if the deployment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception details if the deployment failed.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the deployment started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the deployment completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets the total duration of the deployment operation.
    /// </summary>
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

    /// <summary>
    /// Gets or sets the deployment artifacts and outputs.
    /// </summary>
    public Dictionary<string, object> Artifacts { get; set; } = new();

    /// <summary>
    /// Gets or sets the deployment metrics and performance data.
    /// </summary>
    public Dictionary<string, double> Metrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the logs from the deployment operation.
    /// </summary>
    public List<string> Logs { get; set; } = new();
}
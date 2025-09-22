namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Manages deployment stages, including approval gates and validation.
/// </summary>
public interface IDeploymentStageManager
{
    /// <summary>
    /// Executes a specific deployment stage.
    /// </summary>
    /// <param name="context">The deployment context.</param>
    /// <param name="stage">The stage to execute.</param>
    /// <param name="configuration">The stage configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The result of the stage execution.</returns>
    Task<StageExecutionResult> ExecuteStageAsync(
        DeploymentContext context,
        DeploymentStage stage,
        StageConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a stage can be executed based on prerequisites and approval gates.
    /// </summary>
    /// <param name="context">The deployment context.</param>
    /// <param name="stage">The stage to validate.</param>
    /// <param name="configuration">The stage configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Validation result indicating if the stage can proceed.</returns>
    Task<StageValidationResult> ValidateStageAsync(
        DeploymentContext context,
        DeploymentStage stage,
        StageConfiguration configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an approval for a deployment stage.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="stage">The stage being approved.</param>
    /// <param name="approvedBy">The user or system providing approval.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the approval was successfully recorded.</returns>
    Task<bool> RecordApprovalAsync(
        string deploymentId,
        DeploymentStage stage,
        string approvedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the approval status for a specific stage.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="stage">The stage to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The approval status information.</returns>
    Task<ApprovalStatus?> GetApprovalStatusAsync(
        string deploymentId,
        DeploymentStage stage,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of a stage execution.
/// </summary>
public class StageExecutionResult
{
    /// <summary>
    /// Gets or sets the executed stage.
    /// </summary>
    public DeploymentStage Stage { get; set; }

    /// <summary>
    /// Gets or sets whether the stage executed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the stage failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception if the stage failed.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the duration of the stage execution.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets or sets the outputs from the stage execution.
    /// </summary>
    public Dictionary<string, object> Outputs { get; set; } = new();

    /// <summary>
    /// Gets or sets the logs from the stage execution.
    /// </summary>
    public List<string> Logs { get; set; } = new();
}

/// <summary>
/// Represents the result of a stage validation.
/// </summary>
public class StageValidationResult
{
    /// <summary>
    /// Gets or sets whether the stage can be executed.
    /// </summary>
    public bool CanProceed { get; set; }

    /// <summary>
    /// Gets or sets the reason if the stage cannot proceed.
    /// </summary>
    public string? BlockingReason { get; set; }

    /// <summary>
    /// Gets or sets whether the stage is waiting for approval.
    /// </summary>
    public bool WaitingForApproval { get; set; }

    /// <summary>
    /// Gets or sets the list of required approvers.
    /// </summary>
    public List<string> RequiredApprovers { get; set; } = new();
}

/// <summary>
/// Represents the approval status for a deployment stage.
/// </summary>
public class ApprovalStatus
{
    /// <summary>
    /// Gets or sets whether the stage is approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets the user who provided the approval.
    /// </summary>
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Gets or sets when the approval was given.
    /// </summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>
    /// Gets or sets any approval comments or notes.
    /// </summary>
    public string? Comments { get; set; }
}
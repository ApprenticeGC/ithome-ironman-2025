namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Defines a deployment stage interface for individual stages in a deployment pipeline.
/// Supports approval gates, validation, and stage-specific operations.
/// </summary>
public interface IDeploymentStage
{
    /// <summary>
    /// Gets the unique identifier for this deployment stage.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of this deployment stage.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the order of execution for this stage within the pipeline.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Gets a value indicating whether this stage requires manual approval.
    /// </summary>
    bool RequiresApproval { get; }

    /// <summary>
    /// Gets the environment name this stage targets (e.g., "development", "staging", "production").
    /// </summary>
    string TargetEnvironment { get; }

    /// <summary>
    /// Gets the current status of this deployment stage.
    /// </summary>
    StageStatus Status { get; }

    /// <summary>
    /// Executes the deployment stage asynchronously.
    /// </summary>
    /// <param name="stageConfig">Configuration for the stage execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the stage execution result.</returns>
    Task<StageResult> ExecuteAsync(StageConfig stageConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the stage can be executed with the specified configuration.
    /// </summary>
    /// <param name="stageConfig">Configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing validation results.</returns>
    Task<ValidationResult> ValidateAsync(StageConfig stageConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits approval for a stage that requires manual approval.
    /// </summary>
    /// <param name="approval">Approval details including decision and approver information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task indicating completion of the approval submission.</returns>
    Task SubmitApprovalAsync(StageApproval approval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels the execution of this stage if it is currently running.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task indicating completion of the cancellation request.</returns>
    Task CancelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the stage status changes.
    /// </summary>
    event EventHandler<StageStatusChangedEventArgs> StatusChanged;
}
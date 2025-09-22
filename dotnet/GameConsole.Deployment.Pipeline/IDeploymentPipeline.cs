using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Main interface for deployment pipeline orchestration.
/// Provides end-to-end deployment capabilities with stage management and rollback support.
/// </summary>
public interface IDeploymentPipeline : IService
{
    /// <summary>
    /// Executes a deployment pipeline with the specified context.
    /// </summary>
    /// <param name="context">The deployment context containing configuration and metadata.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The result of the deployment operation.</returns>
    Task<DeploymentResult> DeployAsync(DeploymentContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a deployment operation.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current deployment status and progress information.</returns>
    Task<DeploymentResult?> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an in-progress deployment operation.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment to cancel.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the deployment was successfully cancelled.</returns>
    Task<bool> CancelDeploymentAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a deployment stage that is waiting for manual approval.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment.</param>
    /// <param name="stage">The stage to approve.</param>
    /// <param name="approvedBy">The user or system approving the stage.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the approval was successfully recorded.</returns>
    Task<bool> ApproveStageAsync(string deploymentId, DeploymentStage stage, string approvedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the deployment history for analysis and reporting.
    /// </summary>
    /// <param name="environment">Optional environment filter.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of historical deployment results.</returns>
    Task<IReadOnlyCollection<DeploymentResult>> GetDeploymentHistoryAsync(string? environment = null, int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a deployment status changes.
    /// </summary>
    event EventHandler<DeploymentStatusChangedEventArgs>? DeploymentStatusChanged;
}

/// <summary>
/// Event arguments for deployment status changes.
/// </summary>
public class DeploymentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the deployment identifier.
    /// </summary>
    public string DeploymentId { get; }

    /// <summary>
    /// Gets the previous deployment status.
    /// </summary>
    public DeploymentStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the current deployment status.
    /// </summary>
    public DeploymentStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the current deployment stage.
    /// </summary>
    public DeploymentStage CurrentStage { get; }

    /// <summary>
    /// Gets additional context information.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="previousStatus">The previous status.</param>
    /// <param name="currentStatus">The current status.</param>
    /// <param name="currentStage">The current stage.</param>
    /// <param name="message">Optional message.</param>
    public DeploymentStatusChangedEventArgs(
        string deploymentId,
        DeploymentStatus previousStatus,
        DeploymentStatus currentStatus,
        DeploymentStage currentStage,
        string? message = null)
    {
        DeploymentId = deploymentId;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        CurrentStage = currentStage;
        Message = message;
    }
}
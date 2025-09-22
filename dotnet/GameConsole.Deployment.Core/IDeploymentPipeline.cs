using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Core;

/// <summary>
/// Interface for managing deployment pipelines with CI/CD integration and rollback capabilities.
/// </summary>
public interface IDeploymentPipeline : IService, ICapabilityProvider
{
    /// <summary>
    /// Starts a new deployment with the specified configuration.
    /// </summary>
    /// <param name="stages">The deployment stages to execute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a unique deployment identifier.</returns>
    Task<string> StartDeploymentAsync(IEnumerable<DeploymentStage> stages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a deployment.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the deployment status.</returns>
    Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the results of all stages for a deployment.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns stage results.</returns>
    Task<IEnumerable<DeploymentStageResult>> GetStageResultsAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an active deployment.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment to cancel.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task CancelDeploymentAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves a deployment stage that is pending approval.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment.</param>
    /// <param name="stageId">The identifier of the stage to approve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ApproveStageAsync(string deploymentId, string stageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a deployment status changes.
    /// </summary>
    event EventHandler<DeploymentStatusChangedEventArgs>? DeploymentStatusChanged;

    /// <summary>
    /// Event raised when a stage status changes.
    /// </summary>
    event EventHandler<StageStatusChangedEventArgs>? StageStatusChanged;
}

/// <summary>
/// Event arguments for deployment status changes.
/// </summary>
public class DeploymentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the deployment identifier.
    /// </summary>
    public required string DeploymentId { get; init; }

    /// <summary>
    /// Gets the previous status.
    /// </summary>
    public required DeploymentStatus PreviousStatus { get; init; }

    /// <summary>
    /// Gets the new status.
    /// </summary>
    public required DeploymentStatus NewStatus { get; init; }

    /// <summary>
    /// Gets the timestamp of the status change.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}

/// <summary>
/// Event arguments for stage status changes.
/// </summary>
public class StageStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the deployment identifier.
    /// </summary>
    public required string DeploymentId { get; init; }

    /// <summary>
    /// Gets the stage identifier.
    /// </summary>
    public required string StageId { get; init; }

    /// <summary>
    /// Gets the previous status.
    /// </summary>
    public required StageStatus PreviousStatus { get; init; }

    /// <summary>
    /// Gets the new status.
    /// </summary>
    public required StageStatus NewStatus { get; init; }

    /// <summary>
    /// Gets the timestamp of the status change.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
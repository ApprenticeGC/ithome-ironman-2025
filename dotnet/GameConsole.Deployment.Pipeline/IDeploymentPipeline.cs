using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Defines the main deployment pipeline orchestration interface.
/// Manages automated deployment processes with CI/CD integration and rollback capabilities.
/// </summary>
public interface IDeploymentPipeline : IService
{
    /// <summary>
    /// Executes a complete deployment pipeline for the specified configuration.
    /// </summary>
    /// <param name="deploymentConfig">Configuration defining the deployment parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the deployment result.</returns>
    Task<DeploymentResult> ExecutePipelineAsync(DeploymentConfig deploymentConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a specific deployment stage.
    /// </summary>
    /// <param name="stageConfig">Configuration for the deployment stage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the stage execution result.</returns>
    Task<StageResult> ExecuteStageAsync(StageConfig stageConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates rollback to a previous deployment version.
    /// </summary>
    /// <param name="rollbackConfig">Configuration for the rollback operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the rollback result.</returns>
    Task<RollbackResult> RollbackAsync(RollbackConfig rollbackConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates deployment prerequisites and dependencies.
    /// </summary>
    /// <param name="deploymentConfig">Configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing validation results.</returns>
    Task<ValidationResult> ValidateDeploymentAsync(DeploymentConfig deploymentConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of an active deployment.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the current deployment status.</returns>
    Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deployment metrics for performance tracking.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing deployment metrics.</returns>
    Task<DeploymentMetrics> GetDeploymentMetricsAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when deployment status changes.
    /// </summary>
    event EventHandler<DeploymentStatusChangedEventArgs> StatusChanged;
}
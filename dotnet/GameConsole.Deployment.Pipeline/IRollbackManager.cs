using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Defines the rollback manager interface for deployment recovery operations.
/// Handles automatic and manual rollback scenarios with versioning support.
/// </summary>
public interface IRollbackManager : IService
{
    /// <summary>
    /// Initiates an automatic rollback when deployment failures are detected.
    /// </summary>
    /// <param name="deploymentId">The failed deployment identifier.</param>
    /// <param name="reason">The reason for the rollback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the rollback result.</returns>
    Task<RollbackResult> RollbackAsync(string deploymentId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a rollback to a specific previous version.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier to rollback.</param>
    /// <param name="targetVersion">The target version to rollback to.</param>
    /// <param name="reason">The reason for the rollback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the rollback result.</returns>
    Task<RollbackResult> RollbackToVersionAsync(string deploymentId, string targetVersion, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available versions that can be used for rollback.
    /// </summary>
    /// <param name="environment">The target environment for rollback options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing available rollback versions.</returns>
    Task<IReadOnlyCollection<DeploymentVersion>> GetRollbackOptionsAsync(string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a rollback operation can be performed safely.
    /// </summary>
    /// <param name="rollbackConfig">Configuration for the rollback operation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing validation results.</returns>
    Task<ValidationResult> ValidateRollbackAsync(RollbackConfig rollbackConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of an active rollback operation.
    /// </summary>
    /// <param name="rollbackId">The rollback operation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the current rollback status.</returns>
    Task<RollbackStatus> GetRollbackStatusAsync(string rollbackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures automatic rollback triggers and conditions.
    /// </summary>
    /// <param name="triggers">The rollback triggers to configure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task indicating completion of the configuration.</returns>
    Task ConfigureAutoRollbackAsync(RollbackTriggers triggers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a rollback operation status changes.
    /// </summary>
    event EventHandler<RollbackStatusChangedEventArgs> StatusChanged;
}
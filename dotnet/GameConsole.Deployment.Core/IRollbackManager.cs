using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Core;

/// <summary>
/// Interface for managing rollback operations in deployment pipelines.
/// </summary>
public interface IRollbackManager : IService
{
    /// <summary>
    /// Initiates a rollback for a failed deployment.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment to rollback.</param>
    /// <param name="targetVersion">The target version to rollback to. If null, rolls back to previous version.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the rollback deployment identifier.</returns>
    Task<string> InitiateRollbackAsync(string deploymentId, string? targetVersion = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the rollback status for a deployment.
    /// </summary>
    /// <param name="rollbackId">The unique identifier of the rollback operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the rollback status.</returns>
    Task<DeploymentStatus> GetRollbackStatusAsync(string rollbackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a deployment can be rolled back.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if rollback is possible.</returns>
    Task<bool> CanRollbackAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available rollback targets for a deployment.
    /// </summary>
    /// <param name="deploymentId">The unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns available rollback versions.</returns>
    Task<IEnumerable<string>> GetRollbackTargetsAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a rollback status changes.
    /// </summary>
    event EventHandler<RollbackStatusChangedEventArgs>? RollbackStatusChanged;
}

/// <summary>
/// Event arguments for rollback status changes.
/// </summary>
public class RollbackStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the rollback identifier.
    /// </summary>
    public required string RollbackId { get; init; }

    /// <summary>
    /// Gets the original deployment identifier.
    /// </summary>
    public required string OriginalDeploymentId { get; init; }

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
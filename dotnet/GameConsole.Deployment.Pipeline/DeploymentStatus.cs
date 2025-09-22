namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Represents the current status of a deployment operation.
/// </summary>
public enum DeploymentStatus
{
    /// <summary>
    /// Deployment has not started yet.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Deployment is currently in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Deployment is waiting for approval to proceed.
    /// </summary>
    PendingApproval,

    /// <summary>
    /// Deployment has been paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Deployment completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Deployment failed and requires intervention.
    /// </summary>
    Failed,

    /// <summary>
    /// Deployment was cancelled by user or system.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Deployment is being rolled back due to failure.
    /// </summary>
    RollingBack,

    /// <summary>
    /// Deployment was successfully rolled back.
    /// </summary>
    RolledBack,

    /// <summary>
    /// Rollback operation failed.
    /// </summary>
    RollbackFailed
}
namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Represents the status of a deployment operation.
/// </summary>
public enum DeploymentStatusValue
{
    /// <summary>
    /// Deployment is queued and waiting to start.
    /// </summary>
    Queued,

    /// <summary>
    /// Deployment is currently in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Deployment completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Deployment failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Deployment was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Deployment is waiting for approval.
    /// </summary>
    WaitingForApproval,

    /// <summary>
    /// Deployment was rejected during approval.
    /// </summary>
    Rejected,

    /// <summary>
    /// Deployment is being rolled back.
    /// </summary>
    RollingBack,

    /// <summary>
    /// Deployment was successfully rolled back.
    /// </summary>
    RolledBack
}

/// <summary>
/// Represents the status of a deployment stage.
/// </summary>
public enum StageStatus
{
    /// <summary>
    /// Stage is pending execution.
    /// </summary>
    Pending,

    /// <summary>
    /// Stage is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Stage completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Stage failed during execution.
    /// </summary>
    Failed,

    /// <summary>
    /// Stage was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Stage is waiting for manual approval.
    /// </summary>
    WaitingForApproval,

    /// <summary>
    /// Stage was rejected during approval.
    /// </summary>
    Rejected,

    /// <summary>
    /// Stage was skipped.
    /// </summary>
    Skipped
}

/// <summary>
/// Represents the status of a workflow execution.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// Workflow is queued and waiting to start.
    /// </summary>
    Queued,

    /// <summary>
    /// Workflow is currently in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Workflow completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Workflow failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Workflow was cancelled.
    /// </summary>
    Cancelled
}

/// <summary>
/// Represents the status of a rollback operation.
/// </summary>
public enum RollbackStatus
{
    /// <summary>
    /// Rollback is pending initiation.
    /// </summary>
    Pending,

    /// <summary>
    /// Rollback is currently in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Rollback completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Rollback failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Rollback was cancelled.
    /// </summary>
    Cancelled
}
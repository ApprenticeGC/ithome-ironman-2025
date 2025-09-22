namespace GameConsole.Deployment.Core;

/// <summary>
/// Represents the status of a deployment operation.
/// </summary>
public enum DeploymentStatus
{
    /// <summary>
    /// Deployment is waiting to start.
    /// </summary>
    Pending,

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
    /// Deployment was rolled back.
    /// </summary>
    RolledBack
}

/// <summary>
/// Represents the status of a deployment stage.
/// </summary>
public enum StageStatus
{
    /// <summary>
    /// Stage is waiting to start.
    /// </summary>
    Waiting,

    /// <summary>
    /// Stage is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// Stage completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Stage failed during execution.
    /// </summary>
    Failed,

    /// <summary>
    /// Stage was skipped.
    /// </summary>
    Skipped,

    /// <summary>
    /// Stage requires manual approval.
    /// </summary>
    PendingApproval
}

/// <summary>
/// Represents different deployment environments.
/// </summary>
public enum DeploymentEnvironment
{
    /// <summary>
    /// Local development environment.
    /// </summary>
    Development,

    /// <summary>
    /// Testing environment.
    /// </summary>
    Testing,

    /// <summary>
    /// Staging environment.
    /// </summary>
    Staging,

    /// <summary>
    /// Production environment.
    /// </summary>
    Production
}
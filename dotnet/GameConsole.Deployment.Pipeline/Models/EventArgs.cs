namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Event arguments for deployment status changes.
/// </summary>
public class DeploymentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="previousStatus">The previous deployment status.</param>
    /// <param name="currentStatus">The current deployment status.</param>
    public DeploymentStatusChangedEventArgs(string deploymentId, DeploymentStatusValue previousStatus, DeploymentStatusValue currentStatus)
    {
        DeploymentId = deploymentId;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the deployment identifier.
    /// </summary>
    public string DeploymentId { get; }

    /// <summary>
    /// Gets the previous deployment status.
    /// </summary>
    public DeploymentStatusValue PreviousStatus { get; }

    /// <summary>
    /// Gets the current deployment status.
    /// </summary>
    public DeploymentStatusValue CurrentStatus { get; }

    /// <summary>
    /// Gets the timestamp when the status change occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets or sets additional information about the status change.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets additional data related to the status change.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Event arguments for stage status changes.
/// </summary>
public class StageStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StageStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="stageId">The stage identifier.</param>
    /// <param name="previousStatus">The previous stage status.</param>
    /// <param name="currentStatus">The current stage status.</param>
    public StageStatusChangedEventArgs(string stageId, StageStatus previousStatus, StageStatus currentStatus)
    {
        StageId = stageId;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the stage identifier.
    /// </summary>
    public string StageId { get; }

    /// <summary>
    /// Gets the previous stage status.
    /// </summary>
    public StageStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the current stage status.
    /// </summary>
    public StageStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the timestamp when the status change occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets or sets additional information about the status change.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the deployment identifier this stage belongs to.
    /// </summary>
    public string? DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets additional data related to the status change.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Event arguments for rollback status changes.
/// </summary>
public class RollbackStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RollbackStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="rollbackId">The rollback identifier.</param>
    /// <param name="previousStatus">The previous rollback status.</param>
    /// <param name="currentStatus">The current rollback status.</param>
    public RollbackStatusChangedEventArgs(string rollbackId, RollbackStatus previousStatus, RollbackStatus currentStatus)
    {
        RollbackId = rollbackId;
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the rollback identifier.
    /// </summary>
    public string RollbackId { get; }

    /// <summary>
    /// Gets the previous rollback status.
    /// </summary>
    public RollbackStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the current rollback status.
    /// </summary>
    public RollbackStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the timestamp when the status change occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets or sets the original deployment identifier being rolled back.
    /// </summary>
    public string? DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets additional information about the status change.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets additional data related to the status change.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}
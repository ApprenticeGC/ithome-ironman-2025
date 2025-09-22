namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Manages rollback operations for failed or problematic deployments.
/// </summary>
public interface IRollbackManager
{
    /// <summary>
    /// Initiates a rollback operation for a deployment.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier to roll back.</param>
    /// <param name="targetVersion">Optional specific version to roll back to. If null, rolls back to previous version.</param>
    /// <param name="reason">The reason for the rollback.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The result of the rollback operation.</returns>
    Task<RollbackResult> InitiateRollbackAsync(
        string deploymentId,
        string? targetVersion = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a deployment can be rolled back.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if rollback is possible for this deployment.</returns>
    Task<bool> CanRollbackAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the rollback options available for a deployment.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of available rollback options.</returns>
    Task<IReadOnlyCollection<RollbackOption>> GetRollbackOptionsAsync(
        string deploymentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the rollback history for analysis and reporting.
    /// </summary>
    /// <param name="environment">Optional environment filter.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of historical rollback results.</returns>
    Task<IReadOnlyCollection<RollbackResult>> GetRollbackHistoryAsync(
        string? environment = null,
        int limit = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures automatic rollback triggers and conditions.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="triggers">The rollback triggers to configure.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the configuration was successfully applied.</returns>
    Task<bool> ConfigureAutoRollbackAsync(
        string deploymentId,
        IReadOnlyCollection<RollbackTrigger> triggers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a rollback operation status changes.
    /// </summary>
    event EventHandler<RollbackStatusChangedEventArgs>? RollbackStatusChanged;
}

/// <summary>
/// Represents the result of a rollback operation.
/// </summary>
public class RollbackResult
{
    /// <summary>
    /// Gets or sets the original deployment identifier.
    /// </summary>
    public string OriginalDeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rollback operation identifier.
    /// </summary>
    public string RollbackId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the rollback was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the version that was rolled back to.
    /// </summary>
    public string? RolledBackToVersion { get; set; }

    /// <summary>
    /// Gets or sets the reason for the rollback.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the error message if rollback failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when rollback started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when rollback completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets the duration of the rollback operation.
    /// </summary>
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

    /// <summary>
    /// Gets or sets additional rollback metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a rollback option for a deployment.
/// </summary>
public class RollbackOption
{
    /// <summary>
    /// Gets or sets the target version for this rollback option.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for this rollback option.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the recommended rollback option.
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// Gets or sets additional information about this rollback option.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Defines conditions that can trigger automatic rollback.
/// </summary>
public class RollbackTrigger
{
    /// <summary>
    /// Gets or sets the type of trigger.
    /// </summary>
    public RollbackTriggerType Type { get; set; }

    /// <summary>
    /// Gets or sets the condition that triggers rollback.
    /// </summary>
    public string Condition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the threshold value for the trigger.
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Gets or sets how long to wait before triggering rollback.
    /// </summary>
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Types of rollback triggers.
/// </summary>
public enum RollbackTriggerType
{
    /// <summary>
    /// Trigger on error rate threshold.
    /// </summary>
    ErrorRate,

    /// <summary>
    /// Trigger on response time degradation.
    /// </summary>
    ResponseTime,

    /// <summary>
    /// Trigger on throughput drop.
    /// </summary>
    Throughput,

    /// <summary>
    /// Trigger on custom health check failure.
    /// </summary>
    HealthCheck,

    /// <summary>
    /// Trigger on manual intervention.
    /// </summary>
    Manual
}

/// <summary>
/// Event arguments for rollback status changes.
/// </summary>
public class RollbackStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the rollback identifier.
    /// </summary>
    public string RollbackId { get; }

    /// <summary>
    /// Gets the original deployment identifier.
    /// </summary>
    public string OriginalDeploymentId { get; }

    /// <summary>
    /// Gets the current status of the rollback.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets additional context information.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RollbackStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="rollbackId">The rollback identifier.</param>
    /// <param name="originalDeploymentId">The original deployment identifier.</param>
    /// <param name="status">The current status.</param>
    /// <param name="message">Optional message.</param>
    public RollbackStatusChangedEventArgs(string rollbackId, string originalDeploymentId, string status, string? message = null)
    {
        RollbackId = rollbackId;
        OriginalDeploymentId = originalDeploymentId;
        Status = status;
        Message = message;
    }
}
namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets validation error messages.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets validation warning messages.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Gets or sets additional validation details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Gets whether there are any validation errors.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Gets whether there are any validation warnings.
    /// </summary>
    public bool HasWarnings => Warnings.Count > 0;
}

/// <summary>
/// Represents a deployment version that can be used for rollback.
/// </summary>
public class DeploymentVersion
{
    /// <summary>
    /// Gets or sets the version identifier.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the deployment identifier for this version.
    /// </summary>
    public required string DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets the deployment timestamp.
    /// </summary>
    public DateTime DeployedAt { get; set; }

    /// <summary>
    /// Gets or sets the environment where this version is deployed.
    /// </summary>
    public required string Environment { get; set; }

    /// <summary>
    /// Gets or sets the commit or build identifier associated with this version.
    /// </summary>
    public string? CommitSha { get; set; }

    /// <summary>
    /// Gets or sets whether this version is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets whether this version is available for rollback.
    /// </summary>
    public bool IsRollbackEligible { get; set; } = true;

    /// <summary>
    /// Gets or sets additional version metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents approval information for a deployment stage.
/// </summary>
public class StageApproval
{
    /// <summary>
    /// Gets or sets the stage identifier being approved.
    /// </summary>
    public required string StageId { get; set; }

    /// <summary>
    /// Gets or sets whether the stage is approved.
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Gets or sets the approver's identifier.
    /// </summary>
    public required string ApproverId { get; set; }

    /// <summary>
    /// Gets or sets the approver's name.
    /// </summary>
    public string? ApproverName { get; set; }

    /// <summary>
    /// Gets or sets the approval timestamp.
    /// </summary>
    public DateTime ApprovalTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets comments from the approver.
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Gets or sets the reason for rejection if not approved.
    /// </summary>
    public string? RejectionReason { get; set; }
}

/// <summary>
/// Result of a health check operation.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Gets or sets the health check endpoint.
    /// </summary>
    public required string Endpoint { get; set; }

    /// <summary>
    /// Gets or sets whether the health check was successful.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the response time for the health check.
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code returned.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the health check timestamp.
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets any error message from the health check.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the response content if applicable.
    /// </summary>
    public string? ResponseContent { get; set; }

    /// <summary>
    /// Gets or sets additional health check data.
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
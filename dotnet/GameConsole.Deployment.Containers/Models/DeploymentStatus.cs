namespace GameConsole.Deployment.Containers.Models;

/// <summary>
/// Status information about a deployment.
/// </summary>
public class DeploymentStatus
{
    /// <summary>
    /// Unique identifier for the deployment.
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the deployment (Running, Pending, Failed, etc.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Number of replicas that are ready and running.
    /// </summary>
    public int ReadyReplicas { get; set; }

    /// <summary>
    /// Total number of replicas desired for this deployment.
    /// </summary>
    public int TotalReplicas { get; set; }

    /// <summary>
    /// When the deployment status was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Additional details about the deployment status.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();

    /// <summary>
    /// Current health status of the deployment.
    /// </summary>
    public string HealthStatus { get; set; } = "Unknown";

    /// <summary>
    /// List of conditions affecting the deployment.
    /// </summary>
    public List<DeploymentCondition> Conditions { get; set; } = new();

    /// <summary>
    /// Gets whether all replicas are ready.
    /// </summary>
    public bool IsReady => ReadyReplicas == TotalReplicas && TotalReplicas > 0;

    /// <summary>
    /// Gets whether the deployment is healthy.
    /// </summary>
    public bool IsHealthy => HealthStatus.Equals("Healthy", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Represents a condition affecting a deployment.
/// </summary>
public class DeploymentCondition
{
    /// <summary>
    /// Type of the condition.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Status of the condition (True, False, Unknown).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message about the condition.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the current status.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// When the condition was last updated.
    /// </summary>
    public DateTime LastTransitionTime { get; set; }
}
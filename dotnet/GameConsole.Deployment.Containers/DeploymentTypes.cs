namespace GameConsole.Deployment.Containers;

/// <summary>
/// Result of a deployment operation.
/// </summary>
public class DeploymentResult
{
    /// <summary>
    /// Gets or sets whether the deployment was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the deployment identifier.
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployed service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any error message if deployment failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets metadata about the deployment.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when deployment completed.
    /// </summary>
    public DateTime DeployedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Result of a scaling operation.
/// </summary>
public class ScalingResult
{
    /// <summary>
    /// Gets or sets whether the scaling was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the service name that was scaled.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous number of instances.
    /// </summary>
    public int PreviousInstanceCount { get; set; }

    /// <summary>
    /// Gets or sets the new number of instances.
    /// </summary>
    public int NewInstanceCount { get; set; }

    /// <summary>
    /// Gets or sets any error message if scaling failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when scaling completed.
    /// </summary>
    public DateTime ScaledAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Status of a deployed service.
/// </summary>
public class ServiceStatus
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current deployment status.
    /// </summary>
    public DeploymentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the number of running instances.
    /// </summary>
    public int RunningInstances { get; set; }

    /// <summary>
    /// Gets or sets the desired number of instances.
    /// </summary>
    public int DesiredInstances { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last update.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets additional status information.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Information about a deployed service.
/// </summary>
public class ServiceInfo
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container image.
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment strategy used.
    /// </summary>
    public DeploymentStrategy Strategy { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public DeploymentStatus Status { get; set; }

    /// <summary>
    /// Gets or sets service labels.
    /// </summary>
    public Dictionary<string, string> Labels { get; set; } = new();
}

/// <summary>
/// Deployment status enumeration.
/// </summary>
public enum DeploymentStatus
{
    /// <summary>
    /// Deployment is in progress.
    /// </summary>
    Deploying,

    /// <summary>
    /// Deployment is running successfully.
    /// </summary>
    Running,

    /// <summary>
    /// Deployment has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Deployment is being updated.
    /// </summary>
    Updating,

    /// <summary>
    /// Deployment is being terminated.
    /// </summary>
    Terminating,

    /// <summary>
    /// Deployment has been terminated.
    /// </summary>
    Terminated,

    /// <summary>
    /// Deployment status is unknown.
    /// </summary>
    Unknown
}
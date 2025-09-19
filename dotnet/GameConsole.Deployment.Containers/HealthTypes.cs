namespace GameConsole.Deployment.Containers;

/// <summary>
/// Result of a health check operation.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Gets or sets the service name that was checked.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health status.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Gets or sets any message associated with the health check.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the health check.
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the response time of the health check.
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Gets or sets additional health check details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Health status of a service.
/// </summary>
public class ServiceHealthStatus
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current health status.
    /// </summary>
    public HealthStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the last successful health check.
    /// </summary>
    public DateTime? LastHealthyAt { get; set; }

    /// <summary>
    /// Gets or sets the last failed health check.
    /// </summary>
    public DateTime? LastUnhealthyAt { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failed checks.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets the total number of health checks performed.
    /// </summary>
    public long TotalChecks { get; set; }

    /// <summary>
    /// Gets or sets additional status details.
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Event arguments for health status changes.
/// </summary>
public class HealthStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous health status.
    /// </summary>
    public HealthStatus PreviousStatus { get; set; }

    /// <summary>
    /// Gets or sets the new health status.
    /// </summary>
    public HealthStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the status change.
    /// </summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the health check result that triggered the change.
    /// </summary>
    public HealthCheckResult? HealthCheckResult { get; set; }
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Health status is unknown or not yet checked.
    /// </summary>
    Unknown,

    /// <summary>
    /// Service is healthy and responding.
    /// </summary>
    Healthy,

    /// <summary>
    /// Service is unhealthy or not responding.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Service is degraded but partially functional.
    /// </summary>
    Degraded,

    /// <summary>
    /// Health check is in progress.
    /// </summary>
    Checking
}
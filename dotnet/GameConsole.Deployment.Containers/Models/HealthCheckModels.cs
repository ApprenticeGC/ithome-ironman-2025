namespace GameConsole.Deployment.Containers.Models;

/// <summary>
/// Result of a health check operation.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// Indicates whether the health check passed.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Current health status (Healthy, Unhealthy, Degraded, Unknown).
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Time taken to perform the health check.
    /// </summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>
    /// Human-readable message about the health check result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// When the health check was performed.
    /// </summary>
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional data from the health check.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Creates a healthy result.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    /// <param name="responseTime">Time taken for the check.</param>
    /// <returns>A healthy HealthCheckResult.</returns>
    public static HealthCheckResult Healthy(string? message = null, TimeSpan responseTime = default)
    {
        return new HealthCheckResult
        {
            IsHealthy = true,
            Status = "Healthy",
            Message = message ?? "Health check passed",
            ResponseTime = responseTime,
            CheckTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an unhealthy result.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="responseTime">Time taken for the check.</param>
    /// <returns>An unhealthy HealthCheckResult.</returns>
    public static HealthCheckResult Unhealthy(string message, TimeSpan responseTime = default)
    {
        return new HealthCheckResult
        {
            IsHealthy = false,
            Status = "Unhealthy",
            Message = message,
            ResponseTime = responseTime,
            CheckTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a degraded result.
    /// </summary>
    /// <param name="message">Warning message.</param>
    /// <param name="responseTime">Time taken for the check.</param>
    /// <returns>A degraded HealthCheckResult.</returns>
    public static HealthCheckResult Degraded(string message, TimeSpan responseTime = default)
    {
        return new HealthCheckResult
        {
            IsHealthy = false,
            Status = "Degraded",
            Message = message,
            ResponseTime = responseTime,
            CheckTime = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Event data for container health status changes.
/// </summary>
public class ContainerHealthEvent
{
    /// <summary>
    /// Deployment identifier that health status changed for.
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// The health check result.
    /// </summary>
    public HealthCheckResult HealthCheck { get; set; } = null!;

    /// <summary>
    /// When the health status changed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Previous health status, if available.
    /// </summary>
    public string? PreviousStatus { get; set; }

    /// <summary>
    /// Additional context about the health change.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();
}
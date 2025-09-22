namespace GameConsole.Deployment.Containers.Models;

/// <summary>
/// Result of a deployment operation.
/// </summary>
public class DeploymentResult
{
    /// <summary>
    /// Indicates whether the deployment operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Unique identifier for the deployment.
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message about the operation result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata about the deployment result.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Exception that occurred during the operation, if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Timestamp when the operation was performed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful deployment result.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="message">Optional success message.</param>
    /// <returns>A successful DeploymentResult.</returns>
    public static DeploymentResult CreateSuccess(string deploymentId, string? message = null)
    {
        return new DeploymentResult
        {
            Success = true,
            DeploymentId = deploymentId,
            Message = message ?? "Operation completed successfully",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed deployment result.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="exception">Optional exception that caused the failure.</param>
    /// <returns>A failed DeploymentResult.</returns>
    public static DeploymentResult CreateFailure(string message, Exception? exception = null)
    {
        return new DeploymentResult
        {
            Success = false,
            Message = message,
            Exception = exception,
            Timestamp = DateTime.UtcNow
        };
    }
}
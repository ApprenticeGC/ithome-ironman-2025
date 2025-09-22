using GameConsole.Core.Abstractions;
using GameConsole.Deployment.Containers.Models;

namespace GameConsole.Deployment.Containers.Interfaces;

/// <summary>
/// Interface for monitoring container health and providing automated health checks.
/// Supports periodic monitoring, event-based notifications, and recovery actions.
/// </summary>
public interface IContainerHealthMonitor : IService
{
    /// <summary>
    /// Perform a one-time health check for a specific deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Health check result indicating status and details.</returns>
    Task<HealthCheckResult> CheckHealthAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start continuous monitoring for a deployment with the specified interval.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment to monitor.</param>
    /// <param name="interval">Interval between health checks.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the monitoring startup.</returns>
    Task StartMonitoringAsync(string deploymentId, TimeSpan interval, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop continuous monitoring for a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment to stop monitoring.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the monitoring shutdown.</returns>
    Task StopMonitoringAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current health status for all monitored deployments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Dictionary of deployment IDs and their health status.</returns>
    Task<Dictionary<string, HealthCheckResult>> GetAllHealthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Configure health check parameters for a specific deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="healthConfig">Health check configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the configuration update.</returns>
    Task ConfigureHealthCheckAsync(string deploymentId, HealthCheckConfiguration healthConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get health check history for a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="limit">Maximum number of historical entries to return.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of historical health check results.</returns>
    Task<IEnumerable<HealthCheckResult>> GetHealthHistoryAsync(string deploymentId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a container's health status changes.
    /// </summary>
    event EventHandler<ContainerHealthEvent> HealthChanged;

    /// <summary>
    /// Event raised when a health check fails.
    /// </summary>
    event EventHandler<HealthCheckFailedEventArgs> HealthCheckFailed;

    /// <summary>
    /// Event raised when a previously unhealthy container becomes healthy.
    /// </summary>
    event EventHandler<HealthCheckRecoveredEventArgs> HealthCheckRecovered;
}

/// <summary>
/// Event arguments for health check failures.
/// </summary>
public class HealthCheckFailedEventArgs : EventArgs
{
    /// <summary>
    /// Deployment that failed health check.
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// Health check result showing the failure.
    /// </summary>
    public HealthCheckResult HealthCheck { get; set; } = null!;

    /// <summary>
    /// Number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Whether recovery actions should be triggered.
    /// </summary>
    public bool ShouldTriggerRecovery { get; set; }

    /// <summary>
    /// When the failure was detected.
    /// </summary>
    public DateTime FailureTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event arguments for health check recovery.
/// </summary>
public class HealthCheckRecoveredEventArgs : EventArgs
{
    /// <summary>
    /// Deployment that recovered.
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// Health check result showing the recovery.
    /// </summary>
    public HealthCheckResult HealthCheck { get; set; } = null!;

    /// <summary>
    /// How long the deployment was unhealthy.
    /// </summary>
    public TimeSpan DowntimeDuration { get; set; }

    /// <summary>
    /// When the recovery was detected.
    /// </summary>
    public DateTime RecoveryTime { get; set; } = DateTime.UtcNow;
}
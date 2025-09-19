using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Containers;

/// <summary>
/// Interface for container health monitoring services.
/// </summary>
public interface IContainerHealthMonitor : IService, ICapabilityProvider
{
    /// <summary>
    /// Performs a health check on a specific service.
    /// </summary>
    /// <param name="serviceName">The name of the service to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async health check operation.</returns>
    Task<HealthCheckResult> CheckHealthAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts continuous health monitoring for a service.
    /// </summary>
    /// <param name="serviceName">The name of the service to monitor.</param>
    /// <param name="healthCheck">The health check configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StartMonitoringAsync(string serviceName, HealthCheckConfiguration healthCheck, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops health monitoring for a service.
    /// </summary>
    /// <param name="serviceName">The name of the service to stop monitoring.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StopMonitoringAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health status of all monitored services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns health status collection.</returns>
    Task<IEnumerable<ServiceHealthStatus>> GetHealthStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event that is raised when a service health status changes.
    /// </summary>
    event EventHandler<HealthStatusChangedEventArgs>? HealthStatusChanged;
}
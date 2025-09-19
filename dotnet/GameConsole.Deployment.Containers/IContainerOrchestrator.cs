using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Containers;

/// <summary>
/// Interface for container orchestration services that manage deployment and scaling of containerized applications.
/// </summary>
public interface IContainerOrchestrator : IService, ICapabilityProvider
{
    /// <summary>
    /// Deploys a container using the specified configuration.
    /// </summary>
    /// <param name="configuration">The container deployment configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async deployment operation that returns deployment result.</returns>
    Task<DeploymentResult> DeployAsync(ContainerConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales a deployed service to the specified number of instances.
    /// </summary>
    /// <param name="serviceName">The name of the service to scale.</param>
    /// <param name="instanceCount">The target number of instances.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async scaling operation that returns scaling result.</returns>
    Task<ScalingResult> ScaleAsync(string serviceName, int instanceCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a blue-green deployment for zero-downtime updates.
    /// </summary>
    /// <param name="serviceName">The name of the service to update.</param>
    /// <param name="newConfiguration">The new container configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async blue-green deployment operation.</returns>
    Task<DeploymentResult> BlueGreenDeployAsync(string serviceName, ContainerConfiguration newConfiguration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a deployed service.
    /// </summary>
    /// <param name="serviceName">The name of the service to query.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns service status.</returns>
    Task<ServiceStatus> GetServiceStatusAsync(string serviceName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all deployed services managed by this orchestrator.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns collection of service information.</returns>
    Task<IEnumerable<ServiceInfo>> ListServicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a deployed service and all its resources.
    /// </summary>
    /// <param name="serviceName">The name of the service to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async removal operation.</returns>
    Task RemoveServiceAsync(string serviceName, CancellationToken cancellationToken = default);
}
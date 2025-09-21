using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Containers;

/// <summary>
/// Core container orchestration service interface for deployment management.
/// Provides container lifecycle management, scaling, and orchestration capabilities.
/// </summary>
public interface IContainerOrchestrator : IService
{
    /// <summary>
    /// Deploys a containerized application.
    /// </summary>
    /// <param name="deployment">The deployment configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The deployment result with status and metadata.</returns>
    Task<DeploymentResult> DeployAsync(DeploymentConfiguration deployment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales a deployed application to the specified number of replicas.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="replicas">Target number of replicas.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The scaling operation result.</returns>
    Task<ScalingResult> ScaleAsync(string deploymentId, int replicas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a deployed application.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The removal operation result.</returns>
    Task<OperationResult> RemoveAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current deployment status.</returns>
    Task<DeploymentStatus> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active deployments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of active deployment information.</returns>
    Task<IEnumerable<DeploymentInfo>> ListDeploymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs for a specific deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="options">Log retrieval options.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Log entries for the deployment.</returns>
    Task<IEnumerable<LogEntry>> GetLogsAsync(string deploymentId, LogOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for container health monitoring.
/// </summary>
public interface IContainerHealthMonitoring : ICapabilityProvider
{
    /// <summary>
    /// Monitors the health of a specific deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Observable stream of health status updates.</returns>
    IObservable<HealthStatus> MonitorHealthAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current health check result.</returns>
    Task<HealthCheckResult> CheckHealthAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures health check parameters for a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="configuration">Health check configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the configuration operation.</returns>
    Task ConfigureHealthCheckAsync(string deploymentId, HealthCheckConfiguration configuration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for blue-green deployment strategies.
/// </summary>
public interface IBlueGreenDeployment : ICapabilityProvider
{
    /// <summary>
    /// Initiates a blue-green deployment.
    /// </summary>
    /// <param name="blueDeployment">The current (blue) deployment configuration.</param>
    /// <param name="greenDeployment">The new (green) deployment configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The blue-green deployment operation result.</returns>
    Task<BlueGreenResult> DeployBlueGreenAsync(DeploymentConfiguration blueDeployment, DeploymentConfiguration greenDeployment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches traffic from blue to green deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the blue-green deployment.</param>
    /// <param name="switchPercent">Percentage of traffic to switch (0-100).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The traffic switching operation result.</returns>
    Task<OperationResult> SwitchTrafficAsync(string deploymentId, int switchPercent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back from green to blue deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the blue-green deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The rollback operation result.</returns>
    Task<OperationResult> RollbackAsync(string deploymentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for service mesh integration.
/// </summary>
public interface IServiceMeshIntegration : ICapabilityProvider
{
    /// <summary>
    /// Configures service mesh settings for a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="meshConfiguration">Service mesh configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the configuration operation.</returns>
    Task ConfigureMeshAsync(string deploymentId, ServiceMeshConfiguration meshConfiguration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets service mesh metrics for a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="metricsQuery">Metrics query parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Service mesh metrics data.</returns>
    Task<ServiceMeshMetrics> GetMeshMetricsAsync(string deploymentId, MetricsQuery metricsQuery, CancellationToken cancellationToken = default);
}
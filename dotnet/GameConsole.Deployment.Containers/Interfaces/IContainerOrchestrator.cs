using GameConsole.Core.Abstractions;
using GameConsole.Deployment.Containers.Models;

namespace GameConsole.Deployment.Containers.Interfaces;

/// <summary>
/// Main orchestrator interface for managing container deployments across different providers.
/// Coordinates deployment operations, scaling, and status monitoring.
/// </summary>
public interface IContainerOrchestrator : IService
{
    /// <summary>
    /// Deploy a new container configuration.
    /// </summary>
    /// <param name="config">Deployment configuration specifying image, replicas, and settings.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Result of the deployment operation with deployment ID and status.</returns>
    Task<DeploymentResult> DeployAsync(DeploymentConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scale an existing deployment to the specified number of replicas.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment to scale.</param>
    /// <param name="replicas">Target number of replicas.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Result of the scaling operation.</returns>
    Task<DeploymentResult> ScaleAsync(string deploymentId, int replicas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current status of a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current status information about the deployment.</returns>
    Task<DeploymentStatus> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a deployment and all its resources.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the deployment was successfully removed; otherwise, false.</returns>
    Task<bool> RemoveAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// List all deployments managed by this orchestrator.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of deployment status information.</returns>
    Task<IEnumerable<DeploymentStatus>> ListDeploymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing deployment with new configuration.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment to update.</param>
    /// <param name="config">New deployment configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Result of the update operation.</returns>
    Task<DeploymentResult> UpdateAsync(string deploymentId, DeploymentConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a deployment status changes.
    /// </summary>
    event EventHandler<DeploymentStatusChangedEventArgs> DeploymentStatusChanged;
}

/// <summary>
/// Event arguments for deployment status changes.
/// </summary>
public class DeploymentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// The deployment that changed.
    /// </summary>
    public DeploymentStatus Deployment { get; set; } = null!;

    /// <summary>
    /// Previous status, if available.
    /// </summary>
    public string? PreviousStatus { get; set; }

    /// <summary>
    /// When the status changed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
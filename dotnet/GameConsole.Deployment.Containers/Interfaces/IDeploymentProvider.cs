using GameConsole.Core.Abstractions;
using GameConsole.Deployment.Containers.Models;

namespace GameConsole.Deployment.Containers.Interfaces;

/// <summary>
/// Base interface for deployment providers that handle specific container platforms.
/// Providers implement the actual deployment logic for Docker, Kubernetes, etc.
/// </summary>
public interface IDeploymentProvider : IService
{
    /// <summary>
    /// Provider type identifier (e.g., "Docker", "Kubernetes").
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// Create a new deployment using this provider.
    /// </summary>
    /// <param name="config">Deployment configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Result of the deployment creation.</returns>
    Task<DeploymentResult> CreateDeploymentAsync(DeploymentConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing deployment with new configuration.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="config">Updated deployment configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Result of the deployment update.</returns>
    Task<DeploymentResult> UpdateDeploymentAsync(string deploymentId, DeploymentConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a deployment and clean up all resources.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteDeploymentAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current status of a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current deployment status.</returns>
    Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scale a deployment to the specified number of replicas.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="replicas">Target number of replicas.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Result of the scaling operation.</returns>
    Task<DeploymentResult> ScaleDeploymentAsync(string deploymentId, int replicas, CancellationToken cancellationToken = default);

    /// <summary>
    /// List all deployments managed by this provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of deployment status information.</returns>
    Task<IEnumerable<DeploymentStatus>> ListDeploymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if this provider supports the given deployment configuration.
    /// </summary>
    /// <param name="config">Deployment configuration to validate.</param>
    /// <returns>True if the provider can handle this configuration; otherwise, false.</returns>
    bool SupportsConfiguration(DeploymentConfiguration config);

    /// <summary>
    /// Get provider-specific capabilities and limitations.
    /// </summary>
    /// <returns>Dictionary of capability names and their values.</returns>
    Task<Dictionary<string, object>> GetCapabilitiesAsync();
}
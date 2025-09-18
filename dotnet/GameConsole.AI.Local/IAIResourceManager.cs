namespace GameConsole.AI.Local;

/// <summary>
/// AI resource manager for GPU/CPU allocation and optimization.
/// Handles resource allocation, monitoring, and optimization for AI workloads
/// with fallback mechanisms for resource constraints.
/// </summary>
public interface IAIResourceManager
{
    /// <summary>
    /// Allocates resources for AI operations based on requirements and current availability.
    /// </summary>
    /// <param name="requirements">Resource requirements for the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns allocated resources.</returns>
    Task<AllocatedResources> AllocateResourcesAsync(ResourceRequirements requirements, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases previously allocated resources back to the resource pool.
    /// </summary>
    /// <param name="resourceId">Identifier of the resources to release.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resource release operation.</returns>
    Task ReleaseResourcesAsync(string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current resource usage statistics including GPU and CPU utilization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns resource usage statistics.</returns>
    Task<ResourceUsageStatistics> GetResourceUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes resource allocation based on current usage patterns and performance metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async optimization operation.</returns>
    Task OptimizeAllocationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available resource capabilities including GPU memory, CPU cores, and supported features.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns resource capabilities.</returns>
    Task<ResourceCapabilities> GetResourceCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets resource limits to prevent overconsumption and ensure system stability.
    /// </summary>
    /// <param name="limits">Resource limits to apply.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async limit setting operation.</returns>
    Task SetResourceLimitsAsync(ResourceLimits limits, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the specified resource requirements can be satisfied with current availability.
    /// </summary>
    /// <param name="requirements">Resource requirements to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns availability status.</returns>
    Task<bool> CanSatisfyRequirementsAsync(ResourceRequirements requirements, CancellationToken cancellationToken = default);
}
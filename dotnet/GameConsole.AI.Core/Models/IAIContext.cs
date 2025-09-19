namespace GameConsole.AI.Models;

/// <summary>
/// Provides a secure execution environment and context for AI agents.
/// Manages resource allocation, security sandboxing, and performance monitoring.
/// </summary>
public interface IAIContext : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this execution context.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the resource allocation for this context.
    /// </summary>
    AIResourceAllocation ResourceAllocation { get; }

    /// <summary>
    /// Gets the security settings for this context.
    /// </summary>
    AISecuritySettings SecuritySettings { get; }

    /// <summary>
    /// Gets the current performance metrics for this context.
    /// </summary>
    AIPerformanceMetrics PerformanceMetrics { get; }

    /// <summary>
    /// Gets a value indicating whether this context is active and ready for use.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets additional properties and configuration for this context.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }

    /// <summary>
    /// Initializes the execution context with the specified settings.
    /// </summary>
    /// <param name="settings">The initialization settings for the context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(AIContextSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Allocates resources for the specified requirements.
    /// </summary>
    /// <param name="requirements">The resource requirements to allocate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resource allocation operation that returns true if successful.</returns>
    Task<bool> AllocateResourcesAsync(AIResourceRequirements requirements, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases previously allocated resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resource release operation.</returns>
    Task ReleaseResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the specified operation is allowed within this security context.
    /// </summary>
    /// <param name="operation">The operation to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async security validation operation that returns true if allowed.</returns>
    Task<bool> ValidateSecurityAsync(string operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the performance metrics for this context.
    /// </summary>
    /// <param name="metrics">The updated performance metrics.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async metrics update operation.</returns>
    Task UpdatePerformanceMetricsAsync(AIPerformanceMetrics metrics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a nested context for isolated execution.
    /// </summary>
    /// <param name="settings">The settings for the nested context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the nested context.</returns>
    Task<IAIContext> CreateNestedContextAsync(AIContextSettings settings, CancellationToken cancellationToken = default);
}
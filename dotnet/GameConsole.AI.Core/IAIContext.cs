using GameConsole.AI.Core.Models;

namespace GameConsole.AI.Services;

/// <summary>
/// Provides a secure execution environment and context for AI agent operations.
/// Manages resource allocation, security constraints, configuration, and performance monitoring.
/// </summary>
public interface IAIContext : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this context instance.
    /// </summary>
    string ContextId { get; }

    /// <summary>
    /// Gets the security level enforced by this context.
    /// </summary>
    GameConsole.AI.Core.SecurityLevel SecurityLevel { get; }

    /// <summary>
    /// Gets the resource constraints applied to agents running in this context.
    /// </summary>
    IReadOnlyList<GameConsole.AI.Core.ResourceRequirement> ResourceLimits { get; }

    /// <summary>
    /// Gets the configuration settings available to agents in this context.
    /// </summary>
    IReadOnlyDictionary<string, object> Configuration { get; }

    /// <summary>
    /// Gets a value indicating whether the context is active and available for use.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Allocates resources for an agent within this context.
    /// </summary>
    /// <param name="agentId">The identifier of the agent requesting resources.</param>
    /// <param name="requirements">The resource requirements to allocate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if resources were successfully allocated.</returns>
    Task<bool> AllocateResourcesAsync(string agentId, IReadOnlyList<GameConsole.AI.Core.ResourceRequirement> requirements, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases resources previously allocated to an agent.
    /// </summary>
    /// <param name="agentId">The identifier of the agent releasing resources.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ReleaseResourcesAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current resource usage for an agent.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to query.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the current resource usage metrics.</returns>
    Task<IReadOnlyList<GameConsole.AI.Core.PerformanceMetric>> GetResourceUsageAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a performance metric for monitoring purposes.
    /// </summary>
    /// <param name="agentId">The identifier of the agent generating the metric.</param>
    /// <param name="metric">The performance metric to record.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RecordMetricAsync(string agentId, GameConsole.AI.Core.PerformanceMetric metric, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether an operation is permitted within the current security constraints.
    /// </summary>
    /// <param name="agentId">The identifier of the agent requesting the operation.</param>
    /// <param name="operation">The operation to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the operation is permitted.</returns>
    Task<bool> ValidateOperationAsync(string agentId, string operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration value for a specific key, with optional type casting.
    /// </summary>
    /// <typeparam name="T">The expected type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if the key is not found.</param>
    /// <returns>The configuration value cast to the specified type, or the default value.</returns>
    T GetConfiguration<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Sets a configuration value for this context instance.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetConfigurationAsync(string key, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a child context with additional restrictions or configurations.
    /// </summary>
    /// <param name="securityLevel">The security level for the child context.</param>
    /// <param name="resourceLimits">Additional resource limits for the child context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the newly created child context.</returns>
    Task<IAIContext> CreateChildContextAsync(GameConsole.AI.Core.SecurityLevel securityLevel, IReadOnlyList<GameConsole.AI.Core.ResourceRequirement>? resourceLimits = null, CancellationToken cancellationToken = default);
}
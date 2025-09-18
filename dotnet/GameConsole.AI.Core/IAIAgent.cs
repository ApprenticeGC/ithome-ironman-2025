using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Base interface for all AI agents in the GameConsole system.
/// Combines service lifecycle management with AI-specific capabilities
/// for agent execution, context management, and performance monitoring.
/// </summary>
public interface IAIAgent : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the metadata information for this AI agent.
    /// </summary>
    AIAgentMetadata Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether the agent is currently processing a request.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// Gets the number of active contexts currently managed by this agent.
    /// </summary>
    int ActiveContextCount { get; }

    /// <summary>
    /// Creates a new AI execution context for this agent.
    /// </summary>
    /// <param name="configuration">Optional configuration parameters for the context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async operation with the created context.</returns>
    Task<IAIContext> CreateContextAsync(
        IReadOnlyDictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an AI operation using the specified context.
    /// </summary>
    /// <param name="context">The execution context to use.</param>
    /// <param name="input">The input data for the AI operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async operation with the execution result.</returns>
    Task<string> ExecuteAsync(
        IAIContext context,
        string input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams results from an AI operation using the specified context.
    /// </summary>
    /// <param name="context">The execution context to use.</param>
    /// <param name="input">The input data for the AI operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable that yields streaming results.</returns>
    IAsyncEnumerable<string> StreamAsync(
        IAIContext context,
        string input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the agent can handle the specified input type.
    /// </summary>
    /// <param name="inputType">The input type to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async validation operation. Returns true if the input type is supported.</returns>
    Task<bool> CanHandleInputAsync(
        string inputType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for this AI agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async operation with performance metrics.</returns>
    Task<IReadOnlyDictionary<string, object>> GetPerformanceMetricsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the AI agent to ensure it's ready for operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async health check operation. Returns true if the agent is healthy.</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a context that is no longer needed.
    /// </summary>
    /// <param name="context">The context to release.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async release operation.</returns>
    Task ReleaseContextAsync(IAIContext context, CancellationToken cancellationToken = default);
}
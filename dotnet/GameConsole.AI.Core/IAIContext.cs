namespace GameConsole.AI.Core;

/// <summary>
/// Represents the execution context for AI agents, providing a secure
/// and controlled environment for AI operations with resource management
/// and monitoring capabilities.
/// </summary>
public interface IAIContext : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique identifier for this context session.
    /// </summary>
    string SessionId { get; }

    /// <summary>
    /// Gets the framework type supported by this context.
    /// </summary>
    AIFrameworkType FrameworkType { get; }

    /// <summary>
    /// Gets the resource requirements allocated to this context.
    /// </summary>
    AIResourceRequirements AllocatedResources { get; }

    /// <summary>
    /// Gets a value indicating whether the context is currently active and ready for execution.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the creation time of this context.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the last activity time for this context.
    /// </summary>
    DateTimeOffset LastActivityAt { get; }

    /// <summary>
    /// Initializes the context with the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration parameters for the context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async initialization operation.</returns>
    Task InitializeAsync(IReadOnlyDictionary<string, object> configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an AI operation within this secure context.
    /// </summary>
    /// <typeparam name="TInput">The type of input data.</typeparam>
    /// <typeparam name="TOutput">The type of output data.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="input">The input data for the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async execution operation with the result.</returns>
    Task<TOutput> ExecuteAsync<TInput, TOutput>(
        Func<TInput, CancellationToken, Task<TOutput>> operation,
        TInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams results from an AI operation within this secure context.
    /// </summary>
    /// <typeparam name="TInput">The type of input data.</typeparam>
    /// <typeparam name="TOutput">The type of output data.</typeparam>
    /// <param name="operation">The streaming operation to execute.</param>
    /// <param name="input">The input data for the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable that yields streaming results.</returns>
    IAsyncEnumerable<TOutput> StreamAsync<TInput, TOutput>(
        Func<TInput, CancellationToken, IAsyncEnumerable<TOutput>> operation,
        TInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current resource usage statistics for this context.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async operation with resource usage information.</returns>
    Task<IReadOnlyDictionary<string, object>> GetResourceUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a property value in the context for the current session.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    void SetProperty(string key, object value);

    /// <summary>
    /// Gets a property value from the context.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or the default value if not found.</returns>
    T? GetProperty<T>(string key);

    /// <summary>
    /// Gets all properties currently set in the context.
    /// </summary>
    /// <returns>A read-only dictionary of all context properties.</returns>
    IReadOnlyDictionary<string, object> GetAllProperties();
}
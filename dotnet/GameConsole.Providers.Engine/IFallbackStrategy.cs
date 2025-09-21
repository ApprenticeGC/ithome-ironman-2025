namespace GameConsole.Providers.Engine;

/// <summary>
/// Result of a fallback operation.
/// </summary>
public class FallbackResult<T>
{
    /// <summary>
    /// The result of the operation, or null if all providers failed.
    /// </summary>
    public T? Result { get; init; }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => SuccessfulProviderId is not null;

    /// <summary>
    /// List of errors encountered during fallback attempts.
    /// </summary>
    public IReadOnlyList<Exception> Errors { get; init; } = Array.Empty<Exception>();

    /// <summary>
    /// The provider that ultimately succeeded, if any.
    /// </summary>
    public string? SuccessfulProviderId { get; init; }

    /// <summary>
    /// Number of providers attempted.
    /// </summary>
    public int AttemptCount { get; init; }
}

/// <summary>
/// Interface for implementing fallback strategies when primary providers fail.
/// </summary>
public interface IFallbackStrategy
{
    /// <summary>
    /// Executes an operation with automatic fallback to alternative providers.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="criteria">Provider selection criteria.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The fallback result containing the operation result or errors.</returns>
    Task<FallbackResult<TResult>> ExecuteWithFallbackAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        ProviderSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
        where TService : class;

    /// <summary>
    /// Executes an operation with automatic fallback to alternative providers.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="criteria">Provider selection criteria.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The fallback result indicating success or failure.</returns>
    Task<FallbackResult<object?>> ExecuteWithFallbackAsync<TService>(
        Func<TService, CancellationToken, Task> operation,
        ProviderSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
        where TService : class;

    /// <summary>
    /// Gets the delay to wait before retrying a failed provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="attemptNumber">The attempt number (starting from 1).</param>
    /// <returns>The delay before the next retry.</returns>
    TimeSpan GetRetryDelay(string providerId, int attemptNumber);

    /// <summary>
    /// Determines if a provider should be retried based on the exception.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if the provider should be retried, false otherwise.</returns>
    bool ShouldRetry(Exception exception);
}
namespace GameConsole.Providers.Engine;

/// <summary>
/// Interface for defining fallback strategies when primary providers fail or are unavailable.
/// </summary>
/// <typeparam name="T">The type of provider to provide fallback for.</typeparam>
public interface IFallbackStrategy<T> where T : class
{
    /// <summary>
    /// Determines the fallback providers to use when the primary provider fails.
    /// </summary>
    /// <param name="failedProvider">The provider that failed, if known.</param>
    /// <param name="availableProviders">All currently available providers.</param>
    /// <param name="context">Optional context for fallback selection.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An ordered list of fallback providers to try.</returns>
    Task<IEnumerable<T>> GetFallbackProvidersAsync(
        T? failedProvider,
        IEnumerable<T> availableProviders,
        object? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a fallback attempt should be made based on the failure and current state.
    /// </summary>
    /// <param name="failedProvider">The provider that failed.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="attemptCount">The number of attempts already made.</param>
    /// <returns>True if fallback should be attempted, false to give up.</returns>
    bool ShouldAttemptFallback(T failedProvider, Exception exception, int attemptCount);

    /// <summary>
    /// Calculates the delay before attempting the next fallback provider.
    /// </summary>
    /// <param name="attemptCount">The number of attempts already made.</param>
    /// <param name="lastException">The most recent exception encountered.</param>
    /// <returns>The delay before the next attempt.</returns>
    TimeSpan CalculateRetryDelay(int attemptCount, Exception lastException);
}

/// <summary>
/// Configuration options for fallback strategies.
/// </summary>
public sealed class FallbackOptions
{
    /// <summary>
    /// Gets or sets the maximum number of fallback attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff.
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Gets or sets the maximum delay between attempts.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets whether to add jitter to retry delays to avoid thundering herd.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Gets or sets the types of exceptions that should trigger fallback.
    /// If empty, all exceptions trigger fallback.
    /// </summary>
    public HashSet<Type> FallbackExceptionTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the types of exceptions that should not trigger fallback.
    /// These take precedence over FallbackExceptionTypes.
    /// </summary>
    public HashSet<Type> NonFallbackExceptionTypes { get; set; } = new();
}
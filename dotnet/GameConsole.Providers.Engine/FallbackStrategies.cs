using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Default fallback strategy that provides round-robin fallback with exponential backoff.
/// </summary>
/// <typeparam name="T">The type of provider to provide fallback for.</typeparam>
public sealed class DefaultFallbackStrategy<T> : IFallbackStrategy<T> where T : class
{
    private readonly FallbackOptions _options;
    private readonly IProviderPerformanceMonitor? _performanceMonitor;
    private readonly ILogger<DefaultFallbackStrategy<T>>? _logger;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultFallbackStrategy{T}"/> class.
    /// </summary>
    /// <param name="options">Configuration options for fallback behavior.</param>
    /// <param name="performanceMonitor">Optional performance monitor for health checks.</param>
    /// <param name="logger">Optional logger for fallback operations.</param>
    public DefaultFallbackStrategy(
        FallbackOptions? options = null,
        IProviderPerformanceMonitor? performanceMonitor = null,
        ILogger<DefaultFallbackStrategy<T>>? logger = null)
    {
        _options = options ?? new FallbackOptions();
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IEnumerable<T>> GetFallbackProvidersAsync(
        T? failedProvider,
        IEnumerable<T> availableProviders,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        var providers = availableProviders.ToList();
        
        // Remove the failed provider from available options
        if (failedProvider != null)
        {
            providers.Remove(failedProvider);
        }

        // Filter out unhealthy providers if we have a performance monitor
        if (_performanceMonitor != null)
        {
            providers = providers
                .Where(provider => _performanceMonitor.IsProviderHealthy(GetProviderId(provider)))
                .ToList();
        }

        // Sort providers by performance if available
        if (_performanceMonitor != null)
        {
            providers = providers
                .OrderBy(provider => _performanceMonitor.GetMetrics(GetProviderId(provider)).FailureRate)
                .ThenBy(provider => _performanceMonitor.GetMetrics(GetProviderId(provider)).AverageResponseTime)
                .ToList();
        }

        _logger?.LogDebug("Selected {Count} fallback providers from {Total} available providers",
            providers.Count, availableProviders.Count());

        return Task.FromResult<IEnumerable<T>>(providers);
    }

    /// <inheritdoc />
    public bool ShouldAttemptFallback(T failedProvider, Exception exception, int attemptCount)
    {
        // Don't attempt fallback if we've exceeded max attempts
        if (attemptCount >= _options.MaxAttempts)
        {
            _logger?.LogDebug("Fallback attempt limit reached ({MaxAttempts}), giving up", _options.MaxAttempts);
            return false;
        }

        // Check if exception type should trigger fallback
        if (_options.NonFallbackExceptionTypes.Count > 0 && 
            _options.NonFallbackExceptionTypes.Contains(exception.GetType()))
        {
            _logger?.LogDebug("Exception type {ExceptionType} is configured to not trigger fallback",
                exception.GetType().Name);
            return false;
        }

        if (_options.FallbackExceptionTypes.Count > 0 && 
            !_options.FallbackExceptionTypes.Contains(exception.GetType()))
        {
            _logger?.LogDebug("Exception type {ExceptionType} is not configured to trigger fallback",
                exception.GetType().Name);
            return false;
        }

        // Don't fallback for certain types of exceptions that indicate client errors
        if (IsClientError(exception))
        {
            _logger?.LogDebug("Client error detected, not attempting fallback: {ExceptionType}",
                exception.GetType().Name);
            return false;
        }

        _logger?.LogDebug("Fallback attempt {AttemptCount} should proceed for exception: {ExceptionMessage}",
            attemptCount + 1, exception.Message);

        return true;
    }

    /// <inheritdoc />
    public TimeSpan CalculateRetryDelay(int attemptCount, Exception lastException)
    {
        // Calculate exponential backoff delay
        var delay = TimeSpan.FromTicks((long)(_options.BaseDelay.Ticks * 
            Math.Pow(_options.BackoffMultiplier, attemptCount)));

        // Apply maximum delay cap
        if (delay > _options.MaxDelay)
        {
            delay = _options.MaxDelay;
        }

        // Add jitter to prevent thundering herd
        if (_options.UseJitter)
        {
            var jitterMs = _random.Next(0, (int)(delay.TotalMilliseconds * 0.1));
            delay = delay.Add(TimeSpan.FromMilliseconds(jitterMs));
        }

        _logger?.LogDebug("Calculated retry delay of {DelayMs}ms for attempt {AttemptCount}",
            delay.TotalMilliseconds, attemptCount + 1);

        return delay;
    }

    private static bool IsClientError(Exception exception)
    {
        // Common client error types that shouldn't trigger fallback
        return exception is ArgumentException ||
               exception is ArgumentNullException ||
               exception is ArgumentOutOfRangeException ||
               exception is InvalidOperationException ||
               exception is NotSupportedException ||
               exception is UnauthorizedAccessException;
    }

    private static string GetProviderId(T provider)
    {
        // Use provider type name and hash code as a simple ID
        // In a real implementation, providers might implement an IIdentifiable interface
        return $"{provider.GetType().Name}_{provider.GetHashCode()}";
    }
}

/// <summary>
/// Primary-only fallback strategy that doesn't attempt any fallback.
/// Useful for scenarios where only the primary provider should be used.
/// </summary>
/// <typeparam name="T">The type of provider.</typeparam>
public sealed class PrimaryOnlyFallbackStrategy<T> : IFallbackStrategy<T> where T : class
{
    /// <inheritdoc />
    public Task<IEnumerable<T>> GetFallbackProvidersAsync(
        T? failedProvider,
        IEnumerable<T> availableProviders,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        // Return empty list - no fallback providers
        return Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
    }

    /// <inheritdoc />
    public bool ShouldAttemptFallback(T failedProvider, Exception exception, int attemptCount)
    {
        // Never attempt fallback with this strategy
        return false;
    }

    /// <inheritdoc />
    public TimeSpan CalculateRetryDelay(int attemptCount, Exception lastException)
    {
        // No delay needed since we don't retry
        return TimeSpan.Zero;
    }
}

/// <summary>
/// Immediate fallback strategy that attempts all available providers without delay.
/// Useful for scenarios where speed is more important than avoiding thundering herd.
/// </summary>
/// <typeparam name="T">The type of provider.</typeparam>
public sealed class ImmediateFallbackStrategy<T> : IFallbackStrategy<T> where T : class
{
    private readonly int _maxAttempts;
    private readonly IProviderPerformanceMonitor? _performanceMonitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmediateFallbackStrategy{T}"/> class.
    /// </summary>
    /// <param name="maxAttempts">Maximum number of fallback attempts.</param>
    /// <param name="performanceMonitor">Optional performance monitor for health checks.</param>
    public ImmediateFallbackStrategy(int maxAttempts = 3, IProviderPerformanceMonitor? performanceMonitor = null)
    {
        _maxAttempts = maxAttempts;
        _performanceMonitor = performanceMonitor;
    }

    /// <inheritdoc />
    public Task<IEnumerable<T>> GetFallbackProvidersAsync(
        T? failedProvider,
        IEnumerable<T> availableProviders,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        var providers = availableProviders.ToList();
        
        // Remove the failed provider
        if (failedProvider != null)
        {
            providers.Remove(failedProvider);
        }

        // Filter healthy providers if monitor is available
        if (_performanceMonitor != null)
        {
            providers = providers
                .Where(provider => _performanceMonitor.IsProviderHealthy(GetProviderId(provider)))
                .ToList();
        }

        return Task.FromResult<IEnumerable<T>>(providers);
    }

    /// <inheritdoc />
    public bool ShouldAttemptFallback(T failedProvider, Exception exception, int attemptCount)
    {
        return attemptCount < _maxAttempts;
    }

    /// <inheritdoc />
    public TimeSpan CalculateRetryDelay(int attemptCount, Exception lastException)
    {
        // No delay for immediate fallback
        return TimeSpan.Zero;
    }

    private static string GetProviderId(T provider)
    {
        return $"{provider.GetType().Name}_{provider.GetHashCode()}";
    }
}
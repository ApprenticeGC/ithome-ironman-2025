using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Default implementation of fallback strategy with exponential backoff.
/// </summary>
public sealed class ExponentialBackoffFallbackStrategy : IFallbackStrategy
{
    private readonly IProviderSelector _providerSelector;
    private readonly IProviderPerformanceMonitor _performanceMonitor;
    private readonly ILogger<ExponentialBackoffFallbackStrategy> _logger;
    private readonly FallbackOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExponentialBackoffFallbackStrategy"/> class.
    /// </summary>
    /// <param name="providerSelector">Provider selector for getting alternative providers.</param>
    /// <param name="performanceMonitor">Performance monitor for recording metrics.</param>
    /// <param name="logger">Logger for fallback operations.</param>
    /// <param name="options">Configuration options for the fallback strategy.</param>
    public ExponentialBackoffFallbackStrategy(
        IProviderSelector providerSelector,
        IProviderPerformanceMonitor performanceMonitor,
        ILogger<ExponentialBackoffFallbackStrategy> logger,
        FallbackOptions? options = null)
    {
        _providerSelector = providerSelector ?? throw new ArgumentNullException(nameof(providerSelector));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new FallbackOptions();
    }

    /// <inheritdoc />
    public async Task<FallbackResult<TResult>> ExecuteWithFallbackAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        ProviderSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
        where TService : class
    {
        var errors = new List<Exception>();
        var attemptCount = 0;
        var startTime = DateTimeOffset.UtcNow;

        // Get all available providers
        var providers = await _providerSelector.GetAvailableProvidersAsync<TService>(criteria, cancellationToken);

        if (!providers.Any())
        {
            _logger.LogWarning("No providers available for service {ServiceType}", typeof(TService).Name);
            return new FallbackResult<TResult>
            {
                Result = default,
                Errors = errors,
                AttemptCount = attemptCount
            };
        }

        _logger.LogDebug("Starting fallback execution with {ProviderCount} providers for service {ServiceType}", 
            providers.Count, typeof(TService).Name);

        foreach (var provider in providers)
        {
            attemptCount++;
            var providerId = GetProviderId(provider);

            try
            {
                var operationStartTime = DateTimeOffset.UtcNow;
                var result = await operation(provider, cancellationToken);
                var responseTime = DateTimeOffset.UtcNow - operationStartTime;

                // Record success
                await _performanceMonitor.RecordSuccessAsync(providerId, responseTime, cancellationToken);

                _logger.LogDebug("Fallback succeeded on attempt {AttemptCount} with provider {ProviderId} after {Duration}ms", 
                    attemptCount, providerId, (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);

                return new FallbackResult<TResult>
                {
                    Result = result,
                    Errors = errors,
                    SuccessfulProviderId = providerId,
                    AttemptCount = attemptCount
                };
            }
            catch (Exception ex) when (ShouldRetry(ex))
            {
                // Record failure
                await _performanceMonitor.RecordFailureAsync(providerId, ex, cancellationToken);
                errors.Add(ex);

                _logger.LogWarning(ex, "Provider {ProviderId} failed on attempt {AttemptCount}: {ErrorMessage}", 
                    providerId, attemptCount, ex.Message);

                // Apply backoff delay if not the last provider
                if (attemptCount < providers.Count && attemptCount < _options.MaxRetryAttempts)
                {
                    var delay = GetRetryDelay(providerId, attemptCount);
                    if (delay > TimeSpan.Zero)
                    {
                        _logger.LogDebug("Waiting {Delay}ms before trying next provider", delay.TotalMilliseconds);
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // Non-retryable error, record failure and stop
                await _performanceMonitor.RecordFailureAsync(providerId, ex, cancellationToken);
                errors.Add(ex);

                _logger.LogError(ex, "Non-retryable error from provider {ProviderId}: {ErrorMessage}", 
                    providerId, ex.Message);
                break;
            }
        }

        _logger.LogError("All fallback attempts failed after {AttemptCount} attempts in {Duration}ms", 
            attemptCount, (DateTimeOffset.UtcNow - startTime).TotalMilliseconds);

        return new FallbackResult<TResult>
        {
            Result = default,
            Errors = errors,
            AttemptCount = attemptCount
        };
    }

    /// <inheritdoc />
    public async Task<FallbackResult<object?>> ExecuteWithFallbackAsync<TService>(
        Func<TService, CancellationToken, Task> operation,
        ProviderSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
        where TService : class
    {
        var wrappedOperation = async (TService service, CancellationToken ct) =>
        {
            await operation(service, ct);
            return (object?)null;
        };

        return await ExecuteWithFallbackAsync<TService, object?>(wrappedOperation, criteria, cancellationToken);
    }

    /// <inheritdoc />
    public TimeSpan GetRetryDelay(string providerId, int attemptNumber)
    {
        if (attemptNumber <= 0)
            return TimeSpan.Zero;

        // Exponential backoff: base * 2^(attempt - 1)
        var delay = _options.BaseRetryDelay.TotalMilliseconds * Math.Pow(2, attemptNumber - 1);
        
        // Apply jitter to prevent thundering herd
        var jitter = Random.Shared.NextDouble() * _options.JitterFactor;
        delay = delay * (1.0 + jitter);

        // Cap at max delay
        delay = Math.Min(delay, _options.MaxRetryDelay.TotalMilliseconds);

        return TimeSpan.FromMilliseconds(delay);
    }

    /// <inheritdoc />
    public bool ShouldRetry(Exception exception)
    {
        // Don't retry for argument exceptions or cancellation
        if (exception is ArgumentException or OperationCanceledException)
            return false;

        // Don't retry for specific non-transient exceptions
        if (_options.NonRetryableExceptions.Any(t => t.IsAssignableFrom(exception.GetType())))
            return false;

        return true;
    }

    private static string GetProviderId(object provider)
    {
        return provider.GetType().FullName ?? provider.GetType().Name;
    }
}

/// <summary>
/// Configuration options for fallback strategy.
/// </summary>
public class FallbackOptions
{
    /// <summary>
    /// Base delay for retry attempts.
    /// </summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Maximum delay for retry attempts.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retry attempts per provider.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Jitter factor for randomizing retry delays (0.0 - 1.0).
    /// </summary>
    public double JitterFactor { get; set; } = 0.1;

    /// <summary>
    /// Exception types that should not be retried.
    /// </summary>
    public ISet<Type> NonRetryableExceptions { get; set; } = new HashSet<Type>
    {
        typeof(ArgumentException),
        typeof(ArgumentNullException),
        typeof(InvalidOperationException),
        typeof(NotSupportedException),
        typeof(OperationCanceledException)
    };
}
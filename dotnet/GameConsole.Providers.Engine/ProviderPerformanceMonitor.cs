using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Implementation of provider performance monitoring using System.Diagnostics for metrics collection.
/// Tracks success rates, response times, and circuit breaker states for providers.
/// </summary>
public sealed class ProviderPerformanceMonitor : IProviderPerformanceMonitor, IDisposable
{
    private readonly ConcurrentDictionary<string, ProviderMetricsData> _providerMetrics = new();
    private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new();
    private readonly ILogger<ProviderPerformanceMonitor>? _logger;
    private readonly CircuitBreakerOptions _circuitBreakerOptions;
    private readonly Timer _metricsCleanupTimer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderPerformanceMonitor"/> class.
    /// </summary>
    /// <param name="circuitBreakerOptions">Options for circuit breaker configuration.</param>
    /// <param name="logger">Optional logger for monitoring operations.</param>
    public ProviderPerformanceMonitor(
        CircuitBreakerOptions? circuitBreakerOptions = null,
        ILogger<ProviderPerformanceMonitor>? logger = null)
    {
        _circuitBreakerOptions = circuitBreakerOptions ?? new CircuitBreakerOptions();
        _logger = logger;
        
        // Set up periodic cleanup of old metrics
        _metricsCleanupTimer = new Timer(CleanupOldMetrics, null, 
            TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <inheritdoc />
    public void RecordSuccess(string providerId, TimeSpan responseTime)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or whitespace", nameof(providerId));

        var metricsData = GetOrCreateMetricsData(providerId);
        var circuitBreaker = GetOrCreateCircuitBreaker(providerId);

        lock (metricsData.Lock)
        {
            metricsData.SuccessCount++;
            metricsData.LastOperationTime = DateTimeOffset.UtcNow;
            metricsData.ResponseTimes.Add(responseTime);
            
            // Keep only the last 1000 response times for memory efficiency
            if (metricsData.ResponseTimes.Count > 1000)
            {
                metricsData.ResponseTimes.RemoveAt(0);
            }

            metricsData.LastException = null;
        }

        circuitBreaker.RecordSuccess();
        _logger?.LogDebug("Recorded success for provider {ProviderId} with response time {ResponseTime}ms",
            providerId, responseTime.TotalMilliseconds);
    }

    /// <inheritdoc />
    public void RecordFailure(string providerId, Exception exception)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or whitespace", nameof(providerId));
        
        ArgumentNullException.ThrowIfNull(exception);

        var metricsData = GetOrCreateMetricsData(providerId);
        var circuitBreaker = GetOrCreateCircuitBreaker(providerId);

        lock (metricsData.Lock)
        {
            metricsData.FailureCount++;
            metricsData.LastOperationTime = DateTimeOffset.UtcNow;
            metricsData.LastException = exception;
        }

        var wasHealthy = circuitBreaker.State == CircuitBreakerState.Closed;
        circuitBreaker.RecordFailure(exception);
        var isHealthy = circuitBreaker.State == CircuitBreakerState.Closed;

        if (wasHealthy != isHealthy)
        {
            OnProviderHealthChanged(providerId, wasHealthy, isHealthy, circuitBreaker.State);
        }

        _logger?.LogWarning(exception, 
            "Recorded failure for provider {ProviderId}: {ExceptionMessage}",
            providerId, exception.Message);
    }

    /// <inheritdoc />
    public IOperationTracker StartOperation(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or whitespace", nameof(providerId));

        return new OperationTracker(this, providerId);
    }

    /// <inheritdoc />
    public ProviderMetrics GetMetrics(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or whitespace", nameof(providerId));

        var metricsData = GetOrCreateMetricsData(providerId);
        var circuitBreaker = GetOrCreateCircuitBreaker(providerId);

        lock (metricsData.Lock)
        {
            var responseTimes = metricsData.ResponseTimes.OrderBy(rt => rt).ToList();
            
            return new ProviderMetrics
            {
                ProviderId = providerId,
                SuccessCount = metricsData.SuccessCount,
                FailureCount = metricsData.FailureCount,
                AverageResponseTime = responseTimes.Count == 0 
                    ? TimeSpan.Zero 
                    : TimeSpan.FromTicks((long)responseTimes.Average(rt => rt.Ticks)),
                MedianResponseTime = responseTimes.Count == 0 
                    ? TimeSpan.Zero 
                    : responseTimes[responseTimes.Count / 2],
                P95ResponseTime = responseTimes.Count == 0 
                    ? TimeSpan.Zero 
                    : responseTimes[Math.Min((int)(responseTimes.Count * 0.95), responseTimes.Count - 1)],
                CircuitBreakerState = circuitBreaker.State,
                LastOperationTime = metricsData.LastOperationTime,
                MetricsStartTime = metricsData.MetricsStartTime,
                LastException = metricsData.LastException
            };
        }
    }

    /// <inheritdoc />
    public IEnumerable<ProviderMetrics> GetAllMetrics()
    {
        return _providerMetrics.Keys.Select(GetMetrics);
    }

    /// <inheritdoc />
    public bool IsProviderHealthy(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return false;

        var circuitBreaker = GetOrCreateCircuitBreaker(providerId);
        return circuitBreaker.State == CircuitBreakerState.Closed || 
               circuitBreaker.State == CircuitBreakerState.HalfOpen;
    }

    /// <inheritdoc />
    public void ResetMetrics(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or whitespace", nameof(providerId));

        if (_providerMetrics.TryRemove(providerId, out _))
        {
            _logger?.LogInformation("Reset metrics for provider {ProviderId}", providerId);
        }

        if (_circuitBreakers.TryGetValue(providerId, out var circuitBreaker))
        {
            circuitBreaker.Reset();
            _logger?.LogInformation("Reset circuit breaker for provider {ProviderId}", providerId);
        }
    }

    /// <inheritdoc />
    public event EventHandler<ProviderHealthChangedEventArgs>? ProviderHealthChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _metricsCleanupTimer.Dispose();
            
            foreach (var circuitBreaker in _circuitBreakers.Values)
            {
                circuitBreaker.Dispose();
            }
            
            _circuitBreakers.Clear();
            _providerMetrics.Clear();
            
            _disposed = true;
        }
    }

    private ProviderMetricsData GetOrCreateMetricsData(string providerId)
    {
        return _providerMetrics.GetOrAdd(providerId, _ => new ProviderMetricsData
        {
            MetricsStartTime = DateTimeOffset.UtcNow,
            LastOperationTime = DateTimeOffset.UtcNow
        });
    }

    private CircuitBreaker GetOrCreateCircuitBreaker(string providerId)
    {
        return _circuitBreakers.GetOrAdd(providerId, _ => 
            new CircuitBreaker(_circuitBreakerOptions, _logger));
    }

    private void OnProviderHealthChanged(string providerId, bool wasHealthy, bool isHealthy, CircuitBreakerState state)
    {
        var eventArgs = new ProviderHealthChangedEventArgs
        {
            ProviderId = providerId,
            WasHealthy = wasHealthy,
            IsHealthy = isHealthy,
            CircuitBreakerState = state,
            ChangeTime = DateTimeOffset.UtcNow
        };

        ProviderHealthChanged?.Invoke(this, eventArgs);
        
        _logger?.LogInformation(
            "Provider {ProviderId} health changed from {WasHealthy} to {IsHealthy} (State: {State})",
            providerId, wasHealthy, isHealthy, state);
    }

    private void CleanupOldMetrics(object? state)
    {
        try
        {
            var cutoffTime = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(24));
            var toRemove = new List<string>();

            foreach (var kvp in _providerMetrics)
            {
                if (kvp.Value.LastOperationTime < cutoffTime)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var providerId in toRemove)
            {
                _providerMetrics.TryRemove(providerId, out _);
                if (_circuitBreakers.TryRemove(providerId, out var circuitBreaker))
                {
                    circuitBreaker.Dispose();
                }
            }

            if (toRemove.Count > 0)
            {
                _logger?.LogDebug("Cleaned up metrics for {Count} inactive providers", toRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during metrics cleanup");
        }
    }

    private sealed class ProviderMetricsData
    {
        public object Lock { get; } = new object();
        public long SuccessCount { get; set; }
        public long FailureCount { get; set; }
        public List<TimeSpan> ResponseTimes { get; } = new();
        public DateTimeOffset MetricsStartTime { get; set; }
        public DateTimeOffset LastOperationTime { get; set; }
        public Exception? LastException { get; set; }
    }

    private sealed class OperationTracker : IOperationTracker
    {
        private readonly ProviderPerformanceMonitor _monitor;
        private readonly string _providerId;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;
        private bool _resultRecorded;

        public OperationTracker(ProviderPerformanceMonitor monitor, string providerId)
        {
            _monitor = monitor;
            _providerId = providerId;
            _stopwatch = Stopwatch.StartNew();
        }

        public void RecordFailure(Exception exception)
        {
            if (!_resultRecorded)
            {
                _stopwatch.Stop();
                _monitor.RecordFailure(_providerId, exception);
                _resultRecorded = true;
            }
        }

        public void RecordSuccess()
        {
            if (!_resultRecorded)
            {
                _stopwatch.Stop();
                _monitor.RecordSuccess(_providerId, _stopwatch.Elapsed);
                _resultRecorded = true;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (!_resultRecorded)
                {
                    RecordSuccess(); // Default to success if not explicitly marked
                }
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Configuration options for circuit breakers.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Gets or sets the number of consecutive failures required to open the circuit.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the timeout before attempting to close an open circuit.
    /// </summary>
    public TimeSpan OpenCircuitTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the number of test requests to allow in half-open state.
    /// </summary>
    public int HalfOpenMaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the time window for measuring the failure rate.
    /// </summary>
    public TimeSpan SlidingWindowSize { get; set; } = TimeSpan.FromMinutes(1);
}
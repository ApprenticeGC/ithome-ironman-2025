using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Default implementation of provider performance monitoring using System.Diagnostics.
/// </summary>
public sealed class ProviderPerformanceMonitor : IProviderPerformanceMonitor
{
    private readonly ConcurrentDictionary<string, ProviderMetricsData> _metricsData = new();
    private readonly ILogger<ProviderPerformanceMonitor> _logger;
    private readonly PerformanceMonitorOptions _options;

    /// <summary>
    /// Internal metrics data for tracking provider statistics.
    /// </summary>
    private class ProviderMetricsData
    {
        private readonly object _lock = new();
        private long _totalRequests;
        private long _failedRequests;
        private double _totalResponseTime;
        private Exception? _lastError;
        private DateTimeOffset _lastUpdated = DateTimeOffset.UtcNow;

        public ProviderMetrics GetMetrics()
        {
            lock (_lock)
            {
                var successRate = _totalRequests > 0 ? (double)(_totalRequests - _failedRequests) / _totalRequests * 100 : 100.0;
                var avgResponseTime = _totalRequests > 0 ? _totalResponseTime / _totalRequests : 0.0;
                
                // Calculate health score based on success rate and response time
                var healthScore = CalculateHealthScore(successRate, avgResponseTime);

                return new ProviderMetrics
                {
                    AverageResponseTime = avgResponseTime,
                    SuccessRate = successRate,
                    RequestCount = _totalRequests,
                    FailureCount = _failedRequests,
                    LastError = _lastError,
                    LastUpdated = _lastUpdated,
                    HealthScore = healthScore
                };
            }
        }

        public void RecordSuccess(TimeSpan responseTime)
        {
            lock (_lock)
            {
                _totalRequests++;
                _totalResponseTime += responseTime.TotalMilliseconds;
                _lastUpdated = DateTimeOffset.UtcNow;
            }
        }

        public void RecordFailure(Exception error)
        {
            lock (_lock)
            {
                _totalRequests++;
                _failedRequests++;
                _lastError = error;
                _lastUpdated = DateTimeOffset.UtcNow;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _totalRequests = 0;
                _failedRequests = 0;
                _totalResponseTime = 0;
                _lastError = null;
                _lastUpdated = DateTimeOffset.UtcNow;
            }
        }

        private static double CalculateHealthScore(double successRate, double avgResponseTime)
        {
            // Base score from success rate (0-70 points)
            var successScore = successRate * 0.7;
            
            // Performance score based on response time (0-30 points)
            var performanceScore = avgResponseTime switch
            {
                <= 100 => 30.0,        // Excellent
                <= 500 => 25.0,        // Good
                <= 1000 => 20.0,       // Fair
                <= 2000 => 15.0,       // Poor
                <= 5000 => 10.0,       // Very poor
                _ => 5.0                // Critical
            };

            return Math.Min(100.0, successScore + performanceScore);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderPerformanceMonitor"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for monitoring operations.</param>
    /// <param name="options">Configuration options for the monitor.</param>
    public ProviderPerformanceMonitor(ILogger<ProviderPerformanceMonitor> logger, PerformanceMonitorOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new PerformanceMonitorOptions();
    }

    /// <inheritdoc />
    public Task RecordSuccessAsync(string providerId, TimeSpan responseTime, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null, empty, or whitespace.", nameof(providerId));
        
        var data = _metricsData.GetOrAdd(providerId, _ => new ProviderMetricsData());
        data.RecordSuccess(responseTime);

        _logger.LogDebug("Recorded success for provider {ProviderId}: {ResponseTime}ms", 
            providerId, responseTime.TotalMilliseconds);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordFailureAsync(string providerId, Exception error, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null, empty, or whitespace.", nameof(providerId));
        ArgumentNullException.ThrowIfNull(error);

        var data = _metricsData.GetOrAdd(providerId, _ => new ProviderMetricsData());
        data.RecordFailure(error);

        _logger.LogWarning(error, "Recorded failure for provider {ProviderId}: {ErrorMessage}", 
            providerId, error.Message);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<ProviderMetrics> GetMetricsAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null, empty, or whitespace.", nameof(providerId));

        var data = _metricsData.GetOrAdd(providerId, _ => new ProviderMetricsData());
        return Task.FromResult(data.GetMetrics());
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, ProviderMetrics>> GetAllMetricsAsync(CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ProviderMetrics>();
        
        foreach (var kvp in _metricsData)
        {
            result[kvp.Key] = kvp.Value.GetMetrics();
        }

        return Task.FromResult<IReadOnlyDictionary<string, ProviderMetrics>>(result);
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var metrics = await GetMetricsAsync(providerId, cancellationToken);
        return metrics.HealthScore >= _options.MinHealthScoreThreshold &&
               metrics.SuccessRate >= _options.MinSuccessRateThreshold;
    }

    /// <inheritdoc />
    public Task ResetMetricsAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null, empty, or whitespace.", nameof(providerId));

        if (_metricsData.TryGetValue(providerId, out var data))
        {
            data.Reset();
            _logger.LogInformation("Reset metrics for provider {ProviderId}", providerId);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Configuration options for the performance monitor.
/// </summary>
public class PerformanceMonitorOptions
{
    /// <summary>
    /// Minimum health score threshold for considering a provider healthy (0-100).
    /// </summary>
    public double MinHealthScoreThreshold { get; set; } = 50.0;

    /// <summary>
    /// Minimum success rate threshold for considering a provider healthy (0-100).
    /// </summary>
    public double MinSuccessRateThreshold { get; set; } = 80.0;

    /// <summary>
    /// Maximum time to keep metrics data for inactive providers.
    /// </summary>
    public TimeSpan MetricsRetentionTime { get; set; } = TimeSpan.FromHours(24);
}
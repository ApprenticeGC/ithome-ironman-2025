using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace GameConsole.AI.Remote;

/// <summary>
/// Load balancer for distributing requests across multiple remote AI providers.
/// Implements various load balancing strategies with health monitoring.
/// </summary>
public class RemoteAILoadBalancer : IDisposable
{
    private readonly ILogger<RemoteAILoadBalancer> _logger;
    private readonly LoadBalancerOptions _options;
    private readonly ConcurrentDictionary<string, ProviderMetrics> _providerMetrics;
    private readonly ConcurrentDictionary<string, AIProvider> _providers;
    private readonly Timer _healthCheckTimer;
    private readonly object _roundRobinLock = new();
    private int _roundRobinIndex;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the RemoteAILoadBalancer.
    /// </summary>
    /// <param name="options">Load balancer configuration options.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public RemoteAILoadBalancer(IOptions<LoadBalancerOptions> options, ILogger<RemoteAILoadBalancer> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providerMetrics = new ConcurrentDictionary<string, ProviderMetrics>();
        _providers = new ConcurrentDictionary<string, AIProvider>();
        
        _healthCheckTimer = new Timer(PerformHealthChecks, null, 
            _options.HealthCheckInterval, _options.HealthCheckInterval);
        
        _logger.LogInformation("Initialized RemoteAILoadBalancer with strategy {Strategy}", _options.Strategy);
    }

    /// <summary>
    /// Registers an AI provider with the load balancer.
    /// </summary>
    /// <param name="provider">The AI provider to register.</param>
    public void RegisterProvider(AIProvider provider)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAILoadBalancer));
        
        _providers.TryAdd(provider.Id, provider);
        _providerMetrics.TryAdd(provider.Id, new ProviderMetrics
        {
            ProviderId = provider.Id,
            IsHealthy = provider.IsAvailable,
            ActiveRequests = 0,
            TotalRequests = 0,
            FailedRequests = 0,
            AverageResponseTime = TimeSpan.Zero,
            LastHealthCheck = DateTimeOffset.UtcNow
        });
        
        _logger.LogInformation("Registered provider {ProviderId} with load balancer", provider.Id);
    }

    /// <summary>
    /// Unregisters an AI provider from the load balancer.
    /// </summary>
    /// <param name="providerId">The ID of the provider to unregister.</param>
    public void UnregisterProvider(string providerId)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAILoadBalancer));
        
        _providers.TryRemove(providerId, out _);
        _providerMetrics.TryRemove(providerId, out _);
        
        _logger.LogInformation("Unregistered provider {ProviderId} from load balancer", providerId);
    }

    /// <summary>
    /// Selects the best provider for a request based on the configured strategy.
    /// </summary>
    /// <param name="excludeProviderIds">Provider IDs to exclude from selection.</param>
    /// <returns>The selected provider, or null if no healthy providers are available.</returns>
    public AIProvider? SelectProvider(HashSet<string>? excludeProviderIds = null)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAILoadBalancer));
        
        var healthyProviders = GetHealthyProviders(excludeProviderIds);
        
        if (!healthyProviders.Any())
        {
            _logger.LogWarning("No healthy providers available for selection");
            return null;
        }

        var selectedProvider = _options.Strategy switch
        {
            LoadBalancingStrategy.RoundRobin => SelectRoundRobin(healthyProviders),
            LoadBalancingStrategy.LeastConnections => SelectLeastConnections(healthyProviders),
            LoadBalancingStrategy.WeightedRoundRobin => SelectWeightedRoundRobin(healthyProviders),
            LoadBalancingStrategy.FastestResponse => SelectFastestResponse(healthyProviders),
            LoadBalancingStrategy.CostOptimized => SelectCostOptimized(healthyProviders),
            _ => SelectRoundRobin(healthyProviders)
        };

        if (selectedProvider != null)
        {
            _logger.LogDebug("Selected provider {ProviderId} using strategy {Strategy}", 
                selectedProvider.Id, _options.Strategy);
        }

        return selectedProvider;
    }

    /// <summary>
    /// Records the start of a request for tracking metrics.
    /// </summary>
    /// <param name="providerId">The provider ID handling the request.</param>
    /// <returns>A tracking context for the request.</returns>
    public RequestTrackingContext StartRequest(string providerId)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAILoadBalancer));
        
        if (_providerMetrics.TryGetValue(providerId, out var metrics))
        {
            Interlocked.Increment(ref metrics.ActiveRequests);
            metrics.IncrementTotalRequests();
        }

        return new RequestTrackingContext(providerId, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Records the completion of a request and updates metrics.
    /// </summary>
    /// <param name="context">The request tracking context.</param>
    /// <param name="success">Whether the request was successful.</param>
    public void CompleteRequest(RequestTrackingContext context, bool success)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAILoadBalancer));
        
        if (_providerMetrics.TryGetValue(context.ProviderId, out var metrics))
        {
            Interlocked.Decrement(ref metrics.ActiveRequests);
            
            var responseTime = DateTimeOffset.UtcNow - context.StartTime;
            UpdateAverageResponseTime(metrics, responseTime);
            
            if (!success)
            {
                metrics.IncrementFailedRequests();
            }
        }
    }

    /// <summary>
    /// Gets the current load balancing status.
    /// </summary>
    /// <returns>The current load balancing status.</returns>
    public LoadBalancingStatus GetStatus()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAILoadBalancer));
        
        var providerStatuses = _providerMetrics.Values
            .ToDictionary(m => m.ProviderId, m => new ProviderStatus
            {
                ProviderId = m.ProviderId,
                IsHealthy = m.IsHealthy,
                ActiveRequests = m.ActiveRequests,
                AverageResponseTimeMs = m.AverageResponseTime.TotalMilliseconds,
                LoadPercentage = CalculateLoadPercentage(m),
                LastError = m.LastError
            });

        return new LoadBalancingStatus
        {
            ProviderStatuses = providerStatuses,
            Strategy = _options.Strategy,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    private IEnumerable<AIProvider> GetHealthyProviders(HashSet<string>? excludeProviderIds)
    {
        return _providers.Values
            .Where(p => _providerMetrics.TryGetValue(p.Id, out var metrics) && 
                       metrics.IsHealthy && 
                       (excludeProviderIds == null || !excludeProviderIds.Contains(p.Id)));
    }

    private AIProvider SelectRoundRobin(IEnumerable<AIProvider> providers)
    {
        var providerList = providers.ToList();
        if (!providerList.Any()) return null!;

        lock (_roundRobinLock)
        {
            var index = _roundRobinIndex % providerList.Count;
            _roundRobinIndex = (_roundRobinIndex + 1) % providerList.Count;
            return providerList[index];
        }
    }

    private AIProvider? SelectLeastConnections(IEnumerable<AIProvider> providers)
    {
        return providers
            .Where(p => _providerMetrics.ContainsKey(p.Id))
            .OrderBy(p => _providerMetrics[p.Id].ActiveRequests)
            .ThenBy(p => _providerMetrics[p.Id].AverageResponseTime.TotalMilliseconds)
            .FirstOrDefault();
    }

    private AIProvider? SelectWeightedRoundRobin(IEnumerable<AIProvider> providers)
    {
        var weightedProviders = providers
            .SelectMany(p => Enumerable.Repeat(p, p.Priority))
            .ToList();

        return SelectRoundRobin(weightedProviders);
    }

    private AIProvider? SelectFastestResponse(IEnumerable<AIProvider> providers)
    {
        return providers
            .Where(p => _providerMetrics.ContainsKey(p.Id))
            .OrderBy(p => _providerMetrics[p.Id].AverageResponseTime.TotalMilliseconds)
            .ThenBy(p => _providerMetrics[p.Id].ActiveRequests)
            .FirstOrDefault();
    }

    private AIProvider? SelectCostOptimized(IEnumerable<AIProvider> providers)
    {
        // For cost optimization, prefer providers with lower cost per token
        // This is a simplified implementation - in practice would consider actual usage costs
        return providers
            .OrderBy(p => p.Priority) // Lower priority = lower cost in this example
            .ThenBy(p => _providerMetrics.ContainsKey(p.Id) ? _providerMetrics[p.Id].ActiveRequests : 0)
            .FirstOrDefault();
    }

    private void UpdateAverageResponseTime(ProviderMetrics metrics, TimeSpan responseTime)
    {
        // Simple exponential moving average
        var alpha = 0.1; // Smoothing factor
        var newAverage = TimeSpan.FromMilliseconds(
            (1 - alpha) * metrics.AverageResponseTime.TotalMilliseconds + 
            alpha * responseTime.TotalMilliseconds);
        
        metrics.AverageResponseTime = newAverage;
    }

    private double CalculateLoadPercentage(ProviderMetrics metrics)
    {
        if (_providers.TryGetValue(metrics.ProviderId, out var provider))
        {
            return Math.Min(100.0, (double)metrics.ActiveRequests / provider.MaxConcurrentRequests * 100.0);
        }
        return 0.0;
    }

    private async void PerformHealthChecks(object? state)
    {
        if (_disposed) return;

        _logger.LogDebug("Performing health checks for {ProviderCount} providers", _providers.Count);

        var healthCheckTasks = _providers.Values
            .Select(async provider =>
            {
                try
                {
                    // Simplified health check - in practice would ping actual endpoint
                    var isHealthy = await SimulateHealthCheck(provider);
                    
                    if (_providerMetrics.TryGetValue(provider.Id, out var metrics))
                    {
                        var wasHealthy = metrics.IsHealthy;
                        metrics.IsHealthy = isHealthy;
                        metrics.LastHealthCheck = DateTimeOffset.UtcNow;
                        
                        if (wasHealthy != isHealthy)
                        {
                            _logger.LogInformation("Provider {ProviderId} health changed: {OldStatus} -> {NewStatus}", 
                                provider.Id, wasHealthy ? "Healthy" : "Unhealthy", isHealthy ? "Healthy" : "Unhealthy");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Health check failed for provider {ProviderId}", provider.Id);
                    
                    if (_providerMetrics.TryGetValue(provider.Id, out var metrics))
                    {
                        metrics.IsHealthy = false;
                        metrics.LastError = ex.Message;
                        metrics.LastHealthCheck = DateTimeOffset.UtcNow;
                    }
                }
            });

        await Task.WhenAll(healthCheckTasks);
    }

    private async Task<bool> SimulateHealthCheck(AIProvider provider)
    {
        // Simulate health check with random outcome for demo
        // In practice, this would make actual HTTP requests to check provider health
        await Task.Delay(100);
        return Random.Shared.NextDouble() > 0.1; // 90% healthy
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _healthCheckTimer?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Configuration options for the load balancer.
/// </summary>
public class LoadBalancerOptions
{
    /// <summary>Gets or sets the load balancing strategy.</summary>
    public LoadBalancingStrategy Strategy { get; set; } = LoadBalancingStrategy.RoundRobin;
    
    /// <summary>Gets or sets the interval between health checks.</summary>
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(1);
    
    /// <summary>Gets or sets the timeout for health checks.</summary>
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Internal class for tracking provider metrics.
/// </summary>
internal class ProviderMetrics
{
    private long _totalRequests;
    private long _failedRequests;
    private TimeSpan _averageResponseTime = TimeSpan.Zero;
    private readonly object _responseLock = new();

    public required string ProviderId { get; init; }
    public volatile bool IsHealthy = true;
    public volatile int ActiveRequests;
    public DateTimeOffset LastHealthCheck = DateTimeOffset.UtcNow;
    public string? LastError;

    public long TotalRequests
    {
        get => Interlocked.Read(ref _totalRequests);
        set => Interlocked.Exchange(ref _totalRequests, value);
    }

    public long FailedRequests
    {
        get => Interlocked.Read(ref _failedRequests);
        set => Interlocked.Exchange(ref _failedRequests, value);
    }

    public TimeSpan AverageResponseTime
    {
        get
        {
            lock (_responseLock)
            {
                return _averageResponseTime;
            }
        }
        set
        {
            lock (_responseLock)
            {
                _averageResponseTime = value;
            }
        }
    }

    public void IncrementTotalRequests() => Interlocked.Increment(ref _totalRequests);
    public void IncrementFailedRequests() => Interlocked.Increment(ref _failedRequests);
}

/// <summary>
/// Context for tracking individual requests.
/// </summary>
public record RequestTrackingContext(string ProviderId, DateTimeOffset StartTime);
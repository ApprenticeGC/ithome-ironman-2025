using GameConsole.AI.Remote.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Remote.Services;

/// <summary>
/// Load balancer for distributing AI requests across multiple service endpoints.
/// Implements various load balancing strategies with health monitoring and adaptive weighting.
/// </summary>
public sealed class RemoteAILoadBalancer : IDisposable
{
    private readonly ILogger<RemoteAILoadBalancer> _logger;
    private readonly LoadBalancerConfig _config;
    private readonly ConcurrentDictionary<AIProvider, AIServiceClient> _clients;
    private readonly ConcurrentDictionary<AIProvider, EndpointMetrics> _metrics;
    private readonly Timer _healthCheckTimer;
    private readonly object _roundRobinLock = new();

    private int _roundRobinIndex;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteAILoadBalancer"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="config">Load balancer configuration.</param>
    /// <param name="clients">Dictionary of AI service clients by provider.</param>
    public RemoteAILoadBalancer(
        ILogger<RemoteAILoadBalancer> logger,
        LoadBalancerConfig config,
        IDictionary<AIProvider, AIServiceClient> clients)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _clients = new ConcurrentDictionary<AIProvider, AIServiceClient>(clients);
        _metrics = new ConcurrentDictionary<AIProvider, EndpointMetrics>();

        // Initialize metrics for each client
        foreach (var provider in _clients.Keys)
        {
            _metrics[provider] = new EndpointMetrics(provider);
        }

        // Start health check timer
        _healthCheckTimer = new Timer(
            PerformHealthChecks,
            null,
            _config.HealthCheckInterval,
            _config.HealthCheckInterval);

        _logger.LogInformation("Initialized load balancer with {Count} endpoints using {Strategy} strategy",
            _clients.Count, _config.Strategy);
    }

    /// <summary>
    /// Gets the number of healthy endpoints.
    /// </summary>
    public int HealthyEndpointCount => _metrics.Count(m => m.Value.IsHealthy);

    /// <summary>
    /// Gets the total number of configured endpoints.
    /// </summary>
    public int TotalEndpointCount => _clients.Count;

    /// <summary>
    /// Selects the best available AI service client for a request.
    /// </summary>
    /// <param name="request">The AI completion request to route.</param>
    /// <returns>The selected AI service client, or null if no healthy endpoints are available.</returns>
    public AIServiceClient? SelectEndpoint(AICompletionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var healthyClients = GetHealthyClients().ToList();

        if (healthyClients.Count == 0)
        {
            _logger.LogWarning("No healthy endpoints available for request");
            return null;
        }

        if (healthyClients.Count == 1)
        {
            return healthyClients[0].client;
        }

        return _config.Strategy switch
        {
            LoadBalancingStrategy.RoundRobin => SelectRoundRobin(healthyClients),
            LoadBalancingStrategy.WeightedRoundRobin => SelectWeightedRoundRobin(healthyClients, request),
            LoadBalancingStrategy.LeastConnections => SelectLeastConnections(healthyClients),
            LoadBalancingStrategy.FastestResponse => SelectFastestResponse(healthyClients),
            LoadBalancingStrategy.LowestCost => SelectLowestCost(healthyClients, request),
            _ => SelectRoundRobin(healthyClients)
        };
    }

    /// <summary>
    /// Records the completion of a request for metrics tracking.
    /// </summary>
    /// <param name="provider">The AI provider that handled the request.</param>
    /// <param name="responseTime">The response time in milliseconds.</param>
    /// <param name="success">Whether the request was successful.</param>
    /// <param name="tokenCount">Number of tokens used in the request.</param>
    public void RecordRequestCompletion(AIProvider provider, double responseTime, bool success, int tokenCount = 0)
    {
        if (_metrics.TryGetValue(provider, out var metrics))
        {
            metrics.RecordRequest(responseTime, success, tokenCount);

            if (_config.EnableAdaptiveWeighting)
            {
                UpdateAdaptiveWeights();
            }

            _logger.LogDebug("Recorded request completion for {Provider}: {ResponseTime}ms, Success: {Success}",
                provider, responseTime, success);
        }
    }

    /// <summary>
    /// Gets health and performance metrics for all endpoints.
    /// </summary>
    /// <returns>Dictionary of endpoint metrics by provider.</returns>
    public IReadOnlyDictionary<AIProvider, EndpointMetrics> GetMetrics()
    {
        return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Forces a health check on all endpoints.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async health check operation.</returns>
    public async Task ForceHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing forced health check on all endpoints");

        var healthCheckTasks = _clients.Select(async kvp =>
        {
            try
            {
                var health = await kvp.Value.HealthCheckAsync(cancellationToken);
                _metrics[kvp.Key].UpdateHealth(health);
                _logger.LogDebug("Health check completed for {Provider}: {Status}", kvp.Key, health.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for {Provider}", kvp.Key);
                _metrics[kvp.Key].UpdateHealth(new EndpointHealth
                {
                    Status = HealthStatus.Unhealthy,
                    ErrorMessage = ex.Message,
                    ResponseTimeMs = 0
                });
            }
        });

        await Task.WhenAll(healthCheckTasks);
    }

    private IEnumerable<(AIServiceClient client, AIProvider provider)> GetHealthyClients()
    {
        return _clients
            .Where(kvp => kvp.Value.IsHealthy && _metrics[kvp.Key].IsHealthy)
            .Select(kvp => (kvp.Value, kvp.Key));
    }

    private AIServiceClient SelectRoundRobin(IList<(AIServiceClient client, AIProvider provider)> healthyClients)
    {
        lock (_roundRobinLock)
        {
            _roundRobinIndex = (_roundRobinIndex + 1) % healthyClients.Count;
            return healthyClients[_roundRobinIndex].client;
        }
    }

    private AIServiceClient SelectWeightedRoundRobin(IList<(AIServiceClient client, AIProvider provider)> healthyClients, AICompletionRequest request)
    {
        // Calculate weighted selection based on priority and performance
        var weightedClients = healthyClients
            .Select(c => new
            {
                c.client,
                c.provider,
                weight = CalculateWeight(c.provider, request)
            })
            .OrderByDescending(c => c.weight)
            .ToList();

        if (weightedClients.Count == 0)
            return healthyClients[0].client;

        // Select based on weighted probability
        var totalWeight = weightedClients.Sum(c => c.weight);
        var random = Random.Shared.Next(0, (int)totalWeight + 1);
        var currentWeight = 0;

        foreach (var client in weightedClients)
        {
            currentWeight += (int)client.weight;
            if (random <= currentWeight)
            {
                return client.client;
            }
        }

        return weightedClients[0].client;
    }

    private AIServiceClient SelectLeastConnections(IList<(AIServiceClient client, AIProvider provider)> healthyClients)
    {
        var clientWithLeastConnections = healthyClients
            .OrderBy(c => _metrics[c.provider].CurrentConnections)
            .ThenBy(c => _metrics[c.provider].AverageResponseTime)
            .First();

        _metrics[clientWithLeastConnections.provider].IncrementConnections();
        return clientWithLeastConnections.client;
    }

    private AIServiceClient SelectFastestResponse(IList<(AIServiceClient client, AIProvider provider)> healthyClients)
    {
        return healthyClients
            .OrderBy(c => _metrics[c.provider].AverageResponseTime)
            .ThenByDescending(c => _metrics[c.provider].SuccessRate)
            .First()
            .client;
    }

    private AIServiceClient SelectLowestCost(IList<(AIServiceClient client, AIProvider provider)> healthyClients, AICompletionRequest request)
    {
        // This would require access to cost per token configuration
        // For now, use a simple heuristic based on token count and provider
        return healthyClients
            .OrderBy(c => EstimateCost(c.provider, request.MaxTokens))
            .ThenBy(c => _metrics[c.provider].AverageResponseTime)
            .First()
            .client;
    }

    private double CalculateWeight(AIProvider provider, AICompletionRequest request)
    {
        var metrics = _metrics[provider];
        var baseWeight = 100.0; // Base weight

        // Adjust for performance
        var responseTimeFactor = 1000.0 / Math.Max(metrics.AverageResponseTime, 1.0);
        var successRateFactor = metrics.SuccessRate * 100;
        var connectionsFactor = Math.Max(100 - metrics.CurrentConnections * 10, 10);

        // Adjust for request priority
        var priorityFactor = request.Priority switch
        {
            RequestPriority.Critical => 2.0,
            RequestPriority.High => 1.5,
            RequestPriority.Normal => 1.0,
            RequestPriority.Low => 0.5,
            _ => 1.0
        };

        return baseWeight * responseTimeFactor * successRateFactor * connectionsFactor * priorityFactor;
    }

    private static double EstimateCost(AIProvider provider, int tokenCount)
    {
        // Simple cost estimation - in a real implementation, this would use actual pricing data
        var costPerToken = provider switch
        {
            AIProvider.OpenAI => 0.002,
            AIProvider.Azure => 0.002,
            AIProvider.AWS => 0.003,
            AIProvider.Anthropic => 0.008,
            AIProvider.Local => 0.0,
            _ => 0.001
        };

        return tokenCount * costPerToken;
    }

    private void UpdateAdaptiveWeights()
    {
        // This would implement dynamic weight adjustment based on performance metrics
        // For now, this is a placeholder for future enhancement
        _logger.LogTrace("Updating adaptive weights based on performance metrics");
    }

    private async void PerformHealthChecks(object? state)
    {
        try
        {
            await ForceHealthCheckAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled health check");
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="RemoteAILoadBalancer"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _healthCheckTimer?.Dispose();
            
            foreach (var client in _clients.Values)
            {
                client?.Dispose();
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// Performance and health metrics for an AI service endpoint.
/// </summary>
public sealed class EndpointMetrics
{
    private readonly object _lock = new();
    private readonly Queue<double> _recentResponseTimes = new();
    private readonly Queue<bool> _recentResults = new();
    private const int MaxSampleSize = 100;
    private int _currentConnections;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointMetrics"/> class.
    /// </summary>
    /// <param name="provider">The AI provider this metrics instance tracks.</param>
    public EndpointMetrics(AIProvider provider)
    {
        Provider = provider;
        LastHealthCheck = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the AI provider for this metrics instance.
    /// </summary>
    public AIProvider Provider { get; }

    /// <summary>
    /// Gets a value indicating whether the endpoint is healthy.
    /// </summary>
    public bool IsHealthy { get; private set; } = true;

    /// <summary>
    /// Gets the current number of active connections.
    /// </summary>
    public int CurrentConnections => _currentConnections;

    /// <summary>
    /// Gets the average response time in milliseconds.
    /// </summary>
    public double AverageResponseTime { get; private set; } = 100.0;

    /// <summary>
    /// Gets the success rate (0.0 to 1.0).
    /// </summary>
    public double SuccessRate { get; private set; } = 1.0;

    /// <summary>
    /// Gets the total number of requests processed.
    /// </summary>
    public long TotalRequests { get; private set; }

    /// <summary>
    /// Gets the total number of tokens processed.
    /// </summary>
    public long TotalTokens { get; private set; }

    /// <summary>
    /// Gets the timestamp of the last health check.
    /// </summary>
    public DateTimeOffset LastHealthCheck { get; private set; }

    /// <summary>
    /// Gets the last known health status.
    /// </summary>
    public HealthStatus HealthStatus { get; private set; } = HealthStatus.Healthy;

    /// <summary>
    /// Records the completion of a request.
    /// </summary>
    /// <param name="responseTime">Response time in milliseconds.</param>
    /// <param name="success">Whether the request was successful.</param>
    /// <param name="tokenCount">Number of tokens processed.</param>
    public void RecordRequest(double responseTime, bool success, int tokenCount = 0)
    {
        lock (_lock)
        {
            TotalRequests++;
            TotalTokens += tokenCount;

            // Track recent response times for moving average
            _recentResponseTimes.Enqueue(responseTime);
            if (_recentResponseTimes.Count > MaxSampleSize)
            {
                _recentResponseTimes.Dequeue();
            }

            // Track recent results for success rate
            _recentResults.Enqueue(success);
            if (_recentResults.Count > MaxSampleSize)
            {
                _recentResults.Dequeue();
            }

            // Recalculate metrics
            AverageResponseTime = _recentResponseTimes.DefaultIfEmpty(100.0).Average();
            SuccessRate = _recentResults.DefaultIfEmpty(true).Average(r => r ? 1.0 : 0.0);
        }
    }

    /// <summary>
    /// Increments the current connection count.
    /// </summary>
    public void IncrementConnections()
    {
        Interlocked.Increment(ref _currentConnections);
    }

    /// <summary>
    /// Decrements the current connection count.
    /// </summary>
    public void DecrementConnections()
    {
        Interlocked.Decrement(ref _currentConnections);
    }

    /// <summary>
    /// Updates the health status based on a health check result.
    /// </summary>
    /// <param name="health">The health check result.</param>
    public void UpdateHealth(EndpointHealth health)
    {
        lock (_lock)
        {
            HealthStatus = health.Status;
            IsHealthy = health.Status == HealthStatus.Healthy;
            LastHealthCheck = DateTimeOffset.UtcNow;
        }
    }
}
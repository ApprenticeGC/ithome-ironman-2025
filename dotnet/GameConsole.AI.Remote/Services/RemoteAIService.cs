using GameConsole.AI.Remote.Configuration;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
using System.Collections.Concurrent;

namespace GameConsole.AI.Remote.Services;

/// <summary>
/// Implementation of the remote AI service that orchestrates multiple AI providers
/// with load balancing, failover, and comprehensive monitoring capabilities.
/// </summary>
public sealed class RemoteAIService : IRemoteAIService, IBatchProcessingCapability, IRateLimitingCapability
{
    private readonly ILogger<RemoteAIService> _logger;
    private readonly RemoteAIConfiguration _configuration;
    private readonly RemoteAILoadBalancer _loadBalancer;
    private readonly AIServiceFailover _failover;
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<AIProvider, AIServiceClient> _clients;
    private readonly AIUsageTracker _usageTracker;
    private readonly RateLimiter _rateLimiter;

    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Delegate for local AI fallback operations.
    /// </summary>
    public AIServiceFailover.LocalAIFallbackDelegate? LocalAIFallback { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteAIService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages.</param>
    /// <param name="configuration">Configuration for the remote AI service.</param>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="cache">Memory cache for response caching.</param>
    public RemoteAIService(
        ILogger<RemoteAIService> logger,
        RemoteAIConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));

        // Initialize clients for each configured endpoint
        _clients = new ConcurrentDictionary<AIProvider, AIServiceClient>();
        foreach (var endpoint in _configuration.Endpoints.Where(e => e.Value.IsEnabled))
        {
            var httpClient = httpClientFactory.CreateClient($"AI-{endpoint.Key}");
            var client = new AIServiceClient(
                httpClient,
                _cache,
                logger,
                endpoint.Value,
                _configuration.Caching,
                _configuration.Failover)
            {
                Provider = endpoint.Key
            };
            
            _clients[endpoint.Key] = client;
        }

        // Initialize load balancer and failover
        _loadBalancer = new RemoteAILoadBalancer(
            logger,
            _configuration.LoadBalancer,
            _clients);

        _failover = new AIServiceFailover(
            logger,
            _configuration.Failover,
            _loadBalancer);

        // Initialize usage tracking and rate limiting
        _usageTracker = new AIUsageTracker();
        _rateLimiter = new RateLimiter(_configuration.GlobalRateLimit);

        _logger.LogInformation("Initialized RemoteAIService with {ClientCount} providers: {Providers}",
            _clients.Count, string.Join(", ", _clients.Keys));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning && !_disposed;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        if (_isRunning)
        {
            _logger.LogWarning("Service is already initialized");
            return;
        }

        _logger.LogInformation("Initializing RemoteAIService...");

        // Perform initial health checks
        await _loadBalancer.ForceHealthCheckAsync(cancellationToken);

        var healthyCount = _loadBalancer.HealthyEndpointCount;
        if (healthyCount == 0)
        {
            _logger.LogWarning("No healthy endpoints detected during initialization");
        }
        else
        {
            _logger.LogInformation("Found {HealthyCount} healthy endpoints out of {TotalCount}",
                healthyCount, _loadBalancer.TotalEndpointCount);
        }

        _logger.LogInformation("RemoteAIService initialized successfully");
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        if (_isRunning)
        {
            _logger.LogWarning("Service is already running");
            return Task.CompletedTask;
        }

        _isRunning = true;
        _logger.LogInformation("RemoteAIService started");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Service is not running");
            return Task.CompletedTask;
        }

        _isRunning = false;
        _logger.LogInformation("RemoteAIService stopped");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<AICompletionResponse> GetCompletionAsync(AICompletionRequest request, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        if (!_isRunning)
        {
            throw new InvalidOperationException("Service is not running. Call StartAsync first.");
        }

        // Apply rate limiting
        await _rateLimiter.WaitForAvailableSlotAsync(cancellationToken);

        try
        {
            var startTime = DateTimeOffset.UtcNow;
            
            _logger.LogDebug("Processing completion request with prompt length: {PromptLength}, MaxTokens: {MaxTokens}",
                request.Prompt.Length, request.MaxTokens);

            var response = await _failover.ExecuteWithFailoverAsync(request, LocalAIFallback, cancellationToken);

            var duration = DateTimeOffset.UtcNow - startTime;
            _usageTracker.RecordRequest(response.Provider, response.Usage.TotalTokens, duration, true, EstimateCost(response));

            _logger.LogInformation("Completed request using {Provider} in {Duration}ms, Tokens: {TokenCount}",
                response.Provider, duration.TotalMilliseconds, response.Usage.TotalTokens);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process completion request: {Message}", ex.Message);
            _usageTracker.RecordRequest(AIProvider.OpenAI, 0, TimeSpan.Zero, false, 0); // Record failure
            throw;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AIStreamingChunk> GetStreamingCompletionAsync(AICompletionRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        if (!_isRunning)
        {
            throw new InvalidOperationException("Service is not running. Call StartAsync first.");
        }

        // Apply rate limiting
        await _rateLimiter.WaitForAvailableSlotAsync(cancellationToken);

        var startTime = DateTimeOffset.UtcNow;
        var tokenCount = 0;
        AIProvider? usedProvider = null;
        var success = false;

        try
        {
            _logger.LogDebug("Processing streaming completion request with prompt length: {PromptLength}",
                request.Prompt.Length);

            await foreach (var chunk in _failover.ExecuteStreamingWithFailoverAsync(request, cancellationToken))
            {
                usedProvider = chunk.Provider;
                tokenCount += chunk.Content.Length / 4; // Rough token estimation

                if (chunk.IsComplete)
                {
                    success = true;
                }

                yield return chunk;
            }
        }
        finally
        {
            var duration = DateTimeOffset.UtcNow - startTime;
            if (usedProvider.HasValue)
            {
                _usageTracker.RecordRequest(usedProvider.Value, tokenCount, duration, success, EstimateCost(usedProvider.Value, tokenCount));
            }

            _logger.LogInformation("Completed streaming request using {Provider} in {Duration}ms, Success: {Success}",
                usedProvider, duration.TotalMilliseconds, success);
        }
    }

    /// <inheritdoc />
    public async Task<ServiceHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        await _loadBalancer.ForceHealthCheckAsync(cancellationToken);

        var endpointMetrics = _loadBalancer.GetMetrics();
        var endpointStatuses = endpointMetrics.ToDictionary(
            kvp => kvp.Key,
            kvp => new EndpointHealth
            {
                Status = kvp.Value.HealthStatus,
                ResponseTimeMs = kvp.Value.AverageResponseTime,
                LastSuccessfulRequest = kvp.Value.LastHealthCheck
            });

        var overallStatus = CalculateOverallHealthStatus(endpointStatuses.Values);

        return new ServiceHealthStatus
        {
            OverallStatus = overallStatus,
            EndpointStatuses = endpointStatuses,
            LastChecked = DateTimeOffset.UtcNow
        };
    }

    /// <inheritdoc />
    public Task<AIUsageMetrics> GetUsageMetricsAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        return Task.FromResult(_usageTracker.GetMetrics());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AICompletionResponse>> ProcessBatchAsync(IEnumerable<AICompletionRequest> requests, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(requests);

        if (!_isRunning)
        {
            throw new InvalidOperationException("Service is not running. Call StartAsync first.");
        }

        var requestList = requests.ToList();
        _logger.LogInformation("Processing batch of {RequestCount} requests", requestList.Count);

        var batchTasks = requestList.Select(async request =>
        {
            try
            {
                return await GetCompletionAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch request failed: {Message}", ex.Message);
                throw;
            }
        });

        var responses = await Task.WhenAll(batchTasks);
        _logger.LogInformation("Completed batch processing of {RequestCount} requests", requestList.Count);

        return responses;
    }

    /// <inheritdoc />
    public Task<RateLimitStatus> GetRateLimitStatusAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);

        return Task.FromResult(_rateLimiter.GetStatus());
    }

    /// <inheritdoc />
    public Task SetRateLimitConfigAsync(RateLimitConfig config, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIfDisposed(_disposed, this);
        ArgumentNullException.ThrowIfNull(config);

        _rateLimiter.UpdateConfig(config);
        _logger.LogInformation("Updated rate limit configuration: MaxRequests={MaxRequests}, TimeWindow={TimeWindow}",
            config.MaxRequests, config.TimeWindow);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[]
        {
            typeof(IBatchProcessingCapability),
            typeof(IRateLimitingCapability)
        };

        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    /// <inheritdoc />
    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var hasCapability = typeof(T) == typeof(IBatchProcessingCapability) ||
                           typeof(T) == typeof(IRateLimitingCapability);

        return Task.FromResult(hasCapability);
    }

    /// <inheritdoc />
    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IBatchProcessingCapability))
        {
            return Task.FromResult(this as T);
        }

        if (typeof(T) == typeof(IRateLimitingCapability))
        {
            return Task.FromResult(this as T);
        }

        return Task.FromResult<T?>(null);
    }

    private static HealthStatus CalculateOverallHealthStatus(IEnumerable<EndpointHealth> endpointHealths)
    {
        var healthStatuses = endpointHealths.Select(h => h.Status).ToList();

        if (!healthStatuses.Any())
            return HealthStatus.Unhealthy;

        if (healthStatuses.All(s => s == HealthStatus.Healthy))
            return HealthStatus.Healthy;

        if (healthStatuses.Any(s => s == HealthStatus.Healthy))
            return HealthStatus.Degraded;

        return HealthStatus.Unhealthy;
    }

    private static decimal EstimateCost(AICompletionResponse response)
    {
        return EstimateCost(response.Provider, response.Usage.TotalTokens);
    }

    private static decimal EstimateCost(AIProvider provider, int tokenCount)
    {
        var costPerToken = provider switch
        {
            AIProvider.OpenAI => 0.002m,
            AIProvider.Azure => 0.002m,
            AIProvider.AWS => 0.003m,
            AIProvider.Anthropic => 0.008m,
            AIProvider.Local => 0.0m,
            _ => 0.001m
        };

        return tokenCount * costPerToken / 1000m; // Cost per 1K tokens
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await StopAsync();
            
            _loadBalancer?.Dispose();
            
            foreach (var client in _clients.Values)
            {
                client?.Dispose();
            }

            _disposed = true;
        }
    }
}

/// <summary>
/// Tracks usage and cost metrics for AI services.
/// </summary>
internal sealed class AIUsageTracker
{
    private readonly ConcurrentDictionary<AIProvider, ProviderStats> _providerStats = new();
    private readonly object _lock = new();
    
    private long _totalRequests;
    private long _totalTokens;
    private decimal _totalCost;

    public void RecordRequest(AIProvider provider, int tokenCount, TimeSpan duration, bool success, decimal cost)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Add(ref _totalTokens, tokenCount);
        
        lock (_lock)
        {
            _totalCost += cost;
        }

        _providerStats.AddOrUpdate(provider, 
            new ProviderStats { TokensUsed = tokenCount, RequestCount = 1, EstimatedCost = cost, TotalResponseTime = duration.TotalMilliseconds, SuccessCount = success ? 1 : 0 },
            (key, existing) => new ProviderStats 
            {
                TokensUsed = existing.TokensUsed + tokenCount,
                RequestCount = existing.RequestCount + 1,
                EstimatedCost = existing.EstimatedCost + cost,
                TotalResponseTime = existing.TotalResponseTime + duration.TotalMilliseconds,
                SuccessCount = existing.SuccessCount + (success ? 1 : 0)
            });
    }

    public AIUsageMetrics GetMetrics()
    {
        var providerBreakdown = _providerStats.ToDictionary(
            kvp => kvp.Key,
            kvp => new ProviderUsage
            {
                TokensUsed = kvp.Value.TokensUsed,
                RequestCount = kvp.Value.RequestCount,
                EstimatedCostUsd = kvp.Value.EstimatedCost,
                AverageResponseTimeMs = kvp.Value.RequestCount > 0 ? kvp.Value.TotalResponseTime / kvp.Value.RequestCount : 0,
                SuccessRate = kvp.Value.RequestCount > 0 ? (double)kvp.Value.SuccessCount / kvp.Value.RequestCount : 0
            });

        return new AIUsageMetrics
        {
            TotalTokensUsed = _totalTokens,
            TotalRequests = _totalRequests,
            EstimatedCostUsd = _totalCost,
            ProviderBreakdown = providerBreakdown,
            Period = TimeSpan.FromHours(1) // This would be configurable in a real implementation
        };
    }

    private record ProviderStats
    {
        public long TokensUsed { get; init; }
        public long RequestCount { get; init; }
        public decimal EstimatedCost { get; init; }
        public double TotalResponseTime { get; init; }
        public long SuccessCount { get; init; }
    }
}

/// <summary>
/// Simple rate limiter implementation.
/// </summary>
internal sealed class RateLimiter
{
    private readonly object _lock = new();
    private RateLimitConfig _config;
    private readonly Queue<DateTimeOffset> _requestTimestamps = new();

    public RateLimiter(RateLimitConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task WaitForAvailableSlotAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var windowStart = now - _config.TimeWindow;

            // Remove old timestamps outside the current window
            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < windowStart)
            {
                _requestTimestamps.Dequeue();
            }

            if (_requestTimestamps.Count < _config.MaxRequests)
            {
                _requestTimestamps.Enqueue(now);
                return;
            }
        }

        // Rate limit exceeded, wait for the oldest request to expire
        var oldestRequest = _requestTimestamps.Peek();
        var waitTime = oldestRequest + _config.TimeWindow - DateTimeOffset.UtcNow;
        
        if (waitTime > TimeSpan.Zero)
        {
            await Task.Delay(waitTime, cancellationToken);
        }

        // Retry after waiting
        await WaitForAvailableSlotAsync(cancellationToken);
    }

    public RateLimitStatus GetStatus()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            var windowStart = now - _config.TimeWindow;

            // Clean up old timestamps
            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < windowStart)
            {
                _requestTimestamps.Dequeue();
            }

            var oldestRequest = _requestTimestamps.Count > 0 ? _requestTimestamps.Peek() : now;
            var resetTime = oldestRequest + _config.TimeWindow - now;

            return new RateLimitStatus
            {
                CurrentRequests = _requestTimestamps.Count,
                MaxRequests = _config.MaxRequests,
                ResetTime = resetTime > TimeSpan.Zero ? resetTime : TimeSpan.Zero,
                IsThrottled = _requestTimestamps.Count >= _config.MaxRequests
            };
        }
    }

    public void UpdateConfig(RateLimitConfig config)
    {
        lock (_lock)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            // Clean up queue if new limit is lower
            while (_requestTimestamps.Count > _config.MaxRequests)
            {
                _requestTimestamps.Dequeue();
            }
        }
    }
}
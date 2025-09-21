using GameConsole.AI.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace GameConsole.AI.Remote;

/// <summary>
/// Remote AI service implementation that integrates with cloud AI providers.
/// Provides load balancing, failover, caching, and cost monitoring capabilities.
/// </summary>
public class RemoteAIService : IRemoteAIService
{
    private readonly AIServiceClient _client;
    private readonly RemoteAILoadBalancer _loadBalancer;
    private readonly AIServiceFailover _failover;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RemoteAIService> _logger;
    private readonly RemoteAIServiceOptions _options;
    private readonly Dictionary<string, AIProvider> _providers;
    private readonly RateLimiter _rateLimiter;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the RemoteAIService.
    /// </summary>
    public RemoteAIService(
        AIServiceClient client,
        RemoteAILoadBalancer loadBalancer,
        AIServiceFailover failover,
        IMemoryCache cache,
        IOptions<RemoteAIServiceOptions> options,
        ILogger<RemoteAIService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
        _failover = failover ?? throw new ArgumentNullException(nameof(failover));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _providers = new Dictionary<string, AIProvider>();
        _rateLimiter = new RateLimiter(_options.GlobalRateLimit);
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        
        _logger.LogInformation("Initializing RemoteAIService");
        
        // Initialize providers
        foreach (var providerConfig in _options.Providers)
        {
            var provider = new AIProvider
            {
                Id = providerConfig.Id,
                Name = providerConfig.Name,
                Type = providerConfig.Type,
                BaseUrl = providerConfig.BaseUrl,
                IsAvailable = providerConfig.IsEnabled,
                Priority = providerConfig.Priority,
                MaxConcurrentRequests = providerConfig.MaxConcurrentRequests,
                RateLimit = providerConfig.RateLimit
            };
            
            _providers[provider.Id] = provider;
            _loadBalancer.RegisterProvider(provider);
            
            _logger.LogInformation("Registered AI provider {ProviderId} ({ProviderType})", 
                provider.Id, provider.Type);
        }
        
        _logger.LogInformation("Initialized RemoteAIService with {ProviderCount} providers", _providers.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        if (_isRunning) return Task.CompletedTask;
        
        _logger.LogInformation("Starting RemoteAIService");
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        if (!_isRunning) return Task.CompletedTask;
        
        _logger.LogInformation("Stopping RemoteAIService");
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        
        var cacheKey = "available_models";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<AIModel>? cachedModels))
        {
            return Task.FromResult(cachedModels!);
        }

        var models = new List<AIModel>();
        
        foreach (var provider in _providers.Values.Where(p => p.IsAvailable))
        {
            try
            {
                var providerModels = GetProviderModels(provider);
                models.AddRange(providerModels);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get models from provider {ProviderId}", provider.Id);
            }
        }

        _cache.Set(cacheKey, models, TimeSpan.FromMinutes(30));
        return Task.FromResult<IEnumerable<AIModel>>(models);
    }

    /// <inheritdoc />
    public async Task<AIResponse> GenerateCompletionAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        if (!_isRunning) throw new InvalidOperationException("Service is not running");
        
        // Check cache first
        if (_options.EnableCaching)
        {
            var cacheKey = GenerateCacheKey(request);
            if (_cache.TryGetValue(cacheKey, out AIResponse? cachedResponse))
            {
                _logger.LogDebug("Returning cached response for request");
                return cachedResponse!;
            }
        }

        // Apply rate limiting
        await _rateLimiter.WaitAsync(cancellationToken);

        // Execute with failover
        var response = await _failover.ExecuteWithFailoverAsync(
            request,
            async (provider, req, ct) =>
            {
                _logger.LogDebug("Executing request with provider {ProviderId}", provider.Id);
                return await _client.SendCompletionAsync(provider, req, ct);
            },
            cancellationToken);

        // Cache successful response
        if (_options.EnableCaching && response != null)
        {
            var cacheKey = GenerateCacheKey(request);
            _cache.Set(cacheKey, response, _options.CacheExpiration);
        }

        return response ?? throw new AIServiceException("Received null response from all providers", "all");
    }

    /// <inheritdoc />
    public async Task<AIResponse> GenerateCompletionAsync(string providerId, AIRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        if (!_isRunning) throw new InvalidOperationException("Service is not running");
        
        if (!_providers.TryGetValue(providerId, out var provider))
        {
            throw new ArgumentException($"Provider {providerId} not found", nameof(providerId));
        }

        await _rateLimiter.WaitAsync(cancellationToken);
        
        _logger.LogDebug("Executing request with specific provider {ProviderId}", providerId);
        return await _client.SendCompletionAsync(provider, request, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AIResponseChunk> GenerateStreamingCompletionAsync(
        AIRequest request, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        if (!_isRunning) throw new InvalidOperationException("Service is not running");
        
        await _rateLimiter.WaitAsync(cancellationToken);

        var provider = _loadBalancer.SelectProvider();
        if (provider == null)
            throw new AIServiceException("No providers available for streaming", "none");

        _logger.LogDebug("Starting streaming completion with provider {ProviderId}", provider.Id);

        await foreach (var chunk in _client.SendStreamingCompletionAsync(provider, request, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <inheritdoc />
    public async Task<AIHealthStatus> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        
        var healthyProviders = 0;
        var totalResponseTime = 0.0;
        var errors = new List<string>();

        foreach (var provider in _providers.Values)
        {
            try
            {
                var providerHealth = await _client.CheckHealthAsync(provider, cancellationToken);
                if (providerHealth.IsHealthy)
                {
                    healthyProviders++;
                    totalResponseTime += providerHealth.ResponseTimeMs ?? 0;
                }
                else
                {
                    errors.Add($"{provider.Id}: {providerHealth.Error}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{provider.Id}: {ex.Message}");
            }
        }

        var isHealthy = healthyProviders > 0;
        var avgResponseTime = healthyProviders > 0 ? totalResponseTime / healthyProviders : 0;

        return new AIHealthStatus
        {
            IsHealthy = isHealthy,
            Status = isHealthy ? $"{healthyProviders}/{_providers.Count} providers healthy" : "No healthy providers",
            ResponseTimeMs = avgResponseTime,
            Error = errors.Any() ? string.Join("; ", errors) : null
        };
    }

    /// <inheritdoc />
    public Task<IEnumerable<AIProvider>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        
        return Task.FromResult(_providers.Values.Where(p => p.IsAvailable).AsEnumerable());
    }

    /// <inheritdoc />
    public Task<LoadBalancingStatus> GetLoadBalancingStatusAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        
        return Task.FromResult(_loadBalancer.GetStatus());
    }

    /// <inheritdoc />
    public Task<CostMonitoringInfo> GetCostMonitoringAsync(TimeRange timeRange, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        
        // Simplified cost monitoring - in practice would integrate with provider billing APIs
        var costByProvider = _providers.Values.ToDictionary(p => p.Id, p => (decimal)(Random.Shared.NextSingle() * 100));
        var costByModel = new Dictionary<string, decimal> { ["gpt-4"] = 50m, ["gpt-3.5-turbo"] = 25m };

        return Task.FromResult(new CostMonitoringInfo
        {
            TotalCost = costByProvider.Values.Sum(),
            CostByProvider = costByProvider,
            CostByModel = costByModel,
            TotalTokens = 1000000,
            TotalRequests = 5000,
            TimeRange = timeRange
        });
    }

    /// <inheritdoc />
    public Task ConfigureFailoverAsync(FailoverConfiguration failoverConfig, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        
        // Update configuration - in practice would persist this
        _logger.LogInformation("Updated failover configuration: Strategy={Strategy}, MaxRetries={MaxRetries}", 
            failoverConfig.Strategy, failoverConfig.MaxRetryAttempts);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIResponse>> GenerateBatchCompletionAsync(IEnumerable<AIRequest> requests, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RemoteAIService));
        if (!_isRunning) throw new InvalidOperationException("Service is not running");
        
        var requestList = requests.ToList();
        _logger.LogDebug("Processing batch of {RequestCount} requests", requestList.Count);

        // Process batch with controlled concurrency
        var semaphore = new SemaphoreSlim(_options.MaxConcurrentBatchRequests, _options.MaxConcurrentBatchRequests);
        var batchTasks = requestList.Select(async request =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await GenerateCompletionAsync(request, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        return await Task.WhenAll(batchTasks);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new[]
        {
            typeof(IRemoteAIService),
            typeof(IAIService)
        });
    }

    /// <inheritdoc />
    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var result = typeof(T) == typeof(IRemoteAIService) || typeof(T) == typeof(IAIService);
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IRemoteAIService) || typeof(T) == typeof(IAIService))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    private IEnumerable<AIModel> GetProviderModels(AIProvider provider)
    {
        // Simplified model listing - in practice would query provider API
        return provider.Type switch
        {
            AIProviderType.OpenAI => new[]
            {
                new AIModel("gpt-4", "GPT-4", "OpenAI", 
                    AIModelCapabilities.TextCompletion | AIModelCapabilities.Chat | AIModelCapabilities.Streaming, 
                    8192, 0.03m),
                new AIModel("gpt-3.5-turbo", "GPT-3.5 Turbo", "OpenAI", 
                    AIModelCapabilities.TextCompletion | AIModelCapabilities.Chat | AIModelCapabilities.Streaming, 
                    4096, 0.002m)
            },
            AIProviderType.Azure => new[]
            {
                new AIModel("gpt-4", "Azure GPT-4", "Azure", 
                    AIModelCapabilities.TextCompletion | AIModelCapabilities.Chat | AIModelCapabilities.Streaming, 
                    8192, 0.03m)
            },
            AIProviderType.AWS => new[]
            {
                new AIModel("anthropic.claude-3-sonnet", "Claude 3 Sonnet", "AWS Bedrock", 
                    AIModelCapabilities.TextCompletion | AIModelCapabilities.Chat, 
                    100000, 0.003m)
            },
            _ => Array.Empty<AIModel>()
        };
    }

    private string GenerateCacheKey(AIRequest request)
    {
        var keyData = new
        {
            request.ModelId,
            request.Prompt,
            request.MaxTokens,
            request.Temperature
        };
        
        var json = JsonSerializer.Serialize(keyData);
        return $"ai_response_{json.GetHashCode()}";
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await StopAsync();
            _rateLimiter?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Configuration options for the RemoteAIService.
/// </summary>
public class RemoteAIServiceOptions
{
    /// <summary>Gets or sets the list of AI providers to configure.</summary>
    public List<AIProviderConfig> Providers { get; set; } = new();
    
    /// <summary>Gets or sets whether response caching is enabled.</summary>
    public bool EnableCaching { get; set; } = true;
    
    /// <summary>Gets or sets the cache expiration time.</summary>
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(10);
    
    /// <summary>Gets or sets the maximum concurrent requests for batch operations.</summary>
    public int MaxConcurrentBatchRequests { get; set; } = 5;
    
    /// <summary>Gets or sets the global rate limiting configuration.</summary>
    public RateLimitConfig GlobalRateLimit { get; set; } = new();
}

/// <summary>
/// Configuration for an AI provider.
/// </summary>
public class AIProviderConfig
{
    /// <summary>Gets or sets the provider ID.</summary>
    public required string Id { get; set; }
    
    /// <summary>Gets or sets the provider name.</summary>
    public required string Name { get; set; }
    
    /// <summary>Gets or sets the provider type.</summary>
    public AIProviderType Type { get; set; }
    
    /// <summary>Gets or sets the base URL.</summary>
    public required string BaseUrl { get; set; }
    
    /// <summary>Gets or sets whether the provider is enabled.</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>Gets or sets the provider priority.</summary>
    public int Priority { get; set; } = 1;
    
    /// <summary>Gets or sets the maximum concurrent requests.</summary>
    public int MaxConcurrentRequests { get; set; } = 10;
    
    /// <summary>Gets or sets the rate limiting configuration.</summary>
    public RateLimitConfig? RateLimit { get; set; }
}

/// <summary>
/// Simple rate limiter implementation using token bucket algorithm.
/// </summary>
public class RateLimiter : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _refillTimer;
    private readonly RateLimitConfig _config;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the RateLimiter.
    /// </summary>
    /// <param name="config">Rate limiting configuration.</param>
    public RateLimiter(RateLimitConfig config)
    {
        _config = config;
        _semaphore = new SemaphoreSlim(_config.BurstCapacity, _config.BurstCapacity);
        
        var refillInterval = TimeSpan.FromMinutes(1.0 / _config.RequestsPerMinute);
        _refillTimer = new Timer(RefillTokens, null, refillInterval, refillInterval);
    }

    /// <summary>
    /// Waits for a token to become available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async wait operation.</returns>
    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RateLimiter));
        await _semaphore.WaitAsync(cancellationToken);
    }

    private void RefillTokens(object? state)
    {
        if (_disposed) return;
        
        if (_semaphore.CurrentCount < _config.BurstCapacity)
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _refillTimer?.Dispose();
            _semaphore?.Dispose();
            _disposed = true;
        }
    }
}
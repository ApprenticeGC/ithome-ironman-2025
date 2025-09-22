using GameConsole.AI.Remote.Services;

namespace GameConsole.AI.Remote.Configuration;

/// <summary>
/// Configuration for remote AI services.
/// </summary>
public sealed class RemoteAIConfiguration
{
    /// <summary>
    /// Configuration for AI service endpoints.
    /// </summary>
    public required Dictionary<AIProvider, AIEndpointConfig> Endpoints { get; init; }

    /// <summary>
    /// Load balancer configuration.
    /// </summary>
    public LoadBalancerConfig LoadBalancer { get; init; } = new();

    /// <summary>
    /// Failover configuration.
    /// </summary>
    public FailoverConfig Failover { get; init; } = new();

    /// <summary>
    /// Caching configuration.
    /// </summary>
    public CachingConfig Caching { get; init; } = new();

    /// <summary>
    /// Global rate limiting configuration.
    /// </summary>
    public RateLimitConfig GlobalRateLimit { get; init; } = new();

    /// <summary>
    /// Request timeout configuration.
    /// </summary>
    public TimeSpan DefaultRequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether to enable cost monitoring and optimization.
    /// </summary>
    public bool EnableCostMonitoring { get; init; } = true;

    /// <summary>
    /// Whether to enable request/response compression.
    /// </summary>
    public bool EnableCompression { get; init; } = true;
}

/// <summary>
/// Configuration for a specific AI service endpoint.
/// </summary>
public sealed class AIEndpointConfig
{
    /// <summary>
    /// Base URL for the AI service endpoint.
    /// </summary>
    public required string BaseUrl { get; init; }

    /// <summary>
    /// API key for authentication.
    /// </summary>
    public required string ApiKey { get; init; }

    /// <summary>
    /// Optional secondary API keys for rotation.
    /// </summary>
    public List<string> SecondaryApiKeys { get; init; } = [];

    /// <summary>
    /// Default model to use for this endpoint.
    /// </summary>
    public required string DefaultModel { get; init; }

    /// <summary>
    /// Available models for this endpoint.
    /// </summary>
    public List<string> AvailableModels { get; init; } = [];

    /// <summary>
    /// Priority weight for load balancing (higher = more preferred).
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Maximum concurrent requests to this endpoint.
    /// </summary>
    public int MaxConcurrentRequests { get; init; } = 10;

    /// <summary>
    /// Request timeout for this specific endpoint.
    /// </summary>
    public TimeSpan? RequestTimeout { get; init; }

    /// <summary>
    /// Rate limiting configuration specific to this endpoint.
    /// </summary>
    public RateLimitConfig? RateLimit { get; init; }

    /// <summary>
    /// Cost per token for this endpoint (in USD).
    /// </summary>
    public decimal CostPerToken { get; init; } = 0.0001m;

    /// <summary>
    /// Whether this endpoint is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Additional headers to include in requests to this endpoint.
    /// </summary>
    public Dictionary<string, string> AdditionalHeaders { get; init; } = [];

    /// <summary>
    /// Custom configuration for this specific provider.
    /// </summary>
    public Dictionary<string, object> ProviderSpecificConfig { get; init; } = [];
}

/// <summary>
/// Configuration for load balancing across AI service endpoints.
/// </summary>
public sealed class LoadBalancerConfig
{
    /// <summary>
    /// Load balancing strategy to use.
    /// </summary>
    public LoadBalancingStrategy Strategy { get; init; } = LoadBalancingStrategy.WeightedRoundRobin;

    /// <summary>
    /// Health check interval for endpoints.
    /// </summary>
    public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Timeout for health check requests.
    /// </summary>
    public TimeSpan HealthCheckTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum number of consecutive health check failures before marking an endpoint as unhealthy.
    /// </summary>
    public int MaxConsecutiveFailures { get; init; } = 3;

    /// <summary>
    /// Time to wait before retrying a failed endpoint.
    /// </summary>
    public TimeSpan UnhealthyRetryInterval { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to automatically adjust weights based on performance.
    /// </summary>
    public bool EnableAdaptiveWeighting { get; init; } = true;
}

/// <summary>
/// Load balancing strategies.
/// </summary>
public enum LoadBalancingStrategy
{
    /// <summary>
    /// Round robin across all healthy endpoints.
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Weighted round robin based on endpoint priorities.
    /// </summary>
    WeightedRoundRobin,

    /// <summary>
    /// Route to the endpoint with the lowest current load.
    /// </summary>
    LeastConnections,

    /// <summary>
    /// Route to the endpoint with the best response time.
    /// </summary>
    FastestResponse,

    /// <summary>
    /// Route to the endpoint with the lowest cost.
    /// </summary>
    LowestCost
}

/// <summary>
/// Configuration for failover and fallback behavior.
/// </summary>
public sealed class FailoverConfig
{
    /// <summary>
    /// Whether failover is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Maximum number of retry attempts for failed requests.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>
    /// Base delay between retry attempts.
    /// </summary>
    public TimeSpan BaseRetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retry attempts.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Backoff multiplier for exponential backoff.
    /// </summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>
    /// Whether to enable fallback to local AI when all remote endpoints fail.
    /// </summary>
    public bool EnableLocalFallback { get; init; } = true;

    /// <summary>
    /// HTTP status codes that should trigger a failover.
    /// </summary>
    public List<int> FailoverHttpStatusCodes { get; init; } = [500, 502, 503, 504, 429];

    /// <summary>
    /// Exception types that should trigger a failover.
    /// </summary>
    public List<string> FailoverExceptionTypes { get; init; } = 
    [
        "System.Net.Http.HttpRequestException",
        "System.Threading.Tasks.TaskCanceledException",
        "System.Net.Sockets.SocketException"
    ];

    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public CircuitBreakerConfig CircuitBreaker { get; init; } = new();
}

/// <summary>
/// Circuit breaker configuration to prevent cascading failures.
/// </summary>
public sealed class CircuitBreakerConfig
{
    /// <summary>
    /// Whether the circuit breaker is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Number of consecutive failures before opening the circuit.
    /// </summary>
    public int FailureThreshold { get; init; } = 5;

    /// <summary>
    /// Time to wait before attempting to close an open circuit.
    /// </summary>
    public TimeSpan RecoveryTimeout { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Minimum number of requests in the half-open state before closing the circuit.
    /// </summary>
    public int MinimumThroughput { get; init; } = 3;
}

/// <summary>
/// Configuration for response caching.
/// </summary>
public sealed class CachingConfig
{
    /// <summary>
    /// Whether caching is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Default cache expiration time.
    /// </summary>
    public TimeSpan DefaultExpiration { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Maximum number of cached responses.
    /// </summary>
    public int MaxCacheSize { get; init; } = 1000;

    /// <summary>
    /// Cache key strategy to use.
    /// </summary>
    public CacheKeyStrategy KeyStrategy { get; init; } = CacheKeyStrategy.PromptAndModelHash;

    /// <summary>
    /// Whether to cache streaming responses.
    /// </summary>
    public bool CacheStreamingResponses { get; init; } = false;

    /// <summary>
    /// Cache expiration policy for different request types.
    /// </summary>
    public Dictionary<RequestPriority, TimeSpan> ExpirationByPriority { get; init; } = new()
    {
        { RequestPriority.Low, TimeSpan.FromHours(1) },
        { RequestPriority.Normal, TimeSpan.FromMinutes(10) },
        { RequestPriority.High, TimeSpan.FromMinutes(2) },
        { RequestPriority.Critical, TimeSpan.FromMinutes(1) }
    };
}

/// <summary>
/// Cache key generation strategies.
/// </summary>
public enum CacheKeyStrategy
{
    /// <summary>
    /// Use hash of prompt and model name.
    /// </summary>
    PromptAndModelHash,

    /// <summary>
    /// Use hash of full request including all parameters.
    /// </summary>
    FullRequestHash,

    /// <summary>
    /// Use custom key generation logic.
    /// </summary>
    Custom
}

/// <summary>
/// Configuration for API key rotation.
/// </summary>
public sealed class ApiKeyRotationConfig
{
    /// <summary>
    /// Whether API key rotation is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// How often to rotate API keys.
    /// </summary>
    public TimeSpan RotationInterval { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Whether to rotate keys on failures.
    /// </summary>
    public bool RotateOnFailure { get; init; } = true;

    /// <summary>
    /// Maximum number of failures before rotating keys.
    /// </summary>
    public int MaxFailuresBeforeRotation { get; init; } = 10;
}
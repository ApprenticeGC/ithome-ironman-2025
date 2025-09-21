namespace GameConsole.AI.Remote;

/// <summary>
/// Represents a remote AI provider (OpenAI, Azure, AWS, etc.).
/// </summary>
public record AIProvider
{
    /// <summary>Gets or sets the unique provider ID.</summary>
    public required string Id { get; init; }
    
    /// <summary>Gets or sets the provider name.</summary>
    public required string Name { get; init; }
    
    /// <summary>Gets or sets the provider type.</summary>
    public required AIProviderType Type { get; init; }
    
    /// <summary>Gets or sets the base URL for the provider API.</summary>
    public required string BaseUrl { get; init; }
    
    /// <summary>Gets or sets whether the provider is currently available.</summary>
    public bool IsAvailable { get; init; } = true;
    
    /// <summary>Gets or sets the provider priority for load balancing.</summary>
    public int Priority { get; init; } = 1;
    
    /// <summary>Gets or sets the maximum concurrent requests for this provider.</summary>
    public int MaxConcurrentRequests { get; init; } = 10;
    
    /// <summary>Gets or sets rate limiting configuration.</summary>
    public RateLimitConfig? RateLimit { get; init; }
}

/// <summary>
/// Enumeration of supported AI provider types.
/// </summary>
public enum AIProviderType
{
    /// <summary>OpenAI provider.</summary>
    OpenAI,
    
    /// <summary>Azure OpenAI provider.</summary>
    Azure,
    
    /// <summary>AWS Bedrock provider.</summary>
    AWS,
    
    /// <summary>Google AI provider.</summary>
    Google,
    
    /// <summary>Anthropic Claude provider.</summary>
    Anthropic,
    
    /// <summary>Custom/other provider.</summary>
    Custom
}

/// <summary>
/// Represents load balancing status across providers.
/// </summary>
public record LoadBalancingStatus
{
    /// <summary>Gets or sets per-provider status information.</summary>
    public required Dictionary<string, ProviderStatus> ProviderStatuses { get; init; }
    
    /// <summary>Gets or sets the current load balancing strategy.</summary>
    public required LoadBalancingStrategy Strategy { get; init; }
    
    /// <summary>Gets or sets the last update timestamp.</summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents status information for a specific provider.
/// </summary>
public record ProviderStatus
{
    /// <summary>Gets or sets the provider ID.</summary>
    public required string ProviderId { get; init; }
    
    /// <summary>Gets or sets whether the provider is healthy.</summary>
    public bool IsHealthy { get; init; }
    
    /// <summary>Gets or sets the current active requests count.</summary>
    public int ActiveRequests { get; init; }
    
    /// <summary>Gets or sets the average response time in milliseconds.</summary>
    public double AverageResponseTimeMs { get; init; }
    
    /// <summary>Gets or sets the current load percentage (0-100).</summary>
    public double LoadPercentage { get; init; }
    
    /// <summary>Gets or sets the last error message if unhealthy.</summary>
    public string? LastError { get; init; }
}

/// <summary>
/// Enumeration of load balancing strategies.
/// </summary>
public enum LoadBalancingStrategy
{
    /// <summary>Round-robin distribution.</summary>
    RoundRobin,
    
    /// <summary>Least connections distribution.</summary>
    LeastConnections,
    
    /// <summary>Weighted round-robin based on priority.</summary>
    WeightedRoundRobin,
    
    /// <summary>Response time-based distribution.</summary>
    FastestResponse,
    
    /// <summary>Cost-optimized distribution.</summary>
    CostOptimized
}

/// <summary>
/// Represents cost monitoring information.
/// </summary>
public record CostMonitoringInfo
{
    /// <summary>Gets or sets the total cost for the time range.</summary>
    public decimal TotalCost { get; init; }
    
    /// <summary>Gets or sets cost breakdown by provider.</summary>
    public required Dictionary<string, decimal> CostByProvider { get; init; }
    
    /// <summary>Gets or sets cost breakdown by model.</summary>
    public required Dictionary<string, decimal> CostByModel { get; init; }
    
    /// <summary>Gets or sets the total tokens consumed.</summary>
    public long TotalTokens { get; init; }
    
    /// <summary>Gets or sets the total number of requests.</summary>
    public long TotalRequests { get; init; }
    
    /// <summary>Gets or sets the time range for this cost information.</summary>
    public required TimeRange TimeRange { get; init; }
}

/// <summary>
/// Represents a time range for queries.
/// </summary>
public record TimeRange(
    DateTimeOffset Start,
    DateTimeOffset End);

/// <summary>
/// Configuration for failover behavior.
/// </summary>
public record FailoverConfiguration
{
    /// <summary>Gets or sets whether failover is enabled.</summary>
    public bool Enabled { get; init; } = true;
    
    /// <summary>Gets or sets the failover strategy.</summary>
    public FailoverStrategy Strategy { get; init; } = FailoverStrategy.FallbackToLocal;
    
    /// <summary>Gets or sets the maximum retry attempts before failover.</summary>
    public int MaxRetryAttempts { get; init; } = 3;
    
    /// <summary>Gets or sets the timeout before considering a provider failed.</summary>
    public TimeSpan HealthCheckTimeout { get; init; } = TimeSpan.FromSeconds(30);
    
    /// <summary>Gets or sets the interval between health checks.</summary>
    public TimeSpan HealthCheckInterval { get; init; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Enumeration of failover strategies.
/// </summary>
public enum FailoverStrategy
{
    /// <summary>Fail fast with no fallback.</summary>
    FailFast,
    
    /// <summary>Fallback to other remote providers.</summary>
    FallbackToRemote,
    
    /// <summary>Fallback to local AI service.</summary>
    FallbackToLocal,
    
    /// <summary>Queue requests until service recovers.</summary>
    QueueAndRetry
}

/// <summary>
/// Rate limiting configuration for a provider.
/// </summary>
public record RateLimitConfig
{
    /// <summary>Gets or sets the maximum requests per minute.</summary>
    public int RequestsPerMinute { get; init; } = 60;
    
    /// <summary>Gets or sets the maximum tokens per minute.</summary>
    public int TokensPerMinute { get; init; } = 100000;
    
    /// <summary>Gets or sets the burst capacity for requests.</summary>
    public int BurstCapacity { get; init; } = 10;
}
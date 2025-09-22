using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Remote.Services;

/// <summary>
/// Core remote AI service for cloud-based AI operations.
/// Supports integration with multiple AI providers (OpenAI, Azure, AWS) with load balancing and failover.
/// </summary>
public interface IRemoteAIService : IService
{
    /// <summary>
    /// Sends a completion request to the remote AI service.
    /// </summary>
    /// <param name="request">The AI completion request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The AI completion response.</returns>
    Task<AICompletionResponse> GetCompletionAsync(AICompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams a completion response from the remote AI service.
    /// </summary>
    /// <param name="request">The AI completion request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable of streaming response chunks.</returns>
    IAsyncEnumerable<AIStreamingChunk> GetStreamingCompletionAsync(AICompletionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the health status of all configured AI service endpoints.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Health status information for each endpoint.</returns>
    Task<ServiceHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current usage and cost metrics for the remote AI services.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Usage and cost metrics.</returns>
    Task<AIUsageMetrics> GetUsageMetricsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for batch processing AI requests.
/// </summary>
public interface IBatchProcessingCapability : ICapabilityProvider
{
    /// <summary>
    /// Submits multiple AI requests as a batch for processing.
    /// </summary>
    /// <param name="requests">Collection of AI requests to process in batch.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of completion responses corresponding to the input requests.</returns>
    Task<IEnumerable<AICompletionResponse>> ProcessBatchAsync(IEnumerable<AICompletionRequest> requests, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for request rate limiting and quota management.
/// </summary>
public interface IRateLimitingCapability : ICapabilityProvider
{
    /// <summary>
    /// Gets the current rate limit status for the AI service.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current rate limit information.</returns>
    Task<RateLimitStatus> GetRateLimitStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets custom rate limiting parameters for the AI service.
    /// </summary>
    /// <param name="config">Rate limiting configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async configuration operation.</returns>
    Task SetRateLimitConfigAsync(RateLimitConfig config, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an AI completion request.
/// </summary>
public sealed class AICompletionRequest
{
    /// <summary>
    /// The input prompt or messages for the AI model.
    /// </summary>
    public required string Prompt { get; init; }

    /// <summary>
    /// Maximum number of tokens to generate in the response.
    /// </summary>
    public int MaxTokens { get; init; } = 1000;

    /// <summary>
    /// Temperature for controlling randomness in the response (0.0 to 1.0).
    /// </summary>
    public float Temperature { get; init; } = 0.7f;

    /// <summary>
    /// The AI model to use for the request (e.g., "gpt-4", "claude-3-sonnet").
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Additional parameters specific to the AI provider.
    /// </summary>
    public Dictionary<string, object>? AdditionalParameters { get; init; }

    /// <summary>
    /// Priority level for the request (affects load balancing and processing order).
    /// </summary>
    public RequestPriority Priority { get; init; } = RequestPriority.Normal;

    /// <summary>
    /// Timeout for the request processing.
    /// </summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>
/// Represents an AI completion response.
/// </summary>
public sealed class AICompletionResponse
{
    /// <summary>
    /// The generated text response from the AI model.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The model that generated the response.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Token usage information for the request.
    /// </summary>
    public required TokenUsage Usage { get; init; }

    /// <summary>
    /// The AI provider that handled the request.
    /// </summary>
    public required AIProvider Provider { get; init; }

    /// <summary>
    /// Unique identifier for the request/response pair.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Timestamp when the response was generated.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Indicates if the response was served from cache.
    /// </summary>
    public bool FromCache { get; init; }
}

/// <summary>
/// Represents a chunk in a streaming AI response.
/// </summary>
public sealed class AIStreamingChunk
{
    /// <summary>
    /// The incremental content in this chunk.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Indicates if this is the final chunk in the stream.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// The AI provider handling the streaming request.
    /// </summary>
    public required AIProvider Provider { get; init; }

    /// <summary>
    /// Unique identifier for the streaming request.
    /// </summary>
    public required string RequestId { get; init; }
}

/// <summary>
/// Token usage information for AI requests.
/// </summary>
public sealed class TokenUsage
{
    /// <summary>
    /// Number of tokens in the input prompt.
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// Number of tokens in the generated response.
    /// </summary>
    public int CompletionTokens { get; init; }

    /// <summary>
    /// Total tokens used (prompt + completion).
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// Priority levels for AI requests.
/// </summary>
public enum RequestPriority
{
    /// <summary>
    /// Low priority request (background processing).
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority request (default).
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority request (user-facing operations).
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority request (urgent operations).
    /// </summary>
    Critical = 3
}

/// <summary>
/// Supported AI providers.
/// </summary>
public enum AIProvider
{
    /// <summary>
    /// OpenAI provider (GPT models).
    /// </summary>
    OpenAI,

    /// <summary>
    /// Azure OpenAI provider.
    /// </summary>
    Azure,

    /// <summary>
    /// AWS Bedrock provider.
    /// </summary>
    AWS,

    /// <summary>
    /// Anthropic Claude provider.
    /// </summary>
    Anthropic,

    /// <summary>
    /// Local AI provider (fallback).
    /// </summary>
    Local
}

/// <summary>
/// Health status for AI service endpoints.
/// </summary>
public sealed class ServiceHealthStatus
{
    /// <summary>
    /// Overall health status of the remote AI service.
    /// </summary>
    public HealthStatus OverallStatus { get; init; }

    /// <summary>
    /// Health status for each individual AI provider endpoint.
    /// </summary>
    public required Dictionary<AIProvider, EndpointHealth> EndpointStatuses { get; init; }

    /// <summary>
    /// Timestamp when the health check was performed.
    /// </summary>
    public DateTimeOffset LastChecked { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Health status values.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// Service is healthy and operational.
    /// </summary>
    Healthy,

    /// <summary>
    /// Service is degraded but functional.
    /// </summary>
    Degraded,

    /// <summary>
    /// Service is unhealthy and may not function properly.
    /// </summary>
    Unhealthy
}

/// <summary>
/// Health information for a specific endpoint.
/// </summary>
public sealed class EndpointHealth
{
    /// <summary>
    /// Current health status of the endpoint.
    /// </summary>
    public HealthStatus Status { get; init; }

    /// <summary>
    /// Response time in milliseconds for health check.
    /// </summary>
    public double ResponseTimeMs { get; init; }

    /// <summary>
    /// Error message if the endpoint is unhealthy.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Last successful request timestamp.
    /// </summary>
    public DateTimeOffset? LastSuccessfulRequest { get; init; }
}

/// <summary>
/// Usage and cost metrics for AI services.
/// </summary>
public sealed class AIUsageMetrics
{
    /// <summary>
    /// Total tokens consumed across all providers.
    /// </summary>
    public long TotalTokensUsed { get; init; }

    /// <summary>
    /// Total number of requests processed.
    /// </summary>
    public long TotalRequests { get; init; }

    /// <summary>
    /// Estimated total cost in USD.
    /// </summary>
    public decimal EstimatedCostUsd { get; init; }

    /// <summary>
    /// Usage breakdown by AI provider.
    /// </summary>
    public required Dictionary<AIProvider, ProviderUsage> ProviderBreakdown { get; init; }

    /// <summary>
    /// Time period these metrics cover.
    /// </summary>
    public TimeSpan Period { get; init; }
}

/// <summary>
/// Usage metrics for a specific AI provider.
/// </summary>
public sealed class ProviderUsage
{
    /// <summary>
    /// Number of tokens used with this provider.
    /// </summary>
    public long TokensUsed { get; init; }

    /// <summary>
    /// Number of requests sent to this provider.
    /// </summary>
    public long RequestCount { get; init; }

    /// <summary>
    /// Estimated cost for this provider in USD.
    /// </summary>
    public decimal EstimatedCostUsd { get; init; }

    /// <summary>
    /// Average response time for this provider.
    /// </summary>
    public double AverageResponseTimeMs { get; init; }

    /// <summary>
    /// Success rate for requests to this provider (0.0 to 1.0).
    /// </summary>
    public double SuccessRate { get; init; }
}

/// <summary>
/// Rate limiting status information.
/// </summary>
public sealed class RateLimitStatus
{
    /// <summary>
    /// Current number of requests in the rate limit window.
    /// </summary>
    public int CurrentRequests { get; init; }

    /// <summary>
    /// Maximum requests allowed in the rate limit window.
    /// </summary>
    public int MaxRequests { get; init; }

    /// <summary>
    /// Time until the rate limit window resets.
    /// </summary>
    public TimeSpan ResetTime { get; init; }

    /// <summary>
    /// Whether requests are currently being throttled.
    /// </summary>
    public bool IsThrottled { get; init; }
}

/// <summary>
/// Configuration for rate limiting.
/// </summary>
public sealed class RateLimitConfig
{
    /// <summary>
    /// Maximum requests per time window.
    /// </summary>
    public int MaxRequests { get; init; } = 1000;

    /// <summary>
    /// Time window for rate limiting.
    /// </summary>
    public TimeSpan TimeWindow { get; init; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum tokens per time window.
    /// </summary>
    public int MaxTokens { get; init; } = 150000;
}
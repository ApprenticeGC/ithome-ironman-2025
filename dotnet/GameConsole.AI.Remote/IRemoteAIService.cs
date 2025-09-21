using GameConsole.AI.Core;

namespace GameConsole.AI.Remote;

/// <summary>
/// Interface for remote AI service operations with cloud integration capabilities.
/// Extends the base AI service with remote-specific functionality.
/// </summary>
public interface IRemoteAIService : IAIService
{
    /// <summary>
    /// Gets available remote AI providers (OpenAI, Azure, AWS, etc.).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns available providers.</returns>
    Task<IEnumerable<AIProvider>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a completion using a specific remote provider.
    /// </summary>
    /// <param name="providerId">The ID of the provider to use.</param>
    /// <param name="request">The AI completion request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the AI response.</returns>
    Task<AIResponse> GenerateCompletionAsync(string providerId, AIRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current load balancing status across providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns load balancing status.</returns>
    Task<LoadBalancingStatus> GetLoadBalancingStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cost monitoring information for remote AI usage.
    /// </summary>
    /// <param name="timeRange">The time range for cost information.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns cost information.</returns>
    Task<CostMonitoringInfo> GetCostMonitoringAsync(TimeRange timeRange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures failover behavior when remote services are unavailable.
    /// </summary>
    /// <param name="failoverConfig">The failover configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration operation.</returns>
    Task ConfigureFailoverAsync(FailoverConfiguration failoverConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a batch of requests to remote AI services for efficient processing.
    /// </summary>
    /// <param name="requests">The batch of AI requests.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns batch responses.</returns>
    Task<IEnumerable<AIResponse>> GenerateBatchCompletionAsync(IEnumerable<AIRequest> requests, CancellationToken cancellationToken = default);
}
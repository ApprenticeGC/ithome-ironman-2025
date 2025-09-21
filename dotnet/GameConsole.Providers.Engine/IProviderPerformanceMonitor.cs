namespace GameConsole.Providers.Engine;

/// <summary>
/// Interface for monitoring provider performance metrics.
/// </summary>
public interface IProviderPerformanceMonitor
{
    /// <summary>
    /// Records a successful request for the specified provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="responseTime">The response time in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task RecordSuccessAsync(string providerId, TimeSpan responseTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a failed request for the specified provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="error">The exception that caused the failure.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task RecordFailureAsync(string providerId, Exception error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current metrics for the specified provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The provider metrics.</returns>
    Task<ProviderMetrics> GetMetricsAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metrics for all monitored providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Dictionary of provider metrics keyed by provider ID.</returns>
    Task<IReadOnlyDictionary<string, ProviderMetrics>> GetAllMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the specified provider is healthy based on configured thresholds.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the provider is healthy, false otherwise.</returns>
    Task<bool> IsHealthyAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets metrics for the specified provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task ResetMetricsAsync(string providerId, CancellationToken cancellationToken = default);
}
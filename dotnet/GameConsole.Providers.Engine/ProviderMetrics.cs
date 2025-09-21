namespace GameConsole.Providers.Engine;

/// <summary>
/// Represents provider performance metrics for monitoring and selection decisions.
/// </summary>
public class ProviderMetrics
{
    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    public double AverageResponseTime { get; init; }

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate { get; init; }

    /// <summary>
    /// Number of requests processed.
    /// </summary>
    public long RequestCount { get; init; }

    /// <summary>
    /// Number of failed requests.
    /// </summary>
    public long FailureCount { get; init; }

    /// <summary>
    /// Last recorded error (if any).
    /// </summary>
    public Exception? LastError { get; init; }

    /// <summary>
    /// Timestamp of the last update.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Health score calculated from metrics (0-100, higher is better).
    /// </summary>
    public double HealthScore { get; init; }
}
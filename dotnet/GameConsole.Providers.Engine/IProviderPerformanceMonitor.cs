using System.Diagnostics;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Interface for monitoring provider performance metrics and health status.
/// </summary>
public interface IProviderPerformanceMonitor
{
    /// <summary>
    /// Records a successful provider operation with its response time.
    /// </summary>
    /// <param name="providerId">Unique identifier for the provider.</param>
    /// <param name="responseTime">Time taken for the operation to complete.</param>
    void RecordSuccess(string providerId, TimeSpan responseTime);

    /// <summary>
    /// Records a failed provider operation with the associated exception.
    /// </summary>
    /// <param name="providerId">Unique identifier for the provider.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    void RecordFailure(string providerId, Exception exception);

    /// <summary>
    /// Records the start of a provider operation and returns a disposable tracker.
    /// </summary>
    /// <param name="providerId">Unique identifier for the provider.</param>
    /// <returns>A disposable operation tracker that records the result when disposed.</returns>
    IOperationTracker StartOperation(string providerId);

    /// <summary>
    /// Gets performance metrics for a specific provider.
    /// </summary>
    /// <param name="providerId">Unique identifier for the provider.</param>
    /// <returns>The performance metrics for the provider.</returns>
    ProviderMetrics GetMetrics(string providerId);

    /// <summary>
    /// Gets performance metrics for all monitored providers.
    /// </summary>
    /// <returns>Performance metrics for all providers.</returns>
    IEnumerable<ProviderMetrics> GetAllMetrics();

    /// <summary>
    /// Checks if a provider is currently healthy based on circuit breaker status.
    /// </summary>
    /// <param name="providerId">Unique identifier for the provider.</param>
    /// <returns>True if the provider is healthy and available.</returns>
    bool IsProviderHealthy(string providerId);

    /// <summary>
    /// Resets the performance metrics for a specific provider.
    /// </summary>
    /// <param name="providerId">Unique identifier for the provider.</param>
    void ResetMetrics(string providerId);

    /// <summary>
    /// Event raised when a provider's health status changes.
    /// </summary>
    event EventHandler<ProviderHealthChangedEventArgs>? ProviderHealthChanged;
}

/// <summary>
/// Tracks a provider operation and records its result when disposed.
/// </summary>
public interface IOperationTracker : IDisposable
{
    /// <summary>
    /// Marks the operation as failed with the specified exception.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    void RecordFailure(Exception exception);

    /// <summary>
    /// Marks the operation as successful.
    /// </summary>
    void RecordSuccess();
}

/// <summary>
/// Performance metrics for a provider over time.
/// </summary>
public sealed class ProviderMetrics
{
    /// <summary>
    /// Gets the unique identifier for the provider.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the total number of successful operations.
    /// </summary>
    public long SuccessCount { get; init; }

    /// <summary>
    /// Gets the total number of failed operations.
    /// </summary>
    public long FailureCount { get; init; }

    /// <summary>
    /// Gets the total number of operations (success + failure).
    /// </summary>
    public long TotalOperations => SuccessCount + FailureCount;

    /// <summary>
    /// Gets the success rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => TotalOperations == 0 ? 0.0 : (double)SuccessCount / TotalOperations;

    /// <summary>
    /// Gets the failure rate as a percentage (0.0 to 1.0).
    /// </summary>
    public double FailureRate => 1.0 - SuccessRate;

    /// <summary>
    /// Gets the average response time for successful operations.
    /// </summary>
    public TimeSpan AverageResponseTime { get; init; }

    /// <summary>
    /// Gets the median response time for successful operations.
    /// </summary>
    public TimeSpan MedianResponseTime { get; init; }

    /// <summary>
    /// Gets the 95th percentile response time.
    /// </summary>
    public TimeSpan P95ResponseTime { get; init; }

    /// <summary>
    /// Gets the current circuit breaker state.
    /// </summary>
    public CircuitBreakerState CircuitBreakerState { get; init; }

    /// <summary>
    /// Gets the timestamp of the last operation.
    /// </summary>
    public DateTimeOffset LastOperationTime { get; init; }

    /// <summary>
    /// Gets the timestamp when metrics collection started.
    /// </summary>
    public DateTimeOffset MetricsStartTime { get; init; }

    /// <summary>
    /// Gets the most recent exception that caused a failure, if any.
    /// </summary>
    public Exception? LastException { get; init; }
}

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Circuit is closed, allowing operations to proceed normally.
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open, rejecting operations due to high failure rate.
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open, allowing limited operations to test recovery.
    /// </summary>
    HalfOpen
}

/// <summary>
/// Event arguments for provider health status changes.
/// </summary>
public sealed class ProviderHealthChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the unique identifier for the provider.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the previous health status.
    /// </summary>
    public bool WasHealthy { get; init; }

    /// <summary>
    /// Gets the current health status.
    /// </summary>
    public bool IsHealthy { get; init; }

    /// <summary>
    /// Gets the current circuit breaker state.
    /// </summary>
    public CircuitBreakerState CircuitBreakerState { get; init; }

    /// <summary>
    /// Gets the timestamp when the health status changed.
    /// </summary>
    public DateTimeOffset ChangeTime { get; init; }
}
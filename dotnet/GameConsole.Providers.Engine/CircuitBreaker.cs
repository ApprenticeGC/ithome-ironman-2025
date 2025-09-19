using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Implementation of the circuit breaker pattern for provider fault tolerance.
/// Automatically opens when failure threshold is reached and attempts recovery after timeout.
/// </summary>
internal sealed class CircuitBreaker : IDisposable
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger? _logger;
    private readonly object _lock = new object();
    private readonly ConcurrentQueue<FailureRecord> _failures = new();
    
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private DateTimeOffset _lastOpenTime;
    private int _halfOpenAttempts;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreaker"/> class.
    /// </summary>
    /// <param name="options">Configuration options for the circuit breaker.</param>
    /// <param name="logger">Optional logger for circuit breaker operations.</param>
    public CircuitBreaker(CircuitBreakerOptions options, ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
    }

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    public CircuitBreakerState State
    {
        get
        {
            lock (_lock)
            {
                UpdateState();
                return _state;
            }
        }
    }

    /// <summary>
    /// Records a successful operation, potentially closing an open circuit.
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            if (_state == CircuitBreakerState.HalfOpen)
            {
                _halfOpenAttempts++;
                if (_halfOpenAttempts >= _options.HalfOpenMaxAttempts)
                {
                    // Enough successful attempts in half-open state, close the circuit
                    TransitionTo(CircuitBreakerState.Closed);
                    _failures.Clear();
                    _logger?.LogInformation("Circuit breaker closed after successful recovery attempts");
                }
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                // Remove old failures outside the sliding window
                CleanupOldFailures();
            }
        }
    }

    /// <summary>
    /// Records a failed operation, potentially opening the circuit.
    /// </summary>
    /// <param name="exception">The exception that caused the failure.</param>
    public void RecordFailure(Exception exception)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            _failures.Enqueue(new FailureRecord(now, exception));

            if (_state == CircuitBreakerState.HalfOpen)
            {
                // Failure in half-open state immediately opens the circuit
                TransitionTo(CircuitBreakerState.Open);
                _lastOpenTime = now;
                _logger?.LogWarning("Circuit breaker opened due to failure in half-open state: {Exception}", 
                    exception.Message);
            }
            else if (_state == CircuitBreakerState.Closed)
            {
                CleanupOldFailures();
                
                // Check if we've exceeded the failure threshold
                if (GetRecentFailureCount() >= _options.FailureThreshold)
                {
                    TransitionTo(CircuitBreakerState.Open);
                    _lastOpenTime = now;
                    _logger?.LogWarning("Circuit breaker opened due to {FailureCount} failures in {WindowSize}: {Exception}",
                        GetRecentFailureCount(), _options.SlidingWindowSize, exception.Message);
                }
            }
        }
    }

    /// <summary>
    /// Checks if the circuit breaker allows the operation to proceed.
    /// </summary>
    /// <returns>True if the operation should be allowed, false if it should be blocked.</returns>
    public bool CanExecute()
    {
        lock (_lock)
        {
            UpdateState();
            return _state == CircuitBreakerState.Closed || _state == CircuitBreakerState.HalfOpen;
        }
    }

    /// <summary>
    /// Manually resets the circuit breaker to the closed state.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            TransitionTo(CircuitBreakerState.Closed);
            _failures.Clear();
            _halfOpenAttempts = 0;
            _logger?.LogInformation("Circuit breaker manually reset to closed state");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            lock (_lock)
            {
                while (_failures.TryDequeue(out _))
                {
                    // Clear the queue
                }
                _disposed = true;
            }
        }
    }

    private void UpdateState()
    {
        if (_state == CircuitBreakerState.Open)
        {
            var timeSinceOpen = DateTimeOffset.UtcNow - _lastOpenTime;
            if (timeSinceOpen >= _options.OpenCircuitTimeout)
            {
                TransitionTo(CircuitBreakerState.HalfOpen);
                _halfOpenAttempts = 0;
                _logger?.LogInformation("Circuit breaker transitioned to half-open state after {Timeout}",
                    _options.OpenCircuitTimeout);
            }
        }
    }

    private void TransitionTo(CircuitBreakerState newState)
    {
        if (_state != newState)
        {
            var oldState = _state;
            _state = newState;
            _logger?.LogDebug("Circuit breaker state changed from {OldState} to {NewState}", 
                oldState, newState);
        }
    }

    private void CleanupOldFailures()
    {
        var cutoffTime = DateTimeOffset.UtcNow - _options.SlidingWindowSize;
        
        while (_failures.TryPeek(out var failure) && failure.Timestamp < cutoffTime)
        {
            _failures.TryDequeue(out _);
        }
    }

    private int GetRecentFailureCount()
    {
        var cutoffTime = DateTimeOffset.UtcNow - _options.SlidingWindowSize;
        return _failures.Count(f => f.Timestamp >= cutoffTime);
    }

    private readonly record struct FailureRecord(DateTimeOffset Timestamp, Exception Exception);
}
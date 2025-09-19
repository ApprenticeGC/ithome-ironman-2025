using GameConsole.Providers.Engine;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Providers.Engine.Tests;

/// <summary>
/// Tests for the ProviderPerformanceMonitor implementation.
/// </summary>
public class ProviderPerformanceMonitorTests : IDisposable
{
    private readonly ProviderPerformanceMonitor _monitor;
    private readonly string _providerId = "test-provider-1";

    public ProviderPerformanceMonitorTests()
    {
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 3,
            OpenCircuitTimeout = TimeSpan.FromSeconds(1),
            HalfOpenMaxAttempts = 2
        };
        _monitor = new ProviderPerformanceMonitor(options);
    }

    [Fact]
    public void RecordSuccess_ShouldTrackSuccessMetrics()
    {
        // Arrange
        var responseTime = TimeSpan.FromMilliseconds(150);

        // Act
        _monitor.RecordSuccess(_providerId, responseTime);

        // Assert
        var metrics = _monitor.GetMetrics(_providerId);
        Assert.Equal(1, metrics.SuccessCount);
        Assert.Equal(0, metrics.FailureCount);
        Assert.Equal(1.0, metrics.SuccessRate);
        Assert.Equal(responseTime, metrics.AverageResponseTime);
        Assert.True(_monitor.IsProviderHealthy(_providerId));
    }

    [Fact]
    public void RecordFailure_ShouldTrackFailureMetrics()
    {
        // Arrange
        var exception = new InvalidOperationException("Test failure");

        // Act
        _monitor.RecordFailure(_providerId, exception);

        // Assert
        var metrics = _monitor.GetMetrics(_providerId);
        Assert.Equal(0, metrics.SuccessCount);
        Assert.Equal(1, metrics.FailureCount);
        Assert.Equal(0.0, metrics.SuccessRate);
        Assert.Equal(exception, metrics.LastException);
    }

    [Fact]
    public void RecordFailure_ExceedingThreshold_ShouldTriggerCircuitBreaker()
    {
        // Arrange
        var exception = new InvalidOperationException("Test failure");

        // Act - Record failures beyond threshold
        for (int i = 0; i < 3; i++)
        {
            _monitor.RecordFailure(_providerId, exception);
        }

        // Assert
        var metrics = _monitor.GetMetrics(_providerId);
        Assert.Equal(3, metrics.FailureCount);
        Assert.Equal(CircuitBreakerState.Open, metrics.CircuitBreakerState);
        Assert.False(_monitor.IsProviderHealthy(_providerId));
    }

    [Fact]
    public void StartOperation_ShouldReturnOperationTracker()
    {
        // Act
        using var tracker = _monitor.StartOperation(_providerId);

        // Assert
        Assert.NotNull(tracker);
    }

    [Fact]
    public void OperationTracker_Success_ShouldRecordSuccessMetrics()
    {
        // Act
        using (var tracker = _monitor.StartOperation(_providerId))
        {
            tracker.RecordSuccess();
        }

        // Assert
        var metrics = _monitor.GetMetrics(_providerId);
        Assert.Equal(1, metrics.SuccessCount);
        Assert.Equal(0, metrics.FailureCount);
    }

    [Fact]
    public void OperationTracker_Failure_ShouldRecordFailureMetrics()
    {
        // Arrange
        var exception = new TimeoutException("Operation timeout");

        // Act
        using (var tracker = _monitor.StartOperation(_providerId))
        {
            tracker.RecordFailure(exception);
        }

        // Assert
        var metrics = _monitor.GetMetrics(_providerId);
        Assert.Equal(0, metrics.SuccessCount);
        Assert.Equal(1, metrics.FailureCount);
        Assert.Equal(exception, metrics.LastException);
    }

    [Fact]
    public void OperationTracker_AutoSuccess_ShouldDefaultToSuccess()
    {
        // Act - Complete operation without explicitly recording result
        using (var tracker = _monitor.StartOperation(_providerId))
        {
            // Tracker will default to success when disposed
        }

        // Assert
        var metrics = _monitor.GetMetrics(_providerId);
        Assert.Equal(1, metrics.SuccessCount);
        Assert.Equal(0, metrics.FailureCount);
    }

    [Fact]
    public void GetAllMetrics_ShouldReturnMetricsForAllProviders()
    {
        // Arrange
        var provider1 = "provider-1";
        var provider2 = "provider-2";

        // Act
        _monitor.RecordSuccess(provider1, TimeSpan.FromMilliseconds(100));
        _monitor.RecordSuccess(provider2, TimeSpan.FromMilliseconds(200));

        // Assert
        var allMetrics = _monitor.GetAllMetrics().ToList();
        Assert.Contains(allMetrics, m => m.ProviderId == provider1);
        Assert.Contains(allMetrics, m => m.ProviderId == provider2);
    }

    [Fact]
    public void ResetMetrics_ShouldClearProviderMetrics()
    {
        // Arrange
        _monitor.RecordSuccess(_providerId, TimeSpan.FromMilliseconds(100));
        _monitor.RecordFailure(_providerId, new Exception("Test"));

        // Act
        _monitor.ResetMetrics(_providerId);

        // Assert
        var metrics = _monitor.GetMetrics(_providerId);
        Assert.Equal(0, metrics.SuccessCount);
        Assert.Equal(0, metrics.FailureCount);
        Assert.True(_monitor.IsProviderHealthy(_providerId));
    }

    [Fact]
    public void ProviderHealthChanged_ShouldFireEventOnStateChange()
    {
        // Arrange
        var eventFired = false;
        ProviderHealthChangedEventArgs? eventArgs = null;

        _monitor.ProviderHealthChanged += (sender, args) =>
        {
            eventFired = true;
            eventArgs = args;
        };

        // Act - Cause circuit breaker to open
        for (int i = 0; i < 3; i++)
        {
            _monitor.RecordFailure(_providerId, new Exception("Test failure"));
        }

        // Assert
        Assert.True(eventFired);
        Assert.NotNull(eventArgs);
        Assert.Equal(_providerId, eventArgs.ProviderId);
        Assert.True(eventArgs.WasHealthy);
        Assert.False(eventArgs.IsHealthy);
        Assert.Equal(CircuitBreakerState.Open, eventArgs.CircuitBreakerState);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RecordSuccess_InvalidProviderId_ShouldThrowException(string? providerId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _monitor.RecordSuccess(providerId!, TimeSpan.FromMilliseconds(100)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RecordFailure_InvalidProviderId_ShouldThrowException(string? providerId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _monitor.RecordFailure(providerId!, new Exception()));
    }

    [Fact]
    public void RecordFailure_NullException_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _monitor.RecordFailure(_providerId, null!));
    }

    [Fact]
    public void CalculatePercentiles_ShouldComputeCorrectValues()
    {
        // Arrange - Record response times: 100, 200, 300, 400, 500 ms
        var responseTimes = new[] { 100, 200, 300, 400, 500 };
        foreach (var time in responseTimes)
        {
            _monitor.RecordSuccess(_providerId, TimeSpan.FromMilliseconds(time));
        }

        // Act
        var metrics = _monitor.GetMetrics(_providerId);

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(300), metrics.AverageResponseTime);
        Assert.Equal(TimeSpan.FromMilliseconds(300), metrics.MedianResponseTime);
        // 95th percentile of [100,200,300,400,500] should be around 500ms
        Assert.True(metrics.P95ResponseTime.TotalMilliseconds >= 400);
    }

    [Fact]
    public void CircuitBreaker_Recovery_ShouldTransitionThroughStates()
    {
        // Arrange - Force circuit breaker to open
        for (int i = 0; i < 3; i++)
        {
            _monitor.RecordFailure(_providerId, new Exception("Test failure"));
        }
        
        var metrics = _monitor.GetMetrics(_providerId);
        Assert.Equal(CircuitBreakerState.Open, metrics.CircuitBreakerState);

        // Act - Wait for circuit breaker timeout (1 second in our configuration)
        Thread.Sleep(1100);

        // Verify it's half-open now
        Assert.True(_monitor.IsProviderHealthy(_providerId)); // Half-open allows requests
        
        // Record successful operations to close circuit
        _monitor.RecordSuccess(_providerId, TimeSpan.FromMilliseconds(100));
        _monitor.RecordSuccess(_providerId, TimeSpan.FromMilliseconds(100));

        // Assert - Should be closed now
        metrics = _monitor.GetMetrics(_providerId);
        Assert.Equal(CircuitBreakerState.Closed, metrics.CircuitBreakerState);
        Assert.True(_monitor.IsProviderHealthy(_providerId));
    }

    public void Dispose()
    {
        _monitor?.Dispose();
    }
}

/// <summary>
/// Integration tests for multiple components working together.
/// </summary>
public class ProviderSelectionIntegrationTests : IDisposable
{
    private readonly ProviderPerformanceMonitor _monitor;
    private readonly List<ITestProvider> _providers;

    public ProviderSelectionIntegrationTests()
    {
        _monitor = new ProviderPerformanceMonitor();
        _providers = new List<ITestProvider>
        {
            new TestProvider("provider-1"),
            new TestProvider("provider-2"),
            new TestProvider("provider-3")
        };
    }

    [Fact]
    public async Task PerformanceBasedSelector_ShouldSelectBestPerformingProvider()
    {
        // Arrange
        var selector = new PerformanceBasedProviderSelector<ITestProvider>(_monitor);

        // Record different performance metrics
        _monitor.RecordSuccess("provider-1", TimeSpan.FromMilliseconds(50));  // Fast
        _monitor.RecordSuccess("provider-2", TimeSpan.FromMilliseconds(200)); // Slow
        _monitor.RecordSuccess("provider-3", TimeSpan.FromMilliseconds(100)); // Medium

        // Act
        var selectedProvider = await selector.SelectProviderAsync(_providers, context: null);

        // Assert
        Assert.NotNull(selectedProvider);
        Assert.Equal("provider-1", selectedProvider!.Id); // Should select fastest provider
    }

    [Fact]
    public async Task RoundRobinSelector_ShouldCycleThroughProviders()
    {
        // Arrange
        var selector = new RoundRobinProviderSelector<ITestProvider>(_monitor);
        var selections = new List<ITestProvider?>();

        // Act
        for (int i = 0; i < 6; i++) // More than the number of providers
        {
            var selected = await selector.SelectProviderAsync(_providers, context: null);
            selections.Add(selected);
        }

        // Assert
        Assert.All(selections, p => Assert.NotNull(p));
        
        // Should cycle through providers: 1, 2, 3, 1, 2, 3
        Assert.Equal("provider-1", selections[0]!.Id);
        Assert.Equal("provider-2", selections[1]!.Id);
        Assert.Equal("provider-3", selections[2]!.Id);
        Assert.Equal("provider-1", selections[3]!.Id);
        Assert.Equal("provider-2", selections[4]!.Id);
        Assert.Equal("provider-3", selections[5]!.Id);
    }

    [Fact]
    public async Task DefaultFallbackStrategy_ShouldReturnHealthyProviders()
    {
        // Arrange
        var fallbackStrategy = new DefaultFallbackStrategy<ITestProvider>(
            performanceMonitor: _monitor);
        
        // Make provider-2 unhealthy
        for (int i = 0; i < 5; i++)
        {
            _monitor.RecordFailure("provider-2", new Exception("Failure"));
        }

        // Act
        var fallbackProviders = await fallbackStrategy.GetFallbackProvidersAsync(
            _providers[1], // failed provider (provider-2)
            _providers);

        // Assert
        var fallbackList = fallbackProviders.ToList();
        Assert.Contains(fallbackList, p => p.Id == "provider-1");
        Assert.Contains(fallbackList, p => p.Id == "provider-3");
        Assert.DoesNotContain(fallbackList, p => p.Id == "provider-2"); // Should exclude failed and unhealthy
    }

    public void Dispose()
    {
        _monitor?.Dispose();
    }

    private interface ITestProvider
    {
        string Id { get; }
    }

    private sealed class TestProvider : ITestProvider
    {
        public TestProvider(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public override int GetHashCode() => Id.GetHashCode();
    }
}
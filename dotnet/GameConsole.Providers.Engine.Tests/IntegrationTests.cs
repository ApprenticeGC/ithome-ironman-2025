using GameConsole.Providers.Engine;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine.Tests;

/// <summary>
/// Simplified integration tests demonstrating the core provider selection engine functionality.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public async Task Provider_Selection_Engine_End_To_End_Test()
    {
        // Arrange - Create the core components
        var performanceLogger = new FakeLogger<ProviderPerformanceMonitor>();
        var fallbackLogger = new FakeLogger<ExponentialBackoffFallbackStrategy>();

        var performanceMonitor = new ProviderPerformanceMonitor(performanceLogger);
        
        // Create a simple mock selector that returns providers in order
        var mockSelector = new MockProviderSelector();
        
        var fallbackStrategy = new ExponentialBackoffFallbackStrategy(
            mockSelector,
            performanceMonitor,
            fallbackLogger);

        // Act - Test the complete workflow
        var result = await fallbackStrategy.ExecuteWithFallbackAsync<ITestService, string>(
            async (provider, ct) => await provider.DoWorkAsync("test-data", ct));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("MockProvider processed: test-data", result.Result);
        Assert.Equal(1, result.AttemptCount);
        Assert.NotNull(result.SuccessfulProviderId);

        // Check that performance was recorded
        var metrics = await performanceMonitor.GetMetricsAsync(result.SuccessfulProviderId!);
        Assert.Equal(1, metrics.RequestCount);
        Assert.Equal(0, metrics.FailureCount);
        Assert.Equal(100.0, metrics.SuccessRate);
        Assert.True(metrics.HealthScore > 0);

        // Check logging occurred
        Assert.Contains(performanceLogger.LogEntries, entry => 
            entry.Message.Contains("Recorded success for provider"));
        Assert.Contains(fallbackLogger.LogEntries, entry => 
            entry.Message.Contains("Fallback succeeded"));
    }

    [Fact]
    public async Task Performance_Monitor_Health_Score_Calculation_Test()
    {
        // Arrange
        var logger = new FakeLogger<ProviderPerformanceMonitor>();
        var monitor = new ProviderPerformanceMonitor(logger);

        // Act & Assert - Test different performance scenarios
        
        // Scenario 1: Fast, reliable provider
        const string fastProviderId = "fast-provider";
        for (int i = 0; i < 10; i++)
        {
            await monitor.RecordSuccessAsync(fastProviderId, TimeSpan.FromMilliseconds(50));
        }
        var fastMetrics = await monitor.GetMetricsAsync(fastProviderId);
        
        // Scenario 2: Slow but reliable provider
        const string slowProviderId = "slow-provider";
        for (int i = 0; i < 10; i++)
        {
            await monitor.RecordSuccessAsync(slowProviderId, TimeSpan.FromMilliseconds(2000));
        }
        var slowMetrics = await monitor.GetMetricsAsync(slowProviderId);

        // Scenario 3: Unreliable provider
        const string unreliableProviderId = "unreliable-provider";
        for (int i = 0; i < 5; i++)
        {
            await monitor.RecordSuccessAsync(unreliableProviderId, TimeSpan.FromMilliseconds(100));
            await monitor.RecordFailureAsync(unreliableProviderId, new Exception("Test failure"));
        }
        var unreliableMetrics = await monitor.GetMetricsAsync(unreliableProviderId);

        // Assert health scores reflect performance characteristics
        Assert.True(fastMetrics.HealthScore > slowMetrics.HealthScore,
            $"Fast provider ({fastMetrics.HealthScore}) should have higher score than slow ({slowMetrics.HealthScore})");
        
        Assert.True(slowMetrics.HealthScore > unreliableMetrics.HealthScore,
            $"Slow provider ({slowMetrics.HealthScore}) should have higher score than unreliable ({unreliableMetrics.HealthScore})");

        // Check specific metrics
        Assert.Equal(100.0, fastMetrics.SuccessRate);
        Assert.Equal(100.0, slowMetrics.SuccessRate);
        Assert.Equal(50.0, unreliableMetrics.SuccessRate);

        Assert.True(fastMetrics.HealthScore > 90, "Fast provider should have excellent health score");
        Assert.True(slowMetrics.HealthScore > 70 && slowMetrics.HealthScore < 95, "Slow provider should have good health score");
        Assert.True(unreliableMetrics.HealthScore < 70, "Unreliable provider should have lower health score due to failures");
    }

    [Fact]
    public async Task Exponential_Backoff_Strategy_Test()
    {
        // Arrange
        var logger = new FakeLogger<ExponentialBackoffFallbackStrategy>();
        var performanceMonitor = new ProviderPerformanceMonitor(new FakeLogger<ProviderPerformanceMonitor>());
        
        var failingSelector = new FailingMockProviderSelector();
        var strategy = new ExponentialBackoffFallbackStrategy(failingSelector, performanceMonitor, logger);

        // Act
        var result = await strategy.ExecuteWithFallbackAsync<ITestService, string>(
            async (provider, ct) => await provider.DoWorkAsync("test", ct));

        // Assert - Should eventually succeed after failures
        Assert.True(result.IsSuccess);
        Assert.True(result.AttemptCount > 1, "Should have attempted multiple providers");
        Assert.True(result.Errors.Count > 0, "Should have recorded some failures");
        
        // Check that retry delays were applied (indicated by debug logs)
        Assert.Contains(logger.LogEntries, entry => 
            entry.Message.Contains("Waiting") && entry.Message.Contains("before trying next provider"));
    }

    // Test helper interfaces and classes
    public interface ITestService
    {
        Task<string> DoWorkAsync(string input, CancellationToken cancellationToken = default);
    }

    public class MockTestService : ITestService
    {
        public Task<string> DoWorkAsync(string input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"MockProvider processed: {input}");
        }
    }

    public class FailingTestService : ITestService
    {
        private int _callCount = 0;

        public Task<string> DoWorkAsync(string input, CancellationToken cancellationToken = default)
        {
            _callCount++;
            if (_callCount <= 2) // Fail first 2 attempts
            {
                throw new HttpRequestException($"Simulated failure #{_callCount}");
            }
            return Task.FromResult($"Eventually succeeded: {input}");
        }
    }

    public class MockProviderSelector : IProviderSelector
    {
        public Task<object?> SelectProviderAsync(Type serviceType, ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<object?>(new MockTestService());
        }

        public Task<TService?> SelectProviderAsync<TService>(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default) where TService : class
        {
            return Task.FromResult(new MockTestService() as TService);
        }

        public Task<IReadOnlyList<object>> GetAvailableProvidersAsync(Type serviceType, ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<object>>(new object[] { new MockTestService() });
        }

        public Task<IReadOnlyList<TService>> GetAvailableProvidersAsync<TService>(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default) where TService : class
        {
            return Task.FromResult<IReadOnlyList<TService>>(new TService[] { (new MockTestService() as TService)! });
        }
    }

    public class FailingMockProviderSelector : IProviderSelector
    {
        public Task<object?> SelectProviderAsync(Type serviceType, ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
        {
            // Not used in this test
            return Task.FromResult<object?>(null);
        }

        public Task<TService?> SelectProviderAsync<TService>(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default) where TService : class
        {
            // Not used in this test
            return Task.FromResult<TService?>(null);
        }

        public Task<IReadOnlyList<object>> GetAvailableProvidersAsync(Type serviceType, ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
        {
            // Return multiple providers where some fail
            return Task.FromResult<IReadOnlyList<object>>(new object[]
            {
                new FailingTestService(),
                new FailingTestService(),
                new MockTestService() // This one will succeed
            });
        }

        public Task<IReadOnlyList<TService>> GetAvailableProvidersAsync<TService>(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default) where TService : class
        {
            var services = new TService[]
            {
                (new FailingTestService() as TService)!,
                (new FailingTestService() as TService)!,
                (new MockTestService() as TService)!
            };
            return Task.FromResult<IReadOnlyList<TService>>(services);
        }
    }
}
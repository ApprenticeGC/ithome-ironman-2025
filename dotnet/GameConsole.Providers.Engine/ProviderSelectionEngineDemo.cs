using GameConsole.Providers.Engine;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine.Examples;

/// <summary>
/// Example demonstrating the Provider Selection Engine functionality.
/// Shows performance-based selection, fallback strategies, load balancing, and circuit breaker patterns.
/// </summary>
public static class ProviderSelectionEngineDemo
{
    /// <summary>
    /// Runs a comprehensive demo of the provider selection engine features.
    /// </summary>
    public static async Task RunDemoAsync(ILogger? logger = null)
    {
        logger?.LogInformation("=== Provider Selection Engine Demo ===");
        
        // 1. Set up performance monitor
        using var performanceMonitor = new ProviderPerformanceMonitor();
        
        // 2. Create mock providers
        var providers = new List<IMockService>
        {
            new MockService("FastProvider", TimeSpan.FromMilliseconds(50)),
            new MockService("SlowProvider", TimeSpan.FromMilliseconds(300)),
            new MockService("UnreliableProvider", TimeSpan.FromMilliseconds(100), failureRate: 0.7),
            new MockService("ReliableProvider", TimeSpan.FromMilliseconds(150))
        };

        logger?.LogInformation("Created {Count} mock providers", providers.Count);

        // 3. Generate performance history
        logger?.LogInformation("Generating performance history...");
        foreach (var provider in providers)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    using var tracker = performanceMonitor.StartOperation(provider.Name);
                    await provider.DoWorkAsync();
                    tracker.RecordSuccess();
                }
                catch (Exception ex)
                {
                    using var tracker = performanceMonitor.StartOperation(provider.Name);
                    tracker.RecordFailure(ex);
                }
            }
        }

        // 4. Demonstrate performance-based selection
        logger?.LogInformation("\n--- Demonstrating Performance-Based Selection ---");
        var selector = new PerformanceBasedProviderSelector<IMockService>(performanceMonitor);

        for (int i = 0; i < 5; i++)
        {
            var selectedProvider = await selector.SelectProviderAsync(providers, context: null);
            if (selectedProvider != null)
            {
                logger?.LogInformation("Selected provider: {ProviderName}", selectedProvider.Name);
            }
        }

        // 5. Show performance metrics
        logger?.LogInformation("\n--- Performance Metrics Summary ---");
        foreach (var metrics in performanceMonitor.GetAllMetrics())
        {
            logger?.LogInformation("Provider: {ProviderId}", metrics.ProviderId);
            logger?.LogInformation("  Success Rate: {SuccessRate:P}", metrics.SuccessRate);
            logger?.LogInformation("  Avg Response Time: {ResponseTime}ms", 
                metrics.AverageResponseTime.TotalMilliseconds);
            logger?.LogInformation("  Circuit State: {State}", metrics.CircuitBreakerState);
            logger?.LogInformation("");
        }

        logger?.LogInformation("=== Demo Complete ===");
    }

    /// <summary>
    /// Mock service interface for demonstration.
    /// </summary>
    public interface IMockService
    {
        string Name { get; }
        Task DoWorkAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Mock service implementation that simulates different performance characteristics.
    /// </summary>
    public class MockService : IMockService
    {
        private readonly TimeSpan _responseTime;
        private readonly double _failureRate;
        private readonly Random _random = new();

        public MockService(string name, TimeSpan responseTime, double failureRate = 0.1)
        {
            Name = name;
            _responseTime = responseTime;
            _failureRate = failureRate;
        }

        public string Name { get; }

        public async Task DoWorkAsync(CancellationToken cancellationToken = default)
        {
            // Simulate work time
            await Task.Delay(_responseTime, cancellationToken);

            // Simulate random failures based on failure rate
            if (_random.NextDouble() < _failureRate)
            {
                throw new InvalidOperationException($"Simulated failure in {Name}");
            }
        }

        public override int GetHashCode() => Name.GetHashCode();
        public override string ToString() => Name;
    }
}
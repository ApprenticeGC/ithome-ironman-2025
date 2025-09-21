# GameConsole.Providers.Engine

This package implements the Service Provider Selection Engine for GameConsole's 4-tier architecture, providing intelligent provider selection with performance monitoring, fallback strategies, and load balancing.

## Features

### 🎯 Smart Provider Selection
- **Health-based Selection**: Choose providers based on performance metrics and health scores
- **Multiple Algorithms**: Round-robin, weighted, random, least-load, and health-based selection
- **Circuit Breaker Pattern**: Prevent cascading failures with automatic recovery

### 📊 Performance Monitoring
- **Real-time Metrics**: Track response times, success rates, and failure counts
- **Health Scoring**: Composite health scores based on performance and reliability
- **System.Diagnostics Integration**: Built-in performance monitoring using .NET diagnostics

### 🔄 Fallback Strategies
- **Exponential Backoff**: Configurable retry delays with jitter
- **Automatic Failover**: Seamless fallback to alternative providers
- **Non-retryable Exception Handling**: Smart retry logic based on exception types

### ⚖️ Load Balancing
- **Request Distribution**: Distribute load across multiple provider instances
- **Load Metrics**: Real-time load distribution monitoring
- **Configurable Strategies**: Multiple load balancing algorithms

## Quick Start

### Basic Usage

```csharp
using GameConsole.Providers.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup services
var services = new ServiceCollection();
services.AddSingleton<IProviderPerformanceMonitor, ProviderPerformanceMonitor>();
services.AddSingleton<IProviderSelector, ProviderSelector>();
services.AddSingleton<IFallbackStrategy, ExponentialBackoffFallbackStrategy>();
services.AddSingleton<ILoadBalancingProvider, LoadBalancingProvider>();

// Register some example providers
services.AddTransient<IExampleService, FastProvider>();
services.AddTransient<IExampleService, ReliableProvider>();
services.AddTransient<IExampleService, BackupProvider>();

var serviceProvider = services.BuildServiceProvider();

// Use the provider selector
var selector = serviceProvider.GetRequiredService<IProviderSelector>();

// Select best provider based on health
var provider = await selector.SelectProviderAsync<IExampleService>(
    new ProviderSelectionCriteria 
    { 
        Algorithm = SelectionAlgorithm.HealthBased,
        MinHealthScore = 70.0 
    });

// Use with fallback strategy
var fallbackStrategy = serviceProvider.GetRequiredService<IFallbackStrategy>();

var result = await fallbackStrategy.ExecuteWithFallbackAsync<IExampleService, string>(
    async (provider, ct) => await provider.DoWorkAsync(ct));

if (result.IsSuccess)
{
    Console.WriteLine($"Operation succeeded: {result.Result}");
    Console.WriteLine($"Provider: {result.SuccessfulProviderId}");
}
```

### Load Balancing Example

```csharp
var loadBalancer = serviceProvider.GetRequiredService<ILoadBalancingProvider>();

// Execute operation with automatic load balancing
var result = await loadBalancer.ExecuteAsync<IExampleService, string>(
    async (provider, ct) => await provider.ProcessRequestAsync("data", ct),
    new ProviderSelectionCriteria { Algorithm = SelectionAlgorithm.LeastLoad });

// Check load distribution
var distribution = await loadBalancer.GetLoadDistributionAsync<IExampleService>();
foreach (var kvp in distribution)
{
    Console.WriteLine($"Provider {kvp.Key}: {kvp.Value:F1}% load");
}
```

## Architecture

The Provider Selection Engine follows GameConsole's 4-tier architecture:

- **Tier 1 (Contracts)**: Interfaces in GameConsole.Core.Abstractions
- **Tier 2 (Proxies)**: Generated proxies for provider access  
- **Tier 3 (Services)**: This package - provider selection logic
- **Tier 4 (Providers)**: Actual service implementations

## Configuration

### Performance Monitor Options

```csharp
services.Configure<PerformanceMonitorOptions>(options =>
{
    options.MinHealthScoreThreshold = 60.0;
    options.MinSuccessRateThreshold = 85.0;
    options.MetricsRetentionTime = TimeSpan.FromHours(24);
});
```

### Fallback Strategy Options

```csharp
services.Configure<FallbackOptions>(options =>
{
    options.BaseRetryDelay = TimeSpan.FromMilliseconds(200);
    options.MaxRetryDelay = TimeSpan.FromSeconds(30);
    options.MaxRetryAttempts = 5;
    options.JitterFactor = 0.2;
});
```

### Provider Selector Options

```csharp
services.Configure<ProviderSelectorOptions>(options =>
{
    options.CircuitBreakerFailureThreshold = 3;
    options.CircuitBreakerRetryInterval = TimeSpan.FromMinutes(2);
    options.DefaultHealthThreshold = 60.0;
});
```

## Selection Algorithms

### HealthBased
Selects providers based on composite health scores calculated from:
- Success rate (0-70 points)
- Response time performance (0-30 points)

### RoundRobin
Simple round-robin selection among available providers.

### Weighted
Weighted selection using health scores as weights.

### Random
Random selection from available providers.

### LeastLoad
Selects provider with the lowest current request count.

## Metrics and Monitoring

### Provider Metrics
Each provider is tracked with:
- **Average Response Time**: Mean response time in milliseconds
- **Success Rate**: Percentage of successful requests (0-100)
- **Request Count**: Total number of requests processed
- **Failure Count**: Total number of failed requests
- **Health Score**: Composite score (0-100, higher is better)
- **Last Error**: Most recent exception (if any)

### Health Score Calculation
```
Health Score = Success Rate * 0.7 + Response Time Score * 0.3

Response Time Score:
- ≤ 100ms: 30 points (Excellent)
- ≤ 500ms: 25 points (Good)  
- ≤ 1000ms: 20 points (Fair)
- ≤ 2000ms: 15 points (Poor)
- ≤ 5000ms: 10 points (Very Poor)
- > 5000ms: 5 points (Critical)
```

## Testing

The package includes comprehensive unit tests covering:
- Provider performance monitoring
- Fallback strategy execution
- Selection algorithm behavior
- Error handling and edge cases
- Input validation

Run tests with:
```bash
dotnet test GameConsole.Providers.Engine.Tests
```

## Integration

This package integrates with:
- **GameConsole.Core.Abstractions**: Base interfaces and contracts
- **GameConsole.Core.Registry**: Service registration and resolution
- **Microsoft.Extensions.Logging**: Comprehensive logging support
- **Microsoft.Extensions.Hosting**: Background service patterns
- **Microsoft.Extensions.Options**: Configuration options pattern

## Performance

The Provider Selection Engine is designed for high-performance scenarios:
- Thread-safe concurrent operations using `ConcurrentDictionary`
- Minimal allocation patterns
- Fast health score calculations
- Efficient round-robin counter management
- Circuit breaker state tracking with minimal overhead

## Dependencies

- .NET 8.0
- GameConsole.Core.Abstractions
- GameConsole.Core.Registry  
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.Hosting.Abstractions
- Microsoft.Extensions.Options
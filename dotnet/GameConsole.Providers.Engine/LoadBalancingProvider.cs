using System.Collections.Concurrent;
using System.Collections.Immutable;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Load balancing provider implementation that distributes requests across multiple provider instances.
/// Supports different load balancing algorithms and automatic fallback strategies.
/// </summary>
/// <typeparam name="T">The type of provider being load balanced.</typeparam>
public sealed class LoadBalancingProvider<T> : ILoadBalancingProvider<T> where T : class
{
    private readonly ConcurrentDictionary<T, ProviderInfo<T>> _providers = new();
    private readonly IProviderSelector<T> _selector;
    private readonly IFallbackStrategy<T> _fallbackStrategy;
    private readonly IProviderPerformanceMonitor _performanceMonitor;
    private readonly ILogger<LoadBalancingProvider<T>>? _logger;
    private readonly LoadBalancingStatisticsData _statistics = new();
    
    private readonly LoadBalancingAlgorithm _algorithm;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadBalancingProvider{T}"/> class.
    /// </summary>
    /// <param name="selector">The provider selector to use for load balancing.</param>
    /// <param name="fallbackStrategy">The fallback strategy to use when providers fail.</param>
    /// <param name="performanceMonitor">The performance monitor to track provider metrics.</param>
    /// <param name="algorithm">The load balancing algorithm to use.</param>
    /// <param name="logger">Optional logger for load balancing operations.</param>
    public LoadBalancingProvider(
        IProviderSelector<T> selector,
        IFallbackStrategy<T> fallbackStrategy,
        IProviderPerformanceMonitor performanceMonitor,
        LoadBalancingAlgorithm algorithm = LoadBalancingAlgorithm.PerformanceBased,
        ILogger<LoadBalancingProvider<T>>? logger = null)
    {
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _fallbackStrategy = fallbackStrategy ?? throw new ArgumentNullException(nameof(fallbackStrategy));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _algorithm = algorithm;
        _logger = logger;
    }

    /// <inheritdoc />
    public LoadBalancingAlgorithm Algorithm => _algorithm;

    /// <inheritdoc />
    public LoadBalancingStatistics Statistics => _statistics.CreateSnapshot();

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public void AddProvider(T provider, int weight = 1)
    {
        if (provider == null)
            throw new ArgumentNullException(nameof(provider));
        
        if (weight <= 0)
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be positive");

        var providerInfo = new ProviderInfo<T>
        {
            Provider = provider,
            Weight = weight,
            IsHealthy = true,
            ActiveOperations = 0,
            AddedTime = DateTimeOffset.UtcNow
        };

        if (_providers.TryAdd(provider, providerInfo))
        {
            _logger?.LogInformation("Added provider {ProviderType} with weight {Weight}",
                provider.GetType().Name, weight);

            ProviderAdded?.Invoke(this, new ProviderAddedEventArgs<T>
            {
                Provider = provider,
                Weight = weight,
                AddedTime = providerInfo.AddedTime
            });
        }
        else
        {
            _logger?.LogWarning("Provider {ProviderType} is already in the load balancing pool",
                provider.GetType().Name);
        }
    }

    /// <inheritdoc />
    public bool RemoveProvider(T provider)
    {
        if (provider == null)
            return false;

        if (_providers.TryRemove(provider, out var providerInfo))
        {
            _logger?.LogInformation("Removed provider {ProviderType} from load balancing pool",
                provider.GetType().Name);

            ProviderRemoved?.Invoke(this, new ProviderRemovedEventArgs<T>
            {
                Provider = provider,
                RemovedTime = DateTimeOffset.UtcNow
            });

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<T?> GetNextProviderAsync(object? context = null, CancellationToken cancellationToken = default)
    {
        var availableProviders = GetHealthyProviders();
        if (!availableProviders.Any())
        {
            _logger?.LogWarning("No healthy providers available for load balancing");
            return null;
        }

        var selectedProvider = await _selector.SelectProviderAsync(availableProviders, context, cancellationToken);
        
        if (selectedProvider != null)
        {
            _logger?.LogDebug("Selected provider {ProviderType} for load balancing",
                selectedProvider.GetType().Name);
        }

        return selectedProvider;
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<T, CancellationToken, Task<TResult>> operation,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

        var attemptCount = 0;
        var availableProviders = GetHealthyProviders().ToList();
        T? lastFailedProvider = null;
        Exception? lastException = null;

        _statistics.IncrementTotalRequests();

        while (attemptCount < 10) // Safety limit
        {
            var provider = await GetNextProviderAsync(context, cancellationToken);
            if (provider == null)
            {
                break;
            }

            try
            {
                using var tracker = _performanceMonitor.StartOperation(GetProviderId(provider));
                var result = await operation(provider, cancellationToken);
                tracker.RecordSuccess();
                
                _statistics.IncrementSuccessfulRequests();
                _statistics.RecordProviderUsage(GetProviderId(provider));
                
                _logger?.LogDebug("Successfully executed operation using provider {ProviderType}",
                    provider.GetType().Name);

                return result;
            }
            catch (Exception ex)
            {
                attemptCount++;
                lastFailedProvider = provider;
                lastException = ex;

                using var tracker = _performanceMonitor.StartOperation(GetProviderId(provider));
                tracker.RecordFailure(ex);

                _logger?.LogWarning(ex, "Operation failed on provider {ProviderType}, attempt {AttemptCount}",
                    provider.GetType().Name, attemptCount);

                // Check if we should attempt fallback
                if (!_fallbackStrategy.ShouldAttemptFallback(provider, ex, attemptCount))
                {
                    break;
                }

                // Get fallback providers
                var fallbackProviders = await _fallbackStrategy.GetFallbackProvidersAsync(
                    lastFailedProvider, availableProviders, context, cancellationToken);

                availableProviders = fallbackProviders.ToList();
                if (!availableProviders.Any())
                {
                    _logger?.LogWarning("No fallback providers available after attempt {AttemptCount}",
                        attemptCount);
                    break;
                }

                // Wait before retry if configured
                var retryDelay = _fallbackStrategy.CalculateRetryDelay(attemptCount, ex);
                if (retryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(retryDelay, cancellationToken);
                }

                _statistics.IncrementFallbackAttempts();
            }
        }

        _statistics.IncrementFailedRequests();
        
        var finalException = lastException ?? new InvalidOperationException("No providers available");
        _logger?.LogError(finalException, "All providers failed after {AttemptCount} attempts", attemptCount);
        
        throw finalException;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(
        Func<T, CancellationToken, Task> operation,
        object? context = null,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<object?>(async (provider, ct) =>
        {
            await operation(provider, ct);
            return null;
        }, context, cancellationToken);
    }

    /// <inheritdoc />
    public IEnumerable<ProviderInfo<T>> GetAvailableProviders()
    {
        return _providers.Values
            .Where(info => _performanceMonitor.IsProviderHealthy(GetProviderId(info.Provider)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Initializing LoadBalancingProvider with algorithm {Algorithm}",
            _algorithm);
        
        // Initialize any providers that implement IService
        foreach (var provider in _providers.Keys.OfType<IService>())
        {
            try
            {
                await provider.InitializeAsync(cancellationToken);
                _logger?.LogDebug("Initialized provider {ProviderType}",
                    provider.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize provider {ProviderType}",
                    provider.GetType().Name);
            }
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting LoadBalancingProvider");
        
        // Start any providers that implement IService
        foreach (var provider in _providers.Keys.OfType<IService>())
        {
            try
            {
                await provider.StartAsync(cancellationToken);
                _logger?.LogDebug("Started provider {ProviderType}",
                    provider.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start provider {ProviderType}",
                    provider.GetType().Name);
            }
        }

        _isRunning = true;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Stopping LoadBalancingProvider");
        
        _isRunning = false;

        // Stop any providers that implement IService
        foreach (var provider in _providers.Keys.OfType<IService>())
        {
            try
            {
                await provider.StopAsync(cancellationToken);
                _logger?.LogDebug("Stopped provider {ProviderType}",
                    provider.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to stop provider {ProviderType}",
                    provider.GetType().Name);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await StopAsync();

            // Dispose any providers that implement IAsyncDisposable
            foreach (var provider in _providers.Keys.OfType<IAsyncDisposable>())
            {
                try
                {
                    await provider.DisposeAsync();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to dispose provider {ProviderType}",
                        provider.GetType().Name);
                }
            }

            _providers.Clear();
            _disposed = true;
        }
    }

    /// <inheritdoc />
    public event EventHandler<ProviderAddedEventArgs<T>>? ProviderAdded;

    /// <inheritdoc />
    public event EventHandler<ProviderRemovedEventArgs<T>>? ProviderRemoved;

    private IEnumerable<T> GetHealthyProviders()
    {
        return _providers.Values
            .Where(info => _performanceMonitor.IsProviderHealthy(GetProviderId(info.Provider)))
            .Select(info => info.Provider);
    }

    private static string GetProviderId(T provider)
    {
        return $"{provider.GetType().Name}_{provider.GetHashCode()}";
    }

    private sealed class LoadBalancingStatisticsData
    {
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;
        private long _fallbackAttempts;
        private readonly ConcurrentDictionary<string, long> _requestDistribution = new();
        private readonly List<TimeSpan> _responseTimes = new();
        private readonly object _lock = new object();

        public void IncrementTotalRequests()
        {
            Interlocked.Increment(ref _totalRequests);
        }

        public void IncrementSuccessfulRequests()
        {
            Interlocked.Increment(ref _successfulRequests);
        }

        public void IncrementFailedRequests()
        {
            Interlocked.Increment(ref _failedRequests);
        }

        public void IncrementFallbackAttempts()
        {
            Interlocked.Increment(ref _fallbackAttempts);
        }

        public void RecordProviderUsage(string providerId)
        {
            _requestDistribution.AddOrUpdate(providerId, 1, (_, count) => count + 1);
        }

        public LoadBalancingStatistics CreateSnapshot()
        {
            lock (_lock)
            {
                var averageResponseTime = _responseTimes.Count > 0
                    ? TimeSpan.FromTicks((long)_responseTimes.Average(rt => rt.Ticks))
                    : TimeSpan.Zero;

                return new LoadBalancingStatistics
                {
                    TotalRequests = _totalRequests,
                    SuccessfulRequests = _successfulRequests,
                    FailedRequests = _failedRequests,
                    FallbackAttempts = _fallbackAttempts,
                    AverageResponseTime = averageResponseTime,
                    RequestDistribution = _requestDistribution.ToImmutableDictionary()
                };
            }
        }
    }
}

/// <summary>
/// Extension methods for creating common load balancing provider configurations.
/// </summary>
public static class LoadBalancingProviderExtensions
{
    /// <summary>
    /// Creates a load balancing provider with round-robin selection and default fallback.
    /// </summary>
    /// <typeparam name="T">The type of provider being load balanced.</typeparam>
    /// <param name="performanceMonitor">The performance monitor to use.</param>
    /// <param name="logger">Optional logger for operations.</param>
    /// <returns>A configured load balancing provider.</returns>
    public static LoadBalancingProvider<T> CreateRoundRobin<T>(
        IProviderPerformanceMonitor performanceMonitor,
        ILogger<LoadBalancingProvider<T>>? logger = null) where T : class
    {
        var selector = new RoundRobinProviderSelector<T>(performanceMonitor);
        var fallbackStrategy = new DefaultFallbackStrategy<T>(performanceMonitor: performanceMonitor);
        
        return new LoadBalancingProvider<T>(
            selector, 
            fallbackStrategy, 
            performanceMonitor, 
            LoadBalancingAlgorithm.RoundRobin, 
            logger);
    }

    /// <summary>
    /// Creates a load balancing provider with performance-based selection and default fallback.
    /// </summary>
    /// <typeparam name="T">The type of provider being load balanced.</typeparam>
    /// <param name="performanceMonitor">The performance monitor to use.</param>
    /// <param name="logger">Optional logger for operations.</param>
    /// <returns>A configured load balancing provider.</returns>
    public static LoadBalancingProvider<T> CreatePerformanceBased<T>(
        IProviderPerformanceMonitor performanceMonitor,
        ILogger<LoadBalancingProvider<T>>? logger = null) where T : class
    {
        var selector = new PerformanceBasedProviderSelector<T>(performanceMonitor);
        var fallbackStrategy = new DefaultFallbackStrategy<T>(performanceMonitor: performanceMonitor);
        
        return new LoadBalancingProvider<T>(
            selector, 
            fallbackStrategy, 
            performanceMonitor, 
            LoadBalancingAlgorithm.PerformanceBased, 
            logger);
    }
}
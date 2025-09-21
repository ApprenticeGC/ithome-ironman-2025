using System.Collections.Concurrent;
using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Default implementation of provider selector with health checks and circuit breaker patterns.
/// </summary>
public sealed class ProviderSelector : IProviderSelector
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceRegistry _serviceRegistry;
    private readonly IProviderPerformanceMonitor _performanceMonitor;
    private readonly ILogger<ProviderSelector> _logger;
    private readonly ProviderSelectorOptions _options;
    
    // Circuit breaker state tracking
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();
    
    // Round-robin state tracking
    private readonly ConcurrentDictionary<Type, int> _roundRobinCounters = new();

    /// <summary>
    /// Circuit breaker state for a provider.
    /// </summary>
    private class CircuitBreakerState
    {
        public bool IsOpen { get; set; }
        public DateTimeOffset LastFailureTime { get; set; }
        public int ConsecutiveFailures { get; set; }
        public DateTimeOffset NextRetryTime { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderSelector"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving services.</param>
    /// <param name="serviceRegistry">Service registry for getting registered services.</param>
    /// <param name="performanceMonitor">Performance monitor for health checks.</param>
    /// <param name="logger">Logger for selection operations.</param>
    /// <param name="options">Configuration options for the selector.</param>
    public ProviderSelector(
        IServiceProvider serviceProvider,
        IServiceRegistry serviceRegistry,
        IProviderPerformanceMonitor performanceMonitor,
        ILogger<ProviderSelector> logger,
        ProviderSelectorOptions? options = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new ProviderSelectorOptions();
    }

    /// <inheritdoc />
    public async Task<object?> SelectProviderAsync(Type serviceType, ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        criteria ??= new ProviderSelectionCriteria();

        _logger.LogDebug("Selecting provider for service {ServiceType} using algorithm {Algorithm}", 
            serviceType.Name, criteria.Algorithm);

        var availableProviders = await GetAvailableProvidersAsync(serviceType, criteria, cancellationToken);
        
        if (!availableProviders.Any())
        {
            _logger.LogWarning("No available providers found for service {ServiceType}", serviceType.Name);
            return null;
        }

        return criteria.Algorithm switch
        {
            SelectionAlgorithm.HealthBased => await SelectHealthBasedProviderAsync(availableProviders, cancellationToken),
            SelectionAlgorithm.RoundRobin => SelectRoundRobinProvider(serviceType, availableProviders),
            SelectionAlgorithm.Weighted => await SelectWeightedProviderAsync(availableProviders, cancellationToken),
            SelectionAlgorithm.Random => SelectRandomProvider(availableProviders),
            SelectionAlgorithm.LeastLoad => await SelectLeastLoadProviderAsync(availableProviders, cancellationToken),
            _ => await SelectHealthBasedProviderAsync(availableProviders, cancellationToken)
        };
    }

    /// <inheritdoc />
    public async Task<TService?> SelectProviderAsync<TService>(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
        where TService : class
    {
        var provider = await SelectProviderAsync(typeof(TService), criteria, cancellationToken);
        return provider as TService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<object>> GetAvailableProvidersAsync(Type serviceType, ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        criteria ??= new ProviderSelectionCriteria();

        var providers = new List<object>();

        // Get all registered services of the type
        var registeredServices = _serviceRegistry.GetRegisteredServices()
            .Where(s => s.ServiceType == serviceType || serviceType.IsAssignableFrom(s.ServiceType));

        foreach (var service in registeredServices)
        {
            try
            {
                var instance = _serviceProvider.GetService(service.ServiceType);
                if (instance == null)
                    continue;

                var providerId = GetProviderId(instance);

                // Check circuit breaker
                if (await IsCircuitBreakerOpenAsync(providerId, cancellationToken))
                {
                    _logger.LogDebug("Skipping provider {ProviderId} - circuit breaker is open", providerId);
                    continue;
                }

                // Check health if required
                if (criteria.ExcludeUnhealthy)
                {
                    var isHealthy = await _performanceMonitor.IsHealthyAsync(providerId, cancellationToken);
                    if (!isHealthy)
                    {
                        _logger.LogDebug("Skipping provider {ProviderId} - health check failed", providerId);
                        continue;
                    }
                }

                // Check capabilities if required
                if (criteria.RequiredCapabilities?.Any() == true && instance is ICapabilityProvider capabilityProvider)
                {
                    var capabilities = await capabilityProvider.GetCapabilitiesAsync(cancellationToken);
                    var capabilityNames = capabilities.Select(c => c.Name).ToHashSet();
                    
                    if (!criteria.RequiredCapabilities.All(required => capabilityNames.Contains(required)))
                    {
                        _logger.LogDebug("Skipping provider {ProviderId} - missing required capabilities", providerId);
                        continue;
                    }
                }

                // Check performance criteria
                if (criteria.MaxResponseTime.HasValue)
                {
                    var metrics = await _performanceMonitor.GetMetricsAsync(providerId, cancellationToken);
                    if (metrics.AverageResponseTime > criteria.MaxResponseTime.Value.TotalMilliseconds)
                    {
                        _logger.LogDebug("Skipping provider {ProviderId} - response time too high", providerId);
                        continue;
                    }
                }

                providers.Add(instance);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting provider instance for service {ServiceType}: {ErrorMessage}", 
                    service.ServiceType.Name, ex.Message);
            }
        }

        _logger.LogDebug("Found {ProviderCount} available providers for service {ServiceType}", 
            providers.Count, serviceType.Name);

        return providers;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TService>> GetAvailableProvidersAsync<TService>(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
        where TService : class
    {
        var providers = await GetAvailableProvidersAsync(typeof(TService), criteria, cancellationToken);
        return providers.OfType<TService>().ToList();
    }

    private async Task<object> SelectHealthBasedProviderAsync(IReadOnlyList<object> providers, CancellationToken cancellationToken)
    {
        var providerScores = new List<(object Provider, double Score)>();

        foreach (var provider in providers)
        {
            var providerId = GetProviderId(provider);
            var metrics = await _performanceMonitor.GetMetricsAsync(providerId, cancellationToken);
            providerScores.Add((provider, metrics.HealthScore));
        }

        var best = providerScores.OrderByDescending(p => p.Score).First();
        _logger.LogDebug("Selected provider with health score {HealthScore}", best.Score);
        
        return best.Provider;
    }

    private object SelectRoundRobinProvider(Type serviceType, IReadOnlyList<object> providers)
    {
        var counter = _roundRobinCounters.AddOrUpdate(serviceType, 0, (_, current) => (current + 1) % providers.Count);
        var selected = providers[counter];
        
        _logger.LogDebug("Selected provider using round-robin (index {Index})", counter);
        return selected;
    }

    private async Task<object> SelectWeightedProviderAsync(IReadOnlyList<object> providers, CancellationToken cancellationToken)
    {
        // For weighted selection, use health score as weight
        var weights = new double[providers.Count];
        var totalWeight = 0.0;

        for (var i = 0; i < providers.Count; i++)
        {
            var providerId = GetProviderId(providers[i]);
            var metrics = await _performanceMonitor.GetMetricsAsync(providerId, cancellationToken);
            weights[i] = Math.Max(0.1, metrics.HealthScore / 100.0); // Minimum weight of 0.1
            totalWeight += weights[i];
        }

        var random = Random.Shared.NextDouble() * totalWeight;
        var currentWeight = 0.0;

        for (var i = 0; i < providers.Count; i++)
        {
            currentWeight += weights[i];
            if (random <= currentWeight)
            {
                _logger.LogDebug("Selected provider using weighted selection (weight {Weight})", weights[i]);
                return providers[i];
            }
        }

        // Fallback to last provider
        return providers[^1];
    }

    private object SelectRandomProvider(IReadOnlyList<object> providers)
    {
        var index = Random.Shared.Next(providers.Count);
        _logger.LogDebug("Selected provider using random selection (index {Index})", index);
        return providers[index];
    }

    private async Task<object> SelectLeastLoadProviderAsync(IReadOnlyList<object> providers, CancellationToken cancellationToken)
    {
        var providerLoads = new List<(object Provider, long RequestCount)>();

        foreach (var provider in providers)
        {
            var providerId = GetProviderId(provider);
            var metrics = await _performanceMonitor.GetMetricsAsync(providerId, cancellationToken);
            providerLoads.Add((provider, metrics.RequestCount));
        }

        var leastLoaded = providerLoads.OrderBy(p => p.RequestCount).First();
        _logger.LogDebug("Selected provider with least load (request count: {RequestCount})", leastLoaded.RequestCount);
        
        return leastLoaded.Provider;
    }

    private Task<bool> IsCircuitBreakerOpenAsync(string providerId, CancellationToken cancellationToken)
    {
        if (!_circuitBreakers.TryGetValue(providerId, out var state))
            return Task.FromResult(false);

        if (!state.IsOpen)
            return Task.FromResult(false);

        // Check if it's time to retry
        if (DateTimeOffset.UtcNow >= state.NextRetryTime)
        {
            state.IsOpen = false;
            state.ConsecutiveFailures = 0;
            _logger.LogInformation("Circuit breaker for provider {ProviderId} is now half-open for retry", providerId);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private static string GetProviderId(object provider)
    {
        return provider.GetType().FullName ?? provider.GetType().Name;
    }
}

/// <summary>
/// Configuration options for the provider selector.
/// </summary>
public class ProviderSelectorOptions
{
    /// <summary>
    /// Number of consecutive failures before opening circuit breaker.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Time to wait before retrying a circuit breaker.
    /// </summary>
    public TimeSpan CircuitBreakerRetryInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Default health score threshold for provider selection.
    /// </summary>
    public double DefaultHealthThreshold { get; set; } = 50.0;
}
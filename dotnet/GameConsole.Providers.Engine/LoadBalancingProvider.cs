using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Default implementation of load balancing provider that distributes requests across multiple providers.
/// </summary>
public sealed class LoadBalancingProvider : ILoadBalancingProvider
{
    private readonly IProviderSelector _providerSelector;
    private readonly IProviderPerformanceMonitor _performanceMonitor;
    private readonly ILogger<LoadBalancingProvider> _logger;
    private readonly LoadBalancingOptions _options;
    
    // Request count tracking per provider for load balancing
    private readonly ConcurrentDictionary<string, long> _requestCounts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadBalancingProvider"/> class.
    /// </summary>
    /// <param name="providerSelector">Provider selector for getting available providers.</param>
    /// <param name="performanceMonitor">Performance monitor for tracking metrics.</param>
    /// <param name="logger">Logger for load balancing operations.</param>
    /// <param name="options">Configuration options for load balancing.</param>
    public LoadBalancingProvider(
        IProviderSelector providerSelector,
        IProviderPerformanceMonitor performanceMonitor,
        ILogger<LoadBalancingProvider> logger,
        LoadBalancingOptions? options = null)
    {
        _providerSelector = providerSelector ?? throw new ArgumentNullException(nameof(providerSelector));
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new LoadBalancingOptions();
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        ProviderSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(operation);

        criteria ??= new ProviderSelectionCriteria { Algorithm = SelectionAlgorithm.LeastLoad };

        var provider = await _providerSelector.SelectProviderAsync<TService>(criteria, cancellationToken);
        if (provider == null)
        {
            _logger.LogError("No available providers found for service {ServiceType}", typeof(TService).Name);
            throw new InvalidOperationException($"No available providers found for service {typeof(TService).Name}");
        }

        var providerId = GetProviderId(provider);
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // Increment request count for this provider
            _requestCounts.AddOrUpdate(providerId, 1, (_, count) => count + 1);

            _logger.LogDebug("Executing operation on provider {ProviderId} for service {ServiceType}", 
                providerId, typeof(TService).Name);

            var result = await operation(provider, cancellationToken);
            var responseTime = DateTimeOffset.UtcNow - startTime;

            // Record successful execution
            await _performanceMonitor.RecordSuccessAsync(providerId, responseTime, cancellationToken);

            _logger.LogDebug("Operation completed successfully on provider {ProviderId} in {Duration}ms", 
                providerId, responseTime.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            var responseTime = DateTimeOffset.UtcNow - startTime;
            
            // Record failed execution
            await _performanceMonitor.RecordFailureAsync(providerId, ex, cancellationToken);

            _logger.LogError(ex, "Operation failed on provider {ProviderId} after {Duration}ms: {ErrorMessage}", 
                providerId, responseTime.TotalMilliseconds, ex.Message);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task ExecuteAsync<TService>(
        Func<TService, CancellationToken, Task> operation,
        ProviderSelectionCriteria? criteria = null,
        CancellationToken cancellationToken = default)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(operation);

        var wrappedOperation = async (TService service, CancellationToken ct) =>
        {
            await operation(service, ct);
            return (object?)null;
        };

        await ExecuteAsync<TService, object?>(wrappedOperation, criteria, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, double>> GetLoadDistributionAsync(Type serviceType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        var providers = await _providerSelector.GetAvailableProvidersAsync(serviceType, cancellationToken: cancellationToken);
        var distribution = new Dictionary<string, double>();

        var totalRequests = 0L;
        var providerCounts = new Dictionary<string, long>();

        // Get request counts for each provider
        foreach (var provider in providers)
        {
            var providerId = GetProviderId(provider);
            var count = _requestCounts.GetValueOrDefault(providerId, 0);
            providerCounts[providerId] = count;
            totalRequests += count;
        }

        // Calculate distribution percentages
        foreach (var kvp in providerCounts)
        {
            distribution[kvp.Key] = totalRequests > 0 ? (double)kvp.Value / totalRequests * 100.0 : 0.0;
        }

        _logger.LogDebug("Load distribution for service {ServiceType}: {Distribution}", 
            serviceType.Name, string.Join(", ", distribution.Select(kvp => $"{kvp.Key}: {kvp.Value:F1}%")));

        return distribution;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, double>> GetLoadDistributionAsync<TService>(CancellationToken cancellationToken = default)
        where TService : class
    {
        return await GetLoadDistributionAsync(typeof(TService), cancellationToken);
    }

    private static string GetProviderId(object provider)
    {
        return provider.GetType().FullName ?? provider.GetType().Name;
    }
}

/// <summary>
/// Configuration options for load balancing provider.
/// </summary>
public class LoadBalancingOptions
{
    /// <summary>
    /// Default selection criteria for load balancing.
    /// </summary>
    public ProviderSelectionCriteria DefaultCriteria { get; set; } = new()
    {
        Algorithm = SelectionAlgorithm.LeastLoad,
        ExcludeUnhealthy = true
    };

    /// <summary>
    /// Whether to reset request counts periodically.
    /// </summary>
    public bool ResetCountsPeriodically { get; set; } = true;

    /// <summary>
    /// Interval for resetting request counts.
    /// </summary>
    public TimeSpan ResetInterval { get; set; } = TimeSpan.FromHours(1);
}
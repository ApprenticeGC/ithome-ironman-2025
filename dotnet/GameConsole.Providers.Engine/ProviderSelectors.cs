using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine;

/// <summary>
/// Performance-based provider selector that chooses providers based on success rate and response time.
/// </summary>
/// <typeparam name="T">The type of provider to select.</typeparam>
public sealed class PerformanceBasedProviderSelector<T> : IProviderSelector<T> where T : class
{
    private readonly IProviderPerformanceMonitor _performanceMonitor;
    private readonly ILogger<PerformanceBasedProviderSelector<T>>? _logger;
    private readonly PerformanceSelectionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceBasedProviderSelector{T}"/> class.
    /// </summary>
    /// <param name="performanceMonitor">The performance monitor to use for metrics.</param>
    /// <param name="options">Options for performance-based selection.</param>
    /// <param name="logger">Optional logger for selection operations.</param>
    public PerformanceBasedProviderSelector(
        IProviderPerformanceMonitor performanceMonitor,
        PerformanceSelectionOptions? options = null,
        ILogger<PerformanceBasedProviderSelector<T>>? logger = null)
    {
        _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
        _options = options ?? new PerformanceSelectionOptions();
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T?> SelectProviderAsync(IEnumerable<T> providers, CancellationToken cancellationToken = default)
    {
        return SelectProviderAsync(providers, context: null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<T?> SelectProviderAsync(IEnumerable<T> providers, object? context = null, CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToList();
        if (providerList.Count == 0)
        {
            _logger?.LogWarning("No providers available for selection");
            return Task.FromResult<T?>(null);
        }

        if (providerList.Count == 1)
        {
            var singleProvider = providerList[0];
            var providerId = GetProviderId(singleProvider);
            if (_performanceMonitor.IsProviderHealthy(providerId))
            {
                return Task.FromResult<T?>(singleProvider);
            }
            return Task.FromResult<T?>(null);
        }

        // Filter to healthy providers only
        var healthyProviders = providerList
            .Where(p => _performanceMonitor.IsProviderHealthy(GetProviderId(p)))
            .ToList();

        if (healthyProviders.Count == 0)
        {
            _logger?.LogWarning("No healthy providers available for selection");
            return Task.FromResult<T?>(null);
        }

        // Score providers based on performance metrics
        var scoredProviders = healthyProviders
            .Select(provider => new
            {
                Provider = provider,
                Score = CalculateProviderScore(provider)
            })
            .OrderByDescending(p => p.Score)
            .ToList();

        var selectedProvider = scoredProviders.First().Provider;
        
        _logger?.LogDebug("Selected provider with score {Score} from {Count} healthy providers",
            scoredProviders.First().Score, healthyProviders.Count);

        return Task.FromResult<T?>(selectedProvider);
    }

    /// <inheritdoc />
    public Task<IEnumerable<T>> SelectProvidersAsync(
        IEnumerable<T> providers, 
        int count, 
        object? context = null, 
        CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToList();
        if (providerList.Count == 0 || count <= 0)
        {
            return Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
        }

        // Filter to healthy providers only
        var healthyProviders = providerList
            .Where(p => _performanceMonitor.IsProviderHealthy(GetProviderId(p)))
            .ToList();

        // Score and sort providers
        var selectedProviders = healthyProviders
            .Select(provider => new
            {
                Provider = provider,
                Score = CalculateProviderScore(provider)
            })
            .OrderByDescending(p => p.Score)
            .Take(Math.Min(count, healthyProviders.Count))
            .Select(p => p.Provider)
            .ToList();

        _logger?.LogDebug("Selected {SelectedCount} providers from {TotalCount} healthy providers",
            selectedProviders.Count, healthyProviders.Count);

        return Task.FromResult<IEnumerable<T>>(selectedProviders);
    }

    private double CalculateProviderScore(T provider)
    {
        var providerId = GetProviderId(provider);
        var metrics = _performanceMonitor.GetMetrics(providerId);

        // Base score starts with success rate (0.0 to 1.0)
        double score = metrics.SuccessRate;

        // Penalize high response times
        var responseTimePenalty = Math.Min(metrics.AverageResponseTime.TotalMilliseconds / _options.TargetResponseTimeMs, 1.0);
        score *= (1.0 - responseTimePenalty * _options.ResponseTimeWeight);

        // Boost score for providers with sufficient history
        if (metrics.TotalOperations >= _options.MinOperationsForBonus)
        {
            score *= _options.HistoryBonus;
        }

        // Penalize recent failures more heavily
        if (metrics.LastOperationTime > DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(5)) && 
            metrics.LastException != null)
        {
            score *= _options.RecentFailurePenalty;
        }

        return Math.Max(0.0, score);
    }

    private static string GetProviderId(T provider)
    {
        return $"{provider.GetType().Name}_{provider.GetHashCode()}";
    }
}

/// <summary>
/// Round-robin provider selector that cycles through providers in order.
/// </summary>
/// <typeparam name="T">The type of provider to select.</typeparam>
public sealed class RoundRobinProviderSelector<T> : IProviderSelector<T> where T : class
{
    private readonly IProviderPerformanceMonitor? _performanceMonitor;
    private readonly ILogger<RoundRobinProviderSelector<T>>? _logger;
    private int _currentIndex;
    private readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="RoundRobinProviderSelector{T}"/> class.
    /// </summary>
    /// <param name="performanceMonitor">Optional performance monitor for health checks.</param>
    /// <param name="logger">Optional logger for selection operations.</param>
    public RoundRobinProviderSelector(
        IProviderPerformanceMonitor? performanceMonitor = null,
        ILogger<RoundRobinProviderSelector<T>>? logger = null)
    {
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T?> SelectProviderAsync(IEnumerable<T> providers, CancellationToken cancellationToken = default)
    {
        return SelectProviderAsync(providers, context: null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<T?> SelectProviderAsync(IEnumerable<T> providers, object? context = null, CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToList();
        if (providerList.Count == 0)
        {
            return Task.FromResult<T?>(null);
        }

        // Filter to healthy providers if performance monitor is available
        if (_performanceMonitor != null)
        {
            providerList = providerList
                .Where(p => _performanceMonitor.IsProviderHealthy(GetProviderId(p)))
                .ToList();

            if (providerList.Count == 0)
            {
                _logger?.LogWarning("No healthy providers available for round-robin selection");
                return Task.FromResult<T?>(null);
            }
        }

        lock (_lock)
        {
            // Select next provider in round-robin fashion
            var selectedProvider = providerList[_currentIndex % providerList.Count];
            _currentIndex = (_currentIndex + 1) % providerList.Count;
            
            _logger?.LogDebug("Selected provider at index {Index} from {Count} available providers",
                _currentIndex, providerList.Count);

            return Task.FromResult<T?>(selectedProvider);
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<T>> SelectProvidersAsync(
        IEnumerable<T> providers, 
        int count, 
        object? context = null, 
        CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToList();
        if (providerList.Count == 0 || count <= 0)
        {
            return Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
        }

        // Filter to healthy providers if performance monitor is available
        if (_performanceMonitor != null)
        {
            providerList = providerList
                .Where(p => _performanceMonitor.IsProviderHealthy(GetProviderId(p)))
                .ToList();
        }

        var selectedProviders = new List<T>();
        var actualCount = Math.Min(count, providerList.Count);

        lock (_lock)
        {
            for (int i = 0; i < actualCount; i++)
            {
                selectedProviders.Add(providerList[_currentIndex % providerList.Count]);
                _currentIndex = (_currentIndex + 1) % providerList.Count;
            }
        }

        return Task.FromResult<IEnumerable<T>>(selectedProviders);
    }

    private static string GetProviderId(T provider)
    {
        return $"{provider.GetType().Name}_{provider.GetHashCode()}";
    }
}

/// <summary>
/// Random provider selector that randomly selects from available providers.
/// </summary>
/// <typeparam name="T">The type of provider to select.</typeparam>
public sealed class RandomProviderSelector<T> : IProviderSelector<T> where T : class
{
    private readonly IProviderPerformanceMonitor? _performanceMonitor;
    private readonly ILogger<RandomProviderSelector<T>>? _logger;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RandomProviderSelector{T}"/> class.
    /// </summary>
    /// <param name="performanceMonitor">Optional performance monitor for health checks.</param>
    /// <param name="logger">Optional logger for selection operations.</param>
    public RandomProviderSelector(
        IProviderPerformanceMonitor? performanceMonitor = null,
        ILogger<RandomProviderSelector<T>>? logger = null)
    {
        _performanceMonitor = performanceMonitor;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<T?> SelectProviderAsync(IEnumerable<T> providers, CancellationToken cancellationToken = default)
    {
        return SelectProviderAsync(providers, context: null, cancellationToken);
    }

    /// <inheritdoc />
    public Task<T?> SelectProviderAsync(IEnumerable<T> providers, object? context = null, CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToList();
        if (providerList.Count == 0)
        {
            return Task.FromResult<T?>(null);
        }

        // Filter to healthy providers if performance monitor is available
        if (_performanceMonitor != null)
        {
            providerList = providerList
                .Where(p => _performanceMonitor.IsProviderHealthy(GetProviderId(p)))
                .ToList();

            if (providerList.Count == 0)
            {
                _logger?.LogWarning("No healthy providers available for random selection");
                return Task.FromResult<T?>(null);
            }
        }

        var selectedIndex = _random.Next(providerList.Count);
        var selectedProvider = providerList[selectedIndex];

        _logger?.LogDebug("Randomly selected provider at index {Index} from {Count} available providers",
            selectedIndex, providerList.Count);

        return Task.FromResult<T?>(selectedProvider);
    }

    /// <inheritdoc />
    public Task<IEnumerable<T>> SelectProvidersAsync(
        IEnumerable<T> providers, 
        int count, 
        object? context = null, 
        CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToList();
        if (providerList.Count == 0 || count <= 0)
        {
            return Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
        }

        // Filter to healthy providers if performance monitor is available
        if (_performanceMonitor != null)
        {
            providerList = providerList
                .Where(p => _performanceMonitor.IsProviderHealthy(GetProviderId(p)))
                .ToList();
        }

        var selectedProviders = providerList
            .OrderBy(_ => _random.Next())
            .Take(Math.Min(count, providerList.Count))
            .ToList();

        return Task.FromResult<IEnumerable<T>>(selectedProviders);
    }

    private static string GetProviderId(T provider)
    {
        return $"{provider.GetType().Name}_{provider.GetHashCode()}";
    }
}

/// <summary>
/// Configuration options for performance-based provider selection.
/// </summary>
public sealed class PerformanceSelectionOptions
{
    /// <summary>
    /// Gets or sets the target response time in milliseconds for scoring.
    /// </summary>
    public double TargetResponseTimeMs { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets the weight given to response time in scoring (0.0 to 1.0).
    /// </summary>
    public double ResponseTimeWeight { get; set; } = 0.3;

    /// <summary>
    /// Gets or sets the minimum number of operations before applying history bonus.
    /// </summary>
    public long MinOperationsForBonus { get; set; } = 10;

    /// <summary>
    /// Gets or sets the bonus multiplier for providers with sufficient operation history.
    /// </summary>
    public double HistoryBonus { get; set; } = 1.1;

    /// <summary>
    /// Gets or sets the penalty multiplier for providers with recent failures.
    /// </summary>
    public double RecentFailurePenalty { get; set; } = 0.5;
}
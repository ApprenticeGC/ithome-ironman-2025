using System.Collections.Concurrent;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Registry;

/// <summary>
/// Thread-safe implementation of IProviderRegistry that manages providers with hot-swapping support.
/// </summary>
/// <typeparam name="TContract">The contract type that providers must implement.</typeparam>
public class ProviderRegistry<TContract> : IProviderRegistry<TContract>, IAsyncDisposable where TContract : class
{
    private readonly ConcurrentDictionary<string, ProviderEntry> _providers = new();
    private readonly ILogger<ProviderRegistry<TContract>>? _logger;
    private readonly IServiceProvider? _serviceProvider;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderRegistry{TContract}"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for registry operations.</param>
    /// <param name="serviceProvider">Optional service provider for provider dependency injection.</param>
    public ProviderRegistry(ILogger<ProviderRegistry<TContract>>? logger = null, IServiceProvider? serviceProvider = null)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        _logger?.LogDebug("Created provider registry for contract type: {ContractType}", typeof(TContract).Name);
    }

    /// <inheritdoc/>
    public int ProviderCount => _providers.Count;

    /// <inheritdoc/>
    public event EventHandler<ProviderChangedEventArgs<TContract>>? ProviderChanged;

    /// <inheritdoc/>
    public Task RegisterProviderAsync(TContract provider, ProviderMetadata metadata, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(metadata);

        var entry = new ProviderEntry(provider, metadata, DateTime.UtcNow);
        var key = GetProviderKey(metadata.Name, metadata.Version);

        if (!_providers.TryAdd(key, entry))
        {
            throw new InvalidOperationException($"Provider '{metadata.Name}' version '{metadata.Version}' is already registered");
        }

        _logger?.LogInformation("Registered provider: {ProviderName} v{Version} with {CapabilityCount} capabilities",
            metadata.Name, metadata.Version, metadata.Capabilities.Count);

        OnProviderChanged(new ProviderChangedEventArgs<TContract>(provider, metadata, ProviderChangeType.Registered));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> UnregisterProviderAsync(string providerName, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(providerName);

        // Find the provider by name (may have multiple versions)
        var keysToRemove = _providers.Keys.Where(k => k.StartsWith($"{providerName}|")).ToList();
        bool removed = false;

        foreach (var key in keysToRemove)
        {
            if (_providers.TryRemove(key, out var entry))
            {
                _logger?.LogInformation("Unregistered provider: {ProviderName} v{Version}",
                    entry.Metadata.Name, entry.Metadata.Version);

                OnProviderChanged(new ProviderChangedEventArgs<TContract>(entry.Provider, entry.Metadata, ProviderChangeType.Unregistered));
                removed = true;
            }
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc/>
    public Task<TContract?> GetProviderAsync(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_providers.IsEmpty)
        {
            return Task.FromResult<TContract?>(null);
        }

        var eligibleProviders = GetEligibleProviders(criteria);

        if (eligibleProviders.Count == 0)
        {
            _logger?.LogDebug("No providers found matching selection criteria");
            return Task.FromResult<TContract?>(null);
        }

        // Select the best provider based on compatibility score and priority
        var bestProvider = eligibleProviders
            .Select(entry => new { entry, score = CalculateProviderScore(entry, criteria) })
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.entry.Metadata.Priority)
            .ThenByDescending(x => x.entry.Metadata.Version)
            .First().entry;

        _logger?.LogDebug("Selected provider: {ProviderName} v{Version} (Priority: {Priority})",
            bestProvider.Metadata.Name, bestProvider.Metadata.Version, bestProvider.Metadata.Priority);

        return Task.FromResult<TContract?>(bestProvider.Provider);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<TContract>> GetProvidersAsync(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var eligibleProviders = GetEligibleProviders(criteria)
            .OrderByDescending(entry => entry.Metadata.Priority)
            .ThenByDescending(entry => entry.Metadata.Version)
            .Select(entry => entry.Provider)
            .ToList();

        return Task.FromResult<IReadOnlyList<TContract>>(eligibleProviders.AsReadOnly());
    }

    /// <inheritdoc/>
    public Task<ProviderMetadata?> GetProviderMetadataAsync(TContract provider, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(provider);

        var entry = _providers.Values.FirstOrDefault(e => ReferenceEquals(e.Provider, provider));
        return Task.FromResult(entry?.Metadata);
    }

    /// <inheritdoc/>
    public Task<ProviderMetadata?> GetProviderMetadataAsync(string providerName, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrEmpty(providerName);

        // Find the latest version of the provider with the given name
        var entry = _providers.Values
            .Where(e => e.Metadata.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Metadata.Version)
            .FirstOrDefault();

        return Task.FromResult(entry?.Metadata);
    }

    /// <inheritdoc/>
    public Task<bool> SupportsCapabilitiesAsync(IReadOnlySet<string> requiredCapabilities, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(requiredCapabilities);

        if (requiredCapabilities.Count == 0)
        {
            return Task.FromResult(true);
        }

        var hasCapabilities = _providers.Values.Any(entry => 
            requiredCapabilities.All(capability => entry.Metadata.Capabilities.Contains(capability)));

        return Task.FromResult(hasCapabilities);
    }

    /// <inheritdoc/>
    public Task<IReadOnlySet<string>> GetAvailableCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var allCapabilities = _providers.Values
            .SelectMany(entry => entry.Metadata.Capabilities)
            .Distinct()
            .ToHashSet();

        return Task.FromResult<IReadOnlySet<string>>(allCapabilities);
    }

    /// <summary>
    /// Registers providers discovered through automatic discovery.
    /// </summary>
    /// <param name="discoveredProviders">Collection of provider descriptors to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the number of successfully registered providers.</returns>
    public async Task<int> RegisterDiscoveredProvidersAsync(
        IEnumerable<ProviderDescriptor<TContract>> discoveredProviders,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(discoveredProviders);

        int registeredCount = 0;

        foreach (var descriptor in discoveredProviders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var provider = await CreateProviderInstanceAsync(descriptor, cancellationToken);
                if (provider != null)
                {
                    await RegisterProviderAsync(provider, descriptor.Metadata, cancellationToken);
                    registeredCount++;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to register discovered provider: {ProviderName}", 
                    descriptor.Metadata.Name);
            }
        }

        return registeredCount;
    }

    /// <summary>
    /// Creates a provider instance from a descriptor.
    /// </summary>
    /// <param name="descriptor">The provider descriptor.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a provider instance, or null if creation failed.</returns>
    private Task<TContract?> CreateProviderInstanceAsync(
        ProviderDescriptor<TContract> descriptor,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to use service provider for dependency injection
            if (_serviceProvider != null)
            {
                try
                {
                    var instance = _serviceProvider.GetService(descriptor.ImplementationType) as TContract;
                    if (instance != null)
                    {
                        return Task.FromResult<TContract?>(instance);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogDebug(ex, "Failed to create provider instance using service provider for {TypeName}", 
                        descriptor.ImplementationType.Name);
                }
            }

            // Fall back to Activator.CreateInstance
            var providerInstance = Activator.CreateInstance(descriptor.ImplementationType) as TContract;
            return Task.FromResult<TContract?>(providerInstance);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create provider instance for type {TypeName}", 
                descriptor.ImplementationType.Name);
            return Task.FromResult<TContract?>(null);
        }
    }

    /// <summary>
    /// Gets providers that are eligible based on the selection criteria.
    /// </summary>
    /// <param name="criteria">Selection criteria to filter by.</param>
    /// <returns>Collection of eligible provider entries.</returns>
    private List<ProviderEntry> GetEligibleProviders(ProviderSelectionCriteria? criteria)
    {
        var entries = _providers.Values;

        if (criteria == null)
        {
            return entries.ToList();
        }

        return entries.Where(entry => 
            ProviderCompatibilityChecker.CheckCompatibility(entry.Metadata, criteria).IsCompatible)
            .ToList();
    }

    /// <summary>
    /// Calculates a score for a provider based on how well it matches the selection criteria.
    /// </summary>
    /// <param name="entry">The provider entry to score.</param>
    /// <param name="criteria">Selection criteria to score against.</param>
    /// <returns>Score from 0.0 to 1.0 where higher is better.</returns>
    private static double CalculateProviderScore(ProviderEntry entry, ProviderSelectionCriteria? criteria)
    {
        if (criteria == null)
        {
            return 0.5; // Base score when no criteria specified
        }

        return ProviderCompatibilityChecker.CalculateCompatibilityScore(entry.Metadata, criteria);
    }

    /// <summary>
    /// Generates a unique key for a provider based on name and version.
    /// </summary>
    /// <param name="name">Provider name.</param>
    /// <param name="version">Provider version.</param>
    /// <returns>Unique key string.</returns>
    private static string GetProviderKey(string name, Version version)
    {
        return $"{name}|{version}";
    }

    /// <summary>
    /// Raises the ProviderChanged event.
    /// </summary>
    /// <param name="args">Event arguments.</param>
    protected virtual void OnProviderChanged(ProviderChangedEventArgs<TContract> args)
    {
        ProviderChanged?.Invoke(this, args);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose all providers that implement IAsyncDisposable or IDisposable
        foreach (var entry in _providers.Values)
        {
            try
            {
                if (entry.Provider is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (entry.Provider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing provider: {ProviderName}", entry.Metadata.Name);
            }
        }

        _providers.Clear();
        _logger?.LogDebug("Disposed provider registry for contract type: {ContractType}", typeof(TContract).Name);
    }

    /// <summary>
    /// Internal provider entry that holds provider instance and metadata.
    /// </summary>
    /// <param name="Provider">The provider instance.</param>
    /// <param name="Metadata">Provider metadata.</param>
    /// <param name="RegisteredAt">Timestamp when the provider was registered.</param>
    private record ProviderEntry(TContract Provider, ProviderMetadata Metadata, DateTime RegisteredAt);
}
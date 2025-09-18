using System.Collections.Concurrent;

namespace GameConsole.Providers.Registry;

/// <summary>
/// Implementation of a thread-safe provider registry that manages dynamic provider registration, discovery, and selection.
/// </summary>
/// <typeparam name="TContract">The provider contract type.</typeparam>
public class ProviderRegistry<TContract> : IProviderRegistry<TContract> where TContract : class
{
    private readonly ConcurrentDictionary<string, ProviderEntry> _providers = new();
    private readonly object _lock = new();

    /// <summary>
    /// Event raised when a provider is registered, unregistered, or updated.
    /// </summary>
    public event EventHandler<ProviderChangedEventArgs<TContract>>? ProviderChanged;

    /// <summary>
    /// Registers a provider with the specified metadata.
    /// </summary>
    /// <param name="provider">The provider instance to register.</param>
    /// <param name="metadata">The metadata describing the provider.</param>
    public void RegisterProvider(TContract provider, ProviderMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(metadata);

        var entry = new ProviderEntry(provider, metadata);
        var changeType = ProviderChangeType.Registered;

        // Check if updating existing provider
        if (_providers.ContainsKey(metadata.Name))
        {
            changeType = ProviderChangeType.Updated;
        }

        _providers.AddOrUpdate(metadata.Name, entry, (_, _) => entry);

        // Raise event
        ProviderChanged?.Invoke(this, new ProviderChangedEventArgs<TContract>(provider, metadata, changeType));
    }

    /// <summary>
    /// Unregisters a provider by name.
    /// </summary>
    /// <param name="providerName">The name of the provider to unregister.</param>
    /// <returns>True if the provider was found and unregistered, false otherwise.</returns>
    public bool UnregisterProvider(string providerName)
    {
        ArgumentNullException.ThrowIfNull(providerName);

        if (_providers.TryRemove(providerName, out var entry))
        {
            ProviderChanged?.Invoke(this, new ProviderChangedEventArgs<TContract>(entry.Provider, entry.Metadata, ProviderChangeType.Unregistered));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the best provider based on the specified selection criteria.
    /// </summary>
    /// <param name="criteria">The criteria for selecting a provider. If null, returns the highest priority provider.</param>
    /// <returns>The best matching provider, or null if no provider matches the criteria.</returns>
    public TContract? GetProvider(ProviderSelectionCriteria? criteria = null)
    {
        var providers = GetProviders(criteria);
        return providers.FirstOrDefault();
    }

    /// <summary>
    /// Gets all providers that match the specified selection criteria.
    /// </summary>
    /// <param name="criteria">The criteria for selecting providers. If null, returns all providers.</param>
    /// <returns>A list of providers ordered by priority (highest first).</returns>
    public IReadOnlyList<TContract> GetProviders(ProviderSelectionCriteria? criteria = null)
    {
        var allEntries = _providers.Values.ToList();

        if (criteria == null)
        {
            // Return all providers sorted by priority
            return allEntries
                .OrderByDescending(e => e.Metadata.Priority)
                .ThenBy(e => e.Metadata.Name)
                .Select(e => e.Provider)
                .ToList();
        }

        // Filter compatible providers and calculate scores
        var compatibleProviders = new List<(ProviderEntry Entry, int Score)>();

        foreach (var entry in allEntries)
        {
            var score = ProviderCompatibilityChecker.CalculateCompatibilityScore(entry.Metadata, criteria);
            if (score.HasValue)
            {
                compatibleProviders.Add((entry, score.Value));
            }
        }

        // Sort by score (highest first), then by priority, then by name
        return compatibleProviders
            .OrderByDescending(p => p.Score)
            .ThenByDescending(p => p.Entry.Metadata.Priority)
            .ThenBy(p => p.Entry.Metadata.Name)
            .Select(p => p.Entry.Provider)
            .ToList();
    }

    /// <summary>
    /// Checks if any registered provider supports the specified capabilities.
    /// </summary>
    /// <param name="requiredCapabilities">The required capabilities to check for.</param>
    /// <returns>True if at least one provider supports all the required capabilities.</returns>
    public bool SupportsCapabilities(IReadOnlySet<string> requiredCapabilities)
    {
        ArgumentNullException.ThrowIfNull(requiredCapabilities);

        return _providers.Values.Any(entry => 
            ProviderCompatibilityChecker.HasRequiredCapabilities(entry.Metadata.Capabilities, requiredCapabilities));
    }

    /// <summary>
    /// Gets metadata for a specific provider by name.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <returns>The provider metadata, or null if the provider is not found.</returns>
    public ProviderMetadata? GetProviderMetadata(string providerName)
    {
        ArgumentNullException.ThrowIfNull(providerName);

        return _providers.TryGetValue(providerName, out var entry) ? entry.Metadata : null;
    }

    /// <summary>
    /// Gets all registered provider metadata.
    /// </summary>
    /// <returns>A collection of all provider metadata.</returns>
    public IReadOnlyCollection<ProviderMetadata> GetAllProviderMetadata()
    {
        return _providers.Values.Select(e => e.Metadata).ToList();
    }

    /// <summary>
    /// Internal class to hold provider and its metadata together.
    /// </summary>
    private record ProviderEntry(TContract Provider, ProviderMetadata Metadata);
}
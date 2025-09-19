namespace GameConsole.Providers.Registry;

/// <summary>
/// Interface for registering and managing providers of a specific contract type.
/// Supports dynamic provider registration, discovery, and selection based on capabilities and platform requirements.
/// </summary>
/// <typeparam name="TContract">The contract type that providers must implement.</typeparam>
public interface IProviderRegistry<TContract> where TContract : class
{
    /// <summary>
    /// Registers a provider with the registry.
    /// </summary>
    /// <param name="provider">The provider instance to register.</param>
    /// <param name="metadata">Metadata describing the provider's capabilities and requirements.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when provider or metadata is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a provider with the same name and version is already registered.</exception>
    Task RegisterProviderAsync(TContract provider, ProviderMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a provider from the registry.
    /// </summary>
    /// <param name="providerName">The name of the provider to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that represents the async unregister operation and returns true if the provider was found and removed.</returns>
    Task<bool> UnregisterProviderAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the best provider based on selection criteria.
    /// Uses a priority-based selection algorithm considering capabilities, platform compatibility, and version requirements.
    /// </summary>
    /// <param name="criteria">Selection criteria for choosing the provider. If null, returns the highest priority provider.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the best matching provider, or null if none found.</returns>
    Task<TContract?> GetProviderAsync(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all providers matching the specified criteria.
    /// </summary>
    /// <param name="criteria">Selection criteria for filtering providers. If null, returns all providers.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of matching providers.</returns>
    Task<IReadOnlyList<TContract>> GetProvidersAsync(ProviderSelectionCriteria? criteria = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a specific provider.
    /// </summary>
    /// <param name="provider">The provider instance to get metadata for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the provider metadata, or null if not found.</returns>
    Task<ProviderMetadata?> GetProviderMetadataAsync(TContract provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a provider by name.
    /// </summary>
    /// <param name="providerName">The name of the provider to get metadata for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the provider metadata, or null if not found.</returns>
    Task<ProviderMetadata?> GetProviderMetadataAsync(string providerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any registered provider supports the given capabilities.
    /// </summary>
    /// <param name="requiredCapabilities">The capabilities to check for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if any provider supports all required capabilities.</returns>
    Task<bool> SupportsCapabilitiesAsync(IReadOnlySet<string> requiredCapabilities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all capabilities supported by registered providers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a set of all available capabilities.</returns>
    Task<IReadOnlySet<string>> GetAvailableCapabilitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of registered providers.
    /// </summary>
    int ProviderCount { get; }

    /// <summary>
    /// Event fired when a provider is registered, unregistered, or updated.
    /// </summary>
    event EventHandler<ProviderChangedEventArgs<TContract>>? ProviderChanged;
}
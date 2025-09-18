namespace GameConsole.Providers.Registry;

/// <summary>
/// Interface for a provider registry that manages dynamic provider registration, discovery, and selection.
/// </summary>
/// <typeparam name="TContract">The provider contract type.</typeparam>
public interface IProviderRegistry<TContract> where TContract : class
{
    /// <summary>
    /// Registers a provider with the specified metadata.
    /// </summary>
    /// <param name="provider">The provider instance to register.</param>
    /// <param name="metadata">The metadata describing the provider.</param>
    void RegisterProvider(TContract provider, ProviderMetadata metadata);

    /// <summary>
    /// Unregisters a provider by name.
    /// </summary>
    /// <param name="providerName">The name of the provider to unregister.</param>
    /// <returns>True if the provider was found and unregistered, false otherwise.</returns>
    bool UnregisterProvider(string providerName);

    /// <summary>
    /// Gets the best provider based on the specified selection criteria.
    /// </summary>
    /// <param name="criteria">The criteria for selecting a provider. If null, returns the highest priority provider.</param>
    /// <returns>The best matching provider, or null if no provider matches the criteria.</returns>
    TContract? GetProvider(ProviderSelectionCriteria? criteria = null);

    /// <summary>
    /// Gets all providers that match the specified selection criteria.
    /// </summary>
    /// <param name="criteria">The criteria for selecting providers. If null, returns all providers.</param>
    /// <returns>A list of providers ordered by priority (highest first).</returns>
    IReadOnlyList<TContract> GetProviders(ProviderSelectionCriteria? criteria = null);

    /// <summary>
    /// Checks if any registered provider supports the specified capabilities.
    /// </summary>
    /// <param name="requiredCapabilities">The required capabilities to check for.</param>
    /// <returns>True if at least one provider supports all the required capabilities.</returns>
    bool SupportsCapabilities(IReadOnlySet<string> requiredCapabilities);

    /// <summary>
    /// Gets metadata for a specific provider by name.
    /// </summary>
    /// <param name="providerName">The name of the provider.</param>
    /// <returns>The provider metadata, or null if the provider is not found.</returns>
    ProviderMetadata? GetProviderMetadata(string providerName);

    /// <summary>
    /// Gets all registered provider metadata.
    /// </summary>
    /// <returns>A collection of all provider metadata.</returns>
    IReadOnlyCollection<ProviderMetadata> GetAllProviderMetadata();

    /// <summary>
    /// Event raised when a provider is registered, unregistered, or updated.
    /// </summary>
    event EventHandler<ProviderChangedEventArgs<TContract>>? ProviderChanged;
}
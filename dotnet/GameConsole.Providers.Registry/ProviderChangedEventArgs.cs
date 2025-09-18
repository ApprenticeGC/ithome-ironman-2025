namespace GameConsole.Providers.Registry;

/// <summary>
/// Event arguments for provider registration or unregistration events.
/// </summary>
/// <typeparam name="TContract">The provider contract type.</typeparam>
public class ProviderChangedEventArgs<TContract> : EventArgs
    where TContract : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderChangedEventArgs{TContract}"/> class.
    /// </summary>
    /// <param name="provider">The provider that was changed.</param>
    /// <param name="metadata">The metadata of the provider.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    public ProviderChangedEventArgs(TContract provider, ProviderMetadata metadata, ProviderChangeType changeType)
    {
        Provider = provider ?? throw new ArgumentNullException(nameof(provider));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        ChangeType = changeType;
    }

    /// <summary>
    /// Gets the provider that was changed.
    /// </summary>
    public TContract Provider { get; }

    /// <summary>
    /// Gets the metadata of the provider.
    /// </summary>
    public ProviderMetadata Metadata { get; }

    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public ProviderChangeType ChangeType { get; }
}

/// <summary>
/// Defines the types of changes that can occur to providers.
/// </summary>
public enum ProviderChangeType
{
    /// <summary>
    /// A provider was registered.
    /// </summary>
    Registered,

    /// <summary>
    /// A provider was unregistered.
    /// </summary>
    Unregistered,

    /// <summary>
    /// A provider was updated.
    /// </summary>
    Updated
}
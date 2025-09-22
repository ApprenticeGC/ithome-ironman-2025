namespace GameConsole.Configuration.Security;

/// <summary>
/// Default implementation of UI profile configuration.
/// Provides a configurable UI profile that can be used for different UI modes.
/// </summary>
public class UIProfileConfiguration : IUIProfileConfiguration
{
    private readonly Dictionary<string, object> _settings;
    private readonly Dictionary<Type, Type> _providerMappings;

    /// <summary>
    /// Initializes a new instance of the UIProfileConfiguration class.
    /// </summary>
    /// <param name="profileId">The unique identifier for this profile.</param>
    /// <param name="name">The human-readable name of this profile.</param>
    /// <param name="description">The description of what this profile provides.</param>
    /// <param name="mode">The UI mode that this profile implements.</param>
    public UIProfileConfiguration(string profileId, string name, string description, UIMode mode)
    {
        ProfileId = !string.IsNullOrWhiteSpace(profileId) 
            ? profileId 
            : throw (profileId == null ? new ArgumentNullException(nameof(profileId)) : new ArgumentException("Profile ID cannot be empty", nameof(profileId)));
        Name = !string.IsNullOrWhiteSpace(name) 
            ? name 
            : throw (name == null ? new ArgumentNullException(nameof(name)) : new ArgumentException("Name cannot be empty", nameof(name)));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Mode = mode;
        
        _settings = new Dictionary<string, object>();
        _providerMappings = new Dictionary<Type, Type>();
    }

    /// <inheritdoc />
    public string ProfileId { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public UIMode Mode { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Settings => _settings;

    /// <inheritdoc />
    public IReadOnlyDictionary<Type, Type> ProviderMappings => _providerMappings;

    /// <summary>
    /// Sets a configuration setting for this profile.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public void SetSetting(string key, object value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        _settings[key] = value;
    }

    /// <summary>
    /// Gets a configuration setting for this profile.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="key">The setting key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The setting value, or the default value if not found.</returns>
    public T GetSetting<T>(string key, T defaultValue = default!)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        
        if (_settings.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        
        return defaultValue;
    }

    /// <summary>
    /// Maps a capability interface to a specific provider implementation.
    /// </summary>
    /// <param name="capabilityType">The capability interface type.</param>
    /// <param name="providerType">The provider implementation type.</param>
    /// <exception cref="ArgumentNullException">Thrown when either parameter is null.</exception>
    public void MapProvider(Type capabilityType, Type providerType)
    {
        if (capabilityType == null) throw new ArgumentNullException(nameof(capabilityType));
        if (providerType == null) throw new ArgumentNullException(nameof(providerType));
        
        _providerMappings[capabilityType] = providerType;
    }

    /// <summary>
    /// Gets the provider type mapped to a specific capability interface.
    /// </summary>
    /// <param name="capabilityType">The capability interface type.</param>
    /// <returns>The provider type, or null if no mapping exists.</returns>
    public Type? GetProviderType(Type capabilityType)
    {
        if (capabilityType == null) throw new ArgumentNullException(nameof(capabilityType));
        
        return _providerMappings.TryGetValue(capabilityType, out var providerType) ? providerType : null;
    }

    /// <summary>
    /// Removes a configuration setting.
    /// </summary>
    /// <param name="key">The setting key to remove.</param>
    /// <returns>True if the setting was found and removed; false otherwise.</returns>
    public bool RemoveSetting(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        return _settings.Remove(key);
    }

    /// <summary>
    /// Removes a provider mapping.
    /// </summary>
    /// <param name="capabilityType">The capability interface type to remove.</param>
    /// <returns>True if the mapping was found and removed; false otherwise.</returns>
    public bool RemoveProviderMapping(Type capabilityType)
    {
        if (capabilityType == null) throw new ArgumentNullException(nameof(capabilityType));
        return _providerMappings.Remove(capabilityType);
    }
}
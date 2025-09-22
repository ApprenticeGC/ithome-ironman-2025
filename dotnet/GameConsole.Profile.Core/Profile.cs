namespace GameConsole.Profile.Core;

/// <summary>
/// Default implementation of the IProfile interface.
/// </summary>
public sealed class Profile : IProfile
{
    private readonly Dictionary<string, ServiceConfiguration> _serviceConfigurations;

    /// <summary>
    /// Initializes a new instance of the <see cref="Profile"/> class.
    /// </summary>
    /// <param name="id">The unique profile identifier.</param>
    /// <param name="name">The profile name.</param>
    /// <param name="type">The profile type.</param>
    /// <param name="description">The profile description.</param>
    /// <param name="isReadOnly">Whether the profile is read-only.</param>
    /// <param name="version">The profile version.</param>
    public Profile(string id, string name, ProfileType type, string description = "", bool isReadOnly = false, string version = "1.0")
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        Description = description ?? string.Empty;
        IsReadOnly = isReadOnly;
        Version = version ?? "1.0";
        Created = DateTime.UtcNow;
        LastModified = DateTime.UtcNow;
        _serviceConfigurations = new Dictionary<string, ServiceConfiguration>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Profile"/> class for deserialization.
    /// </summary>
    /// <param name="id">The unique profile identifier.</param>
    /// <param name="name">The profile name.</param>
    /// <param name="type">The profile type.</param>
    /// <param name="description">The profile description.</param>
    /// <param name="serviceConfigurations">The service configurations.</param>
    /// <param name="created">The creation timestamp.</param>
    /// <param name="lastModified">The last modified timestamp.</param>
    /// <param name="isReadOnly">Whether the profile is read-only.</param>
    /// <param name="version">The profile version.</param>
    public Profile(string id, string name, ProfileType type, string description, 
        Dictionary<string, ServiceConfiguration> serviceConfigurations, 
        DateTime created, DateTime lastModified, bool isReadOnly = false, string version = "1.0")
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        Description = description ?? string.Empty;
        IsReadOnly = isReadOnly;
        Version = version ?? "1.0";
        Created = created;
        LastModified = lastModified;
        _serviceConfigurations = serviceConfigurations ?? new Dictionary<string, ServiceConfiguration>();
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public ProfileType Type { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ServiceConfiguration> ServiceConfigurations => _serviceConfigurations;

    /// <inheritdoc />
    public DateTime Created { get; }

    /// <inheritdoc />
    public DateTime LastModified { get; private set; }

    /// <inheritdoc />
    public bool IsReadOnly { get; }

    /// <inheritdoc />
    public string Version { get; }

    /// <summary>
    /// Adds or updates a service configuration.
    /// </summary>
    /// <param name="serviceInterface">The service interface name.</param>
    /// <param name="configuration">The service configuration.</param>
    /// <exception cref="InvalidOperationException">Thrown when the profile is read-only.</exception>
    public void SetServiceConfiguration(string serviceInterface, ServiceConfiguration configuration)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot modify a read-only profile.");

        if (string.IsNullOrWhiteSpace(serviceInterface))
            throw new ArgumentException("Service interface cannot be null or empty.", nameof(serviceInterface));

        _serviceConfigurations[serviceInterface] = configuration ?? throw new ArgumentNullException(nameof(configuration));
        LastModified = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes a service configuration.
    /// </summary>
    /// <param name="serviceInterface">The service interface name to remove.</param>
    /// <returns>True if removed, false if not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the profile is read-only.</exception>
    public bool RemoveServiceConfiguration(string serviceInterface)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot modify a read-only profile.");

        if (_serviceConfigurations.Remove(serviceInterface))
        {
            LastModified = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a mutable copy of this profile.
    /// </summary>
    /// <param name="newId">Optional new ID for the copy.</param>
    /// <param name="newName">Optional new name for the copy.</param>
    /// <returns>A mutable copy of the profile.</returns>
    public Profile CreateCopy(string? newId = null, string? newName = null)
    {
        var copiedConfigurations = new Dictionary<string, ServiceConfiguration>();
        foreach (var kvp in _serviceConfigurations)
        {
            copiedConfigurations[kvp.Key] = new ServiceConfiguration
            {
                Implementation = kvp.Value.Implementation,
                Capabilities = new List<string>(kvp.Value.Capabilities),
                Settings = new Dictionary<string, object>(kvp.Value.Settings),
                Lifetime = kvp.Value.Lifetime,
                Enabled = kvp.Value.Enabled
            };
        }

        return new Profile(
            newId ?? $"{Id}_copy_{Guid.NewGuid():N}",
            newName ?? $"{Name} (Copy)",
            Type,
            Description,
            copiedConfigurations,
            Created,
            DateTime.UtcNow,
            isReadOnly: false,
            Version
        );
    }

    /// <summary>
    /// Returns a string representation of the profile.
    /// </summary>
    /// <returns>String representation of the profile.</returns>
    public override string ToString()
    {
        return $"Profile '{Name}' ({Type}) - {ServiceConfigurations.Count} services configured";
    }
}
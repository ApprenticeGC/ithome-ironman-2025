using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Default implementation of IProfileInheritanceManager for managing configuration inheritance.
/// </summary>
public sealed class ProfileInheritanceManager : IProfileInheritanceManager
{
    private readonly ConcurrentDictionary<string, IProfileConfiguration> _profileCache = new();

    /// <summary>
    /// Initializes a new instance of the ProfileInheritanceManager class.
    /// </summary>
    public ProfileInheritanceManager()
    {
    }

    /// <summary>
    /// Initializes a new instance of the ProfileInheritanceManager class with initial profiles.
    /// </summary>
    /// <param name="initialProfiles">Initial profiles to cache.</param>
    public ProfileInheritanceManager(IEnumerable<IProfileConfiguration> initialProfiles)
    {
        if (initialProfiles != null)
        {
            foreach (var profile in initialProfiles)
            {
                _profileCache.TryAdd(profile.Id, profile);
            }
        }
    }

    /// <summary>
    /// Registers a profile configuration for inheritance resolution.
    /// </summary>
    /// <param name="configuration">The profile configuration to register.</param>
    public void RegisterProfile(IProfileConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        
        _profileCache.AddOrUpdate(configuration.Id, configuration, (_, _) => configuration);
    }

    /// <inheritdoc />
    public async Task<IProfileConfiguration> ResolveConfigurationAsync(
        IProfileConfiguration profileConfiguration, 
        CancellationToken cancellationToken = default)
    {
        if (profileConfiguration == null)
            throw new ArgumentNullException(nameof(profileConfiguration));

        // If no parent, return as-is
        if (string.IsNullOrEmpty(profileConfiguration.ParentProfileId))
            return profileConfiguration;

        // Check for circular dependency
        if (await HasCircularDependencyAsync(profileConfiguration.Id, cancellationToken))
            throw new InvalidOperationException($"Circular dependency detected in profile inheritance chain for profile '{profileConfiguration.Id}'.");

        // Get inheritance chain
        var inheritanceChain = await GetInheritanceChainAsync(profileConfiguration.Id, cancellationToken);
        
        // Merge configurations from root to leaf
        var mergedBuilder = new ConfigurationBuilder();
        var mergedMetadata = new Dictionary<string, object>();

        // Start with root configuration (most inherited)
        foreach (var config in inheritanceChain.Reverse())
        {
            // Add configuration sources in order of inheritance
            var configData = new Dictionary<string, string?>();
            foreach (var child in config.Configuration.GetChildren())
            {
                AddConfigurationData(child, string.Empty, configData);
            }
                
            mergedBuilder.AddInMemoryCollection(configData);
            
            // Merge metadata (child overrides parent)
            foreach (var (key, value) in config.Metadata)
            {
                mergedMetadata[key] = value;
            }
        }

        var mergedConfiguration = mergedBuilder.Build();
        
        // Create a new resolved configuration
        return new ProfileConfiguration(
            profileConfiguration.Id,
            profileConfiguration.Name,
            profileConfiguration.Description,
            profileConfiguration.Version,
            profileConfiguration.Environment,
            profileConfiguration.ParentProfileId,
            mergedConfiguration,
            mergedMetadata.AsReadOnly()
        );
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IProfileConfiguration>> GetInheritanceChainAsync(
        string profileId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            throw new ArgumentException("Profile ID cannot be null or empty.", nameof(profileId));

        var chain = new List<IProfileConfiguration>();
        var visitedProfiles = new HashSet<string>();
        var currentProfileId = profileId;

        while (!string.IsNullOrEmpty(currentProfileId))
        {
            // Check for circular dependency
            if (visitedProfiles.Contains(currentProfileId))
                throw new InvalidOperationException($"Circular dependency detected in inheritance chain for profile '{profileId}'.");

            // Get the profile
            if (!_profileCache.TryGetValue(currentProfileId, out var currentProfile))
                throw new InvalidOperationException($"Profile '{currentProfileId}' not found in the inheritance manager.");

            chain.Add(currentProfile);
            visitedProfiles.Add(currentProfileId);
            
            currentProfileId = currentProfile.ParentProfileId;
            
            // Simulate async work
            await Task.Delay(1, cancellationToken);
        }

        return chain.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<bool> CanInheritFromAsync(
        string childProfileId, 
        string parentProfileId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(childProfileId))
            throw new ArgumentException("Child profile ID cannot be null or empty.", nameof(childProfileId));
        
        if (string.IsNullOrWhiteSpace(parentProfileId))
            throw new ArgumentException("Parent profile ID cannot be null or empty.", nameof(parentProfileId));

        // Cannot inherit from self
        if (childProfileId == parentProfileId)
            return false;

        // Check if both profiles exist
        if (!_profileCache.ContainsKey(childProfileId) || !_profileCache.ContainsKey(parentProfileId))
            return false;

        // Check if parent is already in child's inheritance chain (would create circular dependency)
        try
        {
            var childChain = await GetInheritanceChainAsync(childProfileId, cancellationToken);
            return !childChain.Any(p => p.Id == parentProfileId);
        }
        catch (InvalidOperationException)
        {
            // If we can't get the chain, it's not safe to inherit
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> HasCircularDependencyAsync(
        string profileId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(profileId))
            return false;

        try
        {
            await GetInheritanceChainAsync(profileId, cancellationToken);
            return false;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Circular dependency"))
        {
            return true;
        }
    }

    /// <summary>
    /// Clears the profile cache.
    /// </summary>
    public void ClearCache()
    {
        _profileCache.Clear();
    }

    /// <summary>
    /// Gets all registered profile IDs.
    /// </summary>
    /// <returns>A collection of all registered profile IDs.</returns>
    public IEnumerable<string> GetRegisteredProfileIds()
    {
        return _profileCache.Keys.ToList();
    }

    /// <summary>
    /// Recursively adds configuration data from a configuration section.
    /// </summary>
    /// <param name="section">The configuration section to process.</param>
    /// <param name="prefix">The prefix for the current section.</param>
    /// <param name="data">The dictionary to add the data to.</param>
    private static void AddConfigurationData(Microsoft.Extensions.Configuration.IConfigurationSection section, string prefix, Dictionary<string, string?> data)
    {
        var key = string.IsNullOrEmpty(prefix) ? section.Key : $"{prefix}:{section.Key}";
        
        if (section.Value != null)
        {
            data[key] = section.Value;
        }

        foreach (var child in section.GetChildren())
        {
            AddConfigurationData(child, key, data);
        }
    }
}
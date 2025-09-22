using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Manages configuration inheritance between UI profiles.
/// Handles merging configurations from parent profiles with proper override behavior.
/// </summary>
public class ProfileInheritanceManager
{
    private readonly ILogger<ProfileInheritanceManager>? _logger;
    private readonly Dictionary<string, IProfileConfiguration> _profiles = new();

    public ProfileInheritanceManager(ILogger<ProfileInheritanceManager>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a profile configuration for inheritance resolution.
    /// </summary>
    /// <param name="configuration">The profile configuration to register.</param>
    public void RegisterProfile(IProfileConfiguration configuration)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        _profiles[configuration.ProfileId] = configuration;
        _logger?.LogDebug("Registered profile {ProfileId} for inheritance", configuration.ProfileId);
    }

    /// <summary>
    /// Unregisters a profile configuration.
    /// </summary>
    /// <param name="profileId">The profile identifier to unregister.</param>
    public void UnregisterProfile(string profileId)
    {
        if (_profiles.Remove(profileId))
        {
            _logger?.LogDebug("Unregistered profile {ProfileId}", profileId);
        }
    }

    /// <summary>
    /// Resolves the complete configuration for a profile, including inherited settings.
    /// </summary>
    /// <param name="profileId">The profile identifier to resolve.</param>
    /// <param name="strategy">The inheritance strategy to use for conflict resolution.</param>
    /// <returns>A resolved profile configuration with inherited settings merged.</returns>
    /// <exception cref="ArgumentException">Thrown when profile is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when circular dependency is detected.</exception>
    public IProfileConfiguration ResolveInheritance(string profileId, InheritanceStrategy strategy = InheritanceStrategy.ChildOverrides)
    {
        if (!_profiles.TryGetValue(profileId, out var profile))
        {
            throw new ArgumentException($"Profile '{profileId}' not found", nameof(profileId));
        }

        var inheritanceChain = BuildInheritanceChain(profileId);
        
        _logger?.LogDebug("Resolving inheritance for profile {ProfileId} with chain: {Chain}", 
            profileId, string.Join(" -> ", inheritanceChain));

        return MergeConfigurations(inheritanceChain, strategy);
    }

    /// <summary>
    /// Gets the inheritance chain for a profile, from root to child.
    /// </summary>
    /// <param name="profileId">The profile identifier.</param>
    /// <returns>The inheritance chain ordered from root parent to the specified profile.</returns>
    public IReadOnlyList<string> GetInheritanceChain(string profileId)
    {
        return BuildInheritanceChain(profileId);
    }

    /// <summary>
    /// Validates that no circular dependencies exist in the inheritance chain.
    /// </summary>
    /// <param name="profileId">The profile identifier to validate.</param>
    /// <returns>True if no circular dependencies exist, otherwise false.</returns>
    public bool ValidateInheritanceChain(string profileId)
    {
        try
        {
            BuildInheritanceChain(profileId);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all profiles that inherit from the specified parent profile.
    /// </summary>
    /// <param name="parentProfileId">The parent profile identifier.</param>
    /// <returns>Collection of profiles that inherit from the specified parent.</returns>
    public IEnumerable<IProfileConfiguration> GetChildProfiles(string parentProfileId)
    {
        return _profiles.Values.Where(p => p.ParentProfileId == parentProfileId);
    }

    private List<string> BuildInheritanceChain(string profileId)
    {
        var chain = new List<string>();
        var visited = new HashSet<string>();
        var currentId = profileId;

        while (currentId != null)
        {
            if (visited.Contains(currentId))
            {
                throw new InvalidOperationException($"Circular dependency detected in inheritance chain: {string.Join(" -> ", chain)} -> {currentId}");
            }

            if (!_profiles.TryGetValue(currentId, out var profile))
            {
                throw new ArgumentException($"Profile '{currentId}' not found in inheritance chain", nameof(profileId));
            }

            visited.Add(currentId);
            chain.Add(currentId);
            currentId = profile.ParentProfileId;
        }

        // Reverse to get root-to-child order
        chain.Reverse();
        return chain;
    }

    private IProfileConfiguration MergeConfigurations(List<string> inheritanceChain, InheritanceStrategy strategy)
    {
        if (inheritanceChain.Count == 0)
            throw new ArgumentException("Inheritance chain cannot be empty", nameof(inheritanceChain));

        // Start with the root configuration
        var rootProfile = _profiles[inheritanceChain[0]];
        if (inheritanceChain.Count == 1)
            return rootProfile; // No inheritance needed

        // Build merged configuration
        var mergedBuilder = new ConfigurationBuilder();
        var mergedMetadata = new Dictionary<string, object>();

        // Process from root to child
        var finalProfile = rootProfile;
        foreach (var profileId in inheritanceChain)
        {
            var profile = _profiles[profileId];
            finalProfile = profile; // Keep reference to the final (child) profile

            // Add configuration values
            foreach (var kvp in profile.Configuration.AsEnumerable())
            {
                if (kvp.Value != null)
                {
                    mergedBuilder.AddInMemoryCollection(new[] { kvp });
                }
            }

            // Merge metadata based on strategy
            foreach (var kvp in profile.Metadata)
            {
                if (strategy == InheritanceStrategy.ChildOverrides || !mergedMetadata.ContainsKey(kvp.Key))
                {
                    mergedMetadata[kvp.Key] = kvp.Value;
                }
            }
        }

        var mergedConfiguration = mergedBuilder.Build();

        // Create a new merged profile configuration
        return new ProfileConfiguration(
            finalProfile.ProfileId,
            finalProfile.Name,
            finalProfile.Description,
            finalProfile.Version,
            finalProfile.Scope,
            finalProfile.Environment,
            finalProfile.ParentProfileId,
            mergedConfiguration,
            mergedMetadata);
    }
}

/// <summary>
/// Defines strategies for resolving conflicts during configuration inheritance.
/// </summary>
public enum InheritanceStrategy
{
    /// <summary>
    /// Child configuration values override parent values (default behavior).
    /// </summary>
    ChildOverrides,

    /// <summary>
    /// Parent configuration values take precedence over child values.
    /// </summary>
    ParentOverrides,

    /// <summary>
    /// Merge collections and objects where possible, otherwise use child values.
    /// </summary>
    MergeCollections
}
namespace GameConsole.UI.Configuration;

/// <summary>
/// Manages configuration inheritance for UI profiles, providing override capabilities
/// and hierarchical configuration resolution.
/// </summary>
public class ProfileInheritanceManager
{
    private readonly Dictionary<string, IProfileConfiguration> _profiles = [];
    private readonly Dictionary<string, List<string>> _inheritanceGraph = [];

    /// <summary>
    /// Registers a profile configuration for inheritance management.
    /// </summary>
    /// <param name="configuration">The profile configuration to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when configuration is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a profile with the same ID already exists.</exception>
    public void RegisterProfile(IProfileConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (_profiles.ContainsKey(configuration.Id))
            throw new InvalidOperationException($"Profile with ID '{configuration.Id}' is already registered.");

        _profiles[configuration.Id] = configuration;
        
        // Update inheritance graph
        if (!string.IsNullOrEmpty(configuration.InheritsFrom))
        {
            if (!_inheritanceGraph.ContainsKey(configuration.InheritsFrom))
                _inheritanceGraph[configuration.InheritsFrom] = [];
            
            _inheritanceGraph[configuration.InheritsFrom].Add(configuration.Id);
        }
    }

    /// <summary>
    /// Unregisters a profile configuration from inheritance management.
    /// </summary>
    /// <param name="profileId">The ID of the profile to unregister.</param>
    /// <returns>True if the profile was removed, false if it didn't exist.</returns>
    public bool UnregisterProfile(string profileId)
    {
        if (!_profiles.Remove(profileId))
            return false;

        // Clean up inheritance graph
        foreach (var children in _inheritanceGraph.Values)
        {
            children.Remove(profileId);
        }

        _inheritanceGraph.Remove(profileId);
        return true;
    }

    /// <summary>
    /// Resolves a profile configuration with all inherited settings applied.
    /// </summary>
    /// <param name="profileId">The ID of the profile to resolve.</param>
    /// <returns>A resolved configuration with inheritance applied.</returns>
    /// <exception cref="ArgumentException">Thrown when the profile is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when circular inheritance is detected.</exception>
    public IProfileConfiguration ResolveConfiguration(string profileId)
    {
        if (!_profiles.TryGetValue(profileId, out var profile))
            throw new ArgumentException($"Profile '{profileId}' not found.", nameof(profileId));

        var inheritanceChain = BuildInheritanceChain(profileId);
        return MergeConfigurations(inheritanceChain);
    }

    /// <summary>
    /// Gets all profiles that inherit from the specified profile.
    /// </summary>
    /// <param name="profileId">The ID of the parent profile.</param>
    /// <returns>A collection of child profile IDs.</returns>
    public IEnumerable<string> GetChildProfiles(string profileId)
    {
        return _inheritanceGraph.TryGetValue(profileId, out var children) 
            ? children.AsReadOnly() 
            : [];
    }

    /// <summary>
    /// Gets the complete inheritance hierarchy for a profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile.</param>
    /// <returns>The inheritance hierarchy from root to the specified profile.</returns>
    public IEnumerable<string> GetInheritanceChain(string profileId)
    {
        if (!_profiles.ContainsKey(profileId))
            return [];

        return BuildInheritanceChain(profileId).Select(p => p.Id);
    }

    /// <summary>
    /// Validates the inheritance hierarchy for circular dependencies.
    /// </summary>
    /// <returns>A validation result indicating any inheritance issues.</returns>
    public ValidationResult ValidateInheritance()
    {
        var errors = new List<ValidationError>();

        foreach (var profileId in _profiles.Keys)
        {
            try
            {
                BuildInheritanceChain(profileId);
            }
            catch (InvalidOperationException ex)
            {
                errors.Add(new ValidationError
                {
                    Property = "InheritanceChain",
                    Message = ex.Message,
                    CurrentValue = profileId,
                    ErrorCode = "CIRCULAR_INHERITANCE"
                });
            }
        }

        // Check for missing parent profiles
        foreach (var profile in _profiles.Values)
        {
            if (!string.IsNullOrEmpty(profile.InheritsFrom) && !_profiles.ContainsKey(profile.InheritsFrom))
            {
                errors.Add(new ValidationError
                {
                    Property = "InheritsFrom",
                    Message = $"Parent profile '{profile.InheritsFrom}' not found.",
                    CurrentValue = profile.Id,
                    ErrorCode = "MISSING_PARENT"
                });
            }
        }

        return errors.Count > 0 ? ValidationResult.Failure(errors.ToArray()) : ValidationResult.Success();
    }

    /// <summary>
    /// Creates a profile configuration that inherits from multiple parent profiles.
    /// Settings from later parents override earlier ones.
    /// </summary>
    /// <param name="profileId">The ID for the new profile.</param>
    /// <param name="parentIds">The IDs of parent profiles to inherit from.</param>
    /// <param name="additionalSettings">Additional settings to apply.</param>
    /// <returns>A new profile configuration with multiple inheritance applied.</returns>
    public IProfileConfiguration CreateMultiInheritanceProfile(
        string profileId,
        IEnumerable<string> parentIds,
        IReadOnlyDictionary<string, object?>? additionalSettings = null)
    {
        var parentProfiles = parentIds
            .Select(id => _profiles.TryGetValue(id, out var profile) ? profile : null)
            .Where(p => p != null)
            .Cast<IProfileConfiguration>()
            .ToList();

        if (parentProfiles.Count == 0)
            throw new ArgumentException("At least one valid parent profile must be specified.", nameof(parentIds));

        var mergedSettings = new Dictionary<string, object?>();
        var firstParent = parentProfiles[0];

        // Start with settings from first parent
        foreach (var (key, value) in firstParent.Settings)
        {
            mergedSettings[key] = value;
        }

        // Apply settings from additional parents (later ones override)
        foreach (var parent in parentProfiles.Skip(1))
        {
            foreach (var (key, value) in parent.Settings)
            {
                mergedSettings[key] = value;
            }
        }

        // Apply additional settings
        if (additionalSettings != null)
        {
            foreach (var (key, value) in additionalSettings)
            {
                mergedSettings[key] = value;
            }
        }

        return new ProfileConfigurationBuilder()
            .WithId(profileId)
            .WithName($"Multi-inheritance profile from {string.Join(", ", parentProfiles.Select(p => p.Name))}")
            .WithDescription($"Profile inheriting from: {string.Join(", ", parentProfiles.Select(p => p.Id))}")
            .WithVersion(parentProfiles.Max(p => p.Version) ?? new Version(1, 0, 0))
            .WithScope(firstParent.Scope)
            .WithEnvironment(firstParent.Environment)
            .WithSettings(mergedSettings)
            .Build();
    }

    /// <summary>
    /// Gets all registered profile configurations.
    /// </summary>
    /// <returns>A read-only collection of all registered profiles.</returns>
    public IReadOnlyDictionary<string, IProfileConfiguration> GetAllProfiles()
    {
        return _profiles.AsReadOnly();
    }

    private List<IProfileConfiguration> BuildInheritanceChain(string profileId)
    {
        var chain = new List<IProfileConfiguration>();
        var visited = new HashSet<string>();
        var current = profileId;

        while (!string.IsNullOrEmpty(current))
        {
            if (visited.Contains(current))
                throw new InvalidOperationException($"Circular inheritance detected in profile '{current}'.");

            if (!_profiles.TryGetValue(current, out var profile))
                break;

            visited.Add(current);
            chain.Insert(0, profile); // Insert at beginning to maintain inheritance order
            current = profile.InheritsFrom;
        }

        return chain;
    }

    private IProfileConfiguration MergeConfigurations(List<IProfileConfiguration> inheritanceChain)
    {
        if (inheritanceChain.Count == 0)
            throw new ArgumentException("Inheritance chain cannot be empty.", nameof(inheritanceChain));

        if (inheritanceChain.Count == 1)
            return inheritanceChain[0];

        var mergedSettings = new Dictionary<string, object?>();
        var targetProfile = inheritanceChain[^1]; // Last profile in chain (the target)

        // Apply settings from inheritance chain (earlier profiles first)
        foreach (var profile in inheritanceChain)
        {
            foreach (var (key, value) in profile.Settings)
            {
                mergedSettings[key] = value;
            }
        }

        return new ProfileConfigurationBuilder()
            .WithId(targetProfile.Id)
            .WithName(targetProfile.Name)
            .WithDescription(targetProfile.Description)
            .WithVersion(targetProfile.Version)
            .WithScope(targetProfile.Scope)
            .WithEnvironment(targetProfile.Environment)
            .InheritsFrom(targetProfile.InheritsFrom)
            .WithSettings(mergedSettings)
            .Build();
    }
}
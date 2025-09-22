namespace GameConsole.UI.Profiles;

/// <summary>
/// Validates UI profiles for consistency, compatibility, and completeness.
/// Ensures profile integrity and prevents invalid profile configurations.
/// </summary>
public sealed class ProfileValidator
{
    /// <summary>
    /// Validates a UI profile for consistency and compatibility.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="context">The UI context to validate against.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A validation result indicating whether the profile is valid.</returns>
    public async Task<ProfileValidationResult> ValidateProfileAsync(
        IUIProfile profile, 
        IUIContext context, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(context);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Validate basic profile properties
        ValidateBasicProperties(profile, errors);

        // Validate metadata and compatibility
        ValidateMetadata(profile.Metadata, errors, warnings);

        // Validate command set
        await ValidateCommandSetAsync(profile.GetCommandSet(), errors, warnings, cancellationToken);

        // Validate layout configuration
        ValidateLayoutConfiguration(profile.GetLayoutConfiguration(), errors, warnings);

        // Validate key bindings
        ValidateKeyBindings(profile.GetKeyBindings(), errors, warnings);

        // Validate inheritance chain if applicable
        await ValidateInheritanceChainAsync(profile, errors, warnings, cancellationToken);

        // Check context compatibility
        ValidateContextCompatibility(profile, context, errors, warnings);

        return new ProfileValidationResult(
            errors.Count == 0,
            errors,
            warnings);
    }

    /// <summary>
    /// Validates a collection of profiles for conflicts and consistency.
    /// </summary>
    /// <param name="profiles">The profiles to validate as a group.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A validation result for the entire profile collection.</returns>
    public async Task<ProfileValidationResult> ValidateProfileCollectionAsync(
        IEnumerable<IUIProfile> profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var errors = new List<string>();
        var warnings = new List<string>();
        var profileList = profiles.ToList();

        // Check for duplicate profile names
        var profileNames = profileList.Select(p => p.Name).ToList();
        var duplicateNames = profileNames.GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateName in duplicateNames)
        {
            errors.Add($"Duplicate profile name found: {duplicateName}");
        }

        // Check for conflicting key bindings between profiles of the same mode
        var profilesByMode = profileList.GroupBy(p => p.TargetMode);
        foreach (var modeGroup in profilesByMode)
        {
            ValidateKeyBindingConflicts(modeGroup.ToList(), errors, warnings);
        }

        await Task.CompletedTask;

        return new ProfileValidationResult(
            errors.Count == 0,
            errors,
            warnings);
    }

    private static void ValidateBasicProperties(IUIProfile profile, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            errors.Add("Profile name cannot be null or empty");
        }

        if (profile.Metadata == null)
        {
            errors.Add("Profile metadata cannot be null");
        }
    }

    private static void ValidateMetadata(UIProfileMetadata metadata, List<string> errors, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(metadata.Version))
        {
            warnings.Add("Profile version is not specified");
        }

        if (string.IsNullOrWhiteSpace(metadata.DisplayName))
        {
            warnings.Add("Profile display name is not specified");
        }

        // Validate version format (basic semantic version check)
        if (!string.IsNullOrWhiteSpace(metadata.Version))
        {
            var versionParts = metadata.Version.Split('.');
            if (versionParts.Length < 2 || versionParts.Length > 4)
            {
                warnings.Add($"Profile version '{metadata.Version}' does not follow semantic versioning format");
            }
        }

        // Validate compatibility requirements format
        foreach (var requirement in metadata.CompatibilityRequirements)
        {
            if (string.IsNullOrWhiteSpace(requirement.Key) || string.IsNullOrWhiteSpace(requirement.Value))
            {
                warnings.Add("Compatibility requirement has empty key or value");
            }
        }
    }

    private static async Task ValidateCommandSetAsync(CommandSet commandSet, List<string> errors, List<string> warnings, CancellationToken cancellationToken)
    {
        if (commandSet == null)
        {
            errors.Add("Command set cannot be null");
            return;
        }

        if (!commandSet.AvailableCommands.Any())
        {
            warnings.Add("Profile has no available commands");
        }

        // Validate that all commands have proper definitions
        foreach (var commandName in commandSet.AvailableCommands)
        {
            var command = commandSet.GetCommand(commandName);
            if (command == null)
            {
                errors.Add($"Command '{commandName}' has null definition");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(command.Name))
                {
                    errors.Add($"Command '{commandName}' has empty name");
                }

                if (string.IsNullOrWhiteSpace(command.Description))
                {
                    warnings.Add($"Command '{commandName}' has no description");
                }
            }
        }

        await Task.CompletedTask;
    }

    private static void ValidateLayoutConfiguration(LayoutConfiguration layoutConfig, List<string> errors, List<string> warnings)
    {
        if (layoutConfig == null)
        {
            errors.Add("Layout configuration cannot be null");
            return;
        }

        // Validate main window configuration
        if (layoutConfig.MainWindow != null)
        {
            if (layoutConfig.MainWindow.Width <= 0 || layoutConfig.MainWindow.Height <= 0)
            {
                warnings.Add("Main window has invalid dimensions");
            }
        }

        // Validate panel configurations
        if (layoutConfig.Panels != null)
        {
            foreach (var panel in layoutConfig.Panels)
            {
                if (string.IsNullOrWhiteSpace(panel.Key))
                {
                    warnings.Add("Panel configuration has empty key");
                }

                if (panel.Value?.Size != null)
                {
                    if (panel.Value.Size.Width <= 0 || panel.Value.Size.Height <= 0)
                    {
                        warnings.Add($"Panel '{panel.Key}' has invalid size");
                    }
                }
            }
        }
    }

    private static void ValidateKeyBindings(KeyBindingSet keyBindings, List<string> errors, List<string> warnings)
    {
        if (keyBindings == null)
        {
            warnings.Add("Key bindings set is null");
            return;
        }

        foreach (var binding in keyBindings.Bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.Key))
            {
                errors.Add("Key binding has empty key combination");
            }

            if (string.IsNullOrWhiteSpace(binding.Value))
            {
                errors.Add($"Key binding '{binding.Key}' has empty command");
            }
        }
    }

    private static async Task ValidateInheritanceChainAsync(IUIProfile profile, List<string> errors, List<string> warnings, CancellationToken cancellationToken)
    {
        var inheritanceChain = profile.Metadata.InheritsFrom;
        if (!inheritanceChain.Any())
        {
            return;
        }

        // Check for circular inheritance
        var visited = new HashSet<string> { profile.Name };
        foreach (var parentName in inheritanceChain)
        {
            if (visited.Contains(parentName))
            {
                errors.Add($"Circular inheritance detected: {profile.Name} -> {parentName}");
            }
            visited.Add(parentName);
        }

        await Task.CompletedTask;
    }

    private static void ValidateContextCompatibility(IUIProfile profile, IUIContext context, List<string> errors, List<string> warnings)
    {
        // Validate that the profile's target mode matches the context's current mode
        if (profile.TargetMode != context.CurrentMode)
        {
            warnings.Add($"Profile target mode ({profile.TargetMode}) does not match context mode ({context.CurrentMode})");
        }

        // Validate compatibility requirements against context properties
        foreach (var requirement in profile.Metadata.CompatibilityRequirements)
        {
            if (context.Properties.TryGetValue(requirement.Key, out var contextValue))
            {
                var contextValueString = contextValue?.ToString() ?? string.Empty;
                if (!string.Equals(contextValueString, requirement.Value, StringComparison.OrdinalIgnoreCase))
                {
                    warnings.Add($"Compatibility requirement '{requirement.Key}' not met. Expected: {requirement.Value}, Actual: {contextValueString}");
                }
            }
            else
            {
                warnings.Add($"Context does not provide required property: {requirement.Key}");
            }
        }
    }

    private static void ValidateKeyBindingConflicts(List<IUIProfile> profilesInSameMode, List<string> errors, List<string> warnings)
    {
        var allKeyBindings = new Dictionary<string, List<string>>();

        foreach (var profile in profilesInSameMode)
        {
            var keyBindings = profile.GetKeyBindings();
            foreach (var binding in keyBindings.Bindings)
            {
                if (!allKeyBindings.ContainsKey(binding.Key))
                {
                    allKeyBindings[binding.Key] = new List<string>();
                }
                allKeyBindings[binding.Key].Add(profile.Name);
            }
        }

        foreach (var keyBinding in allKeyBindings.Where(kb => kb.Value.Count > 1))
        {
            warnings.Add($"Key binding conflict for '{keyBinding.Key}' in profiles: {string.Join(", ", keyBinding.Value)}");
        }
    }
}

/// <summary>
/// Represents the result of a profile validation operation.
/// </summary>
public sealed class ProfileValidationResult
{
    /// <summary>
    /// Initializes a new instance of the ProfileValidationResult class.
    /// </summary>
    /// <param name="isValid">Whether the validation passed.</param>
    /// <param name="errors">Collection of validation errors.</param>
    /// <param name="warnings">Collection of validation warnings.</param>
    public ProfileValidationResult(bool isValid, IEnumerable<string> errors, IEnumerable<string> warnings)
    {
        IsValid = isValid;
        Errors = errors.ToList();
        Warnings = warnings.ToList();
    }

    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Gets the collection of validation warnings.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }
}
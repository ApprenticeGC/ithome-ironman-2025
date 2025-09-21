using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Validates UI profiles for consistency, compatibility, and completeness.
/// </summary>
public class ProfileValidator
{
    private readonly ILogger _logger;

    public ProfileValidator(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates a profile against the given context.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="context">The UI context to validate against.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Validation result with details about any issues found.</returns>
    public async Task<ProfileValidationResult> ValidateAsync(IUIProfile profile, UIContext context, CancellationToken cancellationToken = default)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        if (context == null) throw new ArgumentNullException(nameof(context));

        _logger.LogDebug("Validating profile {ProfileId} against context", profile.Id);

        var errors = new List<string>();
        var warnings = new List<string>();
        var details = new Dictionary<string, object>();

        try
        {
            // Basic profile validation
            ValidateBasicProperties(profile, errors, warnings);
            
            // Context compatibility validation
            await ValidateContextCompatibilityAsync(profile, context, errors, warnings, cancellationToken);
            
            // Command set validation
            ValidateCommandSet(profile, errors, warnings);
            
            // Layout configuration validation
            ValidateLayoutConfiguration(profile, errors, warnings);
            
            // Capability validation
            ValidateCapabilities(profile, context, errors, warnings);
            
            // Metadata validation
            ValidateMetadata(profile, errors, warnings);

            // Dependency validation
            ValidateDependencies(profile, errors, warnings);

            details["validation-timestamp"] = DateTime.UtcNow;
            details["profile-id"] = profile.Id;
            details["context-platform"] = context.Platform;

            var result = new ProfileValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                Details = details
            };

            _logger.LogDebug("Profile {ProfileId} validation completed: {IsValid} (errors: {ErrorCount}, warnings: {WarningCount})",
                profile.Id, result.IsValid, errors.Count, warnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during profile validation for {ProfileId}", profile.Id);
            
            return new ProfileValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Validation failed due to exception: {ex.Message}" },
                Details = new Dictionary<string, object>
                {
                    ["validation-timestamp"] = DateTime.UtcNow,
                    ["validation-error"] = ex.ToString()
                }
            };
        }
    }

    /// <summary>
    /// Validates multiple profiles for consistency and conflicts.
    /// </summary>
    /// <param name="profiles">The profiles to validate together.</param>
    /// <param name="context">The UI context to validate against.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collective validation result.</returns>
    public async Task<CollectiveValidationResult> ValidateMultipleAsync(
        IEnumerable<IUIProfile> profiles, 
        UIContext context, 
        CancellationToken cancellationToken = default)
    {
        var profileList = profiles?.ToList() ?? throw new ArgumentNullException(nameof(profiles));
        if (!profileList.Any())
        {
            return new CollectiveValidationResult
            {
                IsValid = true,
                ProfileResults = new Dictionary<string, ProfileValidationResult>(),
                GlobalErrors = Array.Empty<string>(),
                GlobalWarnings = Array.Empty<string>()
            };
        }

        _logger.LogDebug("Validating {Count} profiles collectively", profileList.Count);

        var results = new Dictionary<string, ProfileValidationResult>();
        var globalErrors = new List<string>();
        var globalWarnings = new List<string>();

        // Validate each profile individually
        foreach (var profile in profileList)
        {
            var result = await ValidateAsync(profile, context, cancellationToken);
            results[profile.Id] = result;
        }

        // Check for conflicts between profiles
        ValidateProfileConflicts(profileList, globalErrors, globalWarnings);

        // Check for duplicate profile IDs
        ValidateDuplicateIds(profileList, globalErrors);

        // Check for circular dependencies
        ValidateCircularDependencies(profileList, globalErrors);

        var collectiveResult = new CollectiveValidationResult
        {
            IsValid = results.Values.All(r => r.IsValid) && globalErrors.Count == 0,
            ProfileResults = results,
            GlobalErrors = globalErrors,
            GlobalWarnings = globalWarnings
        };

        _logger.LogDebug("Collective validation completed: {IsValid} (global errors: {ErrorCount}, global warnings: {WarningCount})",
            collectiveResult.IsValid, globalErrors.Count, globalWarnings.Count);

        return collectiveResult;
    }

    private void ValidateBasicProperties(IUIProfile profile, List<string> errors, List<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            errors.Add("Profile ID cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            errors.Add("Profile name cannot be null or empty");
        }

        if (profile.Id != null && profile.Id.Contains(' '))
        {
            warnings.Add("Profile ID contains spaces, which may cause issues");
        }
    }

    private async Task ValidateContextCompatibilityAsync(IUIProfile profile, UIContext context, List<string> errors, List<string> warnings, CancellationToken cancellationToken)
    {
        try
        {
            var canActivate = await profile.CanActivateAsync(context, cancellationToken);
            if (!canActivate)
            {
                errors.Add($"Profile cannot be activated in the current context");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error checking profile activation compatibility: {ex.Message}");
        }

        // Check specific compatibility requirements
        var capabilities = profile.GetSupportedCapabilities();
        
        if (capabilities.HasFlag(UICapabilities.GraphicalElements) && !context.Display.HasGraphicalDisplay)
        {
            errors.Add("Profile requires graphical display but none is available");
        }

        if (capabilities.HasFlag(UICapabilities.NetworkAccess) && !context.Runtime.HasNetworkAccess)
        {
            warnings.Add("Profile requires network access but it may not be available");
        }

        if (capabilities.HasFlag(UICapabilities.FileSystemAccess) && context.Runtime.IsContainer)
        {
            warnings.Add("Profile requires file system access but running in container environment");
        }
    }

    private void ValidateCommandSet(IUIProfile profile, List<string> errors, List<string> warnings)
    {
        try
        {
            var commandSet = profile.GetCommandSet();
            
            if (commandSet.Commands.Count == 0)
            {
                warnings.Add("Profile has no commands defined");
            }

            // Check for duplicate command IDs
            var commandIds = commandSet.Commands.Select(c => c.Id).ToList();
            var duplicateIds = commandIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);
            
            foreach (var duplicateId in duplicateIds)
            {
                errors.Add($"Duplicate command ID found: {duplicateId}");
            }

            // Validate keyboard shortcuts
            foreach (var shortcut in commandSet.Shortcuts)
            {
                if (string.IsNullOrWhiteSpace(shortcut.Key))
                {
                    errors.Add("Empty keyboard shortcut key found");
                }
                
                if (string.IsNullOrWhiteSpace(shortcut.Value))
                {
                    errors.Add($"Empty command mapping for shortcut '{shortcut.Key}'");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error validating command set: {ex.Message}");
        }
    }

    private void ValidateLayoutConfiguration(IUIProfile profile, List<string> errors, List<string> warnings)
    {
        try
        {
            var layout = profile.GetLayoutConfiguration();
            
            if (string.IsNullOrWhiteSpace(layout.LayoutType))
            {
                errors.Add("Layout type cannot be null or empty");
            }

            // Validate panel configurations
            var panelIds = layout.Panels.Select(p => p.Id).ToList();
            var duplicatePanelIds = panelIds.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key);
            
            foreach (var duplicateId in duplicatePanelIds)
            {
                errors.Add($"Duplicate panel ID found: {duplicateId}");
            }

            // Validate panel bounds
            foreach (var panel in layout.Panels)
            {
                if (panel.Bounds.Width <= 0 || panel.Bounds.Height <= 0)
                {
                    errors.Add($"Panel '{panel.Id}' has invalid dimensions");
                }
                
                if (panel.Bounds.X < 0 || panel.Bounds.Y < 0)
                {
                    warnings.Add($"Panel '{panel.Id}' has negative position coordinates");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error validating layout configuration: {ex.Message}");
        }
    }

    private void ValidateCapabilities(IUIProfile profile, UIContext context, List<string> errors, List<string> warnings)
    {
        try
        {
            var capabilities = profile.GetSupportedCapabilities();
            
            // Check for contradictory capabilities
            if (profile.TargetMode == UIMode.Console && capabilities.HasFlag(UICapabilities.GraphicalElements))
            {
                warnings.Add("Console profile declaring graphical element capability may be inconsistent");
            }

            // Check resource requirements vs availability
            if (capabilities.HasFlag(UICapabilities.VideoPlayback) || capabilities.HasFlag(UICapabilities.AudioOutput))
            {
                if (context.Runtime.Resources.AvailableMemoryMB < 512)
                {
                    warnings.Add("Profile requires multimedia capabilities but available memory is low");
                }
            }

            // Check for unused capabilities
            if (capabilities == UICapabilities.None)
            {
                warnings.Add("Profile declares no capabilities, which may indicate an incomplete implementation");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error validating capabilities: {ex.Message}");
        }
    }

    private void ValidateMetadata(IUIProfile profile, List<string> errors, List<string> warnings)
    {
        var metadata = profile.Metadata;
        
        if (string.IsNullOrWhiteSpace(metadata.Version))
        {
            warnings.Add("Profile metadata has no version information");
        }
        else if (!IsValidVersion(metadata.Version))
        {
            warnings.Add($"Profile metadata version '{metadata.Version}' is not a valid semantic version");
        }

        if (string.IsNullOrWhiteSpace(metadata.Description))
        {
            warnings.Add("Profile metadata has no description");
        }

        if (metadata.Priority < 0 || metadata.Priority > 1000)
        {
            warnings.Add($"Profile priority {metadata.Priority} is outside recommended range (0-1000)");
        }
    }

    private void ValidateDependencies(IUIProfile profile, List<string> errors, List<string> warnings)
    {
        var dependencies = profile.Metadata.Dependencies;
        
        foreach (var dependency in dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency))
            {
                errors.Add("Empty dependency reference found in profile metadata");
            }
        }

        // Check for self-dependency
        if (dependencies.Contains(profile.Id))
        {
            errors.Add("Profile cannot depend on itself");
        }
    }

    private void ValidateProfileConflicts(List<IUIProfile> profiles, List<string> globalErrors, List<string> globalWarnings)
    {
        // Check for profiles with same target mode and high priority that might conflict
        var modeGroups = profiles.GroupBy(p => p.TargetMode);
        
        foreach (var group in modeGroups)
        {
            var highPriorityProfiles = group.Where(p => p.Metadata.Priority >= 90).ToList();
            if (highPriorityProfiles.Count > 1)
            {
                var profileNames = string.Join(", ", highPriorityProfiles.Select(p => p.Id));
                globalWarnings.Add($"Multiple high-priority profiles for mode {group.Key}: {profileNames}");
            }
        }
    }

    private void ValidateDuplicateIds(List<IUIProfile> profiles, List<string> globalErrors)
    {
        var duplicateIds = profiles.GroupBy(p => p.Id).Where(g => g.Count() > 1).Select(g => g.Key);
        
        foreach (var duplicateId in duplicateIds)
        {
            globalErrors.Add($"Multiple profiles with same ID: {duplicateId}");
        }
    }

    private void ValidateCircularDependencies(List<IUIProfile> profiles, List<string> globalErrors)
    {
        var profileMap = profiles.ToDictionary(p => p.Id, p => p);
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var profile in profiles)
        {
            if (HasCircularDependency(profile.Id, profileMap, visited, recursionStack))
            {
                globalErrors.Add($"Circular dependency detected involving profile: {profile.Id}");
                break; // One circular dependency error is enough
            }
        }
    }

    private bool HasCircularDependency(string profileId, Dictionary<string, IUIProfile> profileMap, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(profileId))
        {
            return true; // Circular dependency found
        }

        if (visited.Contains(profileId))
        {
            return false; // Already processed
        }

        visited.Add(profileId);
        recursionStack.Add(profileId);

        if (profileMap.TryGetValue(profileId, out var profile))
        {
            foreach (var dependency in profile.Metadata.Dependencies)
            {
                if (HasCircularDependency(dependency, profileMap, visited, recursionStack))
                {
                    return true;
                }
            }
        }

        recursionStack.Remove(profileId);
        return false;
    }

    private bool IsValidVersion(string version)
    {
        // Simple semantic version validation (major.minor.patch)
        var parts = version.Split('.');
        return parts.Length >= 2 && parts.Length <= 3 && 
               parts.All(part => int.TryParse(part, out _));
    }
}

/// <summary>
/// Result of validating multiple profiles together.
/// </summary>
public record CollectiveValidationResult
{
    /// <summary>
    /// Whether all profiles passed validation.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Individual validation results for each profile.
    /// </summary>
    public IReadOnlyDictionary<string, ProfileValidationResult> ProfileResults { get; init; } = new Dictionary<string, ProfileValidationResult>();

    /// <summary>
    /// Global errors that affect multiple profiles.
    /// </summary>
    public IReadOnlyList<string> GlobalErrors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Global warnings that affect multiple profiles.
    /// </summary>
    public IReadOnlyList<string> GlobalWarnings { get; init; } = Array.Empty<string>();
}
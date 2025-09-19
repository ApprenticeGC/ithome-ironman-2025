using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Validates UI profiles for consistency, completeness, and compatibility.
/// Provides comprehensive validation rules and detailed error reporting.
/// </summary>
public class ProfileValidator
{
    private readonly ILogger _logger;
    private readonly List<IProfileValidationRule> _validationRules;

    public ProfileValidator(ILogger<ProfileValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validationRules = InitializeValidationRules();
    }

    /// <summary>
    /// Validates a single profile against all validation rules.
    /// </summary>
    /// <param name="profile">Profile to validate.</param>
    /// <returns>Detailed validation result.</returns>
    public ProfileValidationResult ValidateProfile(IUIProfile profile)
    {
        if (profile == null)
            throw new ArgumentNullException(nameof(profile));

        var errors = new List<string>();
        var warnings = new List<string>();

        _logger.LogDebug("Validating profile '{ProfileName}'", profile.Name);

        foreach (var rule in _validationRules)
        {
            try
            {
                var result = rule.Validate(profile);
                errors.AddRange(result.Errors);
                warnings.AddRange(result.Warnings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation rule '{RuleName}' threw exception for profile '{ProfileName}'",
                    rule.GetType().Name, profile.Name);
                errors.Add($"Validation rule {rule.GetType().Name} failed: {ex.Message}");
            }
        }

        var isValid = errors.Count == 0;
        
        _logger.LogDebug("Profile '{ProfileName}' validation completed: {IsValid} (Errors: {ErrorCount}, Warnings: {WarningCount})",
            profile.Name, isValid, errors.Count, warnings.Count);

        return isValid 
            ? ProfileValidationResult.Success() 
            : ProfileValidationResult.Failed(errors, warnings);
    }

    /// <summary>
    /// Validates multiple profiles and checks for conflicts between them.
    /// </summary>
    /// <param name="profiles">Profiles to validate.</param>
    /// <returns>Dictionary of validation results by profile name.</returns>
    public Dictionary<string, ProfileValidationResult> ValidateProfiles(IEnumerable<IUIProfile> profiles)
    {
        if (profiles == null)
            throw new ArgumentNullException(nameof(profiles));

        var profileArray = profiles.ToArray();
        var results = new Dictionary<string, ProfileValidationResult>();

        // Validate individual profiles
        foreach (var profile in profileArray)
        {
            results[profile.Name] = ValidateProfile(profile);
        }

        // Cross-profile validation
        var crossValidationErrors = ValidateCrossProfileRules(profileArray);
        
        // Add cross-validation errors to affected profiles
        foreach (var (profileName, errors) in crossValidationErrors)
        {
            if (results.ContainsKey(profileName))
            {
                var existingResult = results[profileName];
                var allErrors = existingResult.Errors.Concat(errors).ToArray();
                results[profileName] = existingResult.IsValid && errors.Count == 0
                    ? existingResult
                    : ProfileValidationResult.Failed(allErrors, existingResult.Warnings);
            }
        }

        return results;
    }

    /// <summary>
    /// Validates compatibility between two profiles for potential transitions.
    /// </summary>
    /// <param name="sourceProfile">Source profile.</param>
    /// <param name="targetProfile">Target profile.</param>
    /// <returns>Compatibility validation result.</returns>
    public ProfileCompatibilityResult ValidateCompatibility(IUIProfile sourceProfile, IUIProfile targetProfile)
    {
        if (sourceProfile == null)
            throw new ArgumentNullException(nameof(sourceProfile));
        if (targetProfile == null)
            throw new ArgumentNullException(nameof(targetProfile));

        var issues = new List<string>();
        var warnings = new List<string>();

        // Check mode compatibility
        if (sourceProfile.TargetMode != targetProfile.TargetMode)
        {
            warnings.Add($"Profile modes differ: {sourceProfile.TargetMode} -> {targetProfile.TargetMode}");
        }

        // Check service provider compatibility
        var sourceServices = sourceProfile.GetServiceProviderConfiguration();
        var targetServices = targetProfile.GetServiceProviderConfiguration();

        foreach (var (serviceType, sourceProvider) in sourceServices)
        {
            if (targetServices.TryGetValue(serviceType, out var targetProvider))
            {
                if (sourceProvider != targetProvider)
                {
                    warnings.Add($"Service provider change for {serviceType}: {sourceProvider} -> {targetProvider}");
                }
            }
            else
            {
                warnings.Add($"Service {serviceType} not configured in target profile");
            }
        }

        // Check layout compatibility
        var sourceLayout = sourceProfile.GetLayoutConfiguration();
        var targetLayout = targetProfile.GetLayoutConfiguration();

        if (!AreLayoutsCompatible(sourceLayout, targetLayout))
        {
            warnings.Add("Layout configurations have significant differences that may affect user experience");
        }

        return new ProfileCompatibilityResult
        {
            IsCompatible = issues.Count == 0,
            Issues = issues.AsReadOnly(),
            Warnings = warnings.AsReadOnly()
        };
    }

    private List<IProfileValidationRule> InitializeValidationRules()
    {
        return new List<IProfileValidationRule>
        {
            new ProfileNameValidationRule(),
            new CommandSetValidationRule(),
            new LayoutValidationRule(),
            new ServiceProviderValidationRule(),
            new MetadataValidationRule()
        };
    }

    private Dictionary<string, List<string>> ValidateCrossProfileRules(IUIProfile[] profiles)
    {
        var errors = new Dictionary<string, List<string>>();

        // Check for duplicate profile names (should not happen if ProfileManager is working correctly)
        var nameGroups = profiles.GroupBy(p => p.Name).Where(g => g.Count() > 1);
        foreach (var group in nameGroups)
        {
            foreach (var profile in group)
            {
                if (!errors.ContainsKey(profile.Name))
                    errors[profile.Name] = new List<string>();
                errors[profile.Name].Add($"Duplicate profile name: {profile.Name}");
            }
        }

        // Check for conflicting service configurations within the same mode
        var modeGroups = profiles.GroupBy(p => p.TargetMode);
        foreach (var modeGroup in modeGroups)
        {
            var modeProfiles = modeGroup.ToArray();
            if (modeProfiles.Length > 1)
            {
                // This is normal - multiple profiles can target the same mode
                // But we could add warnings for potentially conflicting configurations
            }
        }

        return errors;
    }

    private bool AreLayoutsCompatible(LayoutConfiguration source, LayoutConfiguration target)
    {
        // Simple heuristic for layout compatibility
        // In a real implementation, this would be much more sophisticated

        var sourcePanelNames = source.Panels.Select(p => p.Name).ToHashSet();
        var targetPanelNames = target.Panels.Select(p => p.Name).ToHashSet();

        // If more than 50% of panels are different, consider incompatible
        var commonPanels = sourcePanelNames.Intersect(targetPanelNames).Count();
        var totalUniquePanels = sourcePanelNames.Union(targetPanelNames).Count();

        return commonPanels >= totalUniquePanels * 0.5;
    }
}

/// <summary>
/// Interface for profile validation rules.
/// </summary>
public interface IProfileValidationRule
{
    /// <summary>
    /// Validates a profile according to this rule.
    /// </summary>
    /// <param name="profile">Profile to validate.</param>
    /// <returns>Validation result for this rule.</returns>
    ProfileValidationResult Validate(IUIProfile profile);
}

/// <summary>
/// Validates profile name requirements.
/// </summary>
public class ProfileNameValidationRule : IProfileValidationRule
{
    public ProfileValidationResult Validate(IUIProfile profile)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.Name))
            errors.Add("Profile name cannot be empty or whitespace");

        if (profile.Name.Length > 100)
            errors.Add("Profile name cannot exceed 100 characters");

        if (profile.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            errors.Add("Profile name contains invalid characters");

        return errors.Count == 0 
            ? ProfileValidationResult.Success() 
            : ProfileValidationResult.Failed(errors);
    }
}

/// <summary>
/// Validates command set requirements.
/// </summary>
public class CommandSetValidationRule : IProfileValidationRule
{
    public ProfileValidationResult Validate(IUIProfile profile)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var commandSet = profile.GetCommandSet();

            if (commandSet.Commands.Count == 0)
                warnings.Add("Profile has no commands defined");

            // Check for required commands
            var requiredCommands = new[] { "help", "exit" };
            foreach (var requiredCommand in requiredCommands)
            {
                if (!commandSet.HasCommand(requiredCommand))
                    warnings.Add($"Profile missing recommended command: {requiredCommand}");
            }

            // Check for duplicate keyboard shortcuts
            var shortcuts = new Dictionary<string, List<string>>();
            foreach (var (commandName, command) in commandSet.Commands)
            {
                foreach (var shortcut in command.KeyboardShortcuts)
                {
                    if (!shortcuts.ContainsKey(shortcut))
                        shortcuts[shortcut] = new List<string>();
                    shortcuts[shortcut].Add(commandName);
                }
            }

            foreach (var (shortcut, commands) in shortcuts.Where(kvp => kvp.Value.Count > 1))
            {
                errors.Add($"Keyboard shortcut '{shortcut}' is assigned to multiple commands: {string.Join(", ", commands)}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to retrieve command set: {ex.Message}");
        }

        return errors.Count == 0 
            ? (warnings.Count == 0 ? ProfileValidationResult.Success() : ProfileValidationResult.Failed(Array.Empty<string>(), warnings))
            : ProfileValidationResult.Failed(errors, warnings);
    }
}

/// <summary>
/// Validates layout configuration requirements.
/// </summary>
public class LayoutValidationRule : IProfileValidationRule
{
    public ProfileValidationResult Validate(IUIProfile profile)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var layout = profile.GetLayoutConfiguration();

            if (!layout.IsValid())
                errors.Add("Layout configuration is invalid");

            // Check for at least one visible panel
            if (!layout.Panels.Any(p => p.IsVisible))
                warnings.Add("No panels are visible by default");

            // Check for window size constraints
            if (layout.Window.Width < 300 || layout.Window.Height < 200)
                warnings.Add("Window size may be too small for usability");

            if (layout.Window.Width > 4096 || layout.Window.Height > 2160)
                warnings.Add("Window size may be too large for most displays");

            // Check theme configuration
            if (layout.Theme.FontSize < 8 || layout.Theme.FontSize > 72)
                warnings.Add("Font size may be outside reasonable range");
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to retrieve layout configuration: {ex.Message}");
        }

        return errors.Count == 0 
            ? (warnings.Count == 0 ? ProfileValidationResult.Success() : ProfileValidationResult.Failed(Array.Empty<string>(), warnings))
            : ProfileValidationResult.Failed(errors, warnings);
    }
}

/// <summary>
/// Validates service provider configuration requirements.
/// </summary>
public class ServiceProviderValidationRule : IProfileValidationRule
{
    public ProfileValidationResult Validate(IUIProfile profile)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var services = profile.GetServiceProviderConfiguration();

            if (services.Count == 0)
                warnings.Add("No service providers configured");

            // Check for required service types
            var requiredServices = new[] { "UI", "Input" };
            foreach (var requiredService in requiredServices)
            {
                if (!services.ContainsKey(requiredService))
                    warnings.Add($"No provider configured for required service: {requiredService}");
            }

            // Validate provider names (basic check)
            foreach (var (serviceType, providerName) in services)
            {
                if (string.IsNullOrWhiteSpace(providerName))
                    errors.Add($"Service provider name for '{serviceType}' cannot be empty");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to retrieve service provider configuration: {ex.Message}");
        }

        return errors.Count == 0 
            ? (warnings.Count == 0 ? ProfileValidationResult.Success() : ProfileValidationResult.Failed(Array.Empty<string>(), warnings))
            : ProfileValidationResult.Failed(errors, warnings);
    }
}

/// <summary>
/// Validates profile metadata requirements.
/// </summary>
public class MetadataValidationRule : IProfileValidationRule
{
    public ProfileValidationResult Validate(IUIProfile profile)
    {
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.Metadata.Description))
            warnings.Add("Profile has no description");

        if (string.IsNullOrWhiteSpace(profile.Metadata.Author))
            warnings.Add("Profile has no author information");

        if (profile.Metadata.Tags.Count == 0)
            warnings.Add("Profile has no tags for categorization");

        // Version format validation (basic)
        if (!System.Text.RegularExpressions.Regex.IsMatch(profile.Metadata.Version, @"^\d+\.\d+\.\d+"))
            warnings.Add("Profile version does not follow semantic versioning (major.minor.patch)");

        return warnings.Count == 0 
            ? ProfileValidationResult.Success() 
            : ProfileValidationResult.Failed(Array.Empty<string>(), warnings);
    }
}

/// <summary>
/// Result of profile compatibility validation.
/// </summary>
public class ProfileCompatibilityResult
{
    /// <summary>
    /// Whether the profiles are compatible for transitions.
    /// </summary>
    public bool IsCompatible { get; init; }

    /// <summary>
    /// Compatibility issues that prevent smooth transitions.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Compatibility warnings that may affect user experience.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
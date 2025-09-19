using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Interface for UI profiles that define specialized interface configurations 
/// for different operational modes (Game vs Editor vs Console vs Web vs Desktop).
/// </summary>
public interface IUIProfile
{
    /// <summary>
    /// Unique name of the profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Target console mode this profile is optimized for.
    /// </summary>
    ConsoleMode TargetMode { get; }

    /// <summary>
    /// Metadata about this profile.
    /// </summary>
    UIProfileMetadata Metadata { get; }

    /// <summary>
    /// Gets the command set available in this profile.
    /// </summary>
    /// <returns>The command set for this profile.</returns>
    CommandSet GetCommandSet();

    /// <summary>
    /// Gets the layout configuration for this profile.
    /// </summary>
    /// <returns>The layout configuration for this profile.</returns>
    LayoutConfiguration GetLayoutConfiguration();

    /// <summary>
    /// Gets service provider configurations for this profile.
    /// This determines which Tier 4 providers are selected for different capabilities.
    /// </summary>
    /// <returns>Service provider configurations.</returns>
    IReadOnlyDictionary<string, string> GetServiceProviderConfiguration();

    /// <summary>
    /// Validates that this profile is consistent and complete.
    /// </summary>
    /// <returns>Validation result with any issues found.</returns>
    ProfileValidationResult Validate();

    /// <summary>
    /// Called when the profile is activated/switched to.
    /// Allows the profile to perform initialization or state setup.
    /// </summary>
    /// <param name="previousProfile">The profile being switched from, if any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the activation operation.</returns>
    Task OnActivatedAsync(IUIProfile? previousProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Called when the profile is being deactivated/switched away from.
    /// Allows the profile to perform cleanup or state preservation.
    /// </summary>
    /// <param name="nextProfile">The profile being switched to, if any.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the deactivation operation.</returns>
    Task OnDeactivatedAsync(IUIProfile? nextProfile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a copy of this profile with modified configuration.
    /// Used for profile inheritance and composition.
    /// </summary>
    /// <param name="name">Name for the new profile.</param>
    /// <param name="modifications">Configuration modifications to apply.</param>
    /// <returns>A new profile instance with the modifications applied.</returns>
    IUIProfile CreateVariant(string name, ProfileModifications modifications);
}

/// <summary>
/// Result of profile validation containing any issues found.
/// </summary>
public class ProfileValidationResult
{
    /// <summary>
    /// Whether the profile passed validation.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// List of validation errors found.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// List of validation warnings found.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ProfileValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">Validation errors.</param>
    /// <param name="warnings">Validation warnings.</param>
    public static ProfileValidationResult Failed(IEnumerable<string> errors, IEnumerable<string>? warnings = null)
    {
        return new()
        {
            IsValid = false,
            Errors = errors.ToArray(),
            Warnings = warnings?.ToArray() ?? Array.Empty<string>()
        };
    }
}

/// <summary>
/// Modifications to apply when creating profile variants.
/// </summary>
public class ProfileModifications
{
    /// <summary>
    /// Commands to add or override.
    /// </summary>
    public Dictionary<string, CommandDefinition> CommandChanges { get; init; } = new();

    /// <summary>
    /// Layout configuration changes.
    /// </summary>
    public LayoutConfiguration? LayoutOverride { get; init; }

    /// <summary>
    /// Service provider configuration changes.
    /// </summary>
    public Dictionary<string, string> ServiceProviderChanges { get; init; } = new();

    /// <summary>
    /// Metadata changes.
    /// </summary>
    public UIProfileMetadata? MetadataOverride { get; init; }
}
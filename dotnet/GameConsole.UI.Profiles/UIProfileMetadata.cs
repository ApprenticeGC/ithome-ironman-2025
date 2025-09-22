namespace GameConsole.UI.Profiles;

/// <summary>
/// Contains metadata information about a UI profile including versioning, compatibility, and feature flags.
/// Used for profile validation, inheritance, and runtime behavior configuration.
/// </summary>
public sealed class UIProfileMetadata
{
    /// <summary>
    /// Gets or sets the version of the profile schema.
    /// Used for compatibility checking and migration purposes.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the display name for the profile.
    /// Used in UI selection and debugging contexts.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a description of what the profile provides and its intended use case.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the author or source of the profile.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets tags for categorization and filtering of profiles.
    /// </summary>
    public HashSet<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets feature flags that control optional profile behaviors.
    /// </summary>
    public Dictionary<string, bool> FeatureFlags { get; set; } = new();

    /// <summary>
    /// Gets or sets compatibility requirements for this profile.
    /// Used by the ProfileValidator to ensure profile consistency.
    /// </summary>
    public Dictionary<string, string> CompatibilityRequirements { get; set; } = new();

    /// <summary>
    /// Gets or sets the profiles that this profile inherits from.
    /// Used for profile composition and inheritance chains.
    /// </summary>
    public List<string> InheritsFrom { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this profile supports hot-reloading during development.
    /// </summary>
    public bool SupportsHotReload { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority level for this profile when multiple profiles are available.
    /// Higher values indicate higher priority.
    /// </summary>
    public int Priority { get; set; } = 0;
}
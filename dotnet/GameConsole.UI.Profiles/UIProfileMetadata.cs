namespace GameConsole.UI.Profiles;

/// <summary>
/// Metadata information about a UI profile including authoring, versioning, and compatibility information.
/// </summary>
public class UIProfileMetadata
{
    /// <summary>
    /// Version of the profile.
    /// </summary>
    public string Version { get; init; } = "1.0.0";

    /// <summary>
    /// Author of the profile.
    /// </summary>
    public string Author { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable description of the profile.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Tags associated with the profile for categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// When the profile was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the profile was last modified.
    /// </summary>
    public DateTime LastModified { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if this profile is a built-in system profile.
    /// </summary>
    public bool IsBuiltIn { get; init; } = false;

    /// <summary>
    /// Priority level for profile selection (higher values have priority).
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Compatibility information indicating which modes this profile supports.
    /// </summary>
    public IReadOnlyList<ConsoleMode> CompatibleModes { get; init; } = Array.Empty<ConsoleMode>();
}
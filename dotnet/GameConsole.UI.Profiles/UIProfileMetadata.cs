namespace GameConsole.UI.Profiles;

/// <summary>
/// Metadata for UI profiles that describes their characteristics and requirements.
/// </summary>
public class UIProfileMetadata
{
    /// <summary>
    /// Gets or sets the description of what this profile provides.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of this profile.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the author of this profile.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tags associated with this profile for categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the priority of this profile when multiple profiles match the same mode.
    /// Higher values indicate higher priority.
    /// </summary>
    public int Priority { get; set; } = 0;
}
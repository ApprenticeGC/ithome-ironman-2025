namespace GameConsole.UI.Profiles;

/// <summary>
/// Metadata for UI profiles, providing descriptive information and categorization.
/// </summary>
public class UIProfileMetadata
{
    /// <summary>
    /// Gets or sets the display name of the profile.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
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
    /// Gets or sets the tags for categorizing this profile.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Gets or sets whether this profile is available for selection by users.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the priority order for profile selection (higher values = higher priority).
    /// </summary>
    public int Priority { get; set; } = 0;
}
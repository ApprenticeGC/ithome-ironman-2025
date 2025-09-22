namespace GameConsole.UI.Profiles;

/// <summary>
/// Metadata associated with a UI profile, providing descriptive information
/// and configuration hints for the profile system.
/// </summary>
public sealed class UIProfileMetadata
{
    /// <summary>
    /// Gets or sets the display name for the profile.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the profile is optimized for.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the author or creator of the profile.
    /// </summary>
    public string Author { get; set; } = "System";

    /// <summary>
    /// Gets or sets the version of the profile configuration.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets tags associated with the profile for categorization.
    /// </summary>
    public IList<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets whether this profile is read-only and cannot be modified by users.
    /// </summary>
    public bool IsSystemProfile { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority for automatic profile selection. Higher values take precedence.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets additional properties as key-value pairs for extensibility.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
namespace GameConsole.UI.Profiles;

/// <summary>
/// Contains metadata information about a UI profile.
/// </summary>
public class UIProfileMetadata
{
    /// <summary>
    /// Gets or sets the description of the profile.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the profile.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the list of dependencies required by this profile.
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional custom properties for the profile.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
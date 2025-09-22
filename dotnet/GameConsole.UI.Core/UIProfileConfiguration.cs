namespace GameConsole.UI.Core;

/// <summary>
/// Defines the configuration settings for a UI profile.
/// Contains all necessary parameters to configure UI behavior for different profiles.
/// </summary>
public class UIProfileConfiguration
{
    /// <summary>
    /// Gets or sets the unique identifier for the UI profile.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the UI profile.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the UI profile.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of UI profile.
    /// </summary>
    public UIProfileType ProfileType { get; set; } = UIProfileType.Default;

    /// <summary>
    /// Gets or sets a value indicating whether this profile is the default profile.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this profile is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets additional configuration properties specific to the profile type.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the version of the profile configuration.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the creation timestamp of the profile.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last modified timestamp of the profile.
    /// </summary>
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
}
namespace GameConsole.Profile.Core;

/// <summary>
/// Represents a service configuration within a profile.
/// </summary>
public sealed class ServiceConfiguration
{
    /// <summary>
    /// Gets or sets the implementation type name for the service.
    /// </summary>
    public string Implementation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the capabilities that should be enabled for this service.
    /// </summary>
    public ICollection<string> Capabilities { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the configuration settings for the service.
    /// </summary>
    public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the service lifetime (Singleton, Scoped, Transient).
    /// </summary>
    public string Lifetime { get; set; } = "Singleton";

    /// <summary>
    /// Gets or sets whether the service is enabled in this profile.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
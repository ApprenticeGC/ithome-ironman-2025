namespace GameConsole.Plugins.Core;

/// <summary>
/// Attribute used to declaratively define plugin metadata.
/// Apply this attribute to plugin classes to provide metadata for plugin discovery,
/// dependency resolution, and runtime management.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class PluginAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the plugin.</param>
    /// <param name="name">The human-readable name of the plugin.</param>
    /// <param name="version">The version of the plugin.</param>
    /// <param name="description">A description of what the plugin does.</param>
    /// <param name="author">The author or organization that created the plugin.</param>
    public PluginAttribute(string id, string name, string version, string description, string author)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Author = author ?? throw new ArgumentNullException(nameof(author));
    }

    /// <summary>
    /// Gets the unique identifier for the plugin.
    /// This should be a stable identifier that doesn't change across versions.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the plugin.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets a description of what the plugin does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the author or organization that created the plugin.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Gets or sets the list of plugin IDs that this plugin depends on.
    /// Dependencies must be loaded before this plugin can be initialized.
    /// </summary>
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the minimum host application version required by this plugin.
    /// If not specified, the plugin will be loaded regardless of host version.
    /// </summary>
    public string? MinimumHostVersion { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this plugin can be unloaded at runtime.
    /// Some plugins may need to remain loaded for the lifetime of the application.
    /// </summary>
    public bool CanUnload { get; set; } = true;

    /// <summary>
    /// Gets or sets tags that categorize or describe the plugin's functionality.
    /// This can be used for plugin filtering and organization.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
}
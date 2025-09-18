namespace GameConsole.Plugins.Core;

/// <summary>
/// Provides metadata information about a plugin including identity, version, and dependencies.
/// Used for plugin discovery, version management, and dependency resolution.
/// </summary>
public interface IPluginMetadata
{
    /// <summary>
    /// Gets the unique identifier for the plugin.
    /// This should be a stable identifier that doesn't change across versions.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the plugin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the plugin.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets a description of what the plugin does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the author or organization that created the plugin.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets the list of plugin IDs that this plugin depends on.
    /// Dependencies must be loaded before this plugin can be initialized.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Gets additional properties and metadata for the plugin.
    /// This can include custom configuration, feature flags, or other plugin-specific data.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}
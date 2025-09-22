using GameConsole.Core.Abstractions;

namespace GameConsole.Profile.Core;

/// <summary>
/// Represents a profile configuration that defines service providers and settings.
/// </summary>
public interface IProfile
{
    /// <summary>
    /// Gets the unique identifier for the profile.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the display name of the profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the profile.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the type of profile.
    /// </summary>
    ProfileType Type { get; }

    /// <summary>
    /// Gets the service configurations for this profile.
    /// Key is the service interface name, value is the configuration.
    /// </summary>
    IReadOnlyDictionary<string, ServiceConfiguration> ServiceConfigurations { get; }

    /// <summary>
    /// Gets the date and time when the profile was created.
    /// </summary>
    DateTime Created { get; }

    /// <summary>
    /// Gets the date and time when the profile was last modified.
    /// </summary>
    DateTime LastModified { get; }

    /// <summary>
    /// Gets whether the profile is read-only (system profiles).
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Gets the version of the profile for compatibility checking.
    /// </summary>
    string Version { get; }
}
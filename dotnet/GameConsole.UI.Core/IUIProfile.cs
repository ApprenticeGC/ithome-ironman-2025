using System;
using System.Collections.Generic;

namespace GameConsole.UI.Core
{
    /// <summary>
    /// Represents a UI profile configuration that defines how the user interface should behave.
    /// Profiles specify provider and system configurations to simulate different game engine behaviors.
    /// </summary>
    public interface IUIProfile
{
    /// <summary>
    /// Gets the unique identifier for this profile.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the profile.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what this profile provides.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the type of this profile.
    /// </summary>
    ProfileType Type { get; }

    /// <summary>
    /// Gets the configuration settings for this profile.
    /// This dictionary contains key-value pairs that define provider and system configurations.
    /// </summary>
    IReadOnlyDictionary<string, object> Configuration { get; }

    /// <summary>
    /// Gets a value indicating whether this profile is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the timestamp when this profile was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the timestamp when this profile was last modified.
    /// </summary>
    DateTimeOffset LastModified { get; }

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <typeparam name="T">The type of the configuration value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value if found; otherwise, the default value for T.</returns>
    T GetConfigurationValue<T>(string key);
    }
}
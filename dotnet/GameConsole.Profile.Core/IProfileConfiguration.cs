using GameConsole.Core.Abstractions;

namespace GameConsole.Profile.Core;

/// <summary>
/// Defines the configuration for a UI profile that determines how the game console interface behaves.
/// Profiles can simulate different environments (TUI, Unity, Godot) by specifying different providers and behaviors.
/// </summary>
public interface IProfileConfiguration
{
    /// <summary>
    /// Gets the unique identifier for this profile configuration.
    /// </summary>
    string ProfileId { get; }

    /// <summary>
    /// Gets the human-readable name of this profile.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the description of what this profile provides.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the priority of this profile. Higher values take precedence when multiple profiles are available.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if this profile is currently supported in the runtime environment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile can be activated in the current environment.</returns>
    Task<bool> IsSupported(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all capability providers that should be activated when this profile is active.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of capability provider types to register.</returns>
    Task<IEnumerable<Type>> GetCapabilityProviders(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration settings specific to this profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A dictionary of configuration key-value pairs.</returns>
    Task<IReadOnlyDictionary<string, object>> GetConfigurationSettings(CancellationToken cancellationToken = default);
}
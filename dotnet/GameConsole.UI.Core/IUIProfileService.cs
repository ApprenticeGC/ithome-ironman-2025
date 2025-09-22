using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Interface for the UI profile service in the GameConsole system.
/// Provides functionality to manage UI profiles, including loading, switching, and configuration management.
/// Extends IService to follow the standard GameConsole service lifecycle pattern.
/// </summary>
public interface IUIProfileService : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    IUIProfile? ActiveProfile { get; }

    /// <summary>
    /// Event raised when the active UI profile changes.
    /// </summary>
    event EventHandler<UIProfileChangedEventArgs>? ProfileChanged;

    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of UI profiles.</returns>
    Task<IEnumerable<IUIProfile>> GetAvailableProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a UI profile by its unique identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the UI profile or null if not found.</returns>
    Task<IUIProfile?> GetProfileByIdAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets UI profiles by their type.
    /// </summary>
    /// <param name="profileType">The type of profiles to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns matching UI profiles.</returns>
    Task<IEnumerable<IUIProfile>> GetProfilesByTypeAsync(UIProfileType profileType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to a different UI profile.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to switch to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async profile switching operation that returns true if successful.</returns>
    Task<bool> SwitchToProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads all UI profiles from configuration sources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async reload operation.</returns>
    Task ReloadProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates all loaded UI profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async validation operation that returns validation results.</returns>
    Task<Dictionary<string, UIProfileValidationResult>> ValidateAllProfilesAsync(CancellationToken cancellationToken = default);
}
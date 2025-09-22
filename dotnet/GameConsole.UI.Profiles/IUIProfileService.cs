using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Service interface for managing UI profiles in the GameConsole system.
/// Provides functionality to register, activate, and manage different UI profiles.
/// </summary>
public interface IUIProfileService : IService
{
    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    IUIProfile? ActiveProfile { get; }

    /// <summary>
    /// Gets all registered UI profiles.
    /// </summary>
    IReadOnlyCollection<IUIProfile> Profiles { get; }

    /// <summary>
    /// Registers a UI profile with the service.
    /// </summary>
    /// <param name="profile">The UI profile to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a UI profile from the service.
    /// </summary>
    /// <param name="profileId">The ID of the profile to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unregistration operation.</returns>
    Task UnregisterProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a UI profile by its ID.
    /// </summary>
    /// <param name="profileId">The ID of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async activation operation.</returns>
    Task ActivateProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a UI profile by its ID.
    /// </summary>
    /// <param name="profileId">The ID of the profile to retrieve.</param>
    /// <returns>The UI profile if found, otherwise null.</returns>
    IUIProfile? GetProfile(string profileId);

    /// <summary>
    /// Gets all profiles that match the specified UI mode.
    /// </summary>
    /// <param name="mode">The UI mode to filter by.</param>
    /// <returns>A collection of profiles matching the specified mode.</returns>
    IReadOnlyCollection<IUIProfile> GetProfilesByMode(UIMode mode);

    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
}

/// <summary>
/// Event arguments for profile change events.
/// </summary>
public class ProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previously active profile (can be null).
    /// </summary>
    public IUIProfile? PreviousProfile { get; }

    /// <summary>
    /// Gets the newly activated profile (can be null).
    /// </summary>
    public IUIProfile? NewProfile { get; }

    /// <summary>
    /// Initializes a new instance of the ProfileChangedEventArgs class.
    /// </summary>
    /// <param name="previousProfile">The previously active profile.</param>
    /// <param name="newProfile">The newly activated profile.</param>
    public ProfileChangedEventArgs(IUIProfile? previousProfile, IUIProfile? newProfile)
    {
        PreviousProfile = previousProfile;
        NewProfile = newProfile;
    }
}
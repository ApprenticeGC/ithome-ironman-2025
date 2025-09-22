using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Interface for the UI Profile Manager service that handles profile detection,
/// loading, and dynamic switching within the GameConsole system.
/// </summary>
public interface IUIProfileManager : IService
{
    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    UIProfile? CurrentProfile { get; }

    /// <summary>
    /// Gets the current console mode.
    /// </summary>
    ConsoleMode CurrentMode { get; }

    /// <summary>
    /// Event fired when the active profile changes.
    /// </summary>
    event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns available profiles.</returns>
    Task<IEnumerable<UIProfile>> GetAvailableProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets profiles available for a specific console mode.
    /// </summary>
    /// <param name="mode">The console mode to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns matching profiles.</returns>
    Task<IEnumerable<UIProfile>> GetProfilesForModeAsync(ConsoleMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a profile by its unique identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the profile, or null if not found.</returns>
    Task<UIProfile?> GetProfileByIdAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to the specified UI profile.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to switch to.</param>
    /// <param name="reason">The reason for the profile switch.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if successful.</returns>
    Task<bool> SwitchToProfileAsync(string profileId, string reason = "Manual", CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to the specified console mode, automatically selecting an appropriate profile.
    /// </summary>
    /// <param name="mode">The console mode to switch to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if successful.</returns>
    Task<bool> SwitchToModeAsync(ConsoleMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new UI profile with the manager.
    /// </summary>
    /// <param name="profile">The profile to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if successful.</returns>
    Task<bool> RegisterProfileAsync(UIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a UI profile from the manager.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if successful.</returns>
    Task<bool> UnregisterProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reloads all profiles from configuration sources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ReloadProfilesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event arguments for profile change events.
/// </summary>
public sealed class ProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the ProfileChangedEventArgs class.
    /// </summary>
    /// <param name="previousProfile">The previously active profile.</param>
    /// <param name="currentProfile">The newly active profile.</param>
    /// <param name="reason">The reason for the profile change.</param>
    public ProfileChangedEventArgs(UIProfile? previousProfile, UIProfile? currentProfile, string reason)
    {
        PreviousProfile = previousProfile;
        CurrentProfile = currentProfile;
        Reason = reason ?? string.Empty;
    }

    /// <summary>
    /// Gets the previously active profile.
    /// </summary>
    public UIProfile? PreviousProfile { get; }

    /// <summary>
    /// Gets the newly active profile.
    /// </summary>
    public UIProfile? CurrentProfile { get; }

    /// <summary>
    /// Gets the reason for the profile change.
    /// </summary>
    public string Reason { get; }
}
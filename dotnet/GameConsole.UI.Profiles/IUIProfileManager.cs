using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Interface for managing UI profiles in the GameConsole system.
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
    /// Registers a UI profile with the manager.
    /// </summary>
    /// <param name="profile">The profile to register.</param>
    void RegisterProfile(UIProfile profile);

    /// <summary>
    /// Unregisters a UI profile from the manager.
    /// </summary>
    /// <param name="profileName">The name of the profile to unregister.</param>
    /// <returns>True if the profile was unregistered; otherwise, false.</returns>
    bool UnregisterProfile(string profileName);

    /// <summary>
    /// Gets all registered profiles.
    /// </summary>
    /// <returns>A collection of all registered profiles.</returns>
    IEnumerable<UIProfile> GetAllProfiles();

    /// <summary>
    /// Gets all profiles that target a specific console mode.
    /// </summary>
    /// <param name="mode">The console mode to filter by.</param>
    /// <returns>A collection of profiles for the specified mode.</returns>
    IEnumerable<UIProfile> GetProfilesForMode(ConsoleMode mode);

    /// <summary>
    /// Gets a profile by its name.
    /// </summary>
    /// <param name="profileName">The name of the profile.</param>
    /// <returns>The profile if found; otherwise, null.</returns>
    UIProfile? GetProfile(string profileName);

    /// <summary>
    /// Activates a specific profile by name.
    /// </summary>
    /// <param name="profileName">The name of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task representing the async activation operation.</returns>
    Task<bool> ActivateProfileAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to a specific console mode and activates the best matching profile.
    /// </summary>
    /// <param name="mode">The console mode to switch to.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task representing the async mode switch operation.</returns>
    Task<bool> SwitchModeAsync(ConsoleMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

    /// <summary>
    /// Event raised when the console mode changes.
    /// </summary>
    event EventHandler<ModeChangedEventArgs>? ModeChanged;
}

/// <summary>
/// Event arguments for profile change events.
/// </summary>
public class ProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous profile that was active.
    /// </summary>
    public UIProfile? PreviousProfile { get; }

    /// <summary>
    /// Gets the new profile that is now active.
    /// </summary>
    public UIProfile? NewProfile { get; }

    /// <summary>
    /// Initializes a new instance of the ProfileChangedEventArgs class.
    /// </summary>
    /// <param name="previousProfile">The previous profile.</param>
    /// <param name="newProfile">The new profile.</param>
    public ProfileChangedEventArgs(UIProfile? previousProfile, UIProfile? newProfile)
    {
        PreviousProfile = previousProfile;
        NewProfile = newProfile;
    }
}

/// <summary>
/// Event arguments for mode change events.
/// </summary>
public class ModeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous console mode.
    /// </summary>
    public ConsoleMode PreviousMode { get; }

    /// <summary>
    /// Gets the new console mode.
    /// </summary>
    public ConsoleMode NewMode { get; }

    /// <summary>
    /// Initializes a new instance of the ModeChangedEventArgs class.
    /// </summary>
    /// <param name="previousMode">The previous mode.</param>
    /// <param name="newMode">The new mode.</param>
    public ModeChangedEventArgs(ConsoleMode previousMode, ConsoleMode newMode)
    {
        PreviousMode = previousMode;
        NewMode = newMode;
    }
}
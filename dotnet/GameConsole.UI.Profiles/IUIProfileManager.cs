using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Interface for managing UI profiles and console modes.
/// </summary>
public interface IUIProfileManager : IService
{
    /// <summary>
    /// Gets the current console mode.
    /// </summary>
    ConsoleMode CurrentMode { get; }

    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    UIProfile? ActiveProfile { get; }

    /// <summary>
    /// Gets the collection of available UI profiles.
    /// </summary>
    IReadOnlyList<UIProfile> AvailableProfiles { get; }

    /// <summary>
    /// Switches to the specified console mode.
    /// </summary>
    /// <param name="mode">The console mode to switch to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the mode was switched successfully; otherwise, false.</returns>
    Task<bool> SwitchModeAsync(ConsoleMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates the specified UI profile by name.
    /// </summary>
    /// <param name="profileName">The name of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was activated successfully; otherwise, false.</returns>
    Task<bool> ActivateProfileAsync(string profileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a UI profile with the manager.
    /// </summary>
    /// <param name="profile">The profile to register.</param>
    void RegisterProfile(UIProfile profile);

    /// <summary>
    /// Gets a UI profile by name.
    /// </summary>
    /// <param name="name">The name of the profile.</param>
    /// <returns>The profile if found; otherwise, null.</returns>
    UIProfile? GetProfileByName(string name);

    /// <summary>
    /// Occurs when the console mode changes.
    /// </summary>
    event EventHandler<ModeChangedEventArgs>? ModeChanged;

    /// <summary>
    /// Occurs when the active UI profile changes.
    /// </summary>
    event EventHandler<ProfileChangedEventArgs>? ProfileChanged;
}
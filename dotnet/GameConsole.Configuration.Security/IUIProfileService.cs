using GameConsole.Core.Abstractions;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Service interface for managing UI profiles in the GameConsole system.
/// Provides functionality to switch between different UI modes and manage profile configurations.
/// </summary>
public interface IUIProfileService : IService
{
    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    IUIProfileConfiguration? ActiveProfile { get; }

    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    IReadOnlyCollection<IUIProfileConfiguration> AvailableProfiles { get; }

    /// <summary>
    /// Switches to the specified UI profile.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that completes when the profile switch is finished.</returns>
    /// <exception cref="ArgumentException">Thrown when the profile ID is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the profile cannot be activated.</exception>
    Task SwitchToProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a UI profile by its unique identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile.</param>
    /// <returns>The profile configuration, or null if not found.</returns>
    IUIProfileConfiguration? GetProfile(string profileId);

    /// <summary>
    /// Registers a new UI profile configuration.
    /// </summary>
    /// <param name="profile">The profile configuration to register.</param>
    /// <exception cref="ArgumentException">Thrown when a profile with the same ID already exists.</exception>
    void RegisterProfile(IUIProfileConfiguration profile);

    /// <summary>
    /// Unregisters a UI profile configuration.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to remove.</param>
    /// <returns>True if the profile was found and removed; false otherwise.</returns>
    bool UnregisterProfile(string profileId);

    /// <summary>
    /// Saves the current profile configuration to persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that completes when the configuration is saved.</returns>
    Task SaveConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads profile configurations from persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A task that completes when the configuration is loaded.</returns>
    Task LoadConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    event EventHandler<UIProfileChangedEventArgs>? ProfileChanged;
}

/// <summary>
/// Event arguments for UI profile change events.
/// </summary>
public class UIProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previously active profile (may be null).
    /// </summary>
    public IUIProfileConfiguration? PreviousProfile { get; }

    /// <summary>
    /// Gets the newly active profile (may be null).
    /// </summary>
    public IUIProfileConfiguration? NewProfile { get; }

    /// <summary>
    /// Initializes a new instance of the UIProfileChangedEventArgs class.
    /// </summary>
    /// <param name="previousProfile">The previously active profile.</param>
    /// <param name="newProfile">The newly active profile.</param>
    public UIProfileChangedEventArgs(IUIProfileConfiguration? previousProfile, IUIProfileConfiguration? newProfile)
    {
        PreviousProfile = previousProfile;
        NewProfile = newProfile;
    }
}
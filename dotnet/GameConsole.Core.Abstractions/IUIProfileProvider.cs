namespace GameConsole.Core.Abstractions;

/// <summary>
/// Arguments for UI profile change events.
/// </summary>
public class UIProfileChangeEventArgs : EventArgs
{
    /// <summary>
    /// The profile that was previously active, or null if this is the first activation.
    /// </summary>
    public IUIProfile? PreviousProfile { get; }
    
    /// <summary>
    /// The profile that is now active.
    /// </summary>
    public IUIProfile NewProfile { get; }
    
    /// <summary>
    /// The timestamp when the profile change occurred.
    /// </summary>
    public DateTime ChangedAt { get; }
    
    /// <summary>
    /// Initializes a new instance of the UIProfileChangeEventArgs class.
    /// </summary>
    /// <param name="previousProfile">The previously active profile.</param>
    /// <param name="newProfile">The newly active profile.</param>
    public UIProfileChangeEventArgs(IUIProfile? previousProfile, IUIProfile newProfile)
    {
        PreviousProfile = previousProfile;
        NewProfile = newProfile ?? throw new ArgumentNullException(nameof(newProfile));
        ChangedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Capability interface for services that can provide UI profile management.
/// Enables discovery and management of different UI modes within the system.
/// </summary>
public interface IUIProfileProvider : ICapabilityProvider
{
    /// <summary>
    /// Event raised when the active UI profile changes.
    /// </summary>
    event EventHandler<UIProfileChangeEventArgs>? ProfileChanged;
    
    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    IUIProfile? ActiveProfile { get; }
    
    /// <summary>
    /// Gets all available UI profiles registered in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns available profiles.</returns>
    Task<IEnumerable<IUIProfile>> GetAvailableProfilesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets profiles of a specific type.
    /// </summary>
    /// <param name="profileType">The type of profiles to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns profiles of the specified type.</returns>
    Task<IEnumerable<IUIProfile>> GetProfilesByTypeAsync(UIProfileType profileType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific UI profile by its identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the profile, or null if not found.</returns>
    Task<IUIProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Switches to a different UI profile.
    /// </summary>
    /// <param name="profileId">The identifier of the profile to switch to.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async profile switch operation.</returns>
    Task SwitchProfileAsync(string profileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers a new UI profile in the system.
    /// </summary>
    /// <param name="profile">The profile to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unregisters a UI profile from the system.
    /// </summary>
    /// <param name="profileId">The identifier of the profile to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unregistration operation.</returns>
    Task UnregisterProfileAsync(string profileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves the current profile configuration to persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async save operation.</returns>
    Task SaveConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads profile configuration from persistent storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async load operation.</returns>
    Task LoadConfigurationAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates that a profile is compatible with the current system.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async validation operation that returns true if compatible.</returns>
    Task<bool> ValidateProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default);
}
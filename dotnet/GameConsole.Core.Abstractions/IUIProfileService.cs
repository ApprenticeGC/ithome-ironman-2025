using System.Collections.Generic;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Service interface for managing UI profiles.
/// Coordinates profile switching across multiple subsystems (input, graphics, etc.).
/// </summary>
public interface IUIProfileService : IService
{
    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    IUIProfile? ActiveProfile { get; }
    
    /// <summary>
    /// Creates a new UI profile.
    /// </summary>
    /// <param name="id">Unique identifier for the profile.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="description">Description of the profile.</param>
    /// <param name="inputProfileName">Associated input profile name.</param>
    /// <param name="graphicsSettings">Graphics settings dictionary.</param>
    /// <param name="uiSettings">UI-specific settings dictionary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created UI profile.</returns>
    Task<IUIProfile> CreateProfileAsync(
        string id, 
        string name, 
        string description,
        string? inputProfileName = null,
        Dictionary<string, object>? graphicsSettings = null,
        Dictionary<string, object>? uiSettings = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing UI profile.
    /// </summary>
    /// <param name="id">Profile ID to update.</param>
    /// <param name="name">New name (optional).</param>
    /// <param name="description">New description (optional).</param>
    /// <param name="inputProfileName">New input profile name (optional).</param>
    /// <param name="graphicsSettings">New graphics settings (optional).</param>
    /// <param name="uiSettings">New UI settings (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated UI profile.</returns>
    Task<IUIProfile> UpdateProfileAsync(
        string id,
        string? name = null,
        string? description = null, 
        string? inputProfileName = null,
        Dictionary<string, object>? graphicsSettings = null,
        Dictionary<string, object>? uiSettings = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a UI profile.
    /// </summary>
    /// <param name="id">Profile ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteProfileAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a UI profile by ID.
    /// </summary>
    /// <param name="id">Profile ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The UI profile, or null if not found.</returns>
    Task<IUIProfile?> GetProfileAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of all UI profiles.</returns>
    Task<IEnumerable<IUIProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activates a UI profile, applying its settings to all relevant subsystems.
    /// </summary>
    /// <param name="id">Profile ID to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ActivateProfileAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    event EventHandler<UIProfileChangedEventArgs>? ActiveProfileChanged;
}

/// <summary>
/// Event arguments for UI profile change events.
/// </summary>
public class UIProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// The previously active profile (null if none was active).
    /// </summary>
    public IUIProfile? PreviousProfile { get; }
    
    /// <summary>
    /// The newly active profile (null if profile was deactivated).
    /// </summary>
    public IUIProfile? NewProfile { get; }
    
    /// <summary>
    /// Initializes a new instance of the UIProfileChangedEventArgs class.
    /// </summary>
    /// <param name="previousProfile">Previous profile.</param>
    /// <param name="newProfile">New profile.</param>
    public UIProfileChangedEventArgs(IUIProfile? previousProfile, IUIProfile? newProfile)
    {
        PreviousProfile = previousProfile;
        NewProfile = newProfile;
    }
}
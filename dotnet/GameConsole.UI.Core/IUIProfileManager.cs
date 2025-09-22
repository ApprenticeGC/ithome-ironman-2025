using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameConsole.UI.Core
{
    /// <summary>
    /// Manages UI profiles including creation, activation, and lifecycle operations.
    /// This interface provides the core profile management capabilities.
    /// </summary>
    public interface IUIProfileManager
{
    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    IReadOnlyCollection<IUIProfile> Profiles { get; }

    /// <summary>
    /// Gets the currently active profile.
    /// </summary>
    IUIProfile ActiveProfile { get; }

    /// <summary>
    /// Creates a new UI profile with the specified configuration.
    /// </summary>
    /// <param name="name">The name of the profile.</param>
    /// <param name="description">The description of the profile.</param>
    /// <param name="type">The type of profile to create.</param>
    /// <param name="configuration">The configuration settings for the profile.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created UI profile.</returns>
    Task<IUIProfile> CreateProfileAsync(
        string name, 
        string description, 
        ProfileType type, 
        IDictionary<string, object> configuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a profile by its unique identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile.</param>
    /// <returns>The profile if found; otherwise, null.</returns>
    IUIProfile GetProfile(string profileId);

    /// <summary>
    /// Gets profiles by type.
    /// </summary>
    /// <param name="type">The profile type to filter by.</param>
    /// <returns>Collection of profiles matching the specified type.</returns>
    IReadOnlyCollection<IUIProfile> GetProfilesByType(ProfileType type);

    /// <summary>
    /// Activates the specified profile, making it the active profile.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was successfully activated; otherwise, false.</returns>
    Task<bool> ActivateProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing profile's configuration.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to update.</param>
    /// <param name="configuration">The new configuration settings.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was successfully updated; otherwise, false.</returns>
    Task<bool> UpdateProfileAsync(
        string profileId, 
        IDictionary<string, object> configuration, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a profile from the system.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was successfully deleted; otherwise, false.</returns>
    Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Occurs when a profile is activated.
    /// </summary>
    event EventHandler<ProfileActivatedEventArgs> ProfileActivated;

    /// <summary>
    /// Occurs when a profile is created.
    /// </summary>
    event EventHandler<ProfileCreatedEventArgs> ProfileCreated;

    /// <summary>
    /// Occurs when a profile is updated.
    /// </summary>
    event EventHandler<ProfileUpdatedEventArgs> ProfileUpdated;

    /// <summary>
    /// Occurs when a profile is deleted.
    /// </summary>
    event EventHandler<ProfileDeletedEventArgs> ProfileDeleted;
    }
}
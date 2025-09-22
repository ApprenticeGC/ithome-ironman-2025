using GameConsole.Core.Abstractions;

namespace GameConsole.Profile.Core;

/// <summary>
/// Service responsible for managing profile configurations and operations.
/// Tier 1 contract for profile management functionality.
/// </summary>
public interface IProfileManager : IService
{
    /// <summary>
    /// Gets the currently active profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The active profile.</returns>
    Task<IProfile> GetActiveProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active profile by ID.
    /// </summary>
    /// <param name="profileId">The ID of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetActiveProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of all profiles.</returns>
    Task<IEnumerable<IProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific profile by ID.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The profile if found, null otherwise.</returns>
    Task<IProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new profile.
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <param name="type">The profile type.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created profile.</returns>
    Task<IProfile> CreateProfileAsync(string name, ProfileType type, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing profile.
    /// </summary>
    /// <param name="profile">The profile to update.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task UpdateProfileAsync(IProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a profile by ID.
    /// </summary>
    /// <param name="profileId">The profile ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a profile configuration.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if valid, false otherwise.</returns>
    Task<bool> ValidateProfileAsync(IProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    event EventHandler<ProfileChangedEventArgs>? ActiveProfileChanged;
}
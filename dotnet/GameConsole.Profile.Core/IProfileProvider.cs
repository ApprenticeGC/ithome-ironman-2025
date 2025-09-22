namespace GameConsole.Profile.Core;

/// <summary>
/// Interface for profile storage and persistence operations.
/// Tier 1 contract for profile data access.
/// </summary>
public interface IProfileProvider
{
    /// <summary>
    /// Loads all profiles from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of profiles.</returns>
    Task<IEnumerable<IProfile>> LoadProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a specific profile by ID.
    /// </summary>
    /// <param name="profileId">The profile ID to load.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The profile if found, null otherwise.</returns>
    Task<IProfile?> LoadProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a profile to storage.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SaveProfileAsync(IProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a profile from storage.
    /// </summary>
    /// <param name="profileId">The profile ID to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a profile exists in storage.
    /// </summary>
    /// <param name="profileId">The profile ID to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile exists.</returns>
    Task<bool> ExistsAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active profile ID from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The active profile ID, or null if none set.</returns>
    Task<string?> GetActiveProfileIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active profile ID in storage.
    /// </summary>
    /// <param name="profileId">The profile ID to set as active.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetActiveProfileIdAsync(string profileId, CancellationToken cancellationToken = default);
}
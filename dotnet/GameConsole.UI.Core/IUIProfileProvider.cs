using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Interface for services that provide UI profile capabilities.
/// Enables discovery and management of UI profiles within the 4-tier architecture.
/// </summary>
public interface IUIProfileProvider : ICapabilityProvider
{
    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of UI profiles.</returns>
    Task<IEnumerable<IUIProfile>> GetProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the active UI profile, or null if none is active.</returns>
    Task<IUIProfile?> GetActiveProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a UI profile by its identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the profile was successfully activated.</returns>
    Task<bool> ActivateProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific UI profile by its identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the UI profile, or null if not found.</returns>
    Task<IUIProfile?> GetProfileByIdAsync(string profileId, CancellationToken cancellationToken = default);
}
using GameConsole.Core.Abstractions;

namespace GameConsole.Profile.Core;

/// <summary>
/// Manages UI profiles and handles switching between different profile configurations.
/// Enables the game console to adapt its interface behavior based on the active profile.
/// </summary>
public interface IProfileManager : IService
{
    /// <summary>
    /// Gets the currently active profile configuration, or null if no profile is active.
    /// </summary>
    IProfileConfiguration? ActiveProfile { get; }

    /// <summary>
    /// Event raised when the active profile changes.
    /// </summary>
    event EventHandler<ProfileChangedEventArgs>? ProfileChanged;

    /// <summary>
    /// Gets all available profile configurations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of all registered profile configurations.</returns>
    Task<IEnumerable<IProfileConfiguration>> GetAvailableProfiles(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all profiles that are supported in the current runtime environment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of supported profile configurations.</returns>
    Task<IEnumerable<IProfileConfiguration>> GetSupportedProfiles(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a profile configuration with the manager.
    /// </summary>
    /// <param name="profile">The profile configuration to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    void RegisterProfile(IProfileConfiguration profile);

    /// <summary>
    /// Activates a specific profile by its identifier.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was successfully activated, false otherwise.</returns>
    Task<bool> ActivateProfile(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates the most suitable profile based on the current environment and priorities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The activated profile configuration, or null if no suitable profile was found.</returns>
    Task<IProfileConfiguration?> ActivateBestProfile(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates the currently active profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task DeactivateCurrentProfile(CancellationToken cancellationToken = default);
}
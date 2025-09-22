using GameConsole.Core.Registry;

namespace GameConsole.UI.Configuration;

/// <summary>
/// Service responsible for managing UI profiles and their configurations.
/// </summary>
public interface IProfileConfigurationService
{
    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of UI profiles.</returns>
    Task<IEnumerable<IUIProfile>> GetAvailableProfilesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the active profile, or null if none is active.</returns>
    Task<IUIProfile?> GetActiveProfileAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a specific UI profile by its identifier.
    /// </summary>
    /// <param name="profileId">The profile identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the profile, or null if not found.</returns>
    Task<IUIProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Activates a UI profile by applying its service configurations.
    /// </summary>
    /// <param name="profileId">The profile identifier to activate.</param>
    /// <param name="serviceRegistry">The service registry to configure.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if activation succeeded.</returns>
    Task<bool> ActivateProfileAsync(string profileId, IServiceRegistry serviceRegistry, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates a UI profile configuration.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns validation results.</returns>
    Task<ProfileValidationResult> ValidateProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Loads UI profiles from configuration sources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async load operation.</returns>
    Task LoadProfilesAsync(CancellationToken cancellationToken = default);
}
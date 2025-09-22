namespace GameConsole.Profile.Core;

/// <summary>
/// Interface for applying profile configurations to service registrations.
/// Tier 1 contract for profile configuration application.
/// </summary>
public interface IProfileConfiguration
{
    /// <summary>
    /// Applies the profile configuration to the service registry.
    /// </summary>
    /// <param name="profile">The profile to apply.</param>
    /// <param name="serviceProvider">The service provider to use for dependencies.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ApplyProfileAsync(IProfile profile, IServiceProvider serviceProvider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a profile configuration can be successfully applied.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile configuration is valid.</returns>
    Task<bool> ValidateProfileAsync(IProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the validation errors for a profile, if any.
    /// </summary>
    /// <param name="profile">The profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of validation error messages.</returns>
    Task<IEnumerable<string>> GetValidationErrorsAsync(IProfile profile, CancellationToken cancellationToken = default);
}
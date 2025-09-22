using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Capability interface for UI profile management operations.
/// Services implementing this capability can manage UI profiles including loading, saving, and switching between profiles.
/// </summary>
public interface IUIProfileCapability
{
    /// <summary>
    /// Gets all available UI profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of all available UI profiles.</returns>
    Task<IEnumerable<UIProfile>> GetAllProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific UI profile by its ID.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The UI profile if found, null otherwise.</returns>
    Task<UIProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active UI profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The currently active UI profile, or null if no profile is active.</returns>
    Task<UIProfile?> GetActiveProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new UI profile.
    /// </summary>
    /// <param name="profile">The UI profile to create.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was created successfully.</returns>
    Task<bool> CreateProfileAsync(UIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing UI profile.
    /// </summary>
    /// <param name="profile">The updated UI profile.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was updated successfully.</returns>
    Task<bool> UpdateProfileAsync(UIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a UI profile. Built-in profiles cannot be deleted.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to delete.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was deleted successfully.</returns>
    Task<bool> DeleteProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates a specific UI profile, making it the current active profile.
    /// </summary>
    /// <param name="profileId">The unique identifier of the profile to activate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile was activated successfully.</returns>
    Task<bool> ActivateProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a UI profile configuration is valid.
    /// </summary>
    /// <param name="profile">The UI profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A validation result indicating whether the profile is valid.</returns>
    Task<UIProfileValidationResult> ValidateProfileAsync(UIProfile profile, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of UI profile validation operations.
/// </summary>
public record UIProfileValidationResult
{
    /// <summary>
    /// Whether the profile is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors if any.
    /// </summary>
    public IEnumerable<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Validation warnings if any.
    /// </summary>
    public IEnumerable<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static UIProfileValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    public static UIProfileValidationResult Failed(params string[] errors) => new() 
    { 
        IsValid = false, 
        Errors = errors 
    };

    /// <summary>
    /// Creates a validation result with warnings.
    /// </summary>
    public static UIProfileValidationResult WithWarnings(params string[] warnings) => new() 
    { 
        IsValid = true, 
        Warnings = warnings 
    };
}

/// <summary>
/// Events related to UI profile changes.
/// </summary>
public interface IUIProfileEvents
{
    /// <summary>
    /// Event fired when a UI profile is created.
    /// </summary>
    event EventHandler<UIProfileChangedEventArgs>? ProfileCreated;

    /// <summary>
    /// Event fired when a UI profile is updated.
    /// </summary>
    event EventHandler<UIProfileChangedEventArgs>? ProfileUpdated;

    /// <summary>
    /// Event fired when a UI profile is deleted.
    /// </summary>
    event EventHandler<UIProfileChangedEventArgs>? ProfileDeleted;

    /// <summary>
    /// Event fired when a UI profile is activated.
    /// </summary>
    event EventHandler<UIProfileChangedEventArgs>? ProfileActivated;
}

/// <summary>
/// Event arguments for UI profile change events.
/// </summary>
public class UIProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// The UI profile that was changed.
    /// </summary>
    public UIProfile Profile { get; }

    /// <summary>
    /// The type of change that occurred.
    /// </summary>
    public UIProfileChangeType ChangeType { get; }

    /// <summary>
    /// When the change occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    public UIProfileChangedEventArgs(UIProfile profile, UIProfileChangeType changeType)
    {
        Profile = profile;
        ChangeType = changeType;
        Timestamp = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Types of UI profile changes.
/// </summary>
public enum UIProfileChangeType
{
    /// <summary>
    /// A new profile was created.
    /// </summary>
    Created,

    /// <summary>
    /// An existing profile was updated.
    /// </summary>
    Updated,

    /// <summary>
    /// A profile was deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// A profile was activated.
    /// </summary>
    Activated
}
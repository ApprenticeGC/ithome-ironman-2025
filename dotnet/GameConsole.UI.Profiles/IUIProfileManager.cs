namespace GameConsole.UI.Profiles;

/// <summary>
/// Manages UI profiles, including registration, activation, and switching between profiles.
/// </summary>
public interface IUIProfileManager
{
    /// <summary>
    /// The currently active profile, or null if no profile is active.
    /// </summary>
    IUIProfile? ActiveProfile { get; }

    /// <summary>
    /// Current UI context being used for profile operations.
    /// </summary>
    UIContext CurrentContext { get; }

    /// <summary>
    /// Event raised when a profile is activated.
    /// </summary>
    event EventHandler<UIProfileActivatedEventArgs> ProfileActivated;

    /// <summary>
    /// Event raised when a profile is deactivated.
    /// </summary>
    event EventHandler<UIProfileDeactivatedEventArgs> ProfileDeactivated;

    /// <summary>
    /// Event raised when profile switching begins.
    /// </summary>
    event EventHandler<UIProfileSwitchingEventArgs> ProfileSwitching;

    /// <summary>
    /// Registers a UI profile with the manager.
    /// </summary>
    /// <param name="profile">The profile to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task RegisterProfileAsync(IUIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters a UI profile from the manager.
    /// </summary>
    /// <param name="profileId">The ID of the profile to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task UnregisterProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered profiles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of all registered profiles.</returns>
    Task<IReadOnlyCollection<IUIProfile>> GetRegisteredProfilesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets profiles that are compatible with the specified UI mode.
    /// </summary>
    /// <param name="mode">The UI mode to filter by.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of compatible profiles.</returns>
    Task<IReadOnlyCollection<IUIProfile>> GetCompatibleProfilesAsync(UIMode mode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to a different UI profile.
    /// </summary>
    /// <param name="profileId">The ID of the profile to switch to.</param>
    /// <param name="options">Options for the profile switch operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task SwitchProfileAsync(string profileId, ProfileSwitchOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches to the best matching profile for the current context.
    /// </summary>
    /// <param name="mode">The desired UI mode.</param>
    /// <param name="options">Options for the profile switch operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task SwitchToBestProfileAsync(UIMode mode, ProfileSwitchOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the current UI context and optionally switches profiles.
    /// </summary>
    /// <param name="context">The new UI context.</param>
    /// <param name="autoSwitch">Whether to automatically switch to a better matching profile if available.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task UpdateContextAsync(UIContext context, bool autoSwitch = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a profile can be activated in the current context.
    /// </summary>
    /// <param name="profileId">The ID of the profile to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Validation result containing success status and any issues found.</returns>
    Task<ProfileValidationResult> ValidateProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a profile by its ID.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The profile, or null if not found.</returns>
    Task<IUIProfile?> GetProfileAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current profile state for restoration later.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A state token that can be used to restore the profile state.</returns>
    Task<string> SaveProfileStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a previously saved profile state.
    /// </summary>
    /// <param name="stateToken">The state token returned from SaveProfileStateAsync.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    Task RestoreProfileStateAsync(string stateToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for profile switching operations.
/// </summary>
public record ProfileSwitchOptions
{
    /// <summary>
    /// Whether to preserve the current state when switching.
    /// </summary>
    public bool PreserveState { get; init; } = true;

    /// <summary>
    /// Whether to perform the switch immediately or defer to the next appropriate time.
    /// </summary>
    public bool Immediate { get; init; } = true;

    /// <summary>
    /// Whether to validate the target profile before switching.
    /// </summary>
    public bool ValidateTarget { get; init; } = true;

    /// <summary>
    /// Custom transition options.
    /// </summary>
    public TransitionOptions Transition { get; init; } = new();

    /// <summary>
    /// Additional options for the switch operation.
    /// </summary>
    public IReadOnlyDictionary<string, object> AdditionalOptions { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Options for profile transition animations and effects.
/// </summary>
public record TransitionOptions
{
    /// <summary>
    /// Duration of the transition in milliseconds.
    /// </summary>
    public int DurationMs { get; init; } = 300;

    /// <summary>
    /// Type of transition effect.
    /// </summary>
    public TransitionType Type { get; init; } = TransitionType.Fade;

    /// <summary>
    /// Whether to show a loading indicator during transition.
    /// </summary>
    public bool ShowProgress { get; init; } = true;
}

/// <summary>
/// Types of profile transition effects.
/// </summary>
public enum TransitionType
{
    None,
    Fade,
    Slide,
    Zoom,
    Flip
}

/// <summary>
/// Result of profile validation.
/// </summary>
public record ProfileValidationResult
{
    /// <summary>
    /// Whether the profile passed validation.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation error messages, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Validation warning messages.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Additional validation details.
    /// </summary>
    public IReadOnlyDictionary<string, object> Details { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Event arguments for profile activation events.
/// </summary>
public class UIProfileActivatedEventArgs : EventArgs
{
    /// <summary>
    /// The profile that was activated.
    /// </summary>
    public IUIProfile Profile { get; }

    /// <summary>
    /// The context in which the profile was activated.
    /// </summary>
    public UIContext Context { get; }

    /// <summary>
    /// The time when the profile was activated.
    /// </summary>
    public DateTime ActivatedAt { get; }

    /// <summary>
    /// Initializes a new instance of the UIProfileActivatedEventArgs class.
    /// </summary>
    public UIProfileActivatedEventArgs(IUIProfile profile, UIContext context)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ActivatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for profile deactivation events.
/// </summary>
public class UIProfileDeactivatedEventArgs : EventArgs
{
    /// <summary>
    /// The profile that was deactivated.
    /// </summary>
    public IUIProfile Profile { get; }

    /// <summary>
    /// The time when the profile was deactivated.
    /// </summary>
    public DateTime DeactivatedAt { get; }

    /// <summary>
    /// The reason for deactivation.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the UIProfileDeactivatedEventArgs class.
    /// </summary>
    public UIProfileDeactivatedEventArgs(IUIProfile profile, string reason = "Manual")
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Reason = reason;
        DeactivatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for profile switching events.
/// </summary>
public class UIProfileSwitchingEventArgs : EventArgs
{
    /// <summary>
    /// The profile being switched from, or null if no profile was active.
    /// </summary>
    public IUIProfile? FromProfile { get; }

    /// <summary>
    /// The profile being switched to.
    /// </summary>
    public IUIProfile ToProfile { get; }

    /// <summary>
    /// Switch options being used.
    /// </summary>
    public ProfileSwitchOptions Options { get; }

    /// <summary>
    /// Whether the switch can be cancelled.
    /// </summary>
    public bool CanCancel { get; set; } = true;

    /// <summary>
    /// Set to true to cancel the profile switch.
    /// </summary>
    public bool Cancel { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the UIProfileSwitchingEventArgs class.
    /// </summary>
    public UIProfileSwitchingEventArgs(IUIProfile? fromProfile, IUIProfile toProfile, ProfileSwitchOptions options)
    {
        FromProfile = fromProfile;
        ToProfile = toProfile ?? throw new ArgumentNullException(nameof(toProfile));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }
}
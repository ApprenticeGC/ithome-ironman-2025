using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Handles runtime profile transitions with state machine pattern and smooth transition effects.
/// </summary>
public class ProfileSwitcher
{
    private readonly ILogger<ProfileSwitcher> _logger;
    private readonly object _transitionLock = new();
    private ProfileTransitionState _currentState = ProfileTransitionState.Idle;

    public ProfileSwitcher(ILogger<ProfileSwitcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Current transition state.
    /// </summary>
    public ProfileTransitionState CurrentState
    {
        get
        {
            lock (_transitionLock)
            {
                return _currentState;
            }
        }
    }

    /// <summary>
    /// Event raised when transition state changes.
    /// </summary>
    public event EventHandler<TransitionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event raised to report transition progress.
    /// </summary>
    public event EventHandler<TransitionProgressEventArgs>? ProgressReported;

    /// <summary>
    /// Performs a profile transition with the specified options.
    /// </summary>
    /// <param name="fromProfile">The profile to transition from (can be null).</param>
    /// <param name="toProfile">The profile to transition to.</param>
    /// <param name="context">The UI context for the transition.</param>
    /// <param name="options">Transition options.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the transition operation.</returns>
    public async Task<TransitionResult> ExecuteTransitionAsync(
        IUIProfile? fromProfile, 
        IUIProfile toProfile, 
        UIContext context, 
        ProfileSwitchOptions options, 
        CancellationToken cancellationToken = default)
    {
        if (toProfile == null) throw new ArgumentNullException(nameof(toProfile));
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (options == null) throw new ArgumentNullException(nameof(options));

        lock (_transitionLock)
        {
            if (_currentState != ProfileTransitionState.Idle)
            {
                return new TransitionResult
                {
                    Success = false,
                    ErrorMessage = $"Cannot start transition: currently in state {_currentState}",
                    Duration = TimeSpan.Zero
                };
            }
            _currentState = ProfileTransitionState.Preparing;
        }

        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Starting profile transition from {FromProfile} to {ToProfile}", 
                fromProfile?.Id ?? "none", toProfile.Id);

            OnStateChanged(ProfileTransitionState.Preparing);
            OnProgressReported(0, "Preparing transition...");

            // Phase 1: Pre-transition validation and preparation
            await PrepareTransitionAsync(fromProfile, toProfile, context, options, cancellationToken);
            OnProgressReported(20, "Preparation complete");

            // Phase 2: Deactivate source profile
            if (fromProfile != null)
            {
                OnStateChanged(ProfileTransitionState.DeactivatingSource);
                OnProgressReported(30, "Deactivating source profile...");
                
                await DeactivateProfileWithTransitionAsync(fromProfile, options, cancellationToken);
                OnProgressReported(50, "Source profile deactivated");
            }

            // Phase 3: Transition effect (if any)
            if (options.Transition.Type != TransitionType.None)
            {
                OnStateChanged(ProfileTransitionState.Transitioning);
                OnProgressReported(60, "Applying transition effects...");
                
                await ApplyTransitionEffectsAsync(options.Transition, cancellationToken);
                OnProgressReported(80, "Transition effects applied");
            }

            // Phase 4: Activate target profile
            OnStateChanged(ProfileTransitionState.ActivatingTarget);
            OnProgressReported(90, "Activating target profile...");
            
            await ActivateProfileWithTransitionAsync(toProfile, context, options, cancellationToken);
            OnProgressReported(100, "Target profile activated");

            // Phase 5: Complete
            OnStateChanged(ProfileTransitionState.Idle);
            
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Profile transition completed successfully in {Duration}ms", duration.TotalMilliseconds);

            return new TransitionResult
            {
                Success = true,
                Duration = duration,
                FromProfile = fromProfile,
                ToProfile = toProfile
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Profile transition failed after {Duration}ms", duration.TotalMilliseconds);

            lock (_transitionLock)
            {
                _currentState = ProfileTransitionState.Idle;
            }
            
            OnStateChanged(ProfileTransitionState.Idle);

            return new TransitionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = duration,
                FromProfile = fromProfile,
                ToProfile = toProfile
            };
        }
    }

    private async Task PrepareTransitionAsync(
        IUIProfile? fromProfile, 
        IUIProfile toProfile, 
        UIContext context, 
        ProfileSwitchOptions options, 
        CancellationToken cancellationToken)
    {
        // Validate target profile can be activated
        var canActivate = await toProfile.CanActivateAsync(context, cancellationToken);
        if (!canActivate)
        {
            throw new InvalidOperationException($"Target profile {toProfile.Id} cannot be activated in the current context");
        }

        // Prepare any resources needed for the transition
        await Task.Delay(10, cancellationToken); // Simulate preparation work
    }

    private async Task DeactivateProfileWithTransitionAsync(IUIProfile profile, ProfileSwitchOptions options, CancellationToken cancellationToken)
    {
        try
        {
            if (options.PreserveState)
            {
                // In a real implementation, this would save profile-specific state
                _logger.LogDebug("Preserving state for profile {ProfileId}", profile.Id);
            }

            await profile.DeactivateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error deactivating profile {ProfileId} during transition", profile.Id);
            // Don't throw - continue with transition to avoid leaving UI in broken state
        }
    }

    private async Task ActivateProfileWithTransitionAsync(IUIProfile profile, UIContext context, ProfileSwitchOptions options, CancellationToken cancellationToken)
    {
        await profile.ActivateAsync(context, cancellationToken);

        if (options.PreserveState)
        {
            // In a real implementation, this would restore profile-specific state
            _logger.LogDebug("Restoring state for profile {ProfileId}", profile.Id);
        }
    }

    private async Task ApplyTransitionEffectsAsync(TransitionOptions transition, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Applying transition effect: {TransitionType} (duration: {Duration}ms)", 
            transition.Type, transition.DurationMs);

        // In a real implementation, this would:
        // - Apply visual transition effects (fade, slide, etc.)
        // - Show progress indicators if requested
        // - Handle transition animations

        // Simulate transition effect duration
        var effectDuration = Math.Max(10, Math.Min(transition.DurationMs, 2000)); // Clamp between 10ms and 2s
        await Task.Delay(effectDuration, cancellationToken);
    }

    private void OnStateChanged(ProfileTransitionState newState)
    {
        lock (_transitionLock)
        {
            var oldState = _currentState;
            _currentState = newState;
            
            StateChanged?.Invoke(this, new TransitionStateChangedEventArgs(oldState, newState));
        }
    }

    private void OnProgressReported(int percentage, string message)
    {
        ProgressReported?.Invoke(this, new TransitionProgressEventArgs(percentage, message));
    }
}

/// <summary>
/// States of profile transition process.
/// </summary>
public enum ProfileTransitionState
{
    /// <summary>
    /// No transition in progress.
    /// </summary>
    Idle,

    /// <summary>
    /// Preparing for transition.
    /// </summary>
    Preparing,

    /// <summary>
    /// Deactivating the source profile.
    /// </summary>
    DeactivatingSource,

    /// <summary>
    /// Applying transition effects.
    /// </summary>
    Transitioning,

    /// <summary>
    /// Activating the target profile.
    /// </summary>
    ActivatingTarget
}

/// <summary>
/// Result of a profile transition operation.
/// </summary>
public record TransitionResult
{
    /// <summary>
    /// Whether the transition was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if transition failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Duration of the transition.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Profile that was transitioned from.
    /// </summary>
    public IUIProfile? FromProfile { get; init; }

    /// <summary>
    /// Profile that was transitioned to.
    /// </summary>
    public IUIProfile? ToProfile { get; init; }

    /// <summary>
    /// Additional result details.
    /// </summary>
    public IReadOnlyDictionary<string, object> Details { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Event arguments for transition state changes.
/// </summary>
public class TransitionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Previous state.
    /// </summary>
    public ProfileTransitionState FromState { get; }

    /// <summary>
    /// New state.
    /// </summary>
    public ProfileTransitionState ToState { get; }

    /// <summary>
    /// When the state change occurred.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the TransitionStateChangedEventArgs class.
    /// </summary>
    public TransitionStateChangedEventArgs(ProfileTransitionState fromState, ProfileTransitionState toState)
    {
        FromState = fromState;
        ToState = toState;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments for transition progress reports.
/// </summary>
public class TransitionProgressEventArgs : EventArgs
{
    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int Percentage { get; }

    /// <summary>
    /// Progress message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// When the progress was reported.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the TransitionProgressEventArgs class.
    /// </summary>
    public TransitionProgressEventArgs(int percentage, string message)
    {
        Percentage = Math.Max(0, Math.Min(100, percentage));
        Message = message ?? string.Empty;
        Timestamp = DateTime.UtcNow;
    }
}
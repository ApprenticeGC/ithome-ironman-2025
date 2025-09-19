using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Handles runtime profile transitions with state machine pattern for smooth switching.
/// Manages transition animations, state preservation, and error recovery.
/// </summary>
public class ProfileSwitcher
{
    private readonly UIProfileManager _profileManager;
    private readonly ILogger _logger;
    private readonly object _transitionLock = new();
    
    private ProfileTransitionState _currentState = ProfileTransitionState.Idle;
    private ProfileTransition? _activeTransition;

    /// <summary>
    /// Current transition state.
    /// </summary>
    public ProfileTransitionState CurrentState => _currentState;

    /// <summary>
    /// Whether a transition is currently in progress.
    /// </summary>
    public bool IsTransitioning => _currentState != ProfileTransitionState.Idle;

    /// <summary>
    /// Event raised when transition state changes.
    /// </summary>
    public event EventHandler<TransitionStateChangedEventArgs>? TransitionStateChanged;

    /// <summary>
    /// Event raised when a transition completes (successfully or with error).
    /// </summary>
    public event EventHandler<TransitionCompletedEventArgs>? TransitionCompleted;

    public ProfileSwitcher(UIProfileManager profileManager, ILogger<ProfileSwitcher> logger)
    {
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initiates a smooth transition to a new profile.
    /// </summary>
    /// <param name="targetProfileName">Target profile to switch to.</param>
    /// <param name="options">Transition options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<ProfileTransitionResult> SwitchProfileAsync(
        string targetProfileName, 
        ProfileTransitionOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetProfileName))
            throw new ArgumentException("Target profile name cannot be empty", nameof(targetProfileName));

        options ??= ProfileTransitionOptions.Default;

        lock (_transitionLock)
        {
            if (IsTransitioning)
            {
                throw new InvalidOperationException("A profile transition is already in progress");
            }

            _currentState = ProfileTransitionState.Starting;
        }

        var transition = new ProfileTransition
        {
            TargetProfileName = targetProfileName,
            SourceProfile = _profileManager.ActiveProfile,
            Options = options,
            StartTime = DateTime.UtcNow
        };

        _activeTransition = transition;
        RaiseTransitionStateChanged(ProfileTransitionState.Starting, transition);

        try
        {
            _logger.LogInformation("Starting profile transition to '{TargetProfile}' with options: {Options}",
                targetProfileName, options);

            var result = await ExecuteTransitionAsync(transition, cancellationToken);

            RaiseTransitionCompleted(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile transition to '{TargetProfile}' failed", targetProfileName);
            
            var errorResult = new ProfileTransitionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = DateTime.UtcNow - transition.StartTime
            };

            RaiseTransitionCompleted(errorResult);
            return errorResult;
        }
        finally
        {
            lock (_transitionLock)
            {
                _currentState = ProfileTransitionState.Idle;
                _activeTransition = null;
            }
        }
    }

    /// <summary>
    /// Cancels the current transition if one is in progress.
    /// </summary>
    public bool CancelTransition()
    {
        ProfileTransition? transition;
        
        lock (_transitionLock)
        {
            if (!IsTransitioning || _activeTransition == null)
                return false;

            transition = _activeTransition;
            _currentState = ProfileTransitionState.Cancelling;
        }

        _logger.LogWarning("Cancelling profile transition to '{TargetProfile}'", transition.TargetProfileName);
        
        RaiseTransitionStateChanged(ProfileTransitionState.Cancelling, transition);

        // TODO: Implement transition cancellation logic
        // This would typically involve stopping any ongoing animations,
        // reverting partial state changes, etc.

        lock (_transitionLock)
        {
            _currentState = ProfileTransitionState.Idle;
            _activeTransition = null;
        }

        RaiseTransitionStateChanged(ProfileTransitionState.Idle, null);
        return true;
    }

    private async Task<ProfileTransitionResult> ExecuteTransitionAsync(
        ProfileTransition transition, 
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Phase 1: Validation
            UpdateTransitionState(ProfileTransitionState.Validating, transition);
            await ValidateTransitionAsync(transition, cancellationToken);

            // Phase 2: Preparation
            UpdateTransitionState(ProfileTransitionState.Preparing, transition);
            await PrepareTransitionAsync(transition, cancellationToken);

            // Phase 3: State preservation (if enabled)
            if (transition.Options.PreserveState)
            {
                UpdateTransitionState(ProfileTransitionState.PreservingState, transition);
                await PreserveCurrentStateAsync(transition, cancellationToken);
            }

            // Phase 4: UI transition animation (if enabled)
            if (transition.Options.EnableAnimation)
            {
                UpdateTransitionState(ProfileTransitionState.Animating, transition);
                await ExecuteTransitionAnimationAsync(transition, cancellationToken);
            }

            // Phase 5: Profile switch
            UpdateTransitionState(ProfileTransitionState.Switching, transition);
            await _profileManager.SwitchProfileAsync(transition.TargetProfileName, cancellationToken);

            // Phase 6: State restoration (if needed)
            if (transition.Options.PreserveState)
            {
                UpdateTransitionState(ProfileTransitionState.RestoringState, transition);
                await RestoreStateAsync(transition, cancellationToken);
            }

            // Phase 7: Finalization
            UpdateTransitionState(ProfileTransitionState.Finalizing, transition);
            await FinalizeTransitionAsync(transition, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("Profile transition to '{TargetProfile}' completed successfully in {Duration}ms",
                transition.TargetProfileName, stopwatch.ElapsedMilliseconds);

            return new ProfileTransitionResult
            {
                Success = true,
                Duration = stopwatch.Elapsed,
                SourceProfile = transition.SourceProfile?.Name,
                TargetProfile = transition.TargetProfileName
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Profile transition to '{TargetProfile}' was cancelled", transition.TargetProfileName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Profile transition to '{TargetProfile}' failed during {State} phase", 
                transition.TargetProfileName, _currentState);
            throw;
        }
    }

    private async Task ValidateTransitionAsync(ProfileTransition transition, CancellationToken cancellationToken)
    {
        // Validate target profile exists and is valid
        if (!_profileManager.Profiles.ContainsKey(transition.TargetProfileName))
            throw new ArgumentException($"Target profile '{transition.TargetProfileName}' not found");

        var targetProfile = _profileManager.Profiles[transition.TargetProfileName];
        var validationResult = targetProfile.Validate();

        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Target profile '{transition.TargetProfileName}' is invalid: {string.Join(", ", validationResult.Errors)}");
        }

        await Task.Delay(transition.Options.ValidationDelay, cancellationToken);
    }

    private async Task PrepareTransitionAsync(ProfileTransition transition, CancellationToken cancellationToken)
    {
        // Prepare UI for transition - this could involve hiding/showing elements,
        // preparing animations, etc.
        _logger.LogDebug("Preparing transition from '{SourceProfile}' to '{TargetProfile}'",
            transition.SourceProfile?.Name ?? "none", transition.TargetProfileName);

        await Task.Delay(transition.Options.PreparationDelay, cancellationToken);
    }

    private async Task PreserveCurrentStateAsync(ProfileTransition transition, CancellationToken cancellationToken)
    {
        if (transition.SourceProfile == null)
            return;

        _logger.LogDebug("Preserving state from profile '{SourceProfile}'", transition.SourceProfile.Name);

        // TODO: Implement state preservation logic
        // This would typically save current UI state, window positions, user data, etc.

        await Task.Delay(50, cancellationToken); // Simulate state preservation work
    }

    private async Task ExecuteTransitionAnimationAsync(ProfileTransition transition, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Executing transition animation for profile switch to '{TargetProfile}'", 
            transition.TargetProfileName);

        // TODO: Implement transition animations
        // This could involve fade effects, slide transitions, etc.
        
        await Task.Delay(transition.Options.AnimationDuration, cancellationToken);
    }

    private async Task RestoreStateAsync(ProfileTransition transition, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Restoring preserved state for profile '{TargetProfile}'", transition.TargetProfileName);

        // TODO: Implement state restoration logic
        // This would restore saved state to the new profile context

        await Task.Delay(50, cancellationToken); // Simulate state restoration work
    }

    private async Task FinalizeTransitionAsync(ProfileTransition transition, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Finalizing transition to profile '{TargetProfile}'", transition.TargetProfileName);

        // TODO: Final cleanup, notifications, etc.

        await Task.Delay(10, cancellationToken);
    }

    private void UpdateTransitionState(ProfileTransitionState newState, ProfileTransition transition)
    {
        lock (_transitionLock)
        {
            _currentState = newState;
        }

        RaiseTransitionStateChanged(newState, transition);
    }

    private void RaiseTransitionStateChanged(ProfileTransitionState state, ProfileTransition? transition)
    {
        TransitionStateChanged?.Invoke(this, new TransitionStateChangedEventArgs(state, transition));
    }

    private void RaiseTransitionCompleted(ProfileTransitionResult result)
    {
        TransitionCompleted?.Invoke(this, new TransitionCompletedEventArgs(result));
    }
}

/// <summary>
/// States of the profile transition state machine.
/// </summary>
public enum ProfileTransitionState
{
    Idle,
    Starting,
    Validating,
    Preparing,
    PreservingState,
    Animating,
    Switching,
    RestoringState,
    Finalizing,
    Cancelling
}

/// <summary>
/// Options for controlling profile transitions.
/// </summary>
public class ProfileTransitionOptions
{
    /// <summary>
    /// Whether to preserve current state during transition.
    /// </summary>
    public bool PreserveState { get; init; } = true;

    /// <summary>
    /// Whether to enable transition animations.
    /// </summary>
    public bool EnableAnimation { get; init; } = true;

    /// <summary>
    /// Duration of transition animations.
    /// </summary>
    public TimeSpan AnimationDuration { get; init; } = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Delay during validation phase.
    /// </summary>
    public TimeSpan ValidationDelay { get; init; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Delay during preparation phase.
    /// </summary>
    public TimeSpan PreparationDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Default transition options.
    /// </summary>
    public static ProfileTransitionOptions Default => new();

    /// <summary>
    /// Fast transition options with minimal delays and no animations.
    /// </summary>
    public static ProfileTransitionOptions Fast => new()
    {
        PreserveState = false,
        EnableAnimation = false,
        AnimationDuration = TimeSpan.Zero,
        ValidationDelay = TimeSpan.Zero,
        PreparationDelay = TimeSpan.Zero
    };

    public override string ToString()
    {
        return $"PreserveState={PreserveState}, Animation={EnableAnimation}, Duration={AnimationDuration.TotalMilliseconds}ms";
    }
}

/// <summary>
/// Represents a profile transition operation.
/// </summary>
public class ProfileTransition
{
    public string TargetProfileName { get; init; } = string.Empty;
    public IUIProfile? SourceProfile { get; init; }
    public ProfileTransitionOptions Options { get; init; } = ProfileTransitionOptions.Default;
    public DateTime StartTime { get; init; }
}

/// <summary>
/// Result of a profile transition operation.
/// </summary>
public class ProfileTransitionResult
{
    /// <summary>
    /// Whether the transition completed successfully.
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
    /// Source profile name (null if no source).
    /// </summary>
    public string? SourceProfile { get; init; }

    /// <summary>
    /// Target profile name.
    /// </summary>
    public string? TargetProfile { get; init; }
}

/// <summary>
/// Event arguments for transition state changes.
/// </summary>
public class TransitionStateChangedEventArgs : EventArgs
{
    public ProfileTransitionState State { get; }
    public ProfileTransition? Transition { get; }

    public TransitionStateChangedEventArgs(ProfileTransitionState state, ProfileTransition? transition)
    {
        State = state;
        Transition = transition;
    }
}

/// <summary>
/// Event arguments for transition completion.
/// </summary>
public class TransitionCompletedEventArgs : EventArgs
{
    public ProfileTransitionResult Result { get; }

    public TransitionCompletedEventArgs(ProfileTransitionResult result)
    {
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }
}
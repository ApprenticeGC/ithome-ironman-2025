namespace GameConsole.UI.Profiles;

/// <summary>
/// Provides high-level profile switching operations with state machine pattern implementation.
/// Handles smooth transitions with minimal user disruption and state preservation.
/// </summary>
public sealed class ProfileSwitcher
{
    private readonly UIProfileManager _profileManager;
    private ProfileSwitchState _currentState = ProfileSwitchState.Idle;
    private readonly object _stateLock = new();

    /// <summary>
    /// Initializes a new instance of the ProfileSwitcher.
    /// </summary>
    /// <param name="profileManager">The profile manager to use for switching operations.</param>
    public ProfileSwitcher(UIProfileManager profileManager)
    {
        _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
    }

    /// <summary>
    /// Gets the current state of the profile switcher.
    /// </summary>
    public ProfileSwitchState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    /// <summary>
    /// Event raised when a profile switch operation begins.
    /// </summary>
    public event EventHandler<ProfileSwitchEventArgs>? SwitchStarted;

    /// <summary>
    /// Event raised when a profile switch operation completes successfully.
    /// </summary>
    public event EventHandler<ProfileSwitchEventArgs>? SwitchCompleted;

    /// <summary>
    /// Event raised when a profile switch operation fails.
    /// </summary>
    public event EventHandler<ProfileSwitchErrorEventArgs>? SwitchFailed;

    /// <summary>
    /// Performs a runtime transition to a different UI profile.
    /// This method ensures smooth transitions with state preservation.
    /// </summary>
    /// <param name="targetProfileName">The name of the profile to switch to.</param>
    /// <param name="context">The UI context for the switch operation.</param>
    /// <param name="preserveState">Whether to preserve the current UI state during the switch.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A result indicating the success or failure of the switch operation.</returns>
    public async Task<ProfileSwitchResult> SwitchToProfileAsync(
        string targetProfileName, 
        IUIContext context, 
        bool preserveState = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(targetProfileName);
        ArgumentNullException.ThrowIfNull(context);

        lock (_stateLock)
        {
            if (_currentState != ProfileSwitchState.Idle)
            {
                return new ProfileSwitchResult(false, "Profile switch already in progress");
            }
            _currentState = ProfileSwitchState.Switching;
        }

        try
        {
            var switchArgs = new ProfileSwitchEventArgs(
                _profileManager.ActiveProfile?.Name ?? string.Empty,
                targetProfileName,
                preserveState);

            SwitchStarted?.Invoke(this, switchArgs);

            // Preserve current state if requested
            Dictionary<string, object>? preservedState = null;
            if (preserveState && _profileManager.ActiveProfile != null)
            {
                preservedState = await PreserveCurrentStateAsync(cancellationToken);
            }

            // Perform the actual profile switch
            var switchSuccess = await _profileManager.SwitchToProfileAsync(
                targetProfileName, 
                context, 
                cancellationToken);

            if (!switchSuccess)
            {
                var errorArgs = new ProfileSwitchErrorEventArgs(switchArgs, "Failed to switch to target profile");
                SwitchFailed?.Invoke(this, errorArgs);
                return new ProfileSwitchResult(false, "Profile switch failed");
            }

            // Restore preserved state if available
            if (preservedState != null)
            {
                await RestoreStateAsync(preservedState, cancellationToken);
            }

            SwitchCompleted?.Invoke(this, switchArgs);
            return new ProfileSwitchResult(true);
        }
        catch (Exception ex)
        {
            var errorArgs = new ProfileSwitchErrorEventArgs(
                new ProfileSwitchEventArgs(_profileManager.ActiveProfile?.Name ?? string.Empty, targetProfileName, preserveState),
                ex.Message);
            SwitchFailed?.Invoke(this, errorArgs);
            return new ProfileSwitchResult(false, $"Profile switch error: {ex.Message}");
        }
        finally
        {
            lock (_stateLock)
            {
                _currentState = ProfileSwitchState.Idle;
            }
        }
    }

    private async Task<Dictionary<string, object>> PreserveCurrentStateAsync(CancellationToken cancellationToken)
    {
        var state = new Dictionary<string, object>();
        
        // Preserve basic state information
        // In a real implementation, this would capture:
        // - Current UI state (window positions, selected tabs, etc.)
        // - User input state
        // - Any temporary data that should survive the switch
        
        if (_profileManager.ActiveProfile != null)
        {
            state["ActiveProfileName"] = _profileManager.ActiveProfile.Name;
            state["PreservedAt"] = DateTime.UtcNow;
        }

        await Task.CompletedTask; // Placeholder for actual state preservation logic
        return state;
    }

    private async Task RestoreStateAsync(Dictionary<string, object> preservedState, CancellationToken cancellationToken)
    {
        // Restore the preserved state to the new profile
        // In a real implementation, this would:
        // - Restore window positions and UI layout
        // - Restore user selections and preferences
        // - Apply any temporary data that was preserved
        
        await Task.CompletedTask; // Placeholder for actual state restoration logic
    }
}

/// <summary>
/// Represents the current state of a profile switch operation.
/// </summary>
public enum ProfileSwitchState
{
    /// <summary>
    /// No profile switch operation is currently in progress.
    /// </summary>
    Idle,

    /// <summary>
    /// A profile switch operation is currently in progress.
    /// </summary>
    Switching
}

/// <summary>
/// Contains information about a profile switch operation.
/// </summary>
public sealed class ProfileSwitchEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the ProfileSwitchEventArgs class.
    /// </summary>
    /// <param name="fromProfile">The name of the profile being switched from.</param>
    /// <param name="toProfile">The name of the profile being switched to.</param>
    /// <param name="preserveState">Whether state preservation was requested.</param>
    public ProfileSwitchEventArgs(string fromProfile, string toProfile, bool preserveState)
    {
        FromProfile = fromProfile;
        ToProfile = toProfile;
        PreserveState = preserveState;
    }

    /// <summary>
    /// Gets the name of the profile being switched from.
    /// </summary>
    public string FromProfile { get; }

    /// <summary>
    /// Gets the name of the profile being switched to.
    /// </summary>
    public string ToProfile { get; }

    /// <summary>
    /// Gets a value indicating whether state preservation was requested.
    /// </summary>
    public bool PreserveState { get; }
}

/// <summary>
/// Contains information about a failed profile switch operation.
/// </summary>
public sealed class ProfileSwitchErrorEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the ProfileSwitchErrorEventArgs class.
    /// </summary>
    /// <param name="switchArgs">The original switch arguments.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    public ProfileSwitchErrorEventArgs(ProfileSwitchEventArgs switchArgs, string errorMessage)
    {
        SwitchArgs = switchArgs;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets the original switch arguments.
    /// </summary>
    public ProfileSwitchEventArgs SwitchArgs { get; }

    /// <summary>
    /// Gets the error message describing the failure.
    /// </summary>
    public string ErrorMessage { get; }
}

/// <summary>
/// Represents the result of a profile switch operation.
/// </summary>
public sealed class ProfileSwitchResult
{
    /// <summary>
    /// Initializes a new instance of the ProfileSwitchResult class for a successful operation.
    /// </summary>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="errorMessage">The error message if the operation failed.</param>
    public ProfileSwitchResult(bool success, string? errorMessage = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the switch operation was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the error message if the operation failed; otherwise, null.
    /// </summary>
    public string? ErrorMessage { get; }
}
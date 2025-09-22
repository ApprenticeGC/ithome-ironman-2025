namespace GameConsole.UI.Profiles;

/// <summary>
/// Event arguments for mode change events.
/// </summary>
public class ModeChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous console mode.
    /// </summary>
    public ConsoleMode PreviousMode { get; }

    /// <summary>
    /// Gets the new console mode.
    /// </summary>
    public ConsoleMode NewMode { get; }

    /// <summary>
    /// Initializes a new instance of the ModeChangedEventArgs class.
    /// </summary>
    /// <param name="previousMode">The previous console mode.</param>
    /// <param name="newMode">The new console mode.</param>
    public ModeChangedEventArgs(ConsoleMode previousMode, ConsoleMode newMode)
    {
        PreviousMode = previousMode;
        NewMode = newMode;
    }
}

/// <summary>
/// Event arguments for profile change events.
/// </summary>
public class ProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous UI profile.
    /// </summary>
    public UIProfile? PreviousProfile { get; }

    /// <summary>
    /// Gets the new UI profile.
    /// </summary>
    public UIProfile? NewProfile { get; }

    /// <summary>
    /// Initializes a new instance of the ProfileChangedEventArgs class.
    /// </summary>
    /// <param name="previousProfile">The previous UI profile.</param>
    /// <param name="newProfile">The new UI profile.</param>
    public ProfileChangedEventArgs(UIProfile? previousProfile, UIProfile? newProfile)
    {
        PreviousProfile = previousProfile;
        NewProfile = newProfile;
    }
}
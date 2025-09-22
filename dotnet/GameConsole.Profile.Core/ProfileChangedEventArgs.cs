namespace GameConsole.Profile.Core;

/// <summary>
/// Event arguments for profile change notifications.
/// </summary>
public class ProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previously active profile configuration, or null if none was active.
    /// </summary>
    public IProfileConfiguration? PreviousProfile { get; }

    /// <summary>
    /// Gets the currently active profile configuration, or null if none is active.
    /// </summary>
    public IProfileConfiguration? CurrentProfile { get; }

    /// <summary>
    /// Gets the timestamp when the profile change occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the ProfileChangedEventArgs class.
    /// </summary>
    /// <param name="previousProfile">The previously active profile.</param>
    /// <param name="currentProfile">The currently active profile.</param>
    public ProfileChangedEventArgs(IProfileConfiguration? previousProfile, IProfileConfiguration? currentProfile)
    {
        PreviousProfile = previousProfile;
        CurrentProfile = currentProfile;
        Timestamp = DateTimeOffset.UtcNow;
    }
}
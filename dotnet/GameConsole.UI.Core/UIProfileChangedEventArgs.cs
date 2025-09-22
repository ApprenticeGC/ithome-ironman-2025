namespace GameConsole.UI.Core;

/// <summary>
/// Event arguments for UI profile change events.
/// </summary>
public class UIProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previous UI profile (null if no previous profile).
    /// </summary>
    public IUIProfile? PreviousProfile { get; }

    /// <summary>
    /// Gets the new UI profile (null if profile was removed).
    /// </summary>
    public IUIProfile? NewProfile { get; }

    /// <summary>
    /// Gets the timestamp when the profile change occurred.
    /// </summary>
    public DateTimeOffset ChangeTimestamp { get; }

    /// <summary>
    /// Initializes a new instance of the UIProfileChangedEventArgs class.
    /// </summary>
    /// <param name="previousProfile">The previous UI profile.</param>
    /// <param name="newProfile">The new UI profile.</param>
    public UIProfileChangedEventArgs(IUIProfile? previousProfile, IUIProfile? newProfile)
    {
        PreviousProfile = previousProfile;
        NewProfile = newProfile;
        ChangeTimestamp = DateTimeOffset.UtcNow;
    }
}
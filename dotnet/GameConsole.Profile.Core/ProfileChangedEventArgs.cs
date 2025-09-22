namespace GameConsole.Profile.Core;

/// <summary>
/// Event arguments for profile change notifications.
/// </summary>
public sealed class ProfileChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldProfile">The previously active profile, if any.</param>
    /// <param name="newProfile">The newly activated profile.</param>
    public ProfileChangedEventArgs(IProfile? oldProfile, IProfile newProfile)
    {
        OldProfile = oldProfile;
        NewProfile = newProfile ?? throw new ArgumentNullException(nameof(newProfile));
    }

    /// <summary>
    /// Gets the previously active profile.
    /// </summary>
    public IProfile? OldProfile { get; }

    /// <summary>
    /// Gets the newly activated profile.
    /// </summary>
    public IProfile NewProfile { get; }
}
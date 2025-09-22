using System;

namespace GameConsole.UI.Core
{
    /// <summary>
    /// Event arguments for profile activation events.
    /// </summary>
    public class ProfileActivatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the profile that was activated.
    /// </summary>
    public IUIProfile ActivatedProfile { get; }

    /// <summary>
    /// Gets the profile that was previously active (if any).
    /// </summary>
    public IUIProfile PreviousProfile { get; }

    public ProfileActivatedEventArgs(IUIProfile activatedProfile, IUIProfile previousProfile = null)
    {
        ActivatedProfile = activatedProfile;
        PreviousProfile = previousProfile;
    }
}

/// <summary>
/// Event arguments for profile creation events.
/// </summary>
public class ProfileCreatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the profile that was created.
    /// </summary>
    public IUIProfile CreatedProfile { get; }

    public ProfileCreatedEventArgs(IUIProfile createdProfile)
    {
        CreatedProfile = createdProfile;
    }
}

/// <summary>
/// Event arguments for profile update events.
/// </summary>
public class ProfileUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the profile that was updated.
    /// </summary>
    public IUIProfile UpdatedProfile { get; }

    public ProfileUpdatedEventArgs(IUIProfile updatedProfile)
    {
        UpdatedProfile = updatedProfile;
    }
}

/// <summary>
/// Event arguments for profile deletion events.
/// </summary>
public class ProfileDeletedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the identifier of the profile that was deleted.
    /// </summary>
    public string ProfileId { get; }

    /// <summary>
    /// Gets the name of the profile that was deleted.
    /// </summary>
    public string ProfileName { get; }

    public ProfileDeletedEventArgs(string profileId, string profileName)
    {
        ProfileId = profileId;
        ProfileName = profileName;
    }
    }
}
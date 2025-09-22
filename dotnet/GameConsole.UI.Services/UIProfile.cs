using GameConsole.UI.Core;

namespace GameConsole.UI.Services;

/// <summary>
/// Default implementation of IUIProfile.
/// Represents a UI profile with configuration settings for different UI behavior modes.
/// </summary>
public class UIProfile : IUIProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UIProfile"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this profile.</param>
    /// <param name="name">The human-readable name of this profile.</param>
    /// <param name="description">The description of what this profile provides.</param>
    /// <param name="settings">The configuration settings for this profile.</param>
    public UIProfile(string id, string name, string description, UIProfileSettings settings)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public UIProfileSettings Settings { get; }

    /// <inheritdoc />
    public bool IsActive { get; internal set; }

    /// <summary>
    /// Returns a string representation of this UI profile.
    /// </summary>
    /// <returns>A string containing the profile name and ID.</returns>
    public override string ToString()
    {
        return $"{Name} ({Id})";
    }
}
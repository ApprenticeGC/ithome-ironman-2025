using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Abstract base class for UI profiles that define mode-specific configurations.
/// </summary>
public abstract class UIProfile
{
    /// <summary>
    /// Gets the name of the profile.
    /// </summary>
    public string Name { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets the target console mode for this profile.
    /// </summary>
    public ConsoleMode TargetMode { get; protected set; }

    /// <summary>
    /// Gets the metadata for this profile.
    /// </summary>
    public UIProfileMetadata Metadata { get; protected set; } = new();

    /// <summary>
    /// Gets the command set for this profile.
    /// </summary>
    /// <returns>The command set containing all available commands.</returns>
    public abstract CommandSet GetCommandSet();

    /// <summary>
    /// Gets the layout configuration for this profile.
    /// </summary>
    /// <returns>The layout configuration defining panel arrangement.</returns>
    public abstract LayoutConfiguration GetLayoutConfiguration();

    /// <summary>
    /// Determines whether this profile can be activated.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the profile can be activated; otherwise, false.</returns>
    public virtual Task<bool> CanActivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    /// <summary>
    /// Activates the profile, initializing any required resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the activation operation.</returns>
    public virtual Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deactivates the profile, cleaning up any resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the deactivation operation.</returns>
    public virtual Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
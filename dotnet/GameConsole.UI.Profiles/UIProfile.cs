namespace GameConsole.UI.Profiles;

/// <summary>
/// Abstract base class for UI profiles that define mode-specific interface configurations.
/// UI profiles provide specialized interface settings for different operational modes
/// while maintaining consistency and allowing seamless transitions.
/// </summary>
public abstract class UIProfile
{
    /// <summary>
    /// Gets the unique name of this UI profile.
    /// </summary>
    public string Name { get; protected set; } = string.Empty;

    /// <summary>
    /// Gets the target console mode this profile is designed for.
    /// </summary>
    public ConsoleMode TargetMode { get; protected set; }

    /// <summary>
    /// Gets the metadata describing this profile's characteristics.
    /// </summary>
    public UIProfileMetadata Metadata { get; protected set; } = new();

    /// <summary>
    /// Initializes a new instance of the UIProfile class.
    /// </summary>
    /// <param name="name">The unique name of the profile.</param>
    /// <param name="targetMode">The console mode this profile targets.</param>
    protected UIProfile(string name, ConsoleMode targetMode)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TargetMode = targetMode;
    }

    /// <summary>
    /// Gets the command set configuration for this profile.
    /// </summary>
    /// <returns>The command set configuration.</returns>
    public abstract CommandSet GetCommandSet();

    /// <summary>
    /// Gets the layout configuration for this profile.
    /// </summary>
    /// <returns>The layout configuration.</returns>
    public abstract LayoutConfiguration GetLayoutConfiguration();

    /// <summary>
    /// Gets the key binding configuration for this profile.
    /// </summary>
    /// <returns>The key binding configuration.</returns>
    public abstract KeyBindingSet GetKeyBindings();

    /// <summary>
    /// Gets the menu configuration for this profile.
    /// </summary>
    /// <returns>The menu configuration.</returns>
    public abstract MenuConfiguration GetMenuConfiguration();

    /// <summary>
    /// Gets the status bar configuration for this profile.
    /// </summary>
    /// <returns>The status bar configuration.</returns>
    public abstract StatusBarConfiguration GetStatusBarConfiguration();

    /// <summary>
    /// Gets the toolbar configuration for this profile.
    /// </summary>
    /// <returns>The toolbar configuration.</returns>
    public abstract ToolbarConfiguration GetToolbarConfiguration();

    /// <summary>
    /// Determines whether this profile can be activated in the current context.
    /// </summary>
    /// <returns>True if the profile can be activated; otherwise, false.</returns>
    public virtual bool CanActivate()
    {
        return true;
    }

    /// <summary>
    /// Called when this profile is being activated.
    /// Derived classes can override this to perform profile-specific initialization.
    /// </summary>
    public virtual void OnActivating()
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Called when this profile is being deactivated.
    /// Derived classes can override this to perform profile-specific cleanup.
    /// </summary>
    public virtual void OnDeactivating()
    {
        // Default implementation does nothing
    }
}
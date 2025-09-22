namespace GameConsole.UI.Profiles;

/// <summary>
/// Abstract base class for UI profiles that define mode-specific interface configurations.
/// Each profile provides specialized configurations for different console modes (Game vs Editor).
/// </summary>
public abstract class UIProfile
{
    /// <summary>
    /// Gets the unique name of this profile.
    /// </summary>
    public string Name { get; protected set; } = string.Empty;
    
    /// <summary>
    /// Gets the console mode this profile targets.
    /// </summary>
    public ConsoleMode TargetMode { get; protected set; }
    
    /// <summary>
    /// Gets the metadata for this profile.
    /// </summary>
    public UIProfileMetadata Metadata { get; protected set; } = new();
    
    /// <summary>
    /// Gets the command set available in this profile.
    /// </summary>
    /// <returns>The command set configuration.</returns>
    public abstract CommandSet GetCommandSet();
    
    /// <summary>
    /// Gets the layout configuration for this profile.
    /// </summary>
    /// <returns>The layout configuration.</returns>
    public abstract LayoutConfiguration GetLayoutConfiguration();
    
    /// <summary>
    /// Gets the key bindings for this profile.
    /// </summary>
    /// <returns>The key binding set.</returns>
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
    /// Determines if this profile is applicable for the given context.
    /// </summary>
    /// <param name="context">The context to evaluate.</param>
    /// <returns>True if the profile should be used, false otherwise.</returns>
    public virtual bool IsApplicable(UIProfileContext context)
    {
        return context.Mode == TargetMode;
    }
    
    /// <summary>
    /// Called when the profile is activated.
    /// </summary>
    /// <param name="context">The activation context.</param>
    /// <returns>A task representing the async activation operation.</returns>
    public virtual Task OnActivateAsync(UIProfileContext context)
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Called when the profile is deactivated.
    /// </summary>
    /// <param name="context">The deactivation context.</param>
    /// <returns>A task representing the async deactivation operation.</returns>
    public virtual Task OnDeactivateAsync(UIProfileContext context)
    {
        return Task.CompletedTask;
    }
}
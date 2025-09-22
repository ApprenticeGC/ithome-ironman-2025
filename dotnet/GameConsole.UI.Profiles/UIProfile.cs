using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Profiles;

/// <summary>
/// Abstract base class for UI profiles that define specialized interface configurations
/// for different operational modes (Game vs Editor) in the GameConsole system.
/// Each profile optimizes the UI experience for specific workflows and user interactions.
/// </summary>
public abstract class UIProfile
{
    /// <summary>
    /// Gets the unique identifier for the profile.
    /// </summary>
    public abstract string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the profile.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the console mode this profile is designed for.
    /// </summary>
    public abstract ConsoleMode TargetMode { get; }

    /// <summary>
    /// Gets the metadata associated with this profile.
    /// </summary>
    public abstract UIProfileMetadata Metadata { get; }

    /// <summary>
    /// Gets the command set configuration for this profile.
    /// Defines available commands organized by categories and their properties.
    /// </summary>
    /// <returns>The command set configuration.</returns>
    public abstract CommandSet GetCommandSet();

    /// <summary>
    /// Gets the layout configuration for this profile.
    /// Defines how UI elements are arranged and displayed.
    /// </summary>
    /// <returns>The layout configuration.</returns>
    public abstract LayoutConfiguration GetLayoutConfiguration();

    /// <summary>
    /// Gets the key binding configuration for this profile.
    /// Maps keyboard shortcuts to commands and actions.
    /// </summary>
    /// <returns>The key binding configuration.</returns>
    public abstract KeyBindingSet GetKeyBindings();

    /// <summary>
    /// Gets the menu configuration for this profile.
    /// Defines menu structures and their organization.
    /// </summary>
    /// <returns>The menu configuration.</returns>
    public abstract MenuConfiguration GetMenuConfiguration();

    /// <summary>
    /// Gets the status bar configuration for this profile.
    /// Defines what information is displayed in the status bar and how.
    /// </summary>
    /// <returns>The status bar configuration.</returns>
    public abstract StatusBarConfiguration GetStatusBarConfiguration();

    /// <summary>
    /// Gets the toolbar configuration for this profile.
    /// Defines available toolbars and their organization.
    /// </summary>
    /// <returns>The toolbar configuration.</returns>
    public abstract ToolbarConfiguration GetToolbarConfiguration();

    /// <summary>
    /// Called when the profile is activated (switched to).
    /// Override this method to perform profile-specific initialization.
    /// </summary>
    /// <param name="context">The activation context containing relevant information.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async activation operation.</returns>
    public virtual Task OnActivateAsync(UIProfileActivationContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the profile is deactivated (switched away from).
    /// Override this method to perform profile-specific cleanup.
    /// </summary>
    /// <param name="context">The deactivation context containing relevant information.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async deactivation operation.</returns>
    public virtual Task OnDeactivateAsync(UIProfileDeactivationContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates the profile configuration for consistency and completeness.
    /// </summary>
    /// <returns>A collection of validation errors, empty if the profile is valid.</returns>
    public virtual IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Profile Id cannot be null or empty.");

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Profile Name cannot be null or empty.");

        try
        {
            var commandSet = GetCommandSet();
            if (commandSet.Categories.Count == 0 && commandSet.GlobalCommands.Count == 0)
                errors.Add("Profile must define at least one command or command category.");
        }
        catch (Exception ex)
        {
            errors.Add($"Error retrieving command set: {ex.Message}");
        }

        return errors;
    }
}

/// <summary>
/// Context information provided when a UI profile is activated.
/// </summary>
public sealed class UIProfileActivationContext
{
    /// <summary>
    /// Gets or sets the profile that was previously active (if any).
    /// </summary>
    public UIProfile? PreviousProfile { get; set; }

    /// <summary>
    /// Gets or sets the reason for the profile activation.
    /// </summary>
    public string Reason { get; set; } = "Manual";

    /// <summary>
    /// Gets or sets additional context properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Context information provided when a UI profile is deactivated.
/// </summary>
public sealed class UIProfileDeactivationContext
{
    /// <summary>
    /// Gets or sets the profile that is being activated (if any).
    /// </summary>
    public UIProfile? NextProfile { get; set; }

    /// <summary>
    /// Gets or sets the reason for the profile deactivation.
    /// </summary>
    public string Reason { get; set; } = "Manual";

    /// <summary>
    /// Gets or sets additional context properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
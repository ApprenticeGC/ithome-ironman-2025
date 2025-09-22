namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents a set of commands available in a UI profile.
/// Commands are organized by categories and can have different priorities and access levels.
/// </summary>
public sealed class CommandSet
{
    /// <summary>
    /// Gets or sets the available commands grouped by category.
    /// </summary>
    public Dictionary<string, List<CommandDefinition>> Categories { get; set; } = new Dictionary<string, List<CommandDefinition>>();

    /// <summary>
    /// Gets or sets the default command category that should be prominently displayed.
    /// </summary>
    public string DefaultCategory { get; set; } = "General";

    /// <summary>
    /// Gets or sets commands that should be globally available regardless of context.
    /// </summary>
    public List<CommandDefinition> GlobalCommands { get; set; } = new List<CommandDefinition>();
}

/// <summary>
/// Defines a command available in the UI profile.
/// </summary>
public sealed class CommandDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the command.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the command.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the command does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the primary key binding for this command.
    /// </summary>
    public string KeyBinding { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets alternative key bindings for this command.
    /// </summary>
    public List<string> AlternateKeyBindings { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the priority for command ordering (higher values appear first).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this command is enabled in the current context.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this command should be visible in menus.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the icon identifier for the command (if applicable).
    /// </summary>
    public string Icon { get; set; } = string.Empty;
}
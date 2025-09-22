namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents a command that can be executed within the UI system.
/// </summary>
public class Command
{
    /// <summary>
    /// Gets or sets the unique identifier for this command.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display name of the command.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the description of what this command does.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the keyboard shortcut for this command.
    /// </summary>
    public string? KeyBinding { get; set; }
    
    /// <summary>
    /// Gets or sets the category this command belongs to.
    /// </summary>
    public string Category { get; set; } = "General";
    
    /// <summary>
    /// Gets or sets the priority of this command within its category.
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Gets or sets whether this command is currently available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// A collection of commands available in a UI profile.
/// </summary>
public class CommandSet
{
    private readonly Dictionary<string, Command> _commands = new();
    
    /// <summary>
    /// Gets all commands in this set.
    /// </summary>
    public IReadOnlyCollection<Command> Commands => _commands.Values;
    
    /// <summary>
    /// Adds a command to this set.
    /// </summary>
    /// <param name="command">The command to add.</param>
    public void Add(Command command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands[command.Id] = command;
    }
    
    /// <summary>
    /// Gets a command by its ID.
    /// </summary>
    /// <param name="id">The command ID.</param>
    /// <returns>The command if found, null otherwise.</returns>
    public Command? GetCommand(string id)
    {
        return _commands.TryGetValue(id, out var command) ? command : null;
    }
    
    /// <summary>
    /// Gets all commands in a specific category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <returns>Commands in the specified category.</returns>
    public IEnumerable<Command> GetCommandsByCategory(string category)
    {
        return _commands.Values
            .Where(c => string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Priority);
    }
}
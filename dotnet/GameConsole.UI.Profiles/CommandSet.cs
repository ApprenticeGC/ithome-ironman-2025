namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents a set of commands available in a specific UI profile.
/// Commands are organized by category and can have different priorities and availability.
/// </summary>
public class CommandSet
{
    private readonly Dictionary<string, CommandDefinition> _commands = new();

    /// <summary>
    /// All commands in this set.
    /// </summary>
    public IReadOnlyDictionary<string, CommandDefinition> Commands => _commands.AsReadOnly();

    /// <summary>
    /// Adds a command to the set.
    /// </summary>
    /// <param name="name">Command name.</param>
    /// <param name="definition">Command definition.</param>
    public void AddCommand(string name, CommandDefinition definition)
    {
        _commands[name] = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    /// <summary>
    /// Removes a command from the set.
    /// </summary>
    /// <param name="name">Command name to remove.</param>
    public void RemoveCommand(string name)
    {
        _commands.Remove(name);
    }

    /// <summary>
    /// Gets commands by category.
    /// </summary>
    /// <param name="category">Category to filter by.</param>
    /// <returns>Commands in the specified category.</returns>
    public IEnumerable<KeyValuePair<string, CommandDefinition>> GetCommandsByCategory(string category)
    {
        return _commands.Where(kvp => kvp.Value.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks if a command exists in this set.
    /// </summary>
    /// <param name="name">Command name.</param>
    /// <returns>True if the command exists.</returns>
    public bool HasCommand(string name)
    {
        return _commands.ContainsKey(name);
    }
}

/// <summary>
/// Definition of a command including its metadata and behavior.
/// </summary>
public class CommandDefinition
{
    /// <summary>
    /// Category this command belongs to.
    /// </summary>
    public string Category { get; init; } = "General";

    /// <summary>
    /// Human-readable description of the command.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Priority level for command ordering (higher values appear first).
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Keyboard shortcuts for this command.
    /// </summary>
    public IReadOnlyList<string> KeyboardShortcuts { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this command is available in the current context.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Whether this command is visible in the UI.
    /// </summary>
    public bool IsVisible { get; init; } = true;
}
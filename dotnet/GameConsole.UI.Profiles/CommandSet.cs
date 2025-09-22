namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents a set of commands available in a specific UI profile.
/// Provides command registration, discovery, and execution capabilities.
/// </summary>
public sealed class CommandSet
{
    private readonly Dictionary<string, ICommandDefinition> _commands = new();

    /// <summary>
    /// Gets the collection of available command names in this command set.
    /// </summary>
    public IReadOnlyCollection<string> AvailableCommands => _commands.Keys;

    /// <summary>
    /// Adds a command definition to this command set.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <param name="definition">The command definition.</param>
    public void AddCommand(string commandName, ICommandDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(commandName);
        ArgumentNullException.ThrowIfNull(definition);
        
        _commands[commandName] = definition;
    }

    /// <summary>
    /// Removes a command from this command set.
    /// </summary>
    /// <param name="commandName">The name of the command to remove.</param>
    /// <returns>True if the command was removed; false if it was not found.</returns>
    public bool RemoveCommand(string commandName)
    {
        ArgumentNullException.ThrowIfNull(commandName);
        return _commands.Remove(commandName);
    }

    /// <summary>
    /// Gets a command definition by name.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <returns>The command definition if found; otherwise, null.</returns>
    public ICommandDefinition? GetCommand(string commandName)
    {
        ArgumentNullException.ThrowIfNull(commandName);
        return _commands.GetValueOrDefault(commandName);
    }

    /// <summary>
    /// Checks if a command exists in this command set.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <returns>True if the command exists; otherwise, false.</returns>
    public bool HasCommand(string commandName)
    {
        ArgumentNullException.ThrowIfNull(commandName);
        return _commands.ContainsKey(commandName);
    }
}

/// <summary>
/// Represents a command definition with metadata and execution logic.
/// </summary>
public interface ICommandDefinition
{
    /// <summary>
    /// Gets the name of the command.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of what the command does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the category this command belongs to.
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets whether this command is available in the current context.
    /// </summary>
    bool IsEnabled { get; }
}
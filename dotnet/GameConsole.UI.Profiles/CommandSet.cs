namespace GameConsole.UI.Profiles;

/// <summary>
/// Manages a collection of commands and their aliases for a UI profile.
/// </summary>
public class CommandSet
{
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly Dictionary<string, string> _aliases = new();

    /// <summary>
    /// Gets the collection of registered commands.
    /// </summary>
    public IReadOnlyDictionary<string, ICommand> Commands => _commands;

    /// <summary>
    /// Gets the collection of registered aliases.
    /// </summary>
    public IReadOnlyDictionary<string, string> Aliases => _aliases;

    /// <summary>
    /// Registers a command with the specified name.
    /// </summary>
    /// <param name="name">The command name.</param>
    /// <param name="command">The command instance.</param>
    public void RegisterCommand(string name, ICommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(command);
        
        _commands[name] = command;
    }

    /// <summary>
    /// Registers an alias for a command.
    /// </summary>
    /// <param name="alias">The alias.</param>
    /// <param name="commandName">The name of the command to alias.</param>
    public void RegisterAlias(string alias, string commandName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandName);
        
        _aliases[alias] = commandName;
    }

    /// <summary>
    /// Attempts to get a command by name or alias.
    /// </summary>
    /// <param name="nameOrAlias">The command name or alias.</param>
    /// <param name="command">The command instance if found.</param>
    /// <returns>True if the command was found; otherwise, false.</returns>
    public bool TryGetCommand(string nameOrAlias, out ICommand? command)
    {
        command = null;
        
        if (string.IsNullOrWhiteSpace(nameOrAlias))
            return false;

        if (_commands.TryGetValue(nameOrAlias, out command))
            return true;

        if (_aliases.TryGetValue(nameOrAlias, out string? commandName) && 
            !string.IsNullOrEmpty(commandName))
            return _commands.TryGetValue(commandName, out command);

        return false;
    }
}
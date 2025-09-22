namespace GameConsole.UI.Profiles;

/// <summary>
/// Represents a command that can be executed in the console.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the name of the command.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the command.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the usage information for the command.
    /// </summary>
    string Usage { get; }

    /// <summary>
    /// Executes the command with the specified arguments.
    /// </summary>
    /// <param name="args">The command arguments.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async execution operation.</returns>
    Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default);
}
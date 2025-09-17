namespace TestLib.CQRS;

/// <summary>
/// Interface for command handlers in the CQRS system.
/// Command handlers process commands and perform write operations.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles the specified command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Service for managing Akka.NET actor systems within the GameConsole architecture.
/// Provides lifecycle management and access to the underlying actor system.
/// </summary>
public interface IActorSystemService : IService
{
    /// <summary>
    /// Gets the name of the actor system.
    /// </summary>
    string SystemName { get; }

    /// <summary>
    /// Gets a value indicating whether the actor system is running.
    /// </summary>
    bool IsActorSystemRunning { get; }

    /// <summary>
    /// Creates an actor with the specified name and props.
    /// </summary>
    /// <param name="actorName">The name of the actor to create.</param>
    /// <param name="props">The actor properties.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the actor reference.</returns>
    Task<IActorRef> CreateActorAsync(string actorName, Props props);

    /// <summary>
    /// Gets an actor reference by path.
    /// </summary>
    /// <param name="actorPath">The path to the actor.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the actor reference.</returns>
    Task<IActorRef?> GetActorAsync(string actorPath);

    /// <summary>
    /// Terminates the actor system gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async termination operation.</returns>
    Task TerminateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an actor reference in the GameConsole AI system.
/// This is a simplified abstraction over Akka.NET's IActorRef.
/// </summary>
public interface IActorRef
{
    /// <summary>
    /// Gets the path of the actor.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Sends a message to the actor.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="sender">The sender of the message.</param>
    void Tell(object message, IActorRef? sender = null);

    /// <summary>
    /// Asks the actor a question and waits for a response.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="timeout">The timeout for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
    Task<T> Ask<T>(object message, TimeSpan? timeout = null);
}

/// <summary>
/// Represents actor properties for creating actors.
/// This is a simplified abstraction over Akka.NET's Props.
/// </summary>
public interface Props
{
    /// <summary>
    /// Gets the actor type.
    /// </summary>
    Type ActorType { get; }
}
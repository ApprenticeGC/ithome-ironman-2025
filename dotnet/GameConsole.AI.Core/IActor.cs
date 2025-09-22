using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Represents a unique address for an actor in the system.
/// </summary>
public readonly record struct ActorAddress(string Path)
{
    /// <summary>
    /// Creates an actor address from a path string.
    /// </summary>
    /// <param name="path">The path identifying the actor.</param>
    /// <returns>A new ActorAddress instance.</returns>
    public static ActorAddress From(string path) => new(path);
    
    /// <summary>
    /// Returns the string representation of the actor address.
    /// </summary>
    public override string ToString() => Path;
    
    /// <summary>
    /// Implicit conversion from string to ActorAddress.
    /// </summary>
    /// <param name="path">The path string.</param>
    /// <returns>A new ActorAddress instance.</returns>
    public static implicit operator ActorAddress(string path) => new(path);
}

/// <summary>
/// Base interface for all messages that can be sent between actors.
/// </summary>
public interface IActorMessage
{
    /// <summary>
    /// Gets the unique identifier for this message.
    /// </summary>
    Guid MessageId { get; }
    
    /// <summary>
    /// Gets the timestamp when the message was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }
    
    /// <summary>
    /// Gets the address of the sender actor.
    /// </summary>
    ActorAddress? Sender { get; }
}

/// <summary>
/// Abstract base class for actor messages.
/// </summary>
public abstract record ActorMessage : IActorMessage
{
    /// <inheritdoc />
    public Guid MessageId { get; init; } = Guid.NewGuid();
    
    /// <inheritdoc />
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    
    /// <inheritdoc />
    public ActorAddress? Sender { get; init; }
}

/// <summary>
/// Context information available to an actor during message processing.
/// </summary>
public interface IActorContext
{
    /// <summary>
    /// Gets the address of the current actor.
    /// </summary>
    ActorAddress Self { get; }
    
    /// <summary>
    /// Gets the address of the sender of the current message being processed.
    /// </summary>
    ActorAddress? Sender { get; }
    
    /// <summary>
    /// Gets the current message being processed.
    /// </summary>
    IActorMessage CurrentMessage { get; }
    
    /// <summary>
    /// Sends a message to the specified actor address.
    /// </summary>
    /// <param name="target">The target actor address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async send operation.</returns>
    Task SendAsync(ActorAddress target, IActorMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new child actor.
    /// </summary>
    /// <param name="name">The name of the child actor.</param>
    /// <param name="factory">Factory function to create the actor instance.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the child actor address.</returns>
    Task<ActorAddress> SpawnAsync(string name, Func<IActorContext, IActor> factory, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the current actor gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    Task StopSelfAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Base interface for all actors in the system.
/// Actors are reactive entities that process messages asynchronously.
/// </summary>
public interface IActor : IService
{
    /// <summary>
    /// Gets the address of this actor.
    /// </summary>
    ActorAddress Address { get; }
    
    /// <summary>
    /// Processes an incoming message asynchronously.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="context">The actor context for this message processing.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async message processing operation.</returns>
    Task ReceiveAsync(IActorMessage message, IActorContext context, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Called when the actor encounters an error during message processing.
    /// </summary>
    /// <param name="error">The exception that occurred.</param>
    /// <param name="message">The message that caused the error, if available.</param>
    /// <param name="context">The actor context, if available.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async error handling operation.</returns>
    Task OnErrorAsync(Exception error, IActorMessage? message = null, IActorContext? context = null, CancellationToken cancellationToken = default);
}
using Akka.Actor;
using GameConsole.AI.Core;

namespace GameConsole.AI.Services;

/// <summary>
/// Wrapper around Akka.NET's IActorRef to provide the GameConsole AI Core abstraction.
/// </summary>
internal class ActorRefWrapper : GameConsole.AI.Core.IActorRef
{
    private readonly Akka.Actor.IActorRef _actorRef;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorRefWrapper"/> class.
    /// </summary>
    /// <param name="actorRef">The underlying Akka.NET actor reference.</param>
    public ActorRefWrapper(Akka.Actor.IActorRef actorRef)
    {
        _actorRef = actorRef ?? throw new ArgumentNullException(nameof(actorRef));
    }

    /// <summary>
    /// Gets the path of the actor.
    /// </summary>
    public string Path => _actorRef.Path.ToString();

    /// <summary>
    /// Sends a message to the actor.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="sender">The sender of the message.</param>
    public void Tell(object message, GameConsole.AI.Core.IActorRef? sender = null)
    {
        var akkaActorRef = sender != null ? ((ActorRefWrapper)sender)._actorRef : ActorRefs.NoSender;
        _actorRef.Tell(message, akkaActorRef);
    }

    /// <summary>
    /// Asks the actor a question and waits for a response.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="timeout">The timeout for the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response.</returns>
    public async Task<T> Ask<T>(object message, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
        var response = await _actorRef.Ask<T>(message, actualTimeout);
        return response;
    }

    /// <summary>
    /// Gets the underlying Akka.NET actor reference.
    /// </summary>
    internal Akka.Actor.IActorRef UnderlyingActor => _actorRef;
}

/// <summary>
/// Implementation of Props for creating actors.
/// </summary>
public class ActorProps : GameConsole.AI.Core.Props
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActorProps"/> class.
    /// </summary>
    /// <param name="actorType">The type of the actor to create.</param>
    public ActorProps(Type actorType)
    {
        if (actorType == null)
            throw new ArgumentNullException(nameof(actorType));

        if (!typeof(ActorBase).IsAssignableFrom(actorType))
            throw new ArgumentException($"Actor type must inherit from {nameof(ActorBase)}", nameof(actorType));

        ActorType = actorType;
    }

    /// <summary>
    /// Gets the actor type.
    /// </summary>
    public Type ActorType { get; }

    /// <summary>
    /// Creates ActorProps for the specified actor type.
    /// </summary>
    /// <typeparam name="TActor">The actor type.</typeparam>
    /// <returns>An ActorProps instance for the specified type.</returns>
    public static ActorProps Create<TActor>() where TActor : ActorBase, new()
    {
        return new ActorProps(typeof(TActor));
    }

    /// <summary>
    /// Creates ActorProps for the specified actor type.
    /// </summary>
    /// <param name="actorType">The actor type.</param>
    /// <returns>An ActorProps instance for the specified type.</returns>
    public static ActorProps Create(Type actorType)
    {
        return new ActorProps(actorType);
    }
}
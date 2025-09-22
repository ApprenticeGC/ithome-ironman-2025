namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Base class for implementing actors with common functionality.
/// </summary>
public abstract class ActorBase : IActor
{
    /// <summary>
    /// Called when the actor receives a message.
    /// Override this method to implement message handling logic.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async message processing operation.</returns>
    public abstract Task OnReceiveAsync(object message, IActorContext context);

    /// <summary>
    /// Called when the actor is starting.
    /// Override this method for actor initialization logic.
    /// </summary>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async start operation.</returns>
    public virtual Task OnStartAsync(IActorContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the actor is stopping.
    /// Override this method for cleanup logic before the actor terminates.
    /// </summary>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async stop operation.</returns>
    public virtual Task OnStopAsync(IActorContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the actor is being restarted due to a failure.
    /// Override this method to handle restart logic and potentially reset state.
    /// </summary>
    /// <param name="reason">The exception that caused the restart.</param>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async restart operation.</returns>
    public virtual Task OnRestartAsync(Exception reason, IActorContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a response message for the current sender.
    /// </summary>
    /// <typeparam name="T">The response message type.</typeparam>
    /// <param name="response">The response message.</param>
    /// <param name="context">The actor context.</param>
    /// <returns>A task representing the async response operation.</returns>
    protected async Task ReplyAsync<T>(T response, IActorContext context) where T : class
    {
        if (context.Sender != null)
        {
            await context.Sender.TellAsync(response, context.Self);
        }
    }

    /// <summary>
    /// Forwards a message to another actor.
    /// </summary>
    /// <param name="message">The message to forward.</param>
    /// <param name="target">The target actor to forward to.</param>
    /// <param name="context">The actor context.</param>
    /// <returns>A task representing the async forward operation.</returns>
    protected async Task ForwardAsync(object message, IActorRef target, IActorContext context)
    {
        await target.TellAsync(message, context.Sender);
    }
}
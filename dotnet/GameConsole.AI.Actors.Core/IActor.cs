namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Defines the base interface for all actors in the system.
/// Actors are lightweight, concurrent entities that communicate through message passing.
/// </summary>
public interface IActor
{
    /// <summary>
    /// Called when the actor receives a message.
    /// This is the main message processing method that all actors must implement.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async message processing operation.</returns>
    Task OnReceiveAsync(object message, IActorContext context);

    /// <summary>
    /// Called when the actor is starting.
    /// Use this method for actor initialization logic.
    /// </summary>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async start operation.</returns>
    Task OnStartAsync(IActorContext context);

    /// <summary>
    /// Called when the actor is stopping.
    /// Use this method for cleanup logic before the actor terminates.
    /// </summary>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async stop operation.</returns>
    Task OnStopAsync(IActorContext context);

    /// <summary>
    /// Called when the actor is being restarted due to a failure.
    /// Use this method to handle restart logic and potentially reset state.
    /// </summary>
    /// <param name="reason">The exception that caused the restart.</param>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async restart operation.</returns>
    Task OnRestartAsync(Exception reason, IActorContext context);
}
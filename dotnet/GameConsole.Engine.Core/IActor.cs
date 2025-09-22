using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Actor state enumeration for tracking actor lifecycle.
/// </summary>
public enum ActorState
{
    /// <summary>
    /// Actor has been created but not yet initialized.
    /// </summary>
    Created,
    
    /// <summary>
    /// Actor is initializing.
    /// </summary>
    Initializing,
    
    /// <summary>
    /// Actor is active and processing messages.
    /// </summary>
    Active,
    
    /// <summary>
    /// Actor is paused and not processing messages.
    /// </summary>
    Paused,
    
    /// <summary>
    /// Actor is shutting down.
    /// </summary>
    Stopping,
    
    /// <summary>
    /// Actor has been stopped and is no longer processing messages.
    /// </summary>
    Stopped,
    
    /// <summary>
    /// Actor has encountered an error and is in a failed state.
    /// </summary>
    Faulted
}

/// <summary>
/// Represents a message that can be sent to an actor.
/// </summary>
public abstract class ActorMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public Guid MessageId { get; }
    
    /// <summary>
    /// The timestamp when this message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
    
    /// <summary>
    /// The actor ID that sent this message, if any.
    /// </summary>
    public string? SenderId { get; }

    /// <summary>
    /// Initializes a new instance of the ActorMessage class.
    /// </summary>
    /// <param name="senderId">Optional ID of the actor that sent this message.</param>
    protected ActorMessage(string? senderId = null)
    {
        MessageId = Guid.NewGuid();
        Timestamp = DateTimeOffset.UtcNow;
        SenderId = senderId;
    }
}

/// <summary>
/// Arguments for actor state change events.
/// </summary>
public class ActorStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// The actor's previous state.
    /// </summary>
    public ActorState PreviousState { get; }
    
    /// <summary>
    /// The actor's new state.
    /// </summary>
    public ActorState NewState { get; }
    
    /// <summary>
    /// The actor ID that changed state.
    /// </summary>
    public string ActorId { get; }

    /// <summary>
    /// Initializes a new instance of the ActorStateChangedEventArgs class.
    /// </summary>
    /// <param name="actorId">The actor ID that changed state.</param>
    /// <param name="previousState">The actor's previous state.</param>
    /// <param name="newState">The actor's new state.</param>
    public ActorStateChangedEventArgs(string actorId, ActorState previousState, ActorState newState)
    {
        ActorId = actorId ?? throw new ArgumentNullException(nameof(actorId));
        PreviousState = previousState;
        NewState = newState;
    }
}

/// <summary>
/// Tier 2: Base interface for all actors in the AI Agent Actor system.
/// Defines the fundamental operations for message-based actor communication,
/// state management, and lifecycle operations within clusters.
/// </summary>
public interface IActor : IAsyncDisposable
{
    /// <summary>
    /// Event raised when the actor's state changes.
    /// </summary>
    event EventHandler<ActorStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Gets the unique identifier for this actor.
    /// </summary>
    string ActorId { get; }
    
    /// <summary>
    /// Gets the current state of this actor.
    /// </summary>
    ActorState State { get; }
    
    /// <summary>
    /// Gets the cluster ID this actor belongs to, if any.
    /// </summary>
    string? ClusterId { get; }

    /// <summary>
    /// Initializes the actor asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the actor and begins processing messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the actor, temporarily stopping message processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async pause operation.</returns>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes the actor after being paused.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resume operation.</returns>
    Task ResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the actor and ceases message processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to this actor for processing.
    /// </summary>
    /// <param name="message">The message to send to this actor.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async message sending operation.</returns>
    Task SendMessageAsync(ActorMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to another actor.
    /// </summary>
    /// <param name="targetActorId">The ID of the target actor.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async message sending operation.</returns>
    Task SendMessageToActorAsync(string targetActorId, ActorMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Joins the actor to a cluster.
    /// </summary>
    /// <param name="clusterId">The ID of the cluster to join.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster join operation.</returns>
    Task JoinClusterAsync(string clusterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves the current cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster leave operation.</returns>
    Task LeaveClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for this actor.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns performance metrics.</returns>
    Task<IDictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default);
}
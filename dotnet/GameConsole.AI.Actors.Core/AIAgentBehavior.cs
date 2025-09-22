namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Represents the behavior of an AI agent, defining its decision-making logic.
/// </summary>
public abstract class AIAgentBehavior
{
    /// <summary>
    /// Processes a message and determines the appropriate response or action.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="state">The current agent state.</param>
    /// <param name="context">The actor context.</param>
    /// <returns>A task representing the async processing operation that returns the behavior result.</returns>
    public abstract Task<AIBehaviorResult> ProcessAsync(object message, AIAgentState state, IActorContext context);

    /// <summary>
    /// Called when the agent is initializing its behavior.
    /// </summary>
    /// <param name="state">The current agent state.</param>
    /// <param name="context">The actor context.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    public virtual Task InitializeAsync(AIAgentState state, IActorContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the agent behavior is being updated or changed.
    /// </summary>
    /// <param name="oldState">The previous agent state.</param>
    /// <param name="newState">The new agent state.</param>
    /// <param name="context">The actor context.</param>
    /// <returns>A task representing the async update operation.</returns>
    public virtual Task OnStateChangedAsync(AIAgentState oldState, AIAgentState newState, IActorContext context)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Represents the result of an AI behavior processing operation.
/// </summary>
public class AIBehaviorResult
{
    /// <summary>
    /// Gets the action to take as a result of processing.
    /// </summary>
    public AIBehaviorAction Action { get; init; } = AIBehaviorAction.Continue;

    /// <summary>
    /// Gets the response message to send, if any.
    /// </summary>
    public object? Response { get; init; }

    /// <summary>
    /// Gets the target actor to send a message to, if any.
    /// </summary>
    public IActorRef? Target { get; init; }

    /// <summary>
    /// Gets the message to send to the target, if any.
    /// </summary>
    public object? MessageToSend { get; init; }

    /// <summary>
    /// Gets the updated agent state, if changed.
    /// </summary>
    public AIAgentState? UpdatedState { get; init; }

    /// <summary>
    /// Creates a result indicating the agent should continue normally.
    /// </summary>
    public static AIBehaviorResult Continue() => new() { Action = AIBehaviorAction.Continue };

    /// <summary>
    /// Creates a result with a response message.
    /// </summary>
    public static AIBehaviorResult Reply(object response) => new() 
    { 
        Action = AIBehaviorAction.Reply, 
        Response = response 
    };

    /// <summary>
    /// Creates a result that sends a message to another actor.
    /// </summary>
    public static AIBehaviorResult SendTo(IActorRef target, object message) => new()
    {
        Action = AIBehaviorAction.SendMessage,
        Target = target,
        MessageToSend = message
    };

    /// <summary>
    /// Creates a result that updates the agent state.
    /// </summary>
    public static AIBehaviorResult UpdateState(AIAgentState newState) => new()
    {
        Action = AIBehaviorAction.UpdateState,
        UpdatedState = newState
    };
}

/// <summary>
/// Defines the actions an AI behavior can take.
/// </summary>
public enum AIBehaviorAction
{
    /// <summary>
    /// Continue processing normally without special actions.
    /// </summary>
    Continue,

    /// <summary>
    /// Send a reply message to the sender.
    /// </summary>
    Reply,

    /// <summary>
    /// Send a message to another actor.
    /// </summary>
    SendMessage,

    /// <summary>
    /// Update the agent's state.
    /// </summary>
    UpdateState,

    /// <summary>
    /// Stop the agent actor.
    /// </summary>
    Stop
}
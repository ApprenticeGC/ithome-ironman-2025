using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Base class for AI agent actors that provides behavior-driven AI processing.
/// </summary>
public abstract class AIAgentActor : ActorBase
{
    private AIAgentState _state = new();
    private AIAgentBehavior? _behavior;

    /// <summary>
    /// Gets the current state of the AI agent.
    /// </summary>
    protected AIAgentState State => _state;

    /// <summary>
    /// Gets the current behavior of the AI agent.
    /// </summary>
    protected AIAgentBehavior? Behavior => _behavior;

    /// <summary>
    /// Creates and returns the behavior for this AI agent.
    /// Override this method to provide custom AI behavior.
    /// </summary>
    /// <param name="context">The actor context.</param>
    /// <returns>A task that returns the AI agent behavior.</returns>
    protected abstract Task<AIAgentBehavior> CreateBehaviorAsync(IActorContext context);

    /// <summary>
    /// Called when the actor is starting.
    /// Initializes the AI agent behavior and state.
    /// </summary>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async start operation.</returns>
    public override async Task OnStartAsync(IActorContext context)
    {
        // Initialize agent state
        _state.AgentId = context.Self.Name;
        _state.AgentType = GetType().Name;
        _state.Status = AIAgentStatus.Idle;

        // Create behavior
        _behavior = await CreateBehaviorAsync(context);
        if (_behavior != null)
        {
            await _behavior.InitializeAsync(_state, context);
        }

        await base.OnStartAsync(context);
    }

    /// <summary>
    /// Called when the actor receives a message.
    /// Processes the message through the AI behavior pipeline.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async message processing operation.</returns>
    public override async Task OnReceiveAsync(object message, IActorContext context)
    {
        try
        {
            // Update status to processing
            var oldStatus = _state.Status;
            _state.Status = AIAgentStatus.Processing;

            // Process message through behavior
            AIBehaviorResult result;
            if (_behavior != null)
            {
                result = await _behavior.ProcessAsync(message, _state, context);
            }
            else
            {
                result = await ProcessMessageDirectlyAsync(message, context);
            }

            // Handle behavior result
            await HandleBehaviorResultAsync(result, context);

            // Reset status if not changed by behavior
            if (_state.Status == AIAgentStatus.Processing)
            {
                _state.Status = oldStatus == AIAgentStatus.Processing ? AIAgentStatus.Idle : oldStatus;
            }
        }
        catch (Exception ex)
        {
            _state.Status = AIAgentStatus.Error;
            await HandleErrorAsync(ex, message, context);
        }
    }

    /// <summary>
    /// Called when the actor is being restarted due to a failure.
    /// Resets the agent state and reinitializes behavior.
    /// </summary>
    /// <param name="reason">The exception that caused the restart.</param>
    /// <param name="context">The actor context providing runtime information.</param>
    /// <returns>A task representing the async restart operation.</returns>
    public override async Task OnRestartAsync(Exception reason, IActorContext context)
    {
        _state.Status = AIAgentStatus.Error;
        
        // Reinitialize behavior
        try
        {
            _behavior = await CreateBehaviorAsync(context);
            if (_behavior != null)
            {
                await _behavior.InitializeAsync(_state, context);
            }
            _state.Status = AIAgentStatus.Idle;
        }
        catch (Exception)
        {
            _state.Status = AIAgentStatus.Offline;
            // Log error but don't throw to avoid restart loop
        }

        await base.OnRestartAsync(reason, context);
    }

    /// <summary>
    /// Processes a message directly without behavior (fallback method).
    /// Override this method for simple message processing without complex AI behavior.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="context">The actor context.</param>
    /// <returns>A task that returns the behavior result.</returns>
    protected virtual Task<AIBehaviorResult> ProcessMessageDirectlyAsync(object message, IActorContext context)
    {
        // Default implementation returns continue
        return Task.FromResult(AIBehaviorResult.Continue());
    }

    /// <summary>
    /// Handles the result of AI behavior processing.
    /// </summary>
    /// <param name="result">The behavior result to handle.</param>
    /// <param name="context">The actor context.</param>
    /// <returns>A task representing the async handling operation.</returns>
    protected virtual async Task HandleBehaviorResultAsync(AIBehaviorResult result, IActorContext context)
    {
        switch (result.Action)
        {
            case AIBehaviorAction.Reply when result.Response != null:
                await ReplyAsync(result.Response, context);
                break;

            case AIBehaviorAction.SendMessage when result.Target != null && result.MessageToSend != null:
                await result.Target.TellAsync(result.MessageToSend, context.Self);
                break;

            case AIBehaviorAction.UpdateState when result.UpdatedState != null:
                var oldState = _state;
                _state = result.UpdatedState;
                if (_behavior != null)
                {
                    await _behavior.OnStateChangedAsync(oldState, _state, context);
                }
                break;

            case AIBehaviorAction.Stop:
                await context.StopSelfAsync();
                break;

            case AIBehaviorAction.Continue:
            default:
                // No special action needed
                break;
        }
    }

    /// <summary>
    /// Handles errors that occur during message processing.
    /// Override this method to customize error handling behavior.
    /// </summary>
    /// <param name="error">The error that occurred.</param>
    /// <param name="message">The message being processed when the error occurred.</param>
    /// <param name="context">The actor context.</param>
    /// <returns>A task representing the async error handling operation.</returns>
    protected virtual Task HandleErrorAsync(Exception error, object message, IActorContext context)
    {
        // Default implementation does nothing - rely on supervision strategy
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates the agent state.
    /// </summary>
    /// <param name="updater">Function to update the state.</param>
    protected void UpdateState(Action<AIAgentState> updater)
    {
        updater(_state);
        _state.LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the current agent status.
    /// </summary>
    /// <returns>The current status of the agent.</returns>
    public AIAgentStatus GetStatus() => _state.Status;

    /// <summary>
    /// Gets agent information for monitoring purposes.
    /// </summary>
    /// <returns>A read-only copy of the agent state.</returns>
    public AIAgentState GetStateSnapshot() => _state.Clone();
}
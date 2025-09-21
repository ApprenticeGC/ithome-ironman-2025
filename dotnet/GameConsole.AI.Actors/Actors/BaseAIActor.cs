using Akka.Actor;
using Microsoft.Extensions.Logging;
using GameConsole.AI.Actors.Messages;

namespace GameConsole.AI.Actors.Actors;

/// <summary>
/// Base abstract class for all AI agent actors.
/// Provides common functionality and patterns for AI agents.
/// </summary>
public abstract class BaseAIActor : ReceiveActor
{
    protected readonly ILogger Logger;
    protected readonly string ActorId;

    protected BaseAIActor(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ActorId = Self.Path.Name;

        // Common message handlers
        Receive<InvokeAgent>(HandleInvokeAgent);
        Receive<StreamAgent>(HandleStreamAgent);
        Receive<GetAgentInfo>(HandleGetAgentInfo);

        Logger.LogDebug("BaseAIActor {ActorId} created", ActorId);
    }

    /// <summary>
    /// Handle agent invocation request.
    /// Derived classes should override ProcessInvokeAgent to implement specific logic.
    /// </summary>
    protected virtual void HandleInvokeAgent(InvokeAgent message)
    {
        Logger.LogDebug("Processing InvokeAgent request for {AgentId}: {Input}", message.AgentId, message.Input);
        
        try
        {
            var response = ProcessInvokeAgent(message);
            Sender.Tell(response);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing InvokeAgent request for {AgentId}", message.AgentId);
            var errorResponse = new AgentResponse(message.AgentId, "", false, ex.Message);
            Sender.Tell(errorResponse);
        }
    }

    /// <summary>
    /// Handle streaming agent request.
    /// Derived classes should override ProcessStreamAgent to implement specific logic.
    /// </summary>
    protected virtual void HandleStreamAgent(StreamAgent message)
    {
        Logger.LogDebug("Processing StreamAgent request for {AgentId}: {Input}", message.AgentId, message.Input);
        
        try
        {
            ProcessStreamAgent(message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing StreamAgent request for {AgentId}", message.AgentId);
            var errorChunk = new AgentStreamChunk(message.AgentId, $"Error: {ex.Message}", true);
            Sender.Tell(errorChunk);
        }
    }

    /// <summary>
    /// Handle agent info request.
    /// </summary>
    protected virtual void HandleGetAgentInfo(GetAgentInfo message)
    {
        Logger.LogDebug("Processing GetAgentInfo request for {AgentId}", message.AgentId);
        
        var metadata = GetAgentMetadata();
        var response = new AgentInfoResponse(message.AgentId, metadata);
        Sender.Tell(response);
    }

    /// <summary>
    /// Process agent invocation. Must be implemented by derived classes.
    /// </summary>
    protected abstract AgentResponse ProcessInvokeAgent(InvokeAgent message);

    /// <summary>
    /// Process streaming agent request. Must be implemented by derived classes.
    /// </summary>
    protected abstract void ProcessStreamAgent(StreamAgent message);

    /// <summary>
    /// Get metadata for this agent. Must be implemented by derived classes.
    /// </summary>
    protected abstract AgentMetadata GetAgentMetadata();

    /// <summary>
    /// Called when actor is starting.
    /// </summary>
    protected override void PreStart()
    {
        Logger.LogInformation("AI Actor {ActorId} starting", ActorId);
        base.PreStart();
    }

    /// <summary>
    /// Called before actor restart due to failure.
    /// </summary>
    protected override void PreRestart(Exception reason, object? message)
    {
        Logger.LogWarning(reason, "AI Actor {ActorId} restarting due to failure. Message: {Message}", ActorId, message?.ToString());
        base.PreRestart(reason, message);
    }

    /// <summary>
    /// Called after actor restart.
    /// </summary>
    protected override void PostRestart(Exception reason)
    {
        Logger.LogInformation("AI Actor {ActorId} restarted after failure: {Reason}", ActorId, reason.Message);
        base.PostRestart(reason);
    }

    /// <summary>
    /// Called when actor is stopping.
    /// </summary>
    protected override void PostStop()
    {
        Logger.LogInformation("AI Actor {ActorId} stopped", ActorId);
        base.PostStop();
    }

    /// <summary>
    /// Supervision strategy for child actors.
    /// </summary>
    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            maxNrOfRetries: 3,
            withinTimeRange: TimeSpan.FromMinutes(1),
            localOnlyDecider: exception =>
            {
                Logger.LogError(exception, "Child actor of {ActorId} failed", ActorId);
                
                return exception switch
                {
                    ActorInitializationException => Directive.Stop,
                    ActorKilledException => Directive.Stop,
                    ArgumentException => Directive.Restart,
                    InvalidOperationException => Directive.Restart,
                    _ => Directive.Escalate
                };
            });
    }
}
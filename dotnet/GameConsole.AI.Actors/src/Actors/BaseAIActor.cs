using Akka.Actor;
using Akka.Event;
using GameConsole.AI.Actors.Messages;

namespace GameConsole.AI.Actors.Actors;

/// <summary>
/// Base abstract actor class providing common functionality for AI agent actors
/// </summary>
public abstract class BaseAIActor : ReceiveActor
{
    /// <summary>
    /// Logger instance for the actor
    /// </summary>
    protected readonly ILoggingAdapter Logger;

    /// <summary>
    /// Unique identifier for this agent instance
    /// </summary>
    protected readonly string AgentId;

    /// <summary>
    /// Timestamp when the actor was created
    /// </summary>
    protected readonly DateTime CreatedAt;

    /// <summary>
    /// Timestamp of the last activity
    /// </summary>
    protected DateTime LastActivity;

    /// <summary>
    /// Whether the agent is currently running
    /// </summary>
    protected bool IsRunning;

    /// <summary>
    /// Configuration object for the agent
    /// </summary>
    protected readonly object? Configuration;

    /// <summary>
    /// Current conversation contexts managed by this agent
    /// </summary>
    protected readonly Dictionary<string, object> Conversations;

    /// <summary>
    /// Initializes a new instance of the BaseAIActor
    /// </summary>
    /// <param name="agentId">Unique identifier for this agent</param>
    /// <param name="configuration">Optional configuration object</param>
    protected BaseAIActor(string agentId, object? configuration = null)
    {
        AgentId = agentId;
        Configuration = configuration;
        CreatedAt = DateTime.UtcNow;
        LastActivity = CreatedAt;
        IsRunning = false;
        Conversations = new Dictionary<string, object>();
        Logger = Context.GetLogger();

        // Set up common message handlers
        SetupBaseMessageHandlers();
        
        // Allow derived classes to set up their specific handlers
        SetupSpecificMessageHandlers();
        
        // Set up fallback handlers for unhandled agent processing messages
        Receive<InvokeAgent>(msg => HandleInvokeAgent(msg));
        Receive<StreamAgent>(msg => HandleStreamAgent(msg));
    }

    /// <summary>
    /// Sets up base message handlers common to all AI actors
    /// </summary>
    private void SetupBaseMessageHandlers()
    {
        // Agent status queries
        Receive<GetAgentStatus>(msg => HandleGetAgentStatus(msg));

        // Conversation management
        Receive<CreateConversation>(msg => HandleCreateConversation(msg));
        Receive<EndConversation>(msg => HandleEndConversation(msg));

        // Lifecycle events
        Receive<StopAgent>(msg => HandleStopAgent(msg));
    }

    /// <summary>
    /// Abstract method for derived classes to set up their specific message handlers
    /// </summary>
    protected abstract void SetupSpecificMessageHandlers();

    /// <summary>
    /// Handles agent status queries
    /// </summary>
    /// <param name="message">The status query message</param>
    protected virtual void HandleGetAgentStatus(GetAgentStatus message)
    {
        Logger.Debug($"Agent {AgentId} received status query");
        UpdateLastActivity();

        var status = new AgentStatus(AgentId, IsRunning, LastActivity);
        Sender.Tell(status);
    }

    /// <summary>
    /// Handles conversation creation requests
    /// </summary>
    /// <param name="message">The conversation creation message</param>
    protected virtual void HandleCreateConversation(CreateConversation message)
    {
        Logger.Info($"Agent {AgentId} creating conversation {message.ConversationId}");
        UpdateLastActivity();

        if (!Conversations.ContainsKey(message.ConversationId))
        {
            // Create conversation context - can be overridden in derived classes
            var context = CreateConversationContext(message.ConversationId);
            Conversations[message.ConversationId] = context;
            
            Logger.Info($"Conversation {message.ConversationId} created for agent {AgentId}");
            Sender.Tell(true);
        }
        else
        {
            Logger.Warning($"Conversation {message.ConversationId} already exists for agent {AgentId}");
            Sender.Tell(false);
        }
    }

    /// <summary>
    /// Handles conversation termination requests
    /// </summary>
    /// <param name="message">The conversation termination message</param>
    protected virtual void HandleEndConversation(EndConversation message)
    {
        Logger.Info($"Agent {AgentId} ending conversation {message.ConversationId}");
        UpdateLastActivity();

        if (Conversations.Remove(message.ConversationId))
        {
            Logger.Info($"Conversation {message.ConversationId} ended for agent {AgentId}");
            Sender.Tell(true);
        }
        else
        {
            Logger.Warning($"Conversation {message.ConversationId} not found for agent {AgentId}");
            Sender.Tell(false);
        }
    }

    /// <summary>
    /// Handles agent invocation requests - should be overridden in derived classes
    /// </summary>
    /// <param name="message">The agent invocation message</param>
    protected virtual void HandleInvokeAgent(InvokeAgent message)
    {
        Logger.Warning($"Agent {AgentId} received InvokeAgent but no specific handler implemented");
        UpdateLastActivity();

        var response = new AgentResponse(
            AgentId, 
            "No implementation provided", 
            message.ConversationId, 
            false, 
            "Agent type does not support synchronous invocation");
        
        Sender.Tell(response);
    }

    /// <summary>
    /// Handles agent streaming requests - should be overridden in derived classes
    /// </summary>
    /// <param name="message">The agent streaming message</param>
    protected virtual void HandleStreamAgent(StreamAgent message)
    {
        Logger.Warning($"Agent {AgentId} received StreamAgent but no specific handler implemented");
        UpdateLastActivity();

        var response = new AgentStreamChunk(
            AgentId, 
            "No implementation provided", 
            message.ConversationId, 
            true);
        
        Sender.Tell(response);
    }

    /// <summary>
    /// Handles agent stop requests
    /// </summary>
    /// <param name="message">The agent stop message</param>
    protected virtual void HandleStopAgent(StopAgent message)
    {
        Logger.Info($"Agent {AgentId} received stop request");
        UpdateLastActivity();
        IsRunning = false;

        // Clean up conversations
        Conversations.Clear();

        // Perform cleanup
        OnStopping();

        // Stop the actor
        Context.Stop(Self);
    }

    /// <summary>
    /// Creates a conversation context object - can be overridden in derived classes
    /// </summary>
    /// <param name="conversationId">The conversation identifier</param>
    /// <returns>A conversation context object</returns>
    protected virtual object CreateConversationContext(string conversationId)
    {
        return new { ConversationId = conversationId, CreatedAt = DateTime.UtcNow };
    }

    /// <summary>
    /// Updates the last activity timestamp
    /// </summary>
    protected void UpdateLastActivity()
    {
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Called when the actor is starting up
    /// </summary>
    protected override void PreStart()
    {
        base.PreStart();
        Logger.Info($"Agent {AgentId} starting up");
        IsRunning = true;
        OnStarting();
    }

    /// <summary>
    /// Called when the actor is stopping
    /// </summary>
    protected override void PostStop()
    {
        Logger.Info($"Agent {AgentId} stopped");
        IsRunning = false;
        OnStopped();
        base.PostStop();
    }

    /// <summary>
    /// Called when the actor is restarting due to an exception
    /// </summary>
    protected override void PreRestart(Exception reason, object message)
    {
        Logger.Warning($"Agent {AgentId} restarting due to: {reason.Message}");
        OnRestarting(reason, message);
        base.PreRestart(reason, message);
    }

    /// <summary>
    /// Called when the actor has restarted
    /// </summary>
    protected override void PostRestart(Exception reason)
    {
        Logger.Info($"Agent {AgentId} restarted after: {reason.Message}");
        IsRunning = true;
        OnRestarted(reason);
        base.PostRestart(reason);
    }

    /// <summary>
    /// Called during actor startup - can be overridden in derived classes
    /// </summary>
    protected virtual void OnStarting() { }

    /// <summary>
    /// Called during actor stopping - can be overridden in derived classes
    /// </summary>
    protected virtual void OnStopping() { }

    /// <summary>
    /// Called after actor has stopped - can be overridden in derived classes
    /// </summary>
    protected virtual void OnStopped() { }

    /// <summary>
    /// Called before actor restart - can be overridden in derived classes
    /// </summary>
    /// <param name="reason">The exception that caused the restart</param>
    /// <param name="message">The message being processed when the exception occurred</param>
    protected virtual void OnRestarting(Exception reason, object message) { }

    /// <summary>
    /// Called after actor restart - can be overridden in derived classes
    /// </summary>
    /// <param name="reason">The exception that caused the restart</param>
    protected virtual void OnRestarted(Exception reason) { }

    /// <summary>
    /// Supervisor strategy for child actors - can be overridden in derived classes
    /// </summary>
    /// <returns>The supervision strategy</returns>
    protected override SupervisorStrategy SupervisorStrategy()
    {
        return new OneForOneStrategy(
            maxNrOfRetries: 10,
            withinTimeRange: TimeSpan.FromMinutes(1),
            decider: Decider.From(exception => exception switch
            {
                ArgumentException => Directive.Stop,
                InvalidOperationException => Directive.Restart,
                TimeoutException => Directive.Restart,
                _ => Directive.Escalate
            }));
    }
}
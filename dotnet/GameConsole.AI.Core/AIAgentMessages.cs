namespace GameConsole.AI.Core;

/// <summary>
/// Standard system messages for actor lifecycle management.
/// </summary>
public static class SystemMessages
{
    /// <summary>
    /// Message sent to an actor to request it to start.
    /// </summary>
    public sealed record StartMessage : ActorMessage;
    
    /// <summary>
    /// Message sent to an actor to request it to stop.
    /// </summary>
    public sealed record StopMessage(string? Reason = null) : ActorMessage;
    
    /// <summary>
    /// Message sent to notify that an actor has been terminated.
    /// </summary>
    public sealed record TerminatedMessage(ActorAddress Actor, Exception? Error = null) : ActorMessage;
    
    /// <summary>
    /// Message sent to request the status of an actor.
    /// </summary>
    public sealed record StatusRequestMessage : ActorMessage;
    
    /// <summary>
    /// Message sent in response to a status request.
    /// </summary>
    public sealed record StatusResponseMessage(string Status, Dictionary<string, object>? Properties = null) : ActorMessage;
    
    /// <summary>
    /// Message sent to request an actor to restart.
    /// </summary>
    public sealed record RestartMessage(Exception? Cause = null) : ActorMessage;
}

/// <summary>
/// AI-specific messages for agent coordination and behavior.
/// </summary>
public static class AIAgentMessages
{
    /// <summary>
    /// Message to request an AI agent to perform a specific task.
    /// </summary>
    public sealed record TaskRequestMessage(string TaskId, string TaskType, Dictionary<string, object> Parameters) : ActorMessage;
    
    /// <summary>
    /// Message sent when an AI agent completes a task.
    /// </summary>
    public sealed record TaskCompletedMessage(string TaskId, bool Success, Dictionary<string, object>? Results = null, string? Error = null) : ActorMessage;
    
    /// <summary>
    /// Message to report task progress.
    /// </summary>
    public sealed record TaskProgressMessage(string TaskId, double Progress, string? Status = null) : ActorMessage;
    
    /// <summary>
    /// Message to cancel a running task.
    /// </summary>
    public sealed record TaskCancelMessage(string TaskId, string? Reason = null) : ActorMessage;
    
    /// <summary>
    /// Message for agents to coordinate and share information.
    /// </summary>
    public sealed record AgentCoordinationMessage(string CoordinationType, Dictionary<string, object> Data) : ActorMessage;
    
    /// <summary>
    /// Message to request collaboration with another agent.
    /// </summary>
    public sealed record CollaborationRequestMessage(ActorAddress RequestingAgent, string CollaborationType, Dictionary<string, object> Context) : ActorMessage;
    
    /// <summary>
    /// Message to respond to a collaboration request.
    /// </summary>
    public sealed record CollaborationResponseMessage(string CollaborationType, bool Accepted, string? Reason = null) : ActorMessage;
    
    /// <summary>
    /// Message to share knowledge or data between agents.
    /// </summary>
    public sealed record KnowledgeSharingMessage(string KnowledgeType, object Data, ActorAddress OriginAgent) : ActorMessage;
    
    /// <summary>
    /// Message to notify agents about changes in the environment or context.
    /// </summary>
    public sealed record EnvironmentChangeMessage(string ChangeType, object ChangeData, DateTimeOffset OccurredAt) : ActorMessage;
    
    /// <summary>
    /// Message for agents to negotiate resources or priorities.
    /// </summary>
    public sealed record ResourceNegotiationMessage(string ResourceType, string Action, Dictionary<string, object> Parameters) : ActorMessage;
}

/// <summary>
/// Cluster-specific messages for distributed coordination.
/// </summary>
public static class ClusterMessages
{
    /// <summary>
    /// Message sent when a new cluster member joins.
    /// </summary>
    public sealed record MemberJoinedMessage(ClusterMember Member) : ActorMessage;
    
    /// <summary>
    /// Message sent when a cluster member leaves.
    /// </summary>
    public sealed record MemberLeftMessage(ClusterMember Member) : ActorMessage;
    
    /// <summary>
    /// Message sent when a cluster member becomes unreachable.
    /// </summary>
    public sealed record MemberUnreachableMessage(ClusterMember Member) : ActorMessage;
    
    /// <summary>
    /// Message for cluster-wide heartbeat monitoring.
    /// </summary>
    public sealed record HeartbeatMessage(string NodeId, DateTimeOffset Timestamp, Dictionary<string, object>? Metadata = null) : ActorMessage;
    
    /// <summary>
    /// Message for leader election coordination.
    /// </summary>
    public sealed record LeaderElectionMessage(string NodeId, int Priority, DateTimeOffset Timestamp) : ActorMessage;
    
    /// <summary>
    /// Message to announce a new cluster leader.
    /// </summary>
    public sealed record LeaderAnnouncementMessage(ClusterMember Leader) : ActorMessage;
    
    /// <summary>
    /// Message for cluster-wide actor discovery.
    /// </summary>
    public sealed record ActorDiscoveryMessage(ActorAddress Actor, string NodeId, string Action) : ActorMessage;
    
    /// <summary>
    /// Message for cluster state synchronization.
    /// </summary>
    public sealed record ClusterStateSyncMessage(ClusterState State) : ActorMessage;
}

/// <summary>
/// Base class for custom AI agent behaviors.
/// </summary>
public abstract class AIAgent : IActor
{
    protected readonly string _name;
    protected ActorAddress _address = default;
    protected bool _isRunning;
    protected readonly Dictionary<string, object> _properties = new();
    
    /// <summary>
    /// Initializes a new instance of the AIAgent class.
    /// </summary>
    /// <param name="name">The name of the AI agent.</param>
    protected AIAgent(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }
    
    /// <inheritdoc />
    public ActorAddress Address => _address;
    
    /// <inheritdoc />
    public bool IsRunning => _isRunning;
    
    /// <summary>
    /// Gets the name of this AI agent.
    /// </summary>
    public string Name => _name;
    
    /// <summary>
    /// Gets or sets properties associated with this agent.
    /// </summary>
    public Dictionary<string, object> Properties => _properties;

    /// <inheritdoc />
    public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await OnInitializeAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public virtual async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        await OnStartAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public virtual async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        await OnStopAsync(cancellationToken);
    }
    
    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        await OnDisposeAsync();
    }
    
    /// <inheritdoc />
    public async Task ReceiveAsync(IActorMessage message, IActorContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Set the current address from context if not already set
            if (_address.Path != context.Self.Path)
            {
                _address = context.Self;
            }
            
            await message switch
            {
                SystemMessages.StartMessage start => HandleStartMessage(start, context, cancellationToken),
                SystemMessages.StopMessage stop => HandleStopMessage(stop, context, cancellationToken),
                SystemMessages.StatusRequestMessage status => HandleStatusRequest(status, context, cancellationToken),
                AIAgentMessages.TaskRequestMessage task => HandleTaskRequest(task, context, cancellationToken),
                AIAgentMessages.TaskCancelMessage cancel => HandleTaskCancel(cancel, context, cancellationToken),
                AIAgentMessages.CollaborationRequestMessage collab => HandleCollaborationRequest(collab, context, cancellationToken),
                AIAgentMessages.KnowledgeSharingMessage knowledge => HandleKnowledgeSharing(knowledge, context, cancellationToken),
                AIAgentMessages.EnvironmentChangeMessage envChange => HandleEnvironmentChange(envChange, context, cancellationToken),
                _ => HandleCustomMessage(message, context, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            await OnErrorAsync(ex, message, context, cancellationToken);
        }
    }
    
    /// <inheritdoc />
    public virtual async Task OnErrorAsync(Exception error, IActorMessage? message = null, IActorContext? context = null, CancellationToken cancellationToken = default)
    {
        // Default error handling - log the error and continue
        Console.WriteLine($"Error in agent {_name}: {error.Message}");
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Called during agent initialization. Override to provide custom initialization logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Called when the agent starts. Override to provide custom startup logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async startup operation.</returns>
    protected virtual Task OnStartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Called when the agent stops. Override to provide custom shutdown logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async shutdown operation.</returns>
    protected virtual Task OnStopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Called during agent disposal. Override to provide custom cleanup logic.
    /// </summary>
    /// <returns>A task representing the async disposal operation.</returns>
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;
    
    /// <summary>
    /// Handles custom messages not recognized by the base agent.
    /// Override to provide custom message handling logic.
    /// </summary>
    /// <param name="message">The message to handle.</param>
    /// <param name="context">The actor context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async message handling operation.</returns>
    protected virtual Task HandleCustomMessage(IActorMessage message, IActorContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    private async Task HandleStartMessage(SystemMessages.StartMessage message, IActorContext context, CancellationToken cancellationToken)
    {
        if (!_isRunning)
        {
            await StartAsync(cancellationToken);
        }
    }
    
    private async Task HandleStopMessage(SystemMessages.StopMessage message, IActorContext context, CancellationToken cancellationToken)
    {
        await StopAsync(cancellationToken);
    }
    
    private async Task HandleStatusRequest(SystemMessages.StatusRequestMessage message, IActorContext context, CancellationToken cancellationToken)
    {
        var status = _isRunning ? "Running" : "Stopped";
        var response = new SystemMessages.StatusResponseMessage(status, new Dictionary<string, object>(_properties));
        
        if (context.Sender.HasValue)
        {
            await context.SendAsync(context.Sender.Value, response, cancellationToken);
        }
    }
    
    /// <summary>
    /// Handles task request messages. Override to provide custom task handling.
    /// </summary>
    /// <param name="message">The task request message.</param>
    /// <param name="context">The actor context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async task handling operation.</returns>
    protected virtual Task HandleTaskRequest(AIAgentMessages.TaskRequestMessage message, IActorContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Handles task cancellation messages. Override to provide custom cancellation logic.
    /// </summary>
    /// <param name="message">The task cancel message.</param>
    /// <param name="context">The actor context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cancellation handling operation.</returns>
    protected virtual Task HandleTaskCancel(AIAgentMessages.TaskCancelMessage message, IActorContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Handles collaboration request messages. Override to provide custom collaboration logic.
    /// </summary>
    /// <param name="message">The collaboration request message.</param>
    /// <param name="context">The actor context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async collaboration handling operation.</returns>
    protected virtual Task HandleCollaborationRequest(AIAgentMessages.CollaborationRequestMessage message, IActorContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Handles knowledge sharing messages. Override to process shared knowledge.
    /// </summary>
    /// <param name="message">The knowledge sharing message.</param>
    /// <param name="context">The actor context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async knowledge processing operation.</returns>
    protected virtual Task HandleKnowledgeSharing(AIAgentMessages.KnowledgeSharingMessage message, IActorContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    /// <summary>
    /// Handles environment change messages. Override to react to environmental changes.
    /// </summary>
    /// <param name="message">The environment change message.</param>
    /// <param name="context">The actor context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async environment change handling operation.</returns>
    protected virtual Task HandleEnvironmentChange(AIAgentMessages.EnvironmentChangeMessage message, IActorContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
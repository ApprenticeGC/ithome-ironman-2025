using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Base class for AI agent coordination messages.
/// These messages are used for communication between AI agents in clusters.
/// </summary>
public abstract class AIAgentMessage : ActorMessage
{
    /// <summary>
    /// The ID of the sending agent.
    /// </summary>
    public ActorId SenderId { get; }

    /// <summary>
    /// The priority of this message (higher values = higher priority).
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// Initializes a new AI agent message.
    /// </summary>
    /// <param name="senderId">ID of the sending agent.</param>
    /// <param name="priority">Message priority (default is 0).</param>
    protected AIAgentMessage(ActorId senderId, int priority = 0) : base()
    {
        SenderId = senderId;
        Priority = priority;
    }

    /// <summary>
    /// Initializes a new AI agent message as a response to another message.
    /// </summary>
    /// <param name="senderId">ID of the sending agent.</param>
    /// <param name="originalMessage">The message this is responding to.</param>
    /// <param name="priority">Message priority (default is 0).</param>
    protected AIAgentMessage(ActorId senderId, IActorMessage originalMessage, int priority = 0) 
        : base(originalMessage)
    {
        SenderId = senderId;
        Priority = priority;
    }
}

/// <summary>
/// Message sent when an agent wants to coordinate behavior with other agents in the cluster.
/// </summary>
public class CoordinationRequest : AIAgentMessage
{
    /// <summary>
    /// The type of coordination being requested.
    /// </summary>
    public string CoordinationType { get; }

    /// <summary>
    /// Optional data associated with the coordination request.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new coordination request.
    /// </summary>
    public CoordinationRequest(ActorId senderId, string coordinationType, object? data = null, int priority = 0)
        : base(senderId, priority)
    {
        CoordinationType = coordinationType ?? throw new ArgumentNullException(nameof(coordinationType));
        Data = data;
    }
}

/// <summary>
/// Response to a coordination request.
/// </summary>
public class CoordinationResponse : AIAgentMessage
{
    /// <summary>
    /// Whether the coordination request is accepted.
    /// </summary>
    public bool IsAccepted { get; }

    /// <summary>
    /// Optional reason for acceptance or rejection.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Initializes a new coordination response.
    /// </summary>
    public CoordinationResponse(ActorId senderId, IActorMessage originalRequest, bool isAccepted, string? reason = null)
        : base(senderId, originalRequest)
    {
        IsAccepted = isAccepted;
        Reason = reason;
    }
}

/// <summary>
/// Message broadcast when an agent changes its behavior state.
/// Other agents can use this for coordination and awareness.
/// </summary>
public class StateChangeNotification : AIAgentMessage
{
    /// <summary>
    /// The new behavior state of the agent.
    /// </summary>
    public string NewState { get; }

    /// <summary>
    /// The previous behavior state of the agent.
    /// </summary>
    public string PreviousState { get; }

    /// <summary>
    /// Optional reason for the state change.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Initializes a new state change notification.
    /// </summary>
    public StateChangeNotification(ActorId senderId, string newState, string previousState, string? reason = null)
        : base(senderId, priority: 1) // State changes have higher priority
    {
        NewState = newState ?? throw new ArgumentNullException(nameof(newState));
        PreviousState = previousState ?? throw new ArgumentNullException(nameof(previousState));
        Reason = reason;
    }
}

/// <summary>
/// Command message to instruct an agent to perform a specific action.
/// </summary>
public class AgentCommand : AIAgentMessage
{
    /// <summary>
    /// The command to execute.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Optional parameters for the command.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; }

    /// <summary>
    /// Whether this command requires immediate execution.
    /// </summary>
    public bool IsUrgent { get; }

    /// <summary>
    /// Initializes a new agent command.
    /// </summary>
    public AgentCommand(ActorId senderId, string command, Dictionary<string, object>? parameters = null, bool isUrgent = false)
        : base(senderId, isUrgent ? 2 : 0) // Urgent commands have highest priority
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Parameters = parameters;
        IsUrgent = isUrgent;
    }
}

/// <summary>
/// Response to an agent command indicating the result of execution.
/// </summary>
public class AgentCommandResponse : AIAgentMessage
{
    /// <summary>
    /// Whether the command was executed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Optional result data from command execution.
    /// </summary>
    public object? Result { get; }

    /// <summary>
    /// Optional error message if command failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Initializes a new agent command response.
    /// </summary>
    public AgentCommandResponse(ActorId senderId, IActorMessage originalCommand, bool isSuccess, object? result = null, string? errorMessage = null)
        : base(senderId, originalCommand)
    {
        IsSuccess = isSuccess;
        Result = result;
        ErrorMessage = errorMessage;
    }
}
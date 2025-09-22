namespace GameConsole.AI.Core;

/// <summary>
/// Represents the status of an AI agent.
/// </summary>
public enum AIAgentStatus
{
    /// <summary>
    /// The agent is not initialized and cannot process requests.
    /// </summary>
    Uninitialized,

    /// <summary>
    /// The agent is initializing and preparing to process requests.
    /// </summary>
    Initializing,

    /// <summary>
    /// The agent is ready and available to process requests.
    /// </summary>
    Ready,

    /// <summary>
    /// The agent is currently processing a request.
    /// </summary>
    Processing,

    /// <summary>
    /// The agent is busy and cannot accept new requests.
    /// </summary>
    Busy,

    /// <summary>
    /// The agent encountered an error and is temporarily unavailable.
    /// </summary>
    Error,

    /// <summary>
    /// The agent is stopping and will not accept new requests.
    /// </summary>
    Stopping,

    /// <summary>
    /// The agent has stopped and is no longer processing requests.
    /// </summary>
    Stopped
}

/// <summary>
/// Event arguments for AI agent status change events.
/// </summary>
public class AIAgentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="previousStatus">The previous status of the agent.</param>
    /// <param name="currentStatus">The current status of the agent.</param>
    /// <param name="reason">The reason for the status change.</param>
    public AIAgentStatusChangedEventArgs(AIAgentStatus previousStatus, AIAgentStatus currentStatus, string? reason = null)
    {
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
        Reason = reason;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the previous status of the agent.
    /// </summary>
    public AIAgentStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the current status of the agent.
    /// </summary>
    public AIAgentStatus CurrentStatus { get; }

    /// <summary>
    /// Gets the reason for the status change, if available.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Gets the timestamp when the status change occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
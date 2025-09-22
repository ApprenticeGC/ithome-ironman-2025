using System;

namespace GameConsole.AI.Core
{

/// <summary>
/// Event arguments for when an AI agent is discovered.
/// </summary>
public class AIAgentDiscoveredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDiscoveredEventArgs"/> class.
    /// </summary>
    /// <param name="metadata">The metadata of the discovered agent.</param>
    public AIAgentDiscoveredEventArgs(IAIAgentMetadata metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <summary>
    /// Gets the metadata of the discovered AI agent.
    /// </summary>
    public IAIAgentMetadata Metadata { get; }
}

/// <summary>
/// Event arguments for when an AI agent is removed or becomes unavailable.
/// </summary>
public class AIAgentRemovedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentRemovedEventArgs"/> class.
    /// </summary>
    /// <param name="agentId">The ID of the removed agent.</param>
    /// <param name="reason">The reason for removal.</param>
    public AIAgentRemovedEventArgs(string agentId, string reason)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
    }

    /// <summary>
    /// Gets the ID of the removed AI agent.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the reason for the agent removal.
    /// </summary>
    public string Reason { get; }
}

/// <summary>
/// Event arguments for when an AI agent is registered.
/// </summary>
public class AIAgentRegisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentRegisteredEventArgs"/> class.
    /// </summary>
    /// <param name="agent">The registered AI agent.</param>
    public AIAgentRegisteredEventArgs(IAIAgent agent)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    /// <summary>
    /// Gets the registered AI agent.
    /// </summary>
    public IAIAgent Agent { get; }
}

/// <summary>
/// Event arguments for when an AI agent is unregistered.
/// </summary>
public class AIAgentUnregisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentUnregisteredEventArgs"/> class.
    /// </summary>
    /// <param name="agentId">The ID of the unregistered agent.</param>
    /// <param name="agent">The unregistered AI agent, if available.</param>
    public AIAgentUnregisteredEventArgs(string agentId, IAIAgent agent = null)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Agent = agent;
    }

    /// <summary>
    /// Gets the ID of the unregistered AI agent.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the unregistered AI agent, if available.
    /// </summary>
    public IAIAgent Agent { get; }
}

/// <summary>
/// Event arguments for when an AI agent's status changes.
/// </summary>
public class AIAgentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="agentId">The ID of the agent whose status changed.</param>
    /// <param name="previousStatus">The previous status of the agent.</param>
    /// <param name="currentStatus">The current status of the agent.</param>
    public AIAgentStatusChangedEventArgs(string agentId, AIAgentStatus previousStatus, AIAgentStatus currentStatus)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        PreviousStatus = previousStatus;
        CurrentStatus = currentStatus;
    }

    /// <summary>
    /// Gets the ID of the AI agent whose status changed.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the previous status of the AI agent.
    /// </summary>
    public AIAgentStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the current status of the AI agent.
    /// </summary>
    public AIAgentStatus CurrentStatus { get; }
}
}
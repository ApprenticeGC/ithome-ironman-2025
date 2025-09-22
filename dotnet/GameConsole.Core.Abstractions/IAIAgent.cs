using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Represents the execution state of an AI agent.
/// </summary>
public enum AgentState
{
    /// <summary>
    /// Agent is idle and not processing any tasks.
    /// </summary>
    Idle,
    
    /// <summary>
    /// Agent is actively processing a task.
    /// </summary>
    Processing,
    
    /// <summary>
    /// Agent is paused and can be resumed.
    /// </summary>
    Paused,
    
    /// <summary>
    /// Agent has encountered an error and is stopped.
    /// </summary>
    Error,
    
    /// <summary>
    /// Agent has been disposed and is no longer available.
    /// </summary>
    Disposed
}

/// <summary>
/// Arguments for AI agent-related events.
/// </summary>
public class AgentEventArgs : EventArgs
{
    /// <summary>
    /// The unique identifier of the agent.
    /// </summary>
    public string AgentId { get; }
    
    /// <summary>
    /// The current state of the agent.
    /// </summary>
    public AgentState State { get; }
    
    /// <summary>
    /// Optional additional data about the agent event.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new instance of the AgentEventArgs class.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="state">The current state of the agent.</param>
    /// <param name="data">Optional additional data about the agent event.</param>
    public AgentEventArgs(string agentId, AgentState state, object? data = null)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        State = state;
        Data = data;
    }
}

/// <summary>
/// Tier 1: Core abstraction for AI agents in the actor system.
/// Defines the fundamental contract for autonomous AI entities that can process tasks,
/// communicate with other agents, and participate in distributed clustering.
/// </summary>
public interface IAIAgent : IAsyncDisposable
{
    /// <summary>
    /// Event raised when the agent's state changes.
    /// </summary>
    event EventHandler<AgentEventArgs>? StateChanged;
    
    /// <summary>
    /// Event raised when the agent receives a message from another agent.
    /// </summary>
    event EventHandler<AgentEventArgs>? MessageReceived;

    /// <summary>
    /// Gets the unique identifier for this agent.
    /// </summary>
    string AgentId { get; }
    
    /// <summary>
    /// Gets the current execution state of the agent.
    /// </summary>
    AgentState State { get; }
    
    /// <summary>
    /// Gets the type/category of this agent for clustering purposes.
    /// </summary>
    string AgentType { get; }

    /// <summary>
    /// Initializes the agent asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the agent's execution loop asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses the agent's execution temporarily.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async pause operation.</returns>
    Task PauseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes the agent's execution from a paused state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async resume operation.</returns>
    Task ResumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the agent's execution gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to another agent in the cluster.
    /// </summary>
    /// <param name="targetAgentId">The identifier of the target agent.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async message sending operation.</returns>
    Task SendMessageAsync(string targetAgentId, object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a task assigned to this agent.
    /// </summary>
    /// <param name="task">The task to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async task processing operation that returns the result.</returns>
    Task<object?> ProcessTaskAsync(object task, CancellationToken cancellationToken = default);
}
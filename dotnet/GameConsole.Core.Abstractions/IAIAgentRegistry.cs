namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for registering and managing AI agents in the system.
/// Provides mechanisms to register, unregister, and manage the lifecycle of AI agents.
/// </summary>
public interface IAIAgentRegistry : IService
{
    /// <summary>
    /// Registers an AI agent with the registry.
    /// </summary>
    /// <param name="agent">The AI agent to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if registration was successful.</returns>
    Task<bool> RegisterAsync(IAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the registry.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if unregistration was successful.</returns>
    Task<bool> UnregisterAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the registry.
    /// </summary>
    /// <param name="agent">The AI agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if unregistration was successful.</returns>
    Task<bool> UnregisterAsync(IAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a collection of all registered agents.</returns>
    Task<IEnumerable<IAIAgent>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a registered AI agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the agent if found, or null if not found.</returns>
    Task<IAIAgent?> GetByIdAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an AI agent is registered with the given identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the agent is registered.</returns>
    Task<bool> IsRegisteredAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an AI agent is registered.
    /// </summary>
    /// <param name="agent">The AI agent to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the agent is registered.</returns>
    Task<bool> IsRegisteredAsync(IAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the number of registered agents.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all registered AI agents from the registry.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when an AI agent is registered.
    /// </summary>
    event AsyncEventHandler<AIAgentRegisteredEventArgs>? AgentRegistered;

    /// <summary>
    /// Event raised when an AI agent is unregistered.
    /// </summary>
    event AsyncEventHandler<AIAgentUnregisteredEventArgs>? AgentUnregistered;

    /// <summary>
    /// Event raised when an AI agent's status changes.
    /// </summary>
    event AsyncEventHandler<AIAgentStatusChangedEventArgs>? AgentStatusChanged;
}

/// <summary>
/// Delegate for async event handlers.
/// </summary>
/// <typeparam name="TEventArgs">The type of event arguments.</typeparam>
/// <param name="sender">The sender of the event.</param>
/// <param name="e">The event arguments.</param>
/// <returns>A task representing the async event handling.</returns>
public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e) where TEventArgs : EventArgs;

/// <summary>
/// Event arguments for AI agent registration events.
/// </summary>
public sealed class AIAgentRegisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentRegisteredEventArgs"/> class.
    /// </summary>
    /// <param name="agent">The AI agent that was registered.</param>
    public AIAgentRegisteredEventArgs(IAIAgent agent)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the AI agent that was registered.
    /// </summary>
    public IAIAgent Agent { get; }

    /// <summary>
    /// Gets the timestamp when the agent was registered.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}

/// <summary>
/// Event arguments for AI agent unregistration events.
/// </summary>
public sealed class AIAgentUnregisteredEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentUnregisteredEventArgs"/> class.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent that was unregistered.</param>
    /// <param name="agent">The AI agent that was unregistered, if available.</param>
    public AIAgentUnregisteredEventArgs(string agentId, IAIAgent? agent = null)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Agent = agent;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the unique identifier of the AI agent that was unregistered.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the AI agent that was unregistered, if available.
    /// </summary>
    public IAIAgent? Agent { get; }

    /// <summary>
    /// Gets the timestamp when the agent was unregistered.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}

/// <summary>
/// Event arguments for AI agent status change events.
/// </summary>
public sealed class AIAgentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentStatusChangedEventArgs"/> class.
    /// </summary>
    /// <param name="agent">The AI agent whose status changed.</param>
    /// <param name="previousStatus">The previous status of the agent.</param>
    /// <param name="newStatus">The new status of the agent.</param>
    public AIAgentStatusChangedEventArgs(IAIAgent agent, AIAgentStatus previousStatus, AIAgentStatus newStatus)
    {
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the AI agent whose status changed.
    /// </summary>
    public IAIAgent Agent { get; }

    /// <summary>
    /// Gets the previous status of the agent.
    /// </summary>
    public AIAgentStatus PreviousStatus { get; }

    /// <summary>
    /// Gets the new status of the agent.
    /// </summary>
    public AIAgentStatus NewStatus { get; }

    /// <summary>
    /// Gets the timestamp when the status changed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
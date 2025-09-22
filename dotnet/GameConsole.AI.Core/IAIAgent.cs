using GameConsole.Core.Abstractions;
using GameConsole.Plugins.Core;

namespace GameConsole.AI.Core;

/// <summary>
/// Defines the interface for all AI agents in the GameConsole system.
/// Extends IPlugin with AI-specific functionality for behavior management,
/// capability reporting, and AI execution context.
/// </summary>
public interface IAIAgent : IPlugin
{
    /// <summary>
    /// Gets the capabilities that this AI agent provides.
    /// This should match the capabilities declared in the AIAgentAttribute.
    /// </summary>
    AIAgentCapability Capabilities { get; }

    /// <summary>
    /// Gets the current state of the AI agent.
    /// </summary>
    AIAgentState State { get; }

    /// <summary>
    /// Executes a single AI update cycle.
    /// This method is called by the AI system to perform the agent's main logic.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    /// <param name="context">The execution context for this update.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async execution operation.</returns>
    Task ExecuteAsync(float deltaTime, IAIExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether this AI agent can execute in the given context.
    /// This method is called before ExecuteAsync to ensure the agent can operate safely.
    /// </summary>
    /// <param name="context">The execution context to validate.</param>
    /// <returns>True if the agent can execute in the given context, false otherwise.</returns>
    bool CanExecute(IAIExecutionContext context);

    /// <summary>
    /// Resets the AI agent to its initial state.
    /// This method is called when the agent needs to be reinitialized or restarted.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async reset operation.</returns>
    Task ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the current state of an AI agent.
/// </summary>
public enum AIAgentState
{
    /// <summary>
    /// The agent is not initialized.
    /// </summary>
    Uninitialized,

    /// <summary>
    /// The agent is initialized and ready to execute.
    /// </summary>
    Ready,

    /// <summary>
    /// The agent is currently executing.
    /// </summary>
    Executing,

    /// <summary>
    /// The agent is paused and not executing.
    /// </summary>
    Paused,

    /// <summary>
    /// The agent has encountered an error.
    /// </summary>
    Error,

    /// <summary>
    /// The agent has been disposed.
    /// </summary>
    Disposed
}

/// <summary>
/// Provides execution context for AI agents.
/// Contains information about the game state, resources, and environment
/// that the AI agent can use during execution.
/// </summary>
public interface IAIExecutionContext
{
    /// <summary>
    /// Gets the current game time in seconds.
    /// </summary>
    float GameTime { get; }

    /// <summary>
    /// Gets the service provider for accessing game services.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Gets additional properties that can be used by AI agents.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}
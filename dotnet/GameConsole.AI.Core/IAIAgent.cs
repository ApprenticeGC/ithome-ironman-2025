using GameConsole.Plugins.Core;

namespace GameConsole.AI.Core;

/// <summary>
/// Defines the contract for AI agents in the GameConsole system.
/// AI agents extend the plugin system with specialized capabilities for intelligent behavior,
/// decision making, and autonomous operation within the game environment.
/// </summary>
public interface IAIAgent : IPlugin
{
    /// <summary>
    /// Gets the AI agent's capabilities and behavioral characteristics.
    /// This includes supported decision types, learning capabilities, and operational modes.
    /// </summary>
    IAIAgentCapabilities Capabilities { get; }

    /// <summary>
    /// Gets the current state of the AI agent.
    /// This includes learning status, active behaviors, and operational metrics.
    /// </summary>
    IAIAgentState State { get; }

    /// <summary>
    /// Processes input data and generates intelligent responses or actions.
    /// This is the core decision-making method for AI agents.
    /// </summary>
    /// <param name="input">The input context or data to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the AI agent's decision or response.</returns>
    Task<IAIAgentResponse> ProcessAsync(IAIAgentInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the AI agent's internal state based on feedback or new information.
    /// This enables learning and adaptation capabilities.
    /// </summary>
    /// <param name="feedback">Feedback or learning data to incorporate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async learning operation.</returns>
    Task UpdateAsync(IAIAgentFeedback feedback, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the AI agent to its initial state.
    /// This clears learned behaviors and resets to default configuration.
    /// </summary>
    /// <param name="preserveConfiguration">Whether to preserve configuration while resetting learned state.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async reset operation.</returns>
    Task ResetAsync(bool preserveConfiguration = true, CancellationToken cancellationToken = default);
}
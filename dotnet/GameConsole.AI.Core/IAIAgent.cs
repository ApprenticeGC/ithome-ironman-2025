using GameConsole.Plugins.Core;

namespace GameConsole.AI.Core;

/// <summary>
/// Defines the interface for AI agents in the GameConsole system.
/// Extends the plugin interface with AI-specific capabilities and lifecycle operations.
/// </summary>
public interface IAIAgent : IPlugin
{
    /// <summary>
    /// Gets the AI-specific metadata for this agent.
    /// </summary>
    new IAIAgentMetadata Metadata { get; }

    /// <summary>
    /// Processes a request and returns a response asynchronously.
    /// This is the primary method for interacting with the AI agent.
    /// </summary>
    /// <param name="request">The request to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async processing operation with the response.</returns>
    Task<IAIAgentResponse> ProcessRequestAsync(IAIAgentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trains or updates the AI agent with new data.
    /// This method is only applicable for agents that support learning.
    /// </summary>
    /// <param name="trainingData">The training data to learn from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async training operation.</returns>
    Task TrainAsync(IAIAgentTrainingData trainingData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status and health of the AI agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async status check with the agent status.</returns>
    Task<IAIAgentStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the agent's capabilities with a diagnostic request.
    /// </summary>
    /// <param name="diagnosticRequest">The diagnostic request to test with.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async diagnostic operation with the results.</returns>
    Task<IAIAgentDiagnosticResult> RunDiagnosticsAsync(IAIAgentDiagnosticRequest diagnosticRequest, CancellationToken cancellationToken = default);
}
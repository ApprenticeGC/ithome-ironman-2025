using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Core AI service interface for GameConsole's AI orchestration system.
/// Provides agent lifecycle management and distributed processing capabilities.
/// </summary>
public interface IAIService : IService, ICapabilityProvider
{
    /// <summary>
    /// Initializes the AI service with the specified profile configuration.
    /// </summary>
    /// <param name="profile">AI profile configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InitializeAsync(AIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates and starts a new AI agent of the specified type.
    /// </summary>
    /// <param name="agentType">Type of agent to create.</param>
    /// <param name="config">Agent configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unique agent ID.</returns>
    Task<string> StartAgentAsync(string agentType, AgentConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops and removes the specified agent.
    /// </summary>
    /// <param name="agentId">Agent ID to stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task StopAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a request to the specified agent for processing.
    /// </summary>
    /// <param name="agentId">Target agent ID.</param>
    /// <param name="request">Request data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Processing response.</returns>
    Task<object> ProcessRequestAsync(string agentId, object request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available agent types.
    /// </summary>
    IEnumerable<string> GetAvailableAgentTypes();

    /// <summary>
    /// Gets information about the specified agent.
    /// </summary>
    /// <param name="agentId">Agent ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent metadata.</returns>
    Task<AgentMetadata> GetAgentInfoAsync(string agentId, CancellationToken cancellationToken = default);
}
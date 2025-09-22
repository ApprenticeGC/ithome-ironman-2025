using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Services;

/// <summary>
/// Tier 1: AI orchestration service interface (pure .NET, async-first).
/// Manages AI agent lifecycle and routing between different AI capabilities.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService, ICapabilityProvider
{
    /// <summary>
    /// Initializes the AI service with the specified profile configuration.
    /// </summary>
    /// <param name="profile">AI profile configuration for the service.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(AIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the AI service gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async shutdown operation.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    // Core agent orchestration
    /// <summary>
    /// Invokes a specific AI agent with input and returns the response.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent to invoke.</param>
    /// <param name="input">The input text to send to the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the agent's response.</returns>
    Task<string> InvokeAgentAsync(string agentId, string input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams responses from a specific AI agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent to invoke.</param>
    /// <param name="input">The input text to send to the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns an async enumerable of streaming responses.</returns>
    Task<IAsyncEnumerable<string>> StreamAgentAsync(string agentId, string input, CancellationToken cancellationToken = default);

    // Agent discovery and management
    /// <summary>
    /// Gets all available AI agents registered in the system.
    /// </summary>
    /// <returns>An enumerable of available agent identifiers.</returns>
    IEnumerable<string> GetAvailableAgents();

    /// <summary>
    /// Gets detailed information about a specific AI agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to query.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns agent metadata.</returns>
    Task<AgentMetadata> GetAgentInfoAsync(string agentId, CancellationToken cancellationToken = default);

    // Context management
    /// <summary>
    /// Creates a new conversation context for the specified agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to create a conversation with.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the conversation identifier.</returns>
    Task<string> CreateConversationAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends a conversation context and cleans up resources.
    /// </summary>
    /// <param name="conversationId">The identifier of the conversation to end.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if successful.</returns>
    Task<bool> EndConversationAsync(string conversationId, CancellationToken cancellationToken = default);
}
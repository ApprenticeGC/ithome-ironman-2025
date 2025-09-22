using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Services;

/// <summary>
/// Core AI service for AI agent orchestration and management.
/// Manages AI agent lifecycle, discovery, and routing between different AI capabilities.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService, ICapabilityProvider
{
    /// <summary>
    /// Gets all available agents that can be invoked.
    /// </summary>
    /// <returns>Collection of agent identifiers.</returns>
    IEnumerable<string> GetAvailableAgents();

    /// <summary>
    /// Gets metadata information about a specific agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Agent metadata if found, null otherwise.</returns>
    Task<AgentMetadata?> GetAgentInfoAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new agent with the AI service.
    /// </summary>
    /// <param name="agentMetadata">Metadata for the agent to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if registration was successful, false otherwise.</returns>
    Task<bool> RegisterAgentAsync(AgentMetadata agentMetadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an agent from the AI service.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if unregistration was successful, false if agent not found.</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes an agent with input text and returns the response.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to invoke.</param>
    /// <param name="input">Input text to send to the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Agent response text.</returns>
    Task<string> InvokeAgentAsync(string agentId, string input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadata describing an AI agent.
/// </summary>
public record AgentMetadata(
    string Id,
    string Name,
    string Description,
    AgentCapabilities Capabilities,
    string Version = "1.0.0")
{
    /// <summary>
    /// Additional configuration properties for the agent.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Capabilities that an AI agent can support.
/// </summary>
[Flags]
public enum AgentCapabilities
{
    /// <summary>
    /// Basic text processing and generation.
    /// </summary>
    TextGeneration = 1 << 0,

    /// <summary>
    /// Code analysis and generation.
    /// </summary>
    CodeGeneration = 1 << 1,

    /// <summary>
    /// Dialogue and conversational AI.
    /// </summary>
    Dialogue = 1 << 2,

    /// <summary>
    /// Creative content generation (stories, descriptions, etc.).
    /// </summary>
    CreativeWriting = 1 << 3,

    /// <summary>
    /// Game content generation (quests, NPCs, etc.).
    /// </summary>
    GameContentGeneration = 1 << 4,

    /// <summary>
    /// Asset analysis and optimization.
    /// </summary>
    AssetAnalysis = 1 << 5,

    /// <summary>
    /// All capabilities enabled.
    /// </summary>
    All = TextGeneration | CodeGeneration | Dialogue | CreativeWriting | GameContentGeneration | AssetAnalysis
}

/// <summary>
/// Capability interface for streaming AI agent responses.
/// Allows agents to provide real-time streaming of generated content.
/// </summary>
public interface IStreamingCapability : ICapabilityProvider
{
    /// <summary>
    /// Streams agent responses in real-time as they are generated.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to invoke.</param>
    /// <param name="input">Input text to send to the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Async enumerable of response chunks as they are generated.</returns>
    IAsyncEnumerable<string> StreamAgentAsync(string agentId, string input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for conversation management with AI agents.
/// Allows maintaining context across multiple interactions.
/// </summary>
public interface IConversationCapability : ICapabilityProvider
{
    /// <summary>
    /// Creates a new conversation context for an agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Unique conversation identifier.</returns>
    Task<string> CreateConversationAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends a conversation and clears its context.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if conversation was ended successfully, false if not found.</returns>
    Task<bool> EndConversationAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes an agent within a conversation context.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation.</param>
    /// <param name="input">Input text to send to the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Agent response text.</returns>
    Task<string> InvokeInConversationAsync(string conversationId, string input, CancellationToken cancellationToken = default);
}
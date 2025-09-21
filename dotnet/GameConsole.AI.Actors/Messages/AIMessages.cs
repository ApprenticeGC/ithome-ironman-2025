namespace GameConsole.AI.Actors.Messages;

/// <summary>
/// Base class for all AI actor messages.
/// </summary>
public abstract record AIMessage
{
    /// <summary>
    /// Unique identifier for this message.
    /// </summary>
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Message to invoke an AI agent with input.
/// </summary>
public record InvokeAgent(string AgentId, string Input, string? ConversationId = null) : AIMessage;

/// <summary>
/// Response from an AI agent invocation.
/// </summary>
public record AgentResponse(string AgentId, string Output, bool Success, string? ErrorMessage = null) : AIMessage;

/// <summary>
/// Message to stream content from an AI agent.
/// </summary>
public record StreamAgent(string AgentId, string Input, string? ConversationId = null) : AIMessage;

/// <summary>
/// Streaming chunk from an AI agent.
/// </summary>
public record AgentStreamChunk(string AgentId, string Chunk, bool IsComplete = false) : AIMessage;

/// <summary>
/// Message to get available AI agents.
/// </summary>
public record GetAvailableAgents : AIMessage;

/// <summary>
/// Response containing available AI agents.
/// </summary>
public record AvailableAgentsResponse(IEnumerable<string> AgentIds) : AIMessage;

/// <summary>
/// Message to get information about a specific agent.
/// </summary>
public record GetAgentInfo(string AgentId) : AIMessage;

/// <summary>
/// Response containing agent metadata.
/// </summary>
public record AgentInfoResponse(string AgentId, AgentMetadata Metadata) : AIMessage;

/// <summary>
/// Message to create a new conversation.
/// </summary>
public record CreateConversation(string AgentId) : AIMessage;

/// <summary>
/// Response containing the created conversation ID.
/// </summary>
public record ConversationCreated(string ConversationId, string AgentId) : AIMessage;

/// <summary>
/// Message to end a conversation.
/// </summary>
public record EndConversation(string ConversationId) : AIMessage;

/// <summary>
/// Response confirming conversation ended.
/// </summary>
public record ConversationEnded(string ConversationId, bool Success) : AIMessage;

/// <summary>
/// Metadata about an AI agent.
/// </summary>
public record AgentMetadata(
    string Id,
    string Name,
    string Description,
    string Version,
    IEnumerable<string> Capabilities,
    bool IsAvailable);
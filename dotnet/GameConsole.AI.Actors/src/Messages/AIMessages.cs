namespace GameConsole.AI.Actors.Messages;

/// <summary>
/// Base message type for all AI-related actor messages
/// </summary>
public abstract record AIMessage;

/// <summary>
/// Agent management messages
/// </summary>
public abstract record AgentMessage : AIMessage;

/// <summary>
/// Request to create a new agent instance
/// </summary>
/// <param name="AgentId">Unique identifier for the agent</param>
/// <param name="AgentType">Type of agent to create</param>
/// <param name="Configuration">Agent-specific configuration</param>
public record CreateAgent(string AgentId, string AgentType, object? Configuration = null) : AgentMessage;

/// <summary>
/// Request to stop an agent instance
/// </summary>
/// <param name="AgentId">Unique identifier for the agent to stop</param>
public record StopAgent(string AgentId) : AgentMessage;

/// <summary>
/// Query for agent status
/// </summary>
/// <param name="AgentId">Unique identifier for the agent</param>
public record GetAgentStatus(string AgentId) : AgentMessage;

/// <summary>
/// Response containing agent status information
/// </summary>
/// <param name="AgentId">Unique identifier for the agent</param>
/// <param name="IsRunning">Whether the agent is currently running</param>
/// <param name="LastActivity">Timestamp of last agent activity</param>
public record AgentStatus(string AgentId, bool IsRunning, DateTime LastActivity) : AgentMessage;

/// <summary>
/// AI processing messages
/// </summary>
public abstract record AIProcessingMessage : AIMessage;

/// <summary>
/// Request to invoke an AI agent with input
/// </summary>
/// <param name="AgentId">Target agent identifier</param>
/// <param name="Input">Input text or data for processing</param>
/// <param name="ConversationId">Optional conversation context identifier</param>
public record InvokeAgent(string AgentId, string Input, string? ConversationId = null) : AIProcessingMessage;

/// <summary>
/// Response from AI agent processing
/// </summary>
/// <param name="AgentId">Source agent identifier</param>
/// <param name="Output">Generated output from the agent</param>
/// <param name="ConversationId">Conversation context identifier</param>
/// <param name="IsSuccess">Whether the processing was successful</param>
/// <param name="Error">Error message if processing failed</param>
public record AgentResponse(
    string AgentId, 
    string Output, 
    string? ConversationId = null, 
    bool IsSuccess = true, 
    string? Error = null) : AIProcessingMessage;

/// <summary>
/// Request to start streaming output from an AI agent
/// </summary>
/// <param name="AgentId">Target agent identifier</param>
/// <param name="Input">Input text or data for processing</param>
/// <param name="ConversationId">Optional conversation context identifier</param>
public record StreamAgent(string AgentId, string Input, string? ConversationId = null) : AIProcessingMessage;

/// <summary>
/// Streaming chunk response from AI agent
/// </summary>
/// <param name="AgentId">Source agent identifier</param>
/// <param name="Chunk">Partial output chunk</param>
/// <param name="ConversationId">Conversation context identifier</param>
/// <param name="IsComplete">Whether this is the final chunk</param>
public record AgentStreamChunk(
    string AgentId, 
    string Chunk, 
    string? ConversationId = null, 
    bool IsComplete = false) : AIProcessingMessage;

/// <summary>
/// Context management messages
/// </summary>
public abstract record ContextMessage : AIMessage;

/// <summary>
/// Request to create a new conversation context
/// </summary>
/// <param name="AgentId">Agent identifier for the conversation</param>
/// <param name="ConversationId">Unique conversation identifier</param>
public record CreateConversation(string AgentId, string ConversationId) : ContextMessage;

/// <summary>
/// Request to end a conversation context
/// </summary>
/// <param name="ConversationId">Conversation identifier to end</param>
public record EndConversation(string ConversationId) : ContextMessage;

/// <summary>
/// System management messages
/// </summary>
public abstract record SystemMessage : AIMessage;

/// <summary>
/// Request to start the AI actor system
/// </summary>
public record StartSystem : SystemMessage;

/// <summary>
/// Request to stop the AI actor system
/// </summary>
public record StopSystem : SystemMessage;

/// <summary>
/// Query for system health status
/// </summary>
public record GetSystemHealth : SystemMessage;

/// <summary>
/// Response containing system health information
/// </summary>
/// <param name="IsHealthy">Whether the system is healthy</param>
/// <param name="ActiveAgents">Number of active agents</param>
/// <param name="ActiveConversations">Number of active conversations</param>
/// <param name="Uptime">System uptime duration</param>
public record SystemHealth(
    bool IsHealthy, 
    int ActiveAgents, 
    int ActiveConversations, 
    TimeSpan Uptime) : SystemMessage;
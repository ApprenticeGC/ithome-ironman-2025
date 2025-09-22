using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.AI.Core;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Services;

/// <summary>
/// Tier 1: AI orchestration service interface (pure .NET, async-first).
/// Manages AI agent lifecycle and routing between different AI capabilities.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService, ICapabilityProvider
{
    /// <summary>
    /// Initialize the AI service with the specified profile.
    /// </summary>
    /// <param name="profile">AI profile configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task InitializeAsync(AIProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Shutdown the AI service gracefully.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ShutdownAsync(CancellationToken ct = default);

    // Core agent orchestration

    /// <summary>
    /// Invoke an agent synchronously with the given input.
    /// </summary>
    /// <param name="agentId">Unique identifier of the agent to invoke.</param>
    /// <param name="input">Input string to send to the agent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Response from the agent.</returns>
    Task<string> InvokeAgentAsync(string agentId, string input, CancellationToken ct = default);

    /// <summary>
    /// Stream agent responses asynchronously.
    /// </summary>
    /// <param name="agentId">Unique identifier of the agent to invoke.</param>
    /// <param name="input">Input string to send to the agent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Async enumerable stream of response chunks.</returns>
    Task<IAsyncEnumerable<string>> StreamAgentAsync(string agentId, string input, CancellationToken ct = default);

    // Agent discovery and management

    /// <summary>
    /// Get all available agent identifiers.
    /// </summary>
    /// <returns>Collection of available agent IDs.</returns>
    IEnumerable<string> GetAvailableAgents();

    /// <summary>
    /// Get detailed metadata about a specific agent.
    /// </summary>
    /// <param name="agentId">Unique identifier of the agent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Agent metadata information.</returns>
    Task<AgentMetadata> GetAgentInfoAsync(string agentId, CancellationToken ct = default);

    // Context management

    /// <summary>
    /// Create a new conversation context with an agent.
    /// </summary>
    /// <param name="agentId">Unique identifier of the agent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Unique conversation identifier.</returns>
    Task<string> CreateConversationAsync(string agentId, CancellationToken ct = default);

    /// <summary>
    /// End a conversation and clean up associated resources.
    /// </summary>
    /// <param name="conversationId">Unique identifier of the conversation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if conversation was successfully ended.</returns>
    Task<bool> EndConversationAsync(string conversationId, CancellationToken ct = default);
}
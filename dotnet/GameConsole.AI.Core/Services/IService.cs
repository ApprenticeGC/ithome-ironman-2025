using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;
using GameConsole.AI.Core.Configuration;
using GameConsole.AI.Core.Agents;

namespace GameConsole.AI.Core.Services;

/// <summary>
/// Tier 1: AI orchestration service interface (pure .NET, async-first).
/// Manages AI agent lifecycle and routing between different AI capabilities.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService, ICapabilityProvider
{
    Task InitializeAsync(AIProfile profile, CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    // Core agent orchestration
    Task<string> InvokeAgentAsync(string agentId, string input, CancellationToken cancellationToken = default);
    Task<IAsyncEnumerable<string>> StreamAgentAsync(string agentId, string input, CancellationToken cancellationToken = default);

    // Agent discovery and management
    IEnumerable<string> GetAvailableAgents();
    Task<AgentMetadata> GetAgentInfoAsync(string agentId, CancellationToken cancellationToken = default);

    // Context management
    Task<string> CreateConversationAsync(string agentId, CancellationToken cancellationToken = default);
    Task<bool> EndConversationAsync(string conversationId, CancellationToken cancellationToken = default);
}
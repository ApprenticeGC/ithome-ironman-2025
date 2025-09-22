using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.AI.Core;

namespace GameConsole.AI.Services;

/// <summary>
/// Registry for managing available AI agents and their metadata.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Register an agent in the registry.
    /// </summary>
    /// <param name="metadata">Agent metadata.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RegisterAgentAsync(AgentMetadata metadata, CancellationToken ct = default);

    /// <summary>
    /// Unregister an agent from the registry.
    /// </summary>
    /// <param name="agentId">Agent identifier to unregister.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if agent was unregistered, false if not found.</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken ct = default);

    /// <summary>
    /// Get all registered agent identifiers.
    /// </summary>
    /// <returns>Collection of agent IDs.</returns>
    IEnumerable<string> GetAgentIds();

    /// <summary>
    /// Get metadata for a specific agent.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Agent metadata or null if not found.</returns>
    Task<AgentMetadata?> GetAgentAsync(string agentId, CancellationToken ct = default);

    /// <summary>
    /// Get all registered agents.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of all agent metadata.</returns>
    Task<IEnumerable<AgentMetadata>> GetAllAgentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get agents that support a specific task kind.
    /// </summary>
    /// <param name="taskKind">Task kind to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of agents supporting the task kind.</returns>
    Task<IEnumerable<AgentMetadata>> GetAgentsByTaskKindAsync(string taskKind, CancellationToken ct = default);
}

/// <summary>
/// Default implementation of IAgentRegistry using in-memory concurrent storage.
/// </summary>
public class AgentRegistry : IAgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentMetadata> _agents = new();

    /// <inheritdoc />
    public Task RegisterAgentAsync(AgentMetadata metadata, CancellationToken ct = default)
    {
        if (metadata == null) throw new ArgumentNullException(nameof(metadata));
        if (string.IsNullOrWhiteSpace(metadata.AgentId)) throw new ArgumentException("AgentId cannot be null or empty", nameof(metadata));

        _agents.AddOrUpdate(metadata.AgentId, metadata, (_, _) => metadata);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> UnregisterAgentAsync(string agentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("AgentId cannot be null or empty", nameof(agentId));

        return Task.FromResult(_agents.TryRemove(agentId, out _));
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAgentIds()
    {
        return _agents.Keys.ToList();
    }

    /// <inheritdoc />
    public Task<AgentMetadata?> GetAgentAsync(string agentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("AgentId cannot be null or empty", nameof(agentId));

        _agents.TryGetValue(agentId, out var metadata);
        return Task.FromResult(metadata);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AgentMetadata>> GetAllAgentsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<AgentMetadata>>(_agents.Values.ToList());
    }

    /// <inheritdoc />
    public Task<IEnumerable<AgentMetadata>> GetAgentsByTaskKindAsync(string taskKind, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(taskKind)) throw new ArgumentException("TaskKind cannot be null or empty", nameof(taskKind));

        var filteredAgents = _agents.Values.Where(agent => agent.SupportedTaskKinds.Contains(taskKind)).ToList();
        return Task.FromResult<IEnumerable<AgentMetadata>>(filteredAgents);
    }
}
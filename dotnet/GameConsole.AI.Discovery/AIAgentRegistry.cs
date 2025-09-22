using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace GameConsole.AI.Discovery;

/// <summary>
/// In-memory implementation of AI agent registry with optional file persistence.
/// Thread-safe for concurrent access.
/// </summary>
public class AIAgentRegistry : IAIAgentRegistry
{
    private readonly ILogger<AIAgentRegistry> _logger;
    private readonly ConcurrentDictionary<string, AgentMetadata> _agents;
    private readonly string? _persistencePath;
    private readonly SemaphoreSlim _persistenceSemaphore;

    public AIAgentRegistry(ILogger<AIAgentRegistry> logger, string? persistencePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agents = new ConcurrentDictionary<string, AgentMetadata>();
        _persistencePath = persistencePath;
        _persistenceSemaphore = new SemaphoreSlim(1, 1);
    }

    /// <inheritdoc />
    public Task RegisterAgentAsync(AgentMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        _agents.AddOrUpdate(metadata.Id, metadata, (key, existing) =>
        {
            _logger.LogDebug("Updating registration for agent: {AgentId}", key);
            return metadata;
        });

        _logger.LogInformation("Registered agent: {AgentName} ({AgentId})", metadata.Name, metadata.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        var removed = _agents.TryRemove(agentId, out var removedAgent);
        if (removed && removedAgent != null)
        {
            _logger.LogInformation("Unregistered agent: {AgentName} ({AgentId})", removedAgent.Name, agentId);
        }
        else
        {
            _logger.LogWarning("Attempted to unregister unknown agent: {AgentId}", agentId);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AgentMetadata>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        var agents = _agents.Values.ToList().AsEnumerable();
        return Task.FromResult(agents);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AgentMetadata>> GetAgentsByCapabilityAsync(Type capabilityType, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capabilityType);

        var matchingAgents = _agents.Values
            .Where(agent => agent.Capabilities.Contains(capabilityType) || agent.AgentType.IsAssignableTo(capabilityType))
            .ToList()
            .AsEnumerable();

        return Task.FromResult(matchingAgents);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AgentMetadata>> GetAgentsByTagsAsync(IEnumerable<string> tags, bool requireAll = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tags);

        var tagsList = tags.ToList();
        if (tagsList.Count == 0)
        {
            return GetAllAgentsAsync(cancellationToken);
        }

        var matchingAgents = _agents.Values
            .Where(agent => requireAll 
                ? tagsList.All(tag => agent.Tags.Contains(tag))
                : tagsList.Any(tag => agent.Tags.Contains(tag)))
            .ToList()
            .AsEnumerable();

        return Task.FromResult(matchingAgents);
    }

    /// <inheritdoc />
    public Task<AgentMetadata?> GetAgentByIdAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<bool> UpdateAgentAvailabilityAsync(string agentId, bool isAvailable, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        if (_agents.TryGetValue(agentId, out var agent))
        {
            // Create a new metadata instance with updated availability
            var updatedAgent = new AgentMetadata
            {
                Id = agent.Id,
                Name = agent.Name,
                Description = agent.Description,
                Version = agent.Version,
                AgentType = agent.AgentType,
                Capabilities = agent.Capabilities,
                Tags = agent.Tags,
                Priority = agent.Priority,
                IsAvailable = isAvailable,
                ResourceRequirements = agent.ResourceRequirements
            };

            _agents.TryUpdate(agentId, updatedAgent, agent);
            _logger.LogDebug("Updated availability for agent {AgentId}: {IsAvailable}", agentId, isAvailable);
            return Task.FromResult(true);
        }

        _logger.LogWarning("Attempted to update availability for unknown agent: {AgentId}", agentId);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_persistencePath))
        {
            _logger.LogDebug("No persistence path configured, skipping save");
            return;
        }

        await _persistenceSemaphore.WaitAsync(cancellationToken);
        try
        {
            var agentsData = _agents.Values.Select(agent => new
            {
                agent.Id,
                agent.Name,
                agent.Description,
                agent.Version,
                AgentTypeName = agent.AgentType.AssemblyQualifiedName,
                CapabilityTypeNames = agent.Capabilities.Select(c => c.AssemblyQualifiedName).ToArray(),
                agent.Tags,
                agent.Priority,
                agent.IsAvailable,
                agent.ResourceRequirements
            }).ToArray();

            var json = JsonSerializer.Serialize(agentsData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_persistencePath, json, cancellationToken);
            _logger.LogInformation("Saved {Count} agents to {Path}", agentsData.Length, _persistencePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save agents to {Path}", _persistencePath);
            throw;
        }
        finally
        {
            _persistenceSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_persistencePath) || !File.Exists(_persistencePath))
        {
            _logger.LogDebug("No persistence file found at {Path}, starting with empty registry", _persistencePath);
            return;
        }

        await _persistenceSemaphore.WaitAsync(cancellationToken);
        try
        {
            var json = await File.ReadAllTextAsync(_persistencePath, cancellationToken);
            var agentsData = JsonSerializer.Deserialize<dynamic[]>(json);

            if (agentsData == null)
            {
                _logger.LogWarning("Failed to deserialize agents data from {Path}", _persistencePath);
                return;
            }

            // Note: This is a simplified implementation that doesn't fully restore type information
            // In a production system, you'd want more sophisticated serialization/deserialization
            _logger.LogInformation("Loaded agents data from {Path} (type restoration not implemented)", _persistencePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load agents from {Path}", _persistencePath);
            throw;
        }
        finally
        {
            _persistenceSemaphore.Release();
        }
    }
}
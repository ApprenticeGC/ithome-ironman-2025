using GameConsole.AI.Core;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Services;

/// <summary>
/// Default implementation of the AI agent registry service.
/// Provides in-memory agent registration and management capabilities.
/// </summary>
public class AIAgentRegistryService : IAIAgentRegistry
{
    private readonly ILogger<AIAgentRegistryService> _logger;
    private readonly ConcurrentDictionary<string, IAIAgent> _agents = new();
    private readonly object _lock = new();
    private volatile bool _isRunning = false;
    private DateTimeOffset _startTime = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentRegistryService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public AIAgentRegistryService(ILogger<AIAgentRegistryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public IServiceMetadata Metadata => new ServiceMetadata
    {
        Name = "AI Agent Registry",
        Version = "1.0.0",
        Description = "Registry service for managing AI agents in the GameConsole system",
        Categories = new[] { "AI", "Registry", "Management" },
        Properties = new Dictionary<string, object>
        {
            ["MaxAgents"] = int.MaxValue,
            ["InMemory"] = true
        }
    };

    /// <inheritdoc />
    public event EventHandler<AIAgentRegisteredEventArgs>? AgentRegistered;

    /// <inheritdoc />
    public event EventHandler<AIAgentUnregisteredEventArgs>? AgentUnregistered;

    /// <inheritdoc />
    public event EventHandler<AIAgentStatusChangedEventArgs>? AgentStatusChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI Agent Registry Service");
        
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("AI Agent Registry Service is already initialized");
                return;
            }
        }

        _startTime = DateTimeOffset.UtcNow;
        
        _logger.LogInformation("AI Agent Registry Service initialized successfully");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI Agent Registry Service");
        
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("AI Agent Registry Service is already running");
                return;
            }
            
            _isRunning = true;
        }

        _logger.LogInformation("AI Agent Registry Service started successfully");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AI Agent Registry Service");
        
        lock (_lock)
        {
            if (!_isRunning)
            {
                _logger.LogWarning("AI Agent Registry Service is not running");
                return;
            }
            
            _isRunning = false;
        }

        _logger.LogInformation("AI Agent Registry Service stopped successfully");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RegisterAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        if (agent == null) throw new ArgumentNullException(nameof(agent));
        
        var agentId = agent.Metadata.Name;
        _logger.LogDebug("Registering AI agent: {AgentId}", agentId);

        if (_agents.TryAdd(agentId, agent))
        {
            // Subscribe to agent status changes
            agent.StatusChanged += OnAgentStatusChanged;
            
            _logger.LogInformation("Successfully registered AI agent: {AgentId}", agentId);
            AgentRegistered?.Invoke(this, new AIAgentRegisteredEventArgs(agent));
        }
        else
        {
            _logger.LogWarning("AI agent {AgentId} is already registered", agentId);
            throw new InvalidOperationException($"AI agent '{agentId}' is already registered");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        
        _logger.LogDebug("Unregistering AI agent: {AgentId}", agentId);

        if (_agents.TryRemove(agentId, out var agent))
        {
            // Unsubscribe from agent status changes
            agent.StatusChanged -= OnAgentStatusChanged;
            
            _logger.LogInformation("Successfully unregistered AI agent: {AgentId}", agentId);
            AgentUnregistered?.Invoke(this, new AIAgentUnregisteredEventArgs(agentId));
            return true;
        }
        
        _logger.LogWarning("AI agent {AgentId} not found for unregistration", agentId);
        return false;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_agents.Values.ToList());
    }

    /// <inheritdoc />
    public async Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) return null;
        
        _agents.TryGetValue(agentId, out var agent);
        return await Task.FromResult(agent);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default)
    {
        var availableAgents = _agents.Values
            .Where(a => a.Status == AIAgentStatus.Ready)
            .ToList();
            
        return await Task.FromResult(availableAgents);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> GetAgentsByStatusAsync(AIAgentStatus status, CancellationToken cancellationToken = default)
    {
        var agentsByStatus = _agents.Values
            .Where(a => a.Status == status)
            .ToList();
            
        return await Task.FromResult(agentsByStatus);
    }

    /// <inheritdoc />
    public async Task<bool> IsAgentRegisteredAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) return false;
        
        return await Task.FromResult(_agents.ContainsKey(agentId));
    }

    /// <inheritdoc />
    public async Task<int> GetAgentCountAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_agents.Count);
    }

    /// <inheritdoc />
    public async Task<AIAgentRegistryStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var agentsByStatus = new Dictionary<AIAgentStatus, int>();
        
        foreach (var statusGroup in _agents.Values.GroupBy(a => a.Status))
        {
            agentsByStatus[statusGroup.Key] = statusGroup.Count();
        }

        var statistics = new AIAgentRegistryStatistics
        {
            TotalAgents = _agents.Count,
            AgentsByStatus = agentsByStatus,
            RegistryUptime = DateTimeOffset.UtcNow - _startTime
        };

        return await Task.FromResult(statistics);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AIAgentHealthStatus>> PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        var healthResults = new List<AIAgentHealthStatus>();
        
        foreach (var agent in _agents.Values)
        {
            try
            {
                var isHealthy = agent.Status != AIAgentStatus.Error && agent.Status != AIAgentStatus.Stopped;
                var details = $"Agent status: {agent.Status}";
                
                healthResults.Add(new AIAgentHealthStatus(agent.Metadata.Name, isHealthy, details));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for agent {AgentId}", agent.Metadata.Name);
                healthResults.Add(new AIAgentHealthStatus(agent.Metadata.Name, false, $"Health check exception: {ex.Message}"));
            }
        }

        return await Task.FromResult(healthResults);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing AI Agent Registry Service");

        // Unsubscribe from all agent status changes
        foreach (var agent in _agents.Values)
        {
            agent.StatusChanged -= OnAgentStatusChanged;
        }

        _agents.Clear();
        
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }

    private void OnAgentStatusChanged(object? sender, AIAgentStatusChangedEventArgs e)
    {
        if (sender is IAIAgent agent)
        {
            _logger.LogDebug("Agent {AgentId} status changed from {PreviousStatus} to {CurrentStatus}", 
                agent.Metadata.Name, e.PreviousStatus, e.CurrentStatus);
            
            AgentStatusChanged?.Invoke(this, e);
        }
    }
}

/// <summary>
/// Simple implementation of IServiceMetadata for the AI Agent Registry.
/// </summary>
internal class ServiceMetadata : IServiceMetadata
{
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IEnumerable<string> Categories { get; init; } = Enumerable.Empty<string>();
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}
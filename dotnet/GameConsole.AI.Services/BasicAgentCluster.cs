using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Services;

/// <summary>
/// Basic implementation of an agent cluster that manages a collection of AI agents.
/// This is a Tier 3 service that provides clustering behavior and load balancing.
/// </summary>
public class BasicAgentCluster : IAgentCluster
{
    private readonly ILogger<BasicAgentCluster> _logger;
    private readonly ConcurrentDictionary<string, IAIAgent> _agents = new();
    private readonly object _lockObject = new();
    private ClusterHealth _health = ClusterHealth.Healthy;
    private bool _isRunning = false;
    private int _roundRobinIndex = 0;

    /// <inheritdoc />
    public event EventHandler<AgentEventArgs>? AgentJoined;

    /// <inheritdoc />
    public event EventHandler<AgentEventArgs>? AgentLeft;

    /// <inheritdoc />
    public event EventHandler<ClusterEventArgs>? HealthChanged;

    /// <inheritdoc />
    public event EventHandler<ClusterEventArgs>? MembershipChanged;

    /// <inheritdoc />
    public string ClusterId { get; }

    /// <inheritdoc />
    public ClusterHealth Health 
    { 
        get => _health;
        private set
        {
            if (_health != value)
            {
                var oldHealth = _health;
                _health = value;
                _logger.LogInformation("Cluster {ClusterId} health changed from {OldHealth} to {NewHealth}", ClusterId, oldHealth, value);
                HealthChanged?.Invoke(this, new ClusterEventArgs(ClusterId, value));
            }
        }
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes a new instance of the BasicAgentCluster class.
    /// </summary>
    /// <param name="clusterId">The unique identifier for this cluster.</param>
    /// <param name="logger">Logger for this cluster.</param>
    public BasicAgentCluster(string clusterId, ILogger<BasicAgentCluster> logger)
    {
        ClusterId = clusterId ?? throw new ArgumentNullException(nameof(clusterId));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing cluster {ClusterId}", ClusterId);
        Health = ClusterHealth.Healthy;
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cluster {ClusterId}", ClusterId);
        _isRunning = true;
        Health = ClusterHealth.Healthy;
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping cluster {ClusterId}", ClusterId);
        
        // Stop all agents
        var tasks = _agents.Values.Select(agent => agent.StopAsync(cancellationToken)).ToArray();
        await Task.WhenAll(tasks);
        
        _isRunning = false;
        Health = ClusterHealth.Offline;
    }

    /// <inheritdoc />
    public async Task<int> GetAgentCountAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_agents.Count);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAgentIdsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_agents.Keys.ToList());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAgentsByTypeAsync(string agentType, CancellationToken cancellationToken = default)
    {
        var matchingAgents = _agents.Values
            .Where(agent => agent.AgentType == agentType)
            .Select(agent => agent.AgentId)
            .ToList();
        
        return await Task.FromResult(matchingAgents);
    }

    /// <inheritdoc />
    public async Task AddAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        if (agent == null) throw new ArgumentNullException(nameof(agent));
        
        _logger.LogInformation("Adding agent {AgentId} to cluster {ClusterId}", agent.AgentId, ClusterId);
        
        if (!_agents.TryAdd(agent.AgentId, agent))
        {
            throw new InvalidOperationException($"Agent with ID {agent.AgentId} already exists in cluster {ClusterId}");
        }

        // Subscribe to agent state changes
        agent.StateChanged += OnAgentStateChanged;
        
        AgentJoined?.Invoke(this, new AgentEventArgs(agent.AgentId, agent.State));
        MembershipChanged?.Invoke(this, new ClusterEventArgs(ClusterId, Health, $"Agent {agent.AgentId} joined"));
        
        // Start the agent if the cluster is running
        if (_isRunning)
        {
            await agent.InitializeAsync(cancellationToken);
            await agent.StartAsync(cancellationToken);
        }
        
        await UpdateHealthStatusAsync();
    }

    /// <inheritdoc />
    public async Task RemoveAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing agent {AgentId} from cluster {ClusterId}", agentId, ClusterId);
        
        if (_agents.TryRemove(agentId, out var agent))
        {
            agent.StateChanged -= OnAgentStateChanged;
            await agent.StopAsync(cancellationToken);
            await agent.DisposeAsync();
            
            AgentLeft?.Invoke(this, new AgentEventArgs(agentId, AgentState.Disposed));
            MembershipChanged?.Invoke(this, new ClusterEventArgs(ClusterId, Health, $"Agent {agentId} left"));
        }
        
        await UpdateHealthStatusAsync();
    }

    /// <inheritdoc />
    public async Task<(object? result, string agentId)> DistributeTaskAsync(object task, string? agentType = null, CancellationToken cancellationToken = default)
    {
        var availableAgents = _agents.Values
            .Where(agent => agentType == null || agent.AgentType == agentType)
            .Where(agent => agent.State == AgentState.Processing || agent.State == AgentState.Idle)
            .ToList();

        if (!availableAgents.Any())
        {
            throw new InvalidOperationException($"No available agents of type {agentType ?? "any"} in cluster {ClusterId}");
        }

        // Simple round-robin selection
        var selectedAgent = availableAgents[_roundRobinIndex % availableAgents.Count];
        Interlocked.Increment(ref _roundRobinIndex);

        _logger.LogDebug("Distributing task to agent {AgentId} in cluster {ClusterId}", selectedAgent.AgentId, ClusterId);
        
        var result = await selectedAgent.ProcessTaskAsync(task, cancellationToken);
        return (result, selectedAgent.AgentId);
    }

    /// <inheritdoc />
    public async Task BroadcastMessageAsync(object message, string? agentType = null, CancellationToken cancellationToken = default)
    {
        var targetAgents = _agents.Values
            .Where(agent => agentType == null || agent.AgentType == agentType)
            .ToList();

        _logger.LogDebug("Broadcasting message to {Count} agents in cluster {ClusterId}", targetAgents.Count, ClusterId);

        var tasks = targetAgents.Select(agent => agent.SendMessageAsync("broadcast", message, cancellationToken)).ToArray();
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async Task<bool> HasAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_agents.ContainsKey(agentId));
    }

    /// <inheritdoc />
    public async Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        _agents.TryGetValue(agentId, out var agent);
        return await Task.FromResult(agent);
    }

    private async Task UpdateHealthStatusAsync()
    {
        var totalAgents = _agents.Count;
        var healthyAgents = _agents.Values.Count(agent => 
            agent.State == AgentState.Processing || agent.State == AgentState.Idle);

        if (totalAgents == 0)
        {
            Health = ClusterHealth.Offline;
        }
        else if (healthyAgents == totalAgents)
        {
            Health = ClusterHealth.Healthy;
        }
        else if (healthyAgents > totalAgents / 2)
        {
            Health = ClusterHealth.Degraded;
        }
        else
        {
            Health = ClusterHealth.Unhealthy;
        }

        await Task.CompletedTask;
    }

    private async void OnAgentStateChanged(object? sender, AgentEventArgs e)
    {
        _logger.LogDebug("Agent {AgentId} state changed to {State} in cluster {ClusterId}", e.AgentId, e.State, ClusterId);
        await UpdateHealthStatusAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        
        var agents = _agents.Values.ToList();
        foreach (var agent in agents)
        {
            agent.StateChanged -= OnAgentStateChanged;
            await agent.DisposeAsync();
        }
        
        _agents.Clear();
        _logger.LogInformation("Cluster {ClusterId} disposed", ClusterId);
    }
}
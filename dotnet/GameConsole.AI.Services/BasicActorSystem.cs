using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Services;

/// <summary>
/// Basic implementation of an actor system that manages AI agents and clustering.
/// This is a Tier 3 service that provides the core actor system behavior.
/// </summary>
public class BasicActorSystem : IActorSystem
{
    private readonly ILogger<BasicActorSystem> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, IAgentCluster> _clusters = new();
    private readonly ConcurrentDictionary<string, string> _agentToClusterMap = new(); // AgentId -> ClusterId
    private ActorSystemMode _mode = ActorSystemMode.SingleNode;
    private bool _isRunning = false;
    private int _agentIdCounter = 0;

    /// <inheritdoc />
    public event EventHandler<ActorSystemEventArgs>? ModeChanged;

    /// <inheritdoc />
    public event EventHandler<ClusterEventArgs>? ClusterCreated;

    /// <inheritdoc />
    public event EventHandler<ClusterEventArgs>? ClusterRemoved;

    /// <inheritdoc />
    public string SystemName { get; }

    /// <inheritdoc />
    public ActorSystemMode Mode 
    { 
        get => _mode;
        private set
        {
            if (_mode != value)
            {
                var oldMode = _mode;
                _mode = value;
                _logger.LogInformation("Actor system {SystemName} mode changed from {OldMode} to {NewMode}", SystemName, oldMode, value);
                ModeChanged?.Invoke(this, new ActorSystemEventArgs(SystemName, value));
            }
        }
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes a new instance of the BasicActorSystem class.
    /// </summary>
    /// <param name="systemName">The name of this actor system.</param>
    /// <param name="loggerFactory">Factory for creating loggers.</param>
    public BasicActorSystem(string systemName, ILoggerFactory loggerFactory)
    {
        SystemName = systemName ?? throw new ArgumentNullException(nameof(systemName));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<BasicActorSystem>();
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing actor system {SystemName}", SystemName);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting actor system {SystemName} in {Mode} mode", SystemName, Mode);
        _isRunning = true;
        
        // Start all existing clusters
        var tasks = _clusters.Values.Select(cluster => cluster.StartAsync(cancellationToken)).ToArray();
        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping actor system {SystemName}", SystemName);
        
        // Stop all clusters
        var tasks = _clusters.Values.Select(cluster => cluster.StopAsync(cancellationToken)).ToArray();
        await Task.WhenAll(tasks);
        
        _isRunning = false;
    }

    /// <inheritdoc />
    public async Task<IAIAgent> CreateAgentAsync(string agentType, string? agentId = null, object? configuration = null, CancellationToken cancellationToken = default)
    {
        agentId ??= $"{agentType}-{Interlocked.Increment(ref _agentIdCounter):D6}";
        
        _logger.LogInformation("Creating agent {AgentId} of type {AgentType}", agentId, agentType);
        
        var agentLogger = _loggerFactory.CreateLogger<BasicAIAgent>();
        var agent = new BasicAIAgent(agentId, agentType, agentLogger);
        
        await agent.InitializeAsync(cancellationToken);
        
        return agent;
    }

    /// <inheritdoc />
    public async Task DestroyAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Destroying agent {AgentId}", agentId);
        
        // Find which cluster contains this agent and remove it
        if (_agentToClusterMap.TryGetValue(agentId, out var clusterId))
        {
            if (_clusters.TryGetValue(clusterId, out var cluster))
            {
                await cluster.RemoveAgentAsync(agentId, cancellationToken);
                _agentToClusterMap.TryRemove(agentId, out _);
            }
        }
    }

    /// <inheritdoc />
    public async Task<IAgentCluster> CreateClusterAsync(string clusterId, object? configuration = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating cluster {ClusterId}", clusterId);
        
        var clusterLogger = _loggerFactory.CreateLogger<BasicAgentCluster>();
        var cluster = new BasicAgentCluster(clusterId, clusterLogger);
        
        if (!_clusters.TryAdd(clusterId, cluster))
        {
            throw new InvalidOperationException($"Cluster with ID {clusterId} already exists");
        }
        
        await cluster.InitializeAsync(cancellationToken);
        
        if (_isRunning)
        {
            await cluster.StartAsync(cancellationToken);
        }
        
        ClusterCreated?.Invoke(this, new ClusterEventArgs(clusterId, ClusterHealth.Healthy));
        
        return cluster;
    }

    /// <inheritdoc />
    public async Task DestroyClusterAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Destroying cluster {ClusterId}", clusterId);
        
        if (_clusters.TryRemove(clusterId, out var cluster))
        {
            // Remove all agent mappings for this cluster
            var agentsToRemove = _agentToClusterMap
                .Where(kvp => kvp.Value == clusterId)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var agentId in agentsToRemove)
            {
                _agentToClusterMap.TryRemove(agentId, out _);
            }
            
            await cluster.StopAsync(cancellationToken);
            await cluster.DisposeAsync();
            
            ClusterRemoved?.Invoke(this, new ClusterEventArgs(clusterId, ClusterHealth.Offline));
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetClusterIdsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_clusters.Keys.ToList());
    }

    /// <inheritdoc />
    public async Task<IAgentCluster?> GetClusterAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        _clusters.TryGetValue(clusterId, out var cluster);
        return await Task.FromResult(cluster);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetAllAgentIdsAsync(CancellationToken cancellationToken = default)
    {
        var allAgentIds = new List<string>();
        
        foreach (var cluster in _clusters.Values)
        {
            var agentIds = await cluster.GetAgentIdsAsync(cancellationToken);
            allAgentIds.AddRange(agentIds);
        }
        
        return allAgentIds;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> FindAgentsByTypeAsync(string agentType, CancellationToken cancellationToken = default)
    {
        var matchingAgents = new List<string>();
        
        foreach (var cluster in _clusters.Values)
        {
            var agentIds = await cluster.GetAgentsByTypeAsync(agentType, cancellationToken);
            matchingAgents.AddRange(agentIds);
        }
        
        return matchingAgents;
    }

    /// <inheritdoc />
    public async Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (_agentToClusterMap.TryGetValue(agentId, out var clusterId))
        {
            if (_clusters.TryGetValue(clusterId, out var cluster))
            {
                return await cluster.GetAgentAsync(agentId, cancellationToken);
            }
        }
        
        // If not found in mapping, search all clusters
        foreach (var cluster in _clusters.Values)
        {
            var agent = await cluster.GetAgentAsync(agentId, cancellationToken);
            if (agent != null)
            {
                // Update the mapping for faster future lookups
                _agentToClusterMap.TryAdd(agentId, cluster.ClusterId);
                return agent;
            }
        }
        
        return null;
    }

    /// <inheritdoc />
    public async Task SetModeAsync(ActorSystemMode mode, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting actor system {SystemName} mode to {Mode}", SystemName, mode);
        Mode = mode;
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new[]
        {
            typeof(IActorSystem)
        });
    }

    /// <inheritdoc />
    public async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var capabilities = await GetCapabilitiesAsync(cancellationToken);
        return capabilities.Contains(typeof(T));
    }

    /// <inheritdoc />
    public async Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IActorSystem))
        {
            return this as T;
        }
        
        return await Task.FromResult<T?>(null);
    }

    /// <summary>
    /// Helper method to add an agent to a specific cluster and update internal mappings.
    /// </summary>
    /// <param name="clusterId">The target cluster ID.</param>
    /// <param name="agent">The agent to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task AddAgentToClusterAsync(string clusterId, IAIAgent agent, CancellationToken cancellationToken = default)
    {
        if (_clusters.TryGetValue(clusterId, out var cluster))
        {
            await cluster.AddAgentAsync(agent, cancellationToken);
            _agentToClusterMap.TryAdd(agent.AgentId, clusterId);
        }
        else
        {
            throw new InvalidOperationException($"Cluster {clusterId} not found");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        
        var clusters = _clusters.Values.ToList();
        foreach (var cluster in clusters)
        {
            await cluster.DisposeAsync();
        }
        
        _clusters.Clear();
        _agentToClusterMap.Clear();
        
        _logger.LogInformation("Actor system {SystemName} disposed", SystemName);
    }
}
using GameConsole.Core.Abstractions;
using GameConsole.Engine.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services;

/// <summary>
/// Implementation of the agent manager that coordinates AI agent lifecycle and cluster operations.
/// This is a Tier 2 service that acts as a mechanical proxy to the underlying actor system.
/// </summary>
public class DefaultAgentManager : IAgentManager
{
    private readonly ILogger<DefaultAgentManager> _logger;
    private IActorSystem? _actorSystem;
    private AgentManagementConfiguration _configuration = new();
    private bool _isRunning = false;
    private readonly Dictionary<string, LoadBalancingStrategy> _clusterStrategies = new();

    /// <inheritdoc />
    public event EventHandler<EventArgs>? ConfigurationChanged;

    /// <inheritdoc />
    public event EventHandler<AgentEventArgs>? OperationCompleted;

    /// <inheritdoc />
    public event EventHandler<ClusterEventArgs>? TopologyChanged;

    /// <inheritdoc />
    public AgentManagementConfiguration Configuration => _configuration;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes a new instance of the DefaultAgentManager class.
    /// </summary>
    /// <param name="logger">Logger for this service.</param>
    public DefaultAgentManager(ILogger<DefaultAgentManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing DefaultAgentManager");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting DefaultAgentManager");
        
        if (_actorSystem != null && !_actorSystem.IsRunning)
        {
            await _actorSystem.StartAsync(cancellationToken);
        }
        
        _isRunning = true;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping DefaultAgentManager");
        
        if (_actorSystem != null && _actorSystem.IsRunning)
        {
            await _actorSystem.StopAsync(cancellationToken);
        }
        
        _isRunning = false;
    }

    /// <inheritdoc />
    public async Task SetActorSystemAsync(IActorSystem actorSystem, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting actor system: {SystemName}", actorSystem?.SystemName ?? "null");
        _actorSystem = actorSystem;
        
        if (_actorSystem != null)
        {
            // Subscribe to actor system events for forwarding
            _actorSystem.ClusterCreated += (sender, args) => TopologyChanged?.Invoke(this, args);
            _actorSystem.ClusterRemoved += (sender, args) => TopologyChanged?.Invoke(this, args);
            
            if (_isRunning && !_actorSystem.IsRunning)
            {
                await _actorSystem.InitializeAsync(cancellationToken);
                await _actorSystem.StartAsync(cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async Task UpdateConfigurationAsync(AgentManagementConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating agent management configuration");
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task CreateClusterAsync(string clusterId, LoadBalancingStrategy loadBalancingStrategy = LoadBalancingStrategy.RoundRobin, CancellationToken cancellationToken = default)
    {
        ThrowIfActorSystemNotSet();
        
        _logger.LogInformation("Creating cluster {ClusterId} with {LoadBalancingStrategy} strategy", clusterId, loadBalancingStrategy);
        
        await _actorSystem!.CreateClusterAsync(clusterId, null, cancellationToken);
        _clusterStrategies[clusterId] = loadBalancingStrategy;
    }

    /// <inheritdoc />
    public async Task DestroyClusterAsync(string clusterId, bool graceful = true, CancellationToken cancellationToken = default)
    {
        ThrowIfActorSystemNotSet();
        
        _logger.LogInformation("Destroying cluster {ClusterId} (graceful: {Graceful})", clusterId, graceful);
        
        if (graceful)
        {
            // Give agents time to complete current tasks
            await Task.Delay(1000, cancellationToken);
        }
        
        await _actorSystem!.DestroyClusterAsync(clusterId, cancellationToken);
        _clusterStrategies.Remove(clusterId);
    }

    /// <inheritdoc />
    public async Task<string> SpawnAgentAsync(string clusterId, string agentType, string? agentId = null, CancellationToken cancellationToken = default)
    {
        ThrowIfActorSystemNotSet();
        
        _logger.LogInformation("Spawning agent of type {AgentType} in cluster {ClusterId}", agentType, clusterId);
        
        var agent = await _actorSystem!.CreateAgentAsync(agentType, agentId, null, cancellationToken);
        
        // Add to cluster using the helper method
        var basicActorSystem = _actorSystem as BasicActorSystem;
        if (basicActorSystem != null)
        {
            await basicActorSystem.AddAgentToClusterAsync(clusterId, agent, cancellationToken);
        }
        else
        {
            // Fallback for other implementations
            var cluster = await _actorSystem.GetClusterAsync(clusterId, cancellationToken);
            if (cluster != null)
            {
                await cluster.AddAgentAsync(agent, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException($"Cluster {clusterId} not found");
            }
        }
        
        OperationCompleted?.Invoke(this, new AgentEventArgs(agent.AgentId, agent.State, "spawned"));
        return agent.AgentId;
    }

    /// <inheritdoc />
    public async Task TerminateAgentAsync(string agentId, bool graceful = true, CancellationToken cancellationToken = default)
    {
        ThrowIfActorSystemNotSet();
        
        _logger.LogInformation("Terminating agent {AgentId} (graceful: {Graceful})", agentId, graceful);
        
        if (graceful)
        {
            var agent = await _actorSystem!.GetAgentAsync(agentId, cancellationToken);
            if (agent != null)
            {
                await agent.StopAsync(cancellationToken);
            }
        }
        
        await _actorSystem!.DestroyAgentAsync(agentId, cancellationToken);
        OperationCompleted?.Invoke(this, new AgentEventArgs(agentId, AgentState.Disposed, "terminated"));
    }

    /// <inheritdoc />
    public async Task<(object? result, string agentId)> SubmitTaskAsync(string clusterId, object task, string? agentType = null, CancellationToken cancellationToken = default)
    {
        ThrowIfActorSystemNotSet();
        
        _logger.LogDebug("Submitting task to cluster {ClusterId}", clusterId);
        
        var cluster = await _actorSystem!.GetClusterAsync(clusterId, cancellationToken);
        if (cluster == null)
        {
            throw new InvalidOperationException($"Cluster {clusterId} not found");
        }
        
        var result = await cluster.DistributeTaskAsync(task, agentType, cancellationToken);
        OperationCompleted?.Invoke(this, new AgentEventArgs(result.agentId, AgentState.Processing, "task_completed"));
        
        return result;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, ClusterHealth>> GetClusterStatusAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfActorSystemNotSet();
        
        var status = new Dictionary<string, ClusterHealth>();
        var clusterIds = await _actorSystem!.GetClusterIdsAsync(cancellationToken);
        
        foreach (var clusterId in clusterIds)
        {
            var cluster = await _actorSystem.GetClusterAsync(clusterId, cancellationToken);
            if (cluster != null)
            {
                status[clusterId] = cluster.Health;
            }
        }
        
        return status;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>> GetMetricsAsync(string? clusterId = null, CancellationToken cancellationToken = default)
    {
        ThrowIfActorSystemNotSet();
        
        var metrics = new Dictionary<string, object>
        {
            ["system_name"] = _actorSystem!.SystemName,
            ["system_mode"] = _actorSystem.Mode.ToString(),
            ["system_running"] = _actorSystem.IsRunning,
            ["timestamp"] = DateTime.UtcNow
        };
        
        if (clusterId == null)
        {
            // System-wide metrics
            var clusterIds = await _actorSystem.GetClusterIdsAsync(cancellationToken);
            metrics["total_clusters"] = clusterIds.Count();
            
            var allAgentIds = await _actorSystem.GetAllAgentIdsAsync(cancellationToken);
            metrics["total_agents"] = allAgentIds.Count();
        }
        else
        {
            // Cluster-specific metrics
            var cluster = await _actorSystem.GetClusterAsync(clusterId, cancellationToken);
            if (cluster != null)
            {
                metrics["cluster_id"] = clusterId;
                metrics["cluster_health"] = cluster.Health.ToString();
                metrics["agent_count"] = await cluster.GetAgentCountAsync(cancellationToken);
                
                if (_clusterStrategies.TryGetValue(clusterId, out var strategy))
                {
                    metrics["load_balancing_strategy"] = strategy.ToString();
                }
            }
        }
        
        return metrics;
    }

    /// <inheritdoc />
    public async Task PerformHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfActorSystemNotSet();
        
        _logger.LogInformation("Performing health check on all clusters");
        
        var clusterIds = await _actorSystem!.GetClusterIdsAsync(cancellationToken);
        
        foreach (var clusterId in clusterIds)
        {
            var cluster = await _actorSystem.GetClusterAsync(clusterId, cancellationToken);
            if (cluster != null)
            {
                // Check if cluster has agents
                var agentCount = await cluster.GetAgentCountAsync(cancellationToken);
                _logger.LogDebug("Cluster {ClusterId} has {AgentCount} agents, health: {Health}", 
                    clusterId, agentCount, cluster.Health);
            }
        }
    }

    /// <inheritdoc />
    public async Task RebalanceAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfActorSystemNotSet();
        
        _logger.LogInformation("Performing cluster rebalancing");
        
        // Basic rebalancing logic - in a real implementation this would be more sophisticated
        var clusterIds = await _actorSystem!.GetClusterIdsAsync(cancellationToken);
        var clusterAgentCounts = new Dictionary<string, int>();
        
        foreach (var clusterId in clusterIds)
        {
            var cluster = await _actorSystem.GetClusterAsync(clusterId, cancellationToken);
            if (cluster != null)
            {
                clusterAgentCounts[clusterId] = await cluster.GetAgentCountAsync(cancellationToken);
            }
        }
        
        _logger.LogInformation("Cluster agent distribution: {Distribution}", 
            string.Join(", ", clusterAgentCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}")));
        
        // For now, just log the distribution - actual rebalancing would move agents between clusters
        await Task.CompletedTask;
    }

    private void ThrowIfActorSystemNotSet()
    {
        if (_actorSystem == null)
        {
            throw new InvalidOperationException("Actor system must be set before performing operations");
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        
        if (_actorSystem != null)
        {
            await _actorSystem.DisposeAsync();
        }
        
        _logger.LogInformation("DefaultAgentManager disposed");
    }
}
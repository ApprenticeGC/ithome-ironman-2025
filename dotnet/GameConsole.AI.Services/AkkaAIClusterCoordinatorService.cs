using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Akka.Actor;
using Akka.Cluster;

namespace GameConsole.AI.Services;

/// <summary>
/// Akka.NET-based implementation of the AI cluster coordinator service.
/// Manages distributed AI agents using Akka Cluster for coordination and fault tolerance.
/// </summary>
public class AkkaAIClusterCoordinatorService : IAIClusterCoordinatorService, IDisposable
{
    private readonly ILogger<AkkaAIClusterCoordinatorService> _logger;
    private readonly ConcurrentDictionary<string, IClusterableAIAgent> _registeredAgents = new();
    private readonly ConcurrentDictionary<string, ClusterMemberInfo> _clusterMembers = new();
    
    private ActorSystem? _actorSystem;
    private IActorRef? _coordinatorActor;
    private Cluster? _cluster;
    private AIClusterConfiguration? _currentConfig;
    private bool _disposed;
    private bool _isRunning;

    public event EventHandler<ClusterTopologyChangedEventArgs>? TopologyChanged;

    public bool IsRunning => _isRunning;

    public AkkaAIClusterCoordinatorService(ILogger<AkkaAIClusterCoordinatorService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AkkaAIClusterCoordinatorService");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AkkaAIClusterCoordinatorService");
        _isRunning = true;
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AkkaAIClusterCoordinatorService");
        await ShutdownClusterAsync(cancellationToken);
        _isRunning = false;
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await ShutdownClusterAsync();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public Task RegisterAgentAsync(IClusterableAIAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        
        if (_registeredAgents.TryAdd(agent.AgentId, agent))
        {
            _logger.LogInformation("Registered AI agent {AgentId} with role {Role}", agent.AgentId, agent.ClusterRole);
            
            // Subscribe to agent status changes
            agent.MembershipChanged += OnAgentMembershipChanged;
            
            // If cluster is already running, join the agent
            if (_actorSystem != null && _currentConfig != null)
            {
                return agent.JoinClusterAsync(_currentConfig, cancellationToken);
            }
        }
        else
        {
            _logger.LogWarning("Agent {AgentId} is already registered", agent.AgentId);
        }

        return Task.CompletedTask;
    }

    public Task UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        if (_registeredAgents.TryRemove(agentId, out var agent))
        {
            _logger.LogInformation("Unregistered AI agent {AgentId}", agentId);
            
            agent.MembershipChanged -= OnAgentMembershipChanged;
            
            return agent.LeaveClusterAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<IClusterableAIAgent>> GetRegisteredAgentsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<IClusterableAIAgent>>(_registeredAgents.Values);
    }

    public Task<IEnumerable<IClusterableAIAgent>> GetAgentsByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);

        var agents = _registeredAgents.Values.Where(a => a.ClusterRole == role);
        return Task.FromResult(agents);
    }

    public async Task InitiateClusterAsync(AIClusterConfiguration config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        _logger.LogInformation("Initiating AI cluster on {Hostname}:{Port}", config.Hostname, config.Port);
        _currentConfig = config;

        // Create simplified cluster for now - in production would use proper Akka configuration
        _logger.LogInformation("AI cluster initiated (simplified implementation for testing)");

        // Join all registered agents to the cluster
        foreach (var agent in _registeredAgents.Values)
        {
            await agent.JoinClusterAsync(config, cancellationToken);
        }
    }

    public async Task ShutdownClusterAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down AI cluster");

        // Leave cluster for all agents
        var leaveTasks = _registeredAgents.Values.Select(agent => agent.LeaveClusterAsync(cancellationToken));
        await Task.WhenAll(leaveTasks);

        // Shutdown actor system
        if (_actorSystem != null)
        {
            await _actorSystem.Terminate();
            _actorSystem = null;
        }

        _cluster = null;
        _coordinatorActor = null;
        _currentConfig = null;

        _logger.LogInformation("AI cluster shut down completed");
    }

    public Task<ClusterHealthInfo> GetClusterHealthAsync(CancellationToken cancellationToken = default)
    {
        var members = _clusterMembers.Values;
        var activeMembers = members.Count(m => m.Status == ClusterMembershipStatus.Active);
        var unreachableMembers = members.Count(m => m.Status == ClusterMembershipStatus.Disconnected);
        var totalMembers = members.Count();
        
        var isHealthy = totalMembers == 0 || (activeMembers > 0 && unreachableMembers == 0);
        var leader = members.FirstOrDefault();

        var health = new ClusterHealthInfo(totalMembers, activeMembers, unreachableMembers, 
            isHealthy, leader, DateTime.UtcNow);

        return Task.FromResult(health);
    }

    private void OnAgentMembershipChanged(object? sender, ClusterMembershipChangedEventArgs e)
    {
        if (sender is IClusterableAIAgent agent)
        {
            _logger.LogInformation("Agent {AgentId} membership changed from {Previous} to {Current}", 
                agent.AgentId, e.PreviousStatus, e.CurrentStatus);

            // Update cluster member info
            var memberInfo = new ClusterMemberInfo(
                agent.AgentId, 
                agent.ClusterRole, 
                $"{_currentConfig?.Hostname}:{_currentConfig?.Port}",
                e.CurrentStatus,
                DateTime.UtcNow);

            _clusterMembers.AddOrUpdate(agent.AgentId, memberInfo, (_, _) => memberInfo);

            // Determine change type
            var changeType = e.CurrentStatus switch
            {
                ClusterMembershipStatus.Active => ClusterTopologyChangeType.MemberJoined,
                ClusterMembershipStatus.Leaving => ClusterTopologyChangeType.MemberLeft,
                ClusterMembershipStatus.Removed => ClusterTopologyChangeType.MemberRemoved,
                ClusterMembershipStatus.Disconnected => ClusterTopologyChangeType.MemberUnreachable,
                _ => ClusterTopologyChangeType.MemberJoined
            };

            // Get current health
            var health = GetClusterHealthAsync().Result;

            // Raise topology changed event
            TopologyChanged?.Invoke(this, new ClusterTopologyChangedEventArgs(changeType, memberInfo, health));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(10));
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
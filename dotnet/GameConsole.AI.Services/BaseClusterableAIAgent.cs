using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services;

/// <summary>
/// Base implementation of a clusterable AI agent.
/// Provides distributed coordination and messaging capabilities.
/// </summary>
public class BaseClusterableAIAgent : IClusterableAIAgent, IDisposable
{
    private readonly ILogger<BaseClusterableAIAgent> _logger;
    private ClusterMembershipStatus _status = ClusterMembershipStatus.Disconnected;
    private AIClusterConfiguration? _clusterConfig;
    private bool _disposed;

    public string AgentId { get; }
    public string ClusterRole { get; }
    public ClusterMembershipStatus Status => _status;

    public event EventHandler<ClusterMembershipChangedEventArgs>? MembershipChanged;

    public BaseClusterableAIAgent(string agentId, string clusterRole, ILogger<BaseClusterableAIAgent> logger)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        ClusterRole = clusterRole ?? throw new ArgumentNullException(nameof(clusterRole));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IClusterableAIAgent) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public virtual Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T).IsAssignableFrom(typeof(IClusterableAIAgent)));
    }

    public virtual Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T).IsAssignableFrom(typeof(IClusterableAIAgent)))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    public virtual async Task JoinClusterAsync(AIClusterConfiguration clusterConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clusterConfig);

        if (_status != ClusterMembershipStatus.Disconnected)
        {
            _logger.LogWarning("Agent {AgentId} is already connected to cluster", AgentId);
            return;
        }

        _logger.LogInformation("Agent {AgentId} joining cluster with role {Role}", AgentId, ClusterRole);
        
        SetStatus(ClusterMembershipStatus.Joining);
        _clusterConfig = clusterConfig;

        try
        {
            // Simulate cluster join operation
            await Task.Delay(100, cancellationToken); // Simulate network delay
            
            SetStatus(ClusterMembershipStatus.Active);
            _logger.LogInformation("Agent {AgentId} successfully joined cluster", AgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join cluster for agent {AgentId}", AgentId);
            SetStatus(ClusterMembershipStatus.Disconnected);
            throw;
        }
    }

    public virtual async Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        if (_status == ClusterMembershipStatus.Disconnected)
        {
            return;
        }

        _logger.LogInformation("Agent {AgentId} leaving cluster", AgentId);
        SetStatus(ClusterMembershipStatus.Leaving);

        try
        {
            // Simulate cluster leave operation
            await Task.Delay(50, cancellationToken);
            
            SetStatus(ClusterMembershipStatus.Disconnected);
            _clusterConfig = null;
            _logger.LogInformation("Agent {AgentId} left cluster successfully", AgentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving cluster for agent {AgentId}", AgentId);
            SetStatus(ClusterMembershipStatus.Removed);
            throw;
        }
    }

    public virtual Task SendMessageAsync(string targetAgentId, object message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetAgentId);
        ArgumentNullException.ThrowIfNull(message);

        if (_status != ClusterMembershipStatus.Active)
        {
            throw new InvalidOperationException("Agent is not connected to cluster");
        }

        _logger.LogInformation("Agent {AgentId} sending message to {TargetAgent}: {Message}", 
            AgentId, targetAgentId, message);

        // In a real implementation, this would use the cluster messaging system
        return Task.CompletedTask;
    }

    public virtual Task BroadcastToRoleAsync(string role, object message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        ArgumentNullException.ThrowIfNull(message);

        if (_status != ClusterMembershipStatus.Active)
        {
            throw new InvalidOperationException("Agent is not connected to cluster");
        }

        _logger.LogInformation("Agent {AgentId} broadcasting to role {Role}: {Message}", 
            AgentId, role, message);

        return Task.CompletedTask;
    }

    public virtual Task<IEnumerable<ClusterMemberInfo>> GetClusterMembersAsync(CancellationToken cancellationToken = default)
    {
        if (_status != ClusterMembershipStatus.Active)
        {
            return Task.FromResult(Enumerable.Empty<ClusterMemberInfo>());
        }

        // Return self as a cluster member for testing
        var selfMember = new ClusterMemberInfo(
            AgentId,
            ClusterRole,
            $"{_clusterConfig?.Hostname}:{_clusterConfig?.Port}",
            ClusterMembershipStatus.Active,
            DateTime.UtcNow);

        return Task.FromResult<IEnumerable<ClusterMemberInfo>>(new[] { selfMember });
    }

    private void SetStatus(ClusterMembershipStatus newStatus)
    {
        var previousStatus = _status;
        _status = newStatus;
        
        if (previousStatus != newStatus)
        {
            MembershipChanged?.Invoke(this, new ClusterMembershipChangedEventArgs(previousStatus, newStatus));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            LeaveClusterAsync().Wait(TimeSpan.FromSeconds(10));
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
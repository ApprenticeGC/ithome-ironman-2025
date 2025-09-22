using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.Logging;
using Akka.Actor;
using Akka.Cluster;
using System.Collections.Concurrent;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Main AI cluster manager responsible for cluster coordination and orchestration.
/// Manages cluster formation, node discovery, and high-level cluster operations.
/// </summary>
public class AIClusterManager : BaseClusteringService, IClusterMonitoringCapability
{
    private readonly ConcurrentDictionary<string, NodeInfo> _nodes = new();
    private readonly ConcurrentDictionary<string, string[]> _nodeCapabilities = new();
    private string _currentNodeId = string.Empty;

    public AIClusterManager(ILogger<AIClusterManager> logger) : base(logger)
    {
    }

    #region Events
    #pragma warning disable CS0067 // The event is never used - events are part of interface contract
    public event EventHandler<NodeJoinedEventArgs>? NodeJoined;
    public event EventHandler<NodeLeftEventArgs>? NodeLeft;
    #pragma warning restore CS0067
    #endregion

    #region IService Implementation

    public override Task<bool> JoinClusterAsync(string nodeId, string[] capabilities, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
            throw new ArgumentException("Node ID cannot be empty", nameof(nodeId));

        try
        {
            _logger.LogInformation("Joining cluster as node {NodeId} with capabilities: {Capabilities}", 
                nodeId, string.Join(", ", capabilities));

            _currentNodeId = nodeId;
            _nodeCapabilities[nodeId] = capabilities;

            var nodeInfo = new NodeInfo
            {
                NodeId = nodeId,
                Capabilities = capabilities,
                JoinedAt = DateTime.UtcNow,
                Status = NodeStatus.Healthy
            };
            _nodes[nodeId] = nodeInfo;

            _logger.LogInformation("Successfully joined cluster as node {NodeId}", nodeId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join cluster as node {NodeId}", nodeId);
            return Task.FromResult(false);
        }
    }

    public override Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Leaving cluster for node {NodeId}", _currentNodeId);

            _nodes.TryRemove(_currentNodeId, out _);
            _nodeCapabilities.TryRemove(_currentNodeId, out _);

            _logger.LogInformation("Successfully left cluster for node {NodeId}", _currentNodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving cluster for node {NodeId}", _currentNodeId);
        }
        
        return Task.CompletedTask;
    }

    public override Task<string> RouteMessageAsync(string message, string[] requiredCapabilities, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message cannot be empty", nameof(message));

        try
        {
            _logger.LogDebug("Routing message with required capabilities: {Capabilities}", 
                string.Join(", ", requiredCapabilities));

            var eligibleNodes = FindNodesWithCapabilities(requiredCapabilities);
            
            if (eligibleNodes.Length == 0)
            {
                _logger.LogWarning("No nodes found with required capabilities: {Capabilities}", 
                    string.Join(", ", requiredCapabilities));
                return Task.FromResult("No eligible nodes found");
            }

            var targetNode = SelectNodeRoundRobin(eligibleNodes);
            
            _logger.LogDebug("Routing message to node {NodeId}", targetNode);
            
            return Task.FromResult($"Message processed by node {targetNode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing message");
            throw;
        }
    }

    public override Task<ClusterStatus> GetClusterStatusAsync(CancellationToken cancellationToken = default)
    {
        var allCapabilities = _nodeCapabilities.Values
            .SelectMany(caps => caps)
            .Distinct()
            .ToArray();

        var status = new ClusterStatus
        {
            LeaderNodeId = _currentNodeId,
            ActiveNodes = _nodes.Count,
            AvailableCapabilities = allCapabilities,
            LastUpdated = DateTime.UtcNow,
            State = ClusterState.Up
        };
        
        return Task.FromResult(status);
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new Type[] { typeof(IClusterMonitoringCapability) });
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(IClusterMonitoringCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(IClusterMonitoringCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    #endregion

    #region IClusterMonitoringCapability Implementation

    public Task<NodeHealthMetrics[]> GetNodeHealthMetricsAsync(CancellationToken cancellationToken = default)
    {
        var metrics = _nodes.Values.Select(node => new NodeHealthMetrics
        {
            NodeId = node.NodeId,
            CpuUsage = Random.Shared.NextDouble() * 100,
            MemoryUsage = Random.Shared.NextDouble() * 100,
            ActiveMessages = Random.Shared.Next(0, 100),
            ResponseTime = TimeSpan.FromMilliseconds(Random.Shared.Next(10, 1000)),
            Status = node.Status,
            LastHeartbeat = DateTime.UtcNow - TimeSpan.FromSeconds(Random.Shared.Next(1, 30))
        }).ToArray();

        return Task.FromResult(metrics);
    }

    public Task<LoadBalanceStats> GetLoadBalanceStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new LoadBalanceStats
        {
            TotalMessages = _nodes.Count * 10,
            LoadDistributionEfficiency = 0.85
        };

        foreach (var node in _nodes.Keys)
        {
            stats.MessagesPerNode[node] = Random.Shared.Next(5, 15);
            stats.AverageResponseTime[node] = TimeSpan.FromMilliseconds(Random.Shared.Next(100, 500));
        }

        return Task.FromResult(stats);
    }

    #endregion

    #region Private Methods

    private string[] FindNodesWithCapabilities(string[] requiredCapabilities)
    {
        return _nodeCapabilities
            .Where(kvp => requiredCapabilities.All(req => kvp.Value.Contains(req)))
            .Select(kvp => kvp.Key)
            .ToArray();
    }

    private static int _roundRobinIndex = 0;
    private string SelectNodeRoundRobin(string[] eligibleNodes)
    {
        var index = Interlocked.Increment(ref _roundRobinIndex) % eligibleNodes.Length;
        return eligibleNodes[index];
    }

    #endregion

    #region Helper Classes

    private class NodeInfo
    {
        public string NodeId { get; set; } = string.Empty;
        public string[] Capabilities { get; set; } = Array.Empty<string>();
        public DateTime JoinedAt { get; set; }
        public NodeStatus Status { get; set; }
    }

    #endregion
}
using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.Logging;
using Akka.Actor;
using System.Collections.Concurrent;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Health tracking and monitoring service for AI agent clusters.
/// Provides cluster health metrics, failure detection, and monitoring capabilities.
/// </summary>
public class AIClusterMonitor : BaseClusteringService, IClusterMonitoringCapability
{
    private readonly ConcurrentDictionary<string, ClusterNodeHealth> _nodeHealth = new();
    private readonly ConcurrentDictionary<string, List<HealthEvent>> _healthHistory = new();
    private Timer? _healthCheckTimer;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _nodeTimeout = TimeSpan.FromMinutes(5);

    public AIClusterMonitor(ILogger<AIClusterMonitor> logger) : base(logger)
    {
    }

    #region Events
    public event EventHandler<NodeJoinedEventArgs>? NodeJoined;
    public event EventHandler<NodeLeftEventArgs>? NodeLeft;
    public event EventHandler<NodeHealthChangedEventArgs>? NodeHealthChanged;
    #endregion

    #region IService Implementation

    public override Task<bool> JoinClusterAsync(string nodeId, string[] capabilities, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Initializing AI Cluster Monitor for node {NodeId}", nodeId);

            // Start monitoring timers
            StartMonitoring();

            // Register this node
            RegisterNode(nodeId, capabilities);

            _logger.LogInformation("AI Cluster Monitor initialized for node {NodeId}", nodeId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize AI Cluster Monitor for node {NodeId}", nodeId);
            return Task.FromResult(false);
        }
    }

    public override Task LeaveClusterAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Shutting down AI Cluster Monitor");

            // Stop monitoring
            StopMonitoring();

            _logger.LogInformation("AI Cluster Monitor shutdown completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shutting down AI Cluster Monitor");
        }
        
        return Task.CompletedTask;
    }

    public override Task<string> RouteMessageAsync(string message, string[] requiredCapabilities, CancellationToken cancellationToken = default)
    {
        // Monitor doesn't route messages directly, but provides health information for routing decisions
        var healthyNodes = GetHealthyNodesWithCapabilities(requiredCapabilities);
        return Task.FromResult($"Healthy nodes available: {string.Join(", ", healthyNodes)}");
    }

    public override Task<ClusterStatus> GetClusterStatusAsync(CancellationToken cancellationToken = default)
    {
        var healthyNodes = _nodeHealth.Values.Count(n => n.Status == NodeStatus.Healthy);
        var allCapabilities = _nodeHealth.Values
            .Where(n => n.Status != NodeStatus.Unreachable)
            .SelectMany(n => n.Capabilities)
            .Distinct()
            .ToArray();

        var status = new ClusterStatus
        {
            LeaderNodeId = FindHealthiestNode()?.NodeId ?? "Unknown",
            ActiveNodes = healthyNodes,
            AvailableCapabilities = allCapabilities,
            LastUpdated = DateTime.UtcNow,
            State = healthyNodes > 0 ? ClusterState.Up : ClusterState.Down
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
        var metrics = _nodeHealth.Values.Select(health => new NodeHealthMetrics
        {
            NodeId = health.NodeId,
            CpuUsage = health.CpuUsage,
            MemoryUsage = health.MemoryUsage,
            ActiveMessages = health.ActiveMessages,
            ResponseTime = health.ResponseTime,
            Status = health.Status,
            LastHeartbeat = health.LastHeartbeat
        }).ToArray();
        
        return Task.FromResult(metrics);
    }

    public Task<LoadBalanceStats> GetLoadBalanceStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new LoadBalanceStats();
        var totalMessages = 0;

        foreach (var health in _nodeHealth.Values)
        {
            var messageCount = health.MessageCount;
            stats.MessagesPerNode[health.NodeId] = messageCount;
            stats.AverageResponseTime[health.NodeId] = health.ResponseTime;
            totalMessages += messageCount;
        }

        stats.TotalMessages = totalMessages;
        stats.LoadDistributionEfficiency = CalculateLoadDistributionEfficiency();

        return Task.FromResult(stats);
    }

    #endregion

    #region Public Monitoring Methods

    /// <summary>
    /// Registers a node for monitoring.
    /// </summary>
    public void RegisterNode(string nodeId, string[] capabilities)
    {
        _logger.LogInformation("Registering node {NodeId} for monitoring with capabilities: {Capabilities}", 
            nodeId, string.Join(", ", capabilities));

        var nodeHealth = new ClusterNodeHealth
        {
            NodeId = nodeId,
            Capabilities = capabilities,
            Status = NodeStatus.Healthy,
            LastHeartbeat = DateTime.UtcNow,
            FirstSeen = DateTime.UtcNow
        };

        _nodeHealth[nodeId] = nodeHealth;
        _healthHistory[nodeId] = new List<HealthEvent>();

        NodeJoined?.Invoke(this, new NodeJoinedEventArgs
        {
            NodeId = nodeId,
            Capabilities = capabilities,
            JoinedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Unregisters a node from monitoring.
    /// </summary>
    public void UnregisterNode(string nodeId, string reason = "Graceful shutdown")
    {
        if (_nodeHealth.TryRemove(nodeId, out var health))
        {
            _logger.LogInformation("Unregistered node {NodeId} from monitoring. Reason: {Reason}", nodeId, reason);

            NodeLeft?.Invoke(this, new NodeLeftEventArgs
            {
                NodeId = nodeId,
                Reason = reason,
                LeftAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Updates health metrics for a specific node.
    /// </summary>
    public void UpdateNodeHealth(string nodeId, double cpuUsage, double memoryUsage, int activeMessages)
    {
        if (_nodeHealth.TryGetValue(nodeId, out var health))
        {
            var oldStatus = health.Status;
            
            health.CpuUsage = cpuUsage;
            health.MemoryUsage = memoryUsage;
            health.ActiveMessages = activeMessages;
            health.LastHeartbeat = DateTime.UtcNow;
            health.MessageCount++;

            // Determine new status based on metrics
            var newStatus = DetermineNodeStatus(cpuUsage, memoryUsage, activeMessages);
            if (newStatus != oldStatus)
            {
                health.Status = newStatus;
                RecordHealthEvent(nodeId, $"Status changed from {oldStatus} to {newStatus}");
                
                NodeHealthChanged?.Invoke(this, new NodeHealthChangedEventArgs
                {
                    NodeId = nodeId,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangedAt = DateTime.UtcNow
                });
            }
        }
    }

    /// <summary>
    /// Gets health history for a specific node.
    /// </summary>
    public List<HealthEvent> GetNodeHealthHistory(string nodeId)
    {
        return _healthHistory.GetValueOrDefault(nodeId, new List<HealthEvent>());
    }

    /// <summary>
    /// Gets nodes that are healthy and have required capabilities.
    /// </summary>
    public string[] GetHealthyNodesWithCapabilities(string[] requiredCapabilities)
    {
        return _nodeHealth.Values
            .Where(n => n.Status == NodeStatus.Healthy && 
                       requiredCapabilities.All(req => n.Capabilities.Contains(req)))
            .Select(n => n.NodeId)
            .ToArray();
    }

    /// <summary>
    /// Gets cluster-wide health summary.
    /// </summary>
    public ClusterHealthSummary GetClusterHealthSummary()
    {
        var nodes = _nodeHealth.Values.ToArray();
        var summary = new ClusterHealthSummary
        {
            TotalNodes = nodes.Length,
            HealthyNodes = nodes.Count(n => n.Status == NodeStatus.Healthy),
            DegradedNodes = nodes.Count(n => n.Status == NodeStatus.Degraded),
            UnhealthyNodes = nodes.Count(n => n.Status == NodeStatus.Unhealthy),
            UnreachableNodes = nodes.Count(n => n.Status == NodeStatus.Unreachable),
            AverageCpuUsage = nodes.Length > 0 ? nodes.Average(n => n.CpuUsage) : 0,
            AverageMemoryUsage = nodes.Length > 0 ? nodes.Average(n => n.MemoryUsage) : 0,
            TotalActiveMessages = nodes.Sum(n => n.ActiveMessages),
            LastUpdated = DateTime.UtcNow
        };

        return summary;
    }

    #endregion

    #region Private Methods

    private void StartMonitoring()
    {
        _healthCheckTimer = new Timer(PerformHealthCheck, null, TimeSpan.Zero, _healthCheckInterval);
    }

    private void StopMonitoring()
    {
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;
    }

    private void PerformHealthCheck(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            foreach (var (nodeId, health) in _nodeHealth)
            {
                // Check if node is still responsive
                var timeSinceLastHeartbeat = now - health.LastHeartbeat;
                if (timeSinceLastHeartbeat > _nodeTimeout)
                {
                    if (health.Status != NodeStatus.Unreachable)
                    {
                        _logger.LogWarning("Node {NodeId} appears unreachable. Last heartbeat: {LastHeartbeat}", 
                            nodeId, health.LastHeartbeat);
                        health.Status = NodeStatus.Unreachable;
                        RecordHealthEvent(nodeId, "Node marked as unreachable due to timeout");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");
        }
    }

    private NodeStatus DetermineNodeStatus(double cpuUsage, double memoryUsage, int activeMessages)
    {
        // Determine status based on resource usage thresholds
        if (cpuUsage > 90 || memoryUsage > 90)
            return NodeStatus.Unhealthy;
        
        if (cpuUsage > 75 || memoryUsage > 75 || activeMessages > 1000)
            return NodeStatus.Degraded;
            
        return NodeStatus.Healthy;
    }

    private void RecordHealthEvent(string nodeId, string eventDescription)
    {
        if (_healthHistory.TryGetValue(nodeId, out var history))
        {
            history.Add(new HealthEvent
            {
                NodeId = nodeId,
                EventType = "StatusChange",
                Description = eventDescription,
                Timestamp = DateTime.UtcNow
            });

            // Keep only the last 100 events
            if (history.Count > 100)
            {
                history.RemoveAt(0);
            }
        }
    }

    private ClusterNodeHealth? FindHealthiestNode()
    {
        return _nodeHealth.Values
            .Where(n => n.Status == NodeStatus.Healthy)
            .OrderBy(n => n.CpuUsage + n.MemoryUsage)
            .FirstOrDefault();
    }

    private double CalculateLoadDistributionEfficiency()
    {
        var messageCounts = _nodeHealth.Values.Select(n => (double)n.MessageCount).ToArray();
        if (messageCounts.Length == 0) return 1.0;

        var average = messageCounts.Average();
        var variance = messageCounts.Sum(x => Math.Pow(x - average, 2)) / messageCounts.Length;
        var standardDeviation = Math.Sqrt(variance);
        
        // Efficiency is inversely related to standard deviation (lower is better)
        return Math.Max(0.0, 1.0 - (standardDeviation / (average + 1)));
    }

    #endregion

    #region Helper Classes and Records

    public class ClusterNodeHealth
    {
        public string NodeId { get; set; } = string.Empty;
        public string[] Capabilities { get; set; } = Array.Empty<string>();
        public NodeStatus Status { get; set; } = NodeStatus.Healthy;
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public int ActiveMessages { get; set; }
        public int MessageCount { get; set; }
        public TimeSpan ResponseTime { get; set; } = TimeSpan.FromMilliseconds(100);
        public DateTime LastHeartbeat { get; set; }
        public DateTime FirstSeen { get; set; }
    }

    public class HealthEvent
    {
        public string NodeId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ClusterHealthSummary
    {
        public int TotalNodes { get; set; }
        public int HealthyNodes { get; set; }
        public int DegradedNodes { get; set; }
        public int UnhealthyNodes { get; set; }
        public int UnreachableNodes { get; set; }
        public double AverageCpuUsage { get; set; }
        public double AverageMemoryUsage { get; set; }
        public int TotalActiveMessages { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class NodeHealthChangedEventArgs : EventArgs
    {
        public string NodeId { get; set; } = string.Empty;
        public NodeStatus OldStatus { get; set; }
        public NodeStatus NewStatus { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    #endregion
}
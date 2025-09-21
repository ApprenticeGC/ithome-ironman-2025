using Microsoft.Extensions.Logging;
using GameConsole.AI.Clustering.Interfaces;
using GameConsole.AI.Clustering.Models;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Default implementation of IAIClusterMonitor for cluster health monitoring and scaling.
/// </summary>
public class AIClusterMonitor : IAIClusterMonitor
{
    private readonly ILogger<AIClusterMonitor> _logger;
    private readonly IAIClusterManager _clusterManager;
    private readonly IAINodeManager _nodeManager;
    private bool _isRunning;
    private MonitoringConfiguration _configuration = new();
    private Timer? _healthCheckTimer;
    private Timer? _workloadAnalysisTimer;
    private Timer? _partitionDetectionTimer;
    private readonly List<ClusterMetrics> _historicalMetrics = new();

    /// <summary>
    /// Initializes a new instance of the AIClusterMonitor.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="clusterManager">The cluster manager instance.</param>
    /// <param name="nodeManager">The node manager instance.</param>
    public AIClusterMonitor(ILogger<AIClusterMonitor> logger, IAIClusterManager clusterManager, IAINodeManager nodeManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clusterManager = clusterManager ?? throw new ArgumentNullException(nameof(clusterManager));
        _nodeManager = nodeManager ?? throw new ArgumentNullException(nameof(nodeManager));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ClusterHealth>? ClusterHealthChanged;

    /// <inheritdoc />
    #pragma warning disable CS0067 // Event is declared but never used
    public event EventHandler<(string NodeId, NodeHealth Health)>? NodeHealthChanged;
    #pragma warning restore CS0067

    /// <inheritdoc />
    public event EventHandler<ScalingRecommendation>? ScalingRecommended;

    /// <inheritdoc />
    public event EventHandler<NetworkPartition>? NetworkPartitionDetected;

    /// <inheritdoc />
    public event EventHandler<ClusterAlert>? CriticalAlertRaised;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AIClusterMonitor");
        
        // Basic initialization - will be enhanced when actor system foundation is available
        await Task.CompletedTask;
        
        _logger.LogInformation("AIClusterMonitor initialized");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AIClusterMonitor");
        
        _isRunning = true;
        
        _logger.LogInformation("AIClusterMonitor started");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AIClusterMonitor");
        
        _isRunning = false;
        
        await StopMonitoringAsync(cancellationToken);
        
        _logger.LogInformation("AIClusterMonitor stopped");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
        
        _healthCheckTimer?.Dispose();
        _workloadAnalysisTimer?.Dispose();
        _partitionDetectionTimer?.Dispose();
    }

    /// <inheritdoc />
    public async Task<ClusterHealth> GetClusterHealthAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting cluster health status");
        
        var nodes = await _clusterManager.GetClusterNodesAsync(cancellationToken);
        var totalNodes = nodes.Count;
        var healthyNodes = nodes.Count(n => n.Health == NodeHealth.Healthy);
        var degradedNodes = nodes.Count(n => n.Health == NodeHealth.Degraded);
        var unhealthyNodes = nodes.Count(n => n.Health == NodeHealth.Unhealthy);
        
        var clusterStatus = DetermineClusterStatus(totalNodes, healthyNodes, degradedNodes, unhealthyNodes);
        
        var metrics = new ClusterMetrics
        {
            ActiveAgents = nodes.Sum(n => n.Capabilities.Count),
            RequestThroughputPerSecond = 0.0, // Placeholder - will be enhanced with actual metrics
            AverageLatencyMs = 0.0,
            CpuUtilization = 0.0,
            MemoryUtilization = 0.0,
            NetworkUtilization = 0.0
        };
        
        var alerts = GenerateAlerts(clusterStatus, metrics);
        
        var health = new ClusterHealth
        {
            Status = clusterStatus,
            TotalNodes = totalNodes,
            HealthyNodes = healthyNodes,
            DegradedNodes = degradedNodes,
            UnhealthyNodes = unhealthyNodes,
            Metrics = metrics,
            Alerts = alerts
        };
        
        return health;
    }

    /// <inheritdoc />
    public async Task<ClusterHealthCheckResult> PerformClusterHealthCheckAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing comprehensive cluster health check");
        
        var overallHealth = await GetClusterHealthAsync(cancellationToken);
        var nodes = await _clusterManager.GetClusterNodesAsync(cancellationToken);
        var nodeResults = new Dictionary<string, NodeHealthCheckResult>();
        
        // Perform health check on each node
        foreach (var node in nodes)
        {
            // For now, create basic health check result - will be enhanced with actual node communication
            var nodeHealthResult = new NodeHealthCheckResult
            {
                Status = node.Health,
                Issues = node.Health == NodeHealth.Healthy ? Array.Empty<string>() : new[] { $"Node {node.NodeId} is {node.Health}" },
                Metrics = new Dictionary<string, double>
                {
                    ["cpu_utilization"] = 0.0,
                    ["memory_utilization"] = 0.0,
                    ["disk_utilization"] = 0.0,
                    ["network_utilization"] = 0.0
                }
            };
            
            nodeResults[node.NodeId] = nodeHealthResult;
        }
        
        var connectivityResults = await PerformConnectivityTests(nodes, cancellationToken);
        
        var result = new ClusterHealthCheckResult
        {
            OverallHealth = overallHealth,
            NodeResults = nodeResults,
            ConnectivityResults = connectivityResults,
            PerformanceMetrics = new Dictionary<string, double>
            {
                ["cluster_response_time"] = 0.0,
                ["cluster_throughput"] = 0.0
            }
        };
        
        _logger.LogInformation("Cluster health check completed");
        return result;
    }

    /// <inheritdoc />
    public async Task<NodeHealth?> GetNodeHealthAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        var nodes = await _clusterManager.GetClusterNodesAsync(cancellationToken);
        var node = nodes.FirstOrDefault(n => n.NodeId == nodeId);
        
        return node?.Health;
    }

    /// <inheritdoc />
    public async Task<ScalingRecommendation> GetScalingRecommendationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating scaling recommendation");
        
        var health = await GetClusterHealthAsync(cancellationToken);
        var nodes = await _clusterManager.GetClusterNodesAsync(cancellationToken);
        
        var action = DetermineScalingAction(health, nodes);
        var nodeCountChange = CalculateNodeCountChange(action, health, nodes);
        
        var recommendation = new ScalingRecommendation
        {
            Action = action,
            NodeCountChange = nodeCountChange,
            Reason = GenerateScalingReason(action, health, nodes),
            Confidence = CalculateScalingConfidence(action, health),
            Urgency = DetermineScalingUrgency(health),
            ExpectedImpact = CalculateExpectedImpact(action, nodeCountChange, health)
        };
        
        if (_configuration.EnableAutoScaling && recommendation.Action != ScalingAction.NoAction)
        {
            ScalingRecommended?.Invoke(this, recommendation);
        }
        
        return recommendation;
    }

    /// <inheritdoc />
    public async Task StartMonitoringAsync(MonitoringConfiguration monitoringConfiguration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting continuous monitoring");
        
        _configuration = monitoringConfiguration;
        
        // Start periodic health checks
        _healthCheckTimer = new Timer(
            PerformPeriodicHealthCheck, 
            null, 
            TimeSpan.Zero, 
            TimeSpan.FromMilliseconds(monitoringConfiguration.HealthCheckIntervalMs));
        
        // Start workload analysis
        _workloadAnalysisTimer = new Timer(
            PerformWorkloadAnalysis, 
            null, 
            TimeSpan.FromMilliseconds(monitoringConfiguration.WorkloadAnalysisIntervalMs), 
            TimeSpan.FromMilliseconds(monitoringConfiguration.WorkloadAnalysisIntervalMs));
        
        // Start partition detection
        _partitionDetectionTimer = new Timer(
            DetectPartitions, 
            null, 
            TimeSpan.FromMilliseconds(monitoringConfiguration.PartitionDetectionIntervalMs), 
            TimeSpan.FromMilliseconds(monitoringConfiguration.PartitionDetectionIntervalMs));
        
        _logger.LogInformation("Continuous monitoring started");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping continuous monitoring");
        
        _healthCheckTimer?.Dispose();
        _workloadAnalysisTimer?.Dispose();
        _partitionDetectionTimer?.Dispose();
        
        _healthCheckTimer = null;
        _workloadAnalysisTimer = null;
        _partitionDetectionTimer = null;
        
        _logger.LogInformation("Continuous monitoring stopped");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<NetworkPartition>> DetectNetworkPartitionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Detecting network partitions");
        
        return GetClusterNodesFromManagerAsync(cancellationToken);
    }
    
    private async Task<IReadOnlyList<NetworkPartition>> GetClusterNodesFromManagerAsync(CancellationToken cancellationToken)
    {
        var nodes = await _clusterManager.GetClusterNodesAsync(cancellationToken);
        var partitions = new List<NetworkPartition>();
        
        // Simple partition detection - will be enhanced with actual network connectivity tests
        var healthyNodes = nodes.Where(n => n.Health == NodeHealth.Healthy).ToList();
        var unhealthyNodes = nodes.Where(n => n.Health != NodeHealth.Healthy).ToList();
        
        if (unhealthyNodes.Count > 0)
        {
            var partition = new NetworkPartition
            {
                PartitionId = Guid.NewGuid().ToString(),
                NodeIds = unhealthyNodes.Select(n => n.NodeId).ToList(),
                IsMajorityPartition = false,
                Severity = unhealthyNodes.Count > nodes.Count / 2 ? PartitionSeverity.Critical : PartitionSeverity.Warning
            };
            
            partitions.Add(partition);
        }
        
        return partitions;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ClusterMetrics>> GetHistoricalMetricsAsync(DateTime fromTime, DateTime toTime, CancellationToken cancellationToken = default)
    {
        lock (_historicalMetrics)
        {
            var filteredMetrics = _historicalMetrics
                .Where(m => m.GetType().GetProperty("Timestamp")?.GetValue(m) is DateTime timestamp && 
                           timestamp >= fromTime && timestamp <= toTime)
                .ToList();
            
            return Task.FromResult<IReadOnlyList<ClusterMetrics>>(filteredMetrics);
        }
    }

    private ClusterStatus DetermineClusterStatus(int totalNodes, int healthyNodes, int degradedNodes, int unhealthyNodes)
    {
        if (totalNodes == 0) return ClusterStatus.Offline;
        
        var healthyPercentage = (double)healthyNodes / totalNodes;
        
        return healthyPercentage switch
        {
            >= 0.9 => ClusterStatus.Healthy,
            >= 0.7 => ClusterStatus.Degraded,
            > 0.0 => ClusterStatus.Critical,
            _ => ClusterStatus.Offline
        };
    }

    private List<ClusterAlert> GenerateAlerts(ClusterStatus status, ClusterMetrics metrics)
    {
        var alerts = new List<ClusterAlert>();
        
        if (status == ClusterStatus.Critical)
        {
            alerts.Add(new ClusterAlert
            {
                Severity = AlertSeverity.Critical,
                Message = "Cluster is in critical state",
                Source = nameof(AIClusterMonitor)
            });
        }
        
        if (metrics.CpuUtilization > _configuration.AlertThresholds.CpuUtilizationThreshold)
        {
            alerts.Add(new ClusterAlert
            {
                Severity = AlertSeverity.Warning,
                Message = $"CPU utilization is high: {metrics.CpuUtilization:F1}%",
                Source = nameof(AIClusterMonitor)
            });
        }
        
        return alerts;
    }

    private Task<List<ConnectivityTestResult>> PerformConnectivityTests(IReadOnlyList<ClusterNode> nodes, CancellationToken cancellationToken)
    {
        var results = new List<ConnectivityTestResult>();
        
        // Simple connectivity test - will be enhanced with actual network connectivity tests
        foreach (var node in nodes)
        {
            var result = new ConnectivityTestResult
            {
                SourceNodeId = _nodeManager.CurrentNode?.NodeId ?? "local",
                TargetNodeId = node.NodeId,
                Success = node.Health == NodeHealth.Healthy,
                LatencyMs = node.Health == NodeHealth.Healthy ? 5.0 : null,
                ErrorMessage = node.Health == NodeHealth.Healthy ? null : $"Node {node.NodeId} is {node.Health}"
            };
            
            results.Add(result);
        }
        
        return Task.FromResult(results);
    }

    private ScalingAction DetermineScalingAction(ClusterHealth health, IReadOnlyList<ClusterNode> nodes)
    {
        // Simple scaling logic - will be enhanced with more sophisticated analysis
        if (health.Status == ClusterStatus.Critical)
        {
            return ScalingAction.ScaleUp;
        }
        
        if (health.Status == ClusterStatus.Healthy && nodes.Count > _clusterManager.Configuration.MinimumNodes)
        {
            var avgUtilization = (health.Metrics.CpuUtilization + health.Metrics.MemoryUtilization) / 2;
            
            if (avgUtilization < 30.0)
            {
                return ScalingAction.ScaleDown;
            }
        }
        
        return ScalingAction.NoAction;
    }

    private int CalculateNodeCountChange(ScalingAction action, ClusterHealth health, IReadOnlyList<ClusterNode> nodes)
    {
        return action switch
        {
            ScalingAction.ScaleUp => Math.Min(3, _clusterManager.Configuration.MaximumNodes - nodes.Count),
            ScalingAction.ScaleDown => -1,
            _ => 0
        };
    }

    private string GenerateScalingReason(ScalingAction action, ClusterHealth health, IReadOnlyList<ClusterNode> nodes)
    {
        return action switch
        {
            ScalingAction.ScaleUp => $"Cluster status is {health.Status}, need more capacity",
            ScalingAction.ScaleDown => "Cluster has low utilization and can reduce capacity",
            ScalingAction.Rebalance => "Cluster workload is uneven across nodes",
            _ => "No scaling action needed"
        };
    }

    private double CalculateScalingConfidence(ScalingAction action, ClusterHealth health)
    {
        return action switch
        {
            ScalingAction.ScaleUp when health.Status == ClusterStatus.Critical => 0.9,
            ScalingAction.ScaleDown => 0.7,
            ScalingAction.NoAction => 0.8,
            _ => 0.6
        };
    }

    private ScalingUrgency DetermineScalingUrgency(ClusterHealth health)
    {
        return health.Status switch
        {
            ClusterStatus.Critical => ScalingUrgency.Critical,
            ClusterStatus.Degraded => ScalingUrgency.High,
            ClusterStatus.Healthy => ScalingUrgency.Normal,
            _ => ScalingUrgency.Low
        };
    }

    private ScalingImpact CalculateExpectedImpact(ScalingAction action, int nodeCountChange, ClusterHealth health)
    {
        return new ScalingImpact
        {
            ResponseTimeChangePercent = action == ScalingAction.ScaleUp ? -20.0 : 10.0,
            ThroughputChangePercent = action == ScalingAction.ScaleUp ? 30.0 : -15.0,
            ResourceUtilizationChangePercent = action == ScalingAction.ScaleUp ? -25.0 : 20.0,
            EstimatedCostChangePercent = nodeCountChange * 10.0
        };
    }

    private async void PerformPeriodicHealthCheck(object? state)
    {
        try
        {
            if (!_isRunning) return;
            
            var health = await GetClusterHealthAsync();
            ClusterHealthChanged?.Invoke(this, health);
            
            // Store historical metrics
            lock (_historicalMetrics)
            {
                _historicalMetrics.Add(health.Metrics);
                
                // Clean up old metrics
                var cutoff = DateTime.UtcNow.AddHours(-_configuration.MetricsRetentionHours);
                _historicalMetrics.RemoveAll(m => 
                    m.GetType().GetProperty("Timestamp")?.GetValue(m) is DateTime timestamp && 
                    timestamp < cutoff);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing periodic health check");
        }
    }

    private async void PerformWorkloadAnalysis(object? state)
    {
        try
        {
            if (!_isRunning || !_configuration.EnableAutoScaling) return;
            
            var recommendation = await GetScalingRecommendationAsync();
            
            if (recommendation.Action != ScalingAction.NoAction)
            {
                _logger.LogInformation("Generated scaling recommendation: {Action} ({NodeCountChange} nodes)", 
                    recommendation.Action, recommendation.NodeCountChange);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing workload analysis");
        }
    }

    private async void DetectPartitions(object? state)
    {
        try
        {
            if (!_isRunning) return;
            
            var partitions = await DetectNetworkPartitionsAsync();
            
            foreach (var partition in partitions)
            {
                NetworkPartitionDetected?.Invoke(this, partition);
                
                if (partition.Severity == PartitionSeverity.Critical)
                {
                    var alert = new ClusterAlert
                    {
                        Severity = AlertSeverity.Critical,
                        Message = $"Critical network partition detected affecting {partition.NodeIds.Count} nodes",
                        Source = nameof(AIClusterMonitor)
                    };
                    
                    CriticalAlertRaised?.Invoke(this, alert);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting network partitions");
        }
    }
}
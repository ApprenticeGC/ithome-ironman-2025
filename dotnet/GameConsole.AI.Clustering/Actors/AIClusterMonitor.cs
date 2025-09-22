using Akka.Actor;
using Akka.Event;
using GameConsole.AI.Clustering.Configuration;
using GameConsole.AI.Clustering.Messages;

namespace GameConsole.AI.Clustering.Actors;

/// <summary>
/// Monitors cluster health and triggers scaling decisions
/// </summary>
public class AIClusterMonitor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly AIClusterConfig _config;
    private readonly Dictionary<string, NodeHealthInfo> _nodeHealth = new();
    private ICancelable? _healthCheckSchedule;

    public AIClusterMonitor(AIClusterConfig config)
    {
        _config = config;

        Receive<ClusterMessages.NodeJoined>(HandleNodeJoined);
        Receive<ClusterMessages.NodeLeft>(HandleNodeLeft);
        Receive<ClusterMessages.HealthCheckResponse>(HandleHealthCheckResponse);
        Receive<PerformHealthCheck>(_ => PerformHealthChecks());
        Receive<ClusterMessages.GetClusterState>(HandleGetClusterState);
    }

    protected override void PreStart()
    {
        base.PreStart();
        
        // Schedule periodic health checks
        _healthCheckSchedule = Context.System.Scheduler.ScheduleTellRepeatedly(
            TimeSpan.FromSeconds(_config.HealthCheckIntervalSeconds),
            TimeSpan.FromSeconds(_config.HealthCheckIntervalSeconds),
            Self,
            new PerformHealthCheck(),
            Self
        );

        _log.Info("AIClusterMonitor started with health check interval: {0}s", 
                  _config.HealthCheckIntervalSeconds);
    }

    protected override void PostStop()
    {
        _healthCheckSchedule?.Cancel();
        base.PostStop();
    }

    private void HandleNodeJoined(ClusterMessages.NodeJoined message)
    {
        _log.Info("Monitoring new node: {0}", message.NodeId);
        
        _nodeHealth[message.NodeId] = new NodeHealthInfo(
            message.NodeId,
            message.NodeCapabilities,
            DateTime.UtcNow,
            true
        );
    }

    private void HandleNodeLeft(ClusterMessages.NodeLeft message)
    {
        _log.Info("Stopping monitoring for node: {0}", message.NodeId);
        _nodeHealth.Remove(message.NodeId);
    }

    private void HandleHealthCheckResponse(ClusterMessages.HealthCheckResponse message)
    {
        if (_nodeHealth.TryGetValue(message.NodeId, out var healthInfo))
        {
            var wasHealthy = healthInfo.IsHealthy;
            
            healthInfo.UpdateHealth(message.IsHealthy, message.LoadMetrics);

            // Log health status changes
            if (wasHealthy && !message.IsHealthy)
            {
                _log.Warning("Node {0} became unhealthy", message.NodeId);
            }
            else if (!wasHealthy && message.IsHealthy)
            {
                _log.Info("Node {0} recovered and is now healthy", message.NodeId);
            }

            // Check for scaling needs based on load
            CheckScalingNeeds(message.NodeId, message.LoadMetrics);
        }
    }

    private void HandleGetClusterState(ClusterMessages.GetClusterState message)
    {
        var healthyNodes = _nodeHealth.Values
            .Where(n => n.IsHealthy)
            .ToDictionary(n => n.NodeId, n => n.Capabilities);

        Sender.Tell(new ClusterMessages.ClusterState(healthyNodes, healthyNodes.Count));
    }

    private void PerformHealthChecks()
    {
        var currentTime = DateTime.UtcNow;
        
        foreach (var (nodeId, healthInfo) in _nodeHealth.ToList())
        {
            // Check if node has been silent for too long
            var timeSinceLastSeen = currentTime - healthInfo.LastSeen;
            var healthCheckTimeout = TimeSpan.FromSeconds(_config.HealthCheckIntervalSeconds * 3);

            if (timeSinceLastSeen > healthCheckTimeout && healthInfo.IsHealthy)
            {
                _log.Warning("Node {0} has been silent for {1}, marking as unhealthy", 
                             nodeId, timeSinceLastSeen);
                healthInfo.UpdateHealth(false, new Dictionary<string, double>());
            }

            // Send health check request
            _log.Debug("Sending health check to node: {0}", nodeId);
            // In a real implementation, we would send to the actual node
            // For now, we'll simulate responses in tests
        }

        // Check overall cluster health
        CheckClusterHealth();
    }

    private void CheckClusterHealth()
    {
        var totalNodes = _nodeHealth.Count;
        var healthyNodes = _nodeHealth.Values.Count(n => n.IsHealthy);
        var healthPercentage = totalNodes > 0 ? (double)healthyNodes / totalNodes * 100 : 0;

        _log.Debug("Cluster health: {0}/{1} nodes healthy ({2:F1}%)", 
                   healthyNodes, totalNodes, healthPercentage);

        // Check if cluster size is below minimum
        if (healthyNodes < _config.MinClusterSize)
        {
            _log.Warning("Cluster size ({0}) is below minimum ({1}). Scaling needed.", 
                         healthyNodes, _config.MinClusterSize);
            
            // Trigger scale-up
            Context.Parent.Tell(new ClusterMessages.ScaleCluster(_config.MinClusterSize));
        }

        // Check for too many nodes (scale down opportunity)
        if (healthyNodes > _config.MaxClusterSize)
        {
            _log.Info("Cluster size ({0}) exceeds maximum ({1}). Consider scaling down.", 
                      healthyNodes, _config.MaxClusterSize);
            
            // Trigger scale-down
            Context.Parent.Tell(new ClusterMessages.ScaleCluster(_config.MaxClusterSize));
        }

        // Check capability coverage
        CheckCapabilityCoverage();
    }

    private void CheckCapabilityCoverage()
    {
        var requiredCapabilities = new[]
        {
            NodeCapabilities.DialogueAgent,
            NodeCapabilities.CodeGenerationAgent,
            NodeCapabilities.AssetAnalysisAgent,
            NodeCapabilities.WorkflowOrchestration
        };

        foreach (var capability in requiredCapabilities)
        {
            var nodesWithCapability = _nodeHealth.Values
                .Where(n => n.IsHealthy && n.Capabilities.Contains(capability))
                .Count();

            if (nodesWithCapability == 0)
            {
                _log.Warning("No healthy nodes available for capability: {0}", capability);
                Context.Parent.Tell(new ClusterMessages.ScaleCluster(1, capability));
            }
            else if (nodesWithCapability == 1)
            {
                _log.Info("Only one node available for capability: {0}. Consider adding redundancy.", capability);
            }
        }
    }

    private void CheckScalingNeeds(string nodeId, Dictionary<string, double> loadMetrics)
    {
        // Check CPU usage
        if (loadMetrics.TryGetValue("cpu_usage", out var cpuUsage) && cpuUsage > 80.0)
        {
            _log.Warning("Node {0} has high CPU usage: {1:F1}%", nodeId, cpuUsage);
        }

        // Check memory usage
        if (loadMetrics.TryGetValue("memory_usage", out var memoryUsage) && memoryUsage > 85.0)
        {
            _log.Warning("Node {0} has high memory usage: {1:F1}%", nodeId, memoryUsage);
        }

        // Check request queue size
        if (loadMetrics.TryGetValue("request_queue_size", out var queueSize) && queueSize > 100)
        {
            _log.Warning("Node {0} has large request queue: {1}", nodeId, queueSize);
        }

        // Calculate overall node load score
        var loadScore = CalculateLoadScore(loadMetrics);
        if (loadScore > 0.9) // 90% load threshold
        {
            _log.Warning("Node {0} is under high load (score: {1:F2}). Scaling may be needed.", 
                         nodeId, loadScore);
        }
    }

    private double CalculateLoadScore(Dictionary<string, double> metrics)
    {
        var scores = new List<double>();

        if (metrics.TryGetValue("cpu_usage", out var cpu))
            scores.Add(cpu / 100.0);

        if (metrics.TryGetValue("memory_usage", out var memory))
            scores.Add(memory / 100.0);

        if (metrics.TryGetValue("request_queue_size", out var queue))
            scores.Add(Math.Min(queue / 200.0, 1.0)); // Normalize queue size

        return scores.Count > 0 ? scores.Average() : 0.0;
    }

    public static Props Props(AIClusterConfig config) => Akka.Actor.Props.Create<AIClusterMonitor>(config);

    private sealed class PerformHealthCheck
    {
    }
}

/// <summary>
/// Health information for a cluster node
/// </summary>
public class NodeHealthInfo
{
    public string NodeId { get; }
    public string[] Capabilities { get; }
    public DateTime LastSeen { get; private set; }
    public bool IsHealthy { get; private set; }
    public Dictionary<string, double> LoadMetrics { get; private set; } = new();

    public NodeHealthInfo(string nodeId, string[] capabilities, DateTime lastSeen, bool isHealthy)
    {
        NodeId = nodeId;
        Capabilities = capabilities;
        LastSeen = lastSeen;
        IsHealthy = isHealthy;
    }

    public void UpdateHealth(bool isHealthy, Dictionary<string, double> loadMetrics)
    {
        IsHealthy = isHealthy;
        LoadMetrics = loadMetrics ?? new Dictionary<string, double>();
        LastSeen = DateTime.UtcNow;
    }
}
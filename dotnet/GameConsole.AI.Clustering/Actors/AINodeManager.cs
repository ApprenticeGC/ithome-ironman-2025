using System.Diagnostics;
using Akka.Actor;
using Akka.Event;
using GameConsole.AI.Clustering.Configuration;
using GameConsole.AI.Clustering.Messages;

namespace GameConsole.AI.Clustering.Actors;

/// <summary>
/// Manages individual node operations and health reporting
/// </summary>
public class AINodeManager : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly AIClusterConfig _config;
    private readonly string _nodeId;
    private readonly string[] _capabilities;
    private ICancelable? _healthReportSchedule;
    private readonly Dictionary<string, double> _loadMetrics = new();

    public AINodeManager(AIClusterConfig config, string nodeId)
    {
        _config = config;
        _nodeId = nodeId;
        _capabilities = config.NodeCapabilities.ToArray();

        Receive<ClusterMessages.HealthCheck>(HandleHealthCheck);
        Receive<ReportHealth>(_ => ReportHealth());
        Receive<UpdateLoadMetrics>(HandleUpdateLoadMetrics);
        Receive<JoinClusterRequest>(_ => JoinCluster());
        Receive<LeaveClusterRequest>(HandleLeaveCluster);
    }

    protected override void PreStart()
    {
        base.PreStart();
        
        // Schedule periodic health reporting
        _healthReportSchedule = Context.System.Scheduler.ScheduleTellRepeatedly(
            TimeSpan.FromSeconds(5), // Initial delay
            TimeSpan.FromSeconds(_config.HealthCheckIntervalSeconds),
            Self,
            new ReportHealth(),
            Self
        );

        // Join the cluster
        Self.Tell(new JoinClusterRequest());

        _log.Info("AINodeManager started for node {0} with capabilities: {1}", 
                  _nodeId, string.Join(", ", _capabilities));
    }

    protected override void PostStop()
    {
        _healthReportSchedule?.Cancel();
        
        // Leave cluster gracefully
        if (Context.Parent != null)
        {
            Context.Parent.Tell(new ClusterMessages.LeaveCluster(_nodeId, "Node shutting down"));
        }
        
        base.PostStop();
    }

    private void HandleHealthCheck(ClusterMessages.HealthCheck message)
    {
        _log.Debug("Received health check for node: {0}", message.NodeId);
        
        if (message.NodeId == _nodeId)
        {
            var isHealthy = CheckNodeHealth();
            UpdateLoadMetrics();
            
            var response = new ClusterMessages.HealthCheckResponse(_nodeId, isHealthy, _loadMetrics);
            Sender.Tell(response);
        }
    }

    private void ReportHealth()
    {
        var isHealthy = CheckNodeHealth();
        UpdateLoadMetrics();
        
        var healthReport = new ClusterMessages.HealthCheckResponse(_nodeId, isHealthy, _loadMetrics);
        
        // Send to parent (cluster manager)
        Context.Parent.Tell(healthReport);
        
        _log.Debug("Reported health for node {0}: Healthy={1}, Load={2}", 
                   _nodeId, isHealthy, _loadMetrics.Count);
    }

    private void HandleUpdateLoadMetrics(UpdateLoadMetrics message)
    {
        foreach (var (key, value) in message.Metrics)
        {
            _loadMetrics[key] = value;
        }
        
        _log.Debug("Updated load metrics for node {0}: {1} metrics", _nodeId, message.Metrics.Count);
    }

    private void JoinCluster()
    {
        var address = Context.System.Settings.Config.GetString("akka.remote.dot-netty.tcp.hostname", "localhost") +
                      ":" + Context.System.Settings.Config.GetInt("akka.remote.dot-netty.tcp.port", _config.ClusterPort);
        
        var joinMessage = new ClusterMessages.JoinCluster(_nodeId, _capabilities, address);
        Context.Parent.Tell(joinMessage);
        
        _log.Info("Requested to join cluster with node ID: {0}", _nodeId);
    }

    private void HandleLeaveCluster(LeaveClusterRequest message)
    {
        var leaveMessage = new ClusterMessages.LeaveCluster(_nodeId, message.Reason);
        Context.Parent.Tell(leaveMessage);
        
        _log.Info("Requested to leave cluster: {0}", message.Reason);
    }

    private bool CheckNodeHealth()
    {
        try
        {
            // Check basic system health indicators
            var process = Process.GetCurrentProcess();
            var memoryUsageMB = process.WorkingSet64 / (1024 * 1024);
            
            // Check if we have enough free memory (simplified check)
            var isHealthy = memoryUsageMB < 1024; // Less than 1GB
            
            // Check if actor system is responsive
            isHealthy = isHealthy && Context.System.WhenTerminated.IsCompleted == false;
            
            return isHealthy;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error checking node health");
            return false;
        }
    }

    private void UpdateLoadMetrics()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
            // Memory usage
            var memoryUsageBytes = process.WorkingSet64;
            var memoryUsageMB = memoryUsageBytes / (1024.0 * 1024.0);
            _loadMetrics["memory_usage_mb"] = memoryUsageMB;
            _loadMetrics["memory_usage"] = Math.Min((memoryUsageMB / 1024.0) * 100, 100); // Percentage assuming 1GB max
            
            // CPU time (approximation)
            var cpuTime = process.TotalProcessorTime.TotalMilliseconds;
            _loadMetrics["cpu_time_ms"] = cpuTime;
            
            // Thread count
            _loadMetrics["thread_count"] = process.Threads.Count;
            
            // Handle count
            _loadMetrics["handle_count"] = process.HandleCount;
            
            // Request queue size (simulated for now)
            if (!_loadMetrics.ContainsKey("request_queue_size"))
                _loadMetrics["request_queue_size"] = 0;
            
            // Simulate some CPU usage based on thread activity
            var baseCpuUsage = Math.Min((process.Threads.Count / 100.0) * 100, 90);
            _loadMetrics["cpu_usage"] = baseCpuUsage + new Random().NextDouble() * 10;
            
            _log.Debug("Updated load metrics: Memory={0:F1}MB, Threads={1}, CPU={2:F1}%", 
                       memoryUsageMB, process.Threads.Count, _loadMetrics["cpu_usage"]);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error updating load metrics");
        }
    }

    public static Props Props(AIClusterConfig config, string nodeId) => 
        Akka.Actor.Props.Create<AINodeManager>(config, nodeId);

    private sealed class ReportHealth
    {
    }

    private sealed class JoinClusterRequest
    {
    }

    private sealed record LeaveClusterRequest(string Reason);
    
    private sealed record UpdateLoadMetrics(Dictionary<string, double> Metrics);
}
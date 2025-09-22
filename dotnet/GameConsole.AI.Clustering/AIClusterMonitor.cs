using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Clustering;

/// <summary>
/// Implementation of AI cluster monitor for health tracking and performance monitoring.
/// </summary>
public class AIClusterMonitor : IAIClusterMonitor
{
    private readonly ILogger<AIClusterMonitor> _logger;
    private ClusterHealthStatus _healthStatus = ClusterHealthStatus.Healthy;
    private readonly Dictionary<string, NodeMonitoringData> _monitoredNodes = new();
    private readonly Timer? _monitoringTimer;
    private ClusterMetrics _lastMetrics = new();
    private bool _isRunning = false;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIClusterMonitor"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public AIClusterMonitor(ILogger<AIClusterMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _monitoringTimer = new Timer(PerformMonitoringCycle, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <inheritdoc />
    public ClusterHealthStatus HealthStatus => _healthStatus;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<ClusterHealthChangedEventArgs>? HealthChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI Cluster Monitor");
        
        // Initialize monitoring infrastructure
        await Task.Delay(50, cancellationToken);
        
        _logger.LogInformation("AI Cluster Monitor initialized");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI Cluster Monitor");
        
        // Start periodic monitoring
        _monitoringTimer?.Change(TimeSpan.Zero, TimeSpan.FromSeconds(30));
        
        _healthStatus = ClusterHealthStatus.Healthy;
        _isRunning = true;
        
        await Task.Delay(25, cancellationToken);
        
        _logger.LogInformation("AI Cluster Monitor started");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AI Cluster Monitor");
        
        _isRunning = false;
        
        // Stop monitoring timer
        _monitoringTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        
        _healthStatus = ClusterHealthStatus.Unavailable;
        
        await Task.Delay(25, cancellationToken);
        
        _logger.LogInformation("AI Cluster Monitor stopped");
    }

    /// <inheritdoc />
    public async Task<ClusterMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Retrieving cluster performance metrics");
        
        await Task.Delay(10, cancellationToken);
        
        // Calculate current metrics from monitored nodes
        var metrics = CalculateClusterMetrics();
        _lastMetrics = metrics;
        
        _logger.LogDebug("Retrieved cluster metrics: {TotalNodes} nodes, {HealthyNodes} healthy, {AverageLoad:P2} avg load", 
            metrics.TotalNodes, metrics.HealthyNodes, metrics.AverageCpuUtilization / 100.0);
        
        return metrics;
    }

    /// <inheritdoc />
    public async Task StartNodeMonitoringAsync(string nodeAddress, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting monitoring for node {NodeAddress}", nodeAddress);
        
        await Task.Delay(10, cancellationToken);
        
        _monitoredNodes[nodeAddress] = new NodeMonitoringData
        {
            NodeAddress = nodeAddress,
            Health = NodeHealth.Healthy,
            CpuUsage = 0.0,
            MemoryUsage = 0.0,
            ActiveTasks = 0,
            ResponseTime = 100.0,
            LastUpdated = DateTime.UtcNow,
            IsMonitoring = true
        };
        
        _logger.LogInformation("Started monitoring node {NodeAddress}", nodeAddress);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            await StopAsync();
            _monitoringTimer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal");
        }
        finally
        {
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Updates monitoring data for a specific node.
    /// </summary>
    /// <param name="nodeAddress">Address of the node.</param>
    /// <param name="health">Current health status.</param>
    /// <param name="cpuUsage">CPU usage percentage.</param>
    /// <param name="memoryUsage">Memory usage percentage.</param>
    /// <param name="activeTasks">Number of active tasks.</param>
    /// <param name="responseTime">Average response time in milliseconds.</param>
    /// <returns>Task representing the update operation.</returns>
    public async Task UpdateNodeDataAsync(string nodeAddress, NodeHealth health, double cpuUsage, 
        double memoryUsage, int activeTasks, double responseTime)
    {
        if (_monitoredNodes.ContainsKey(nodeAddress))
        {
            var nodeData = _monitoredNodes[nodeAddress];
            var oldHealth = nodeData.Health;
            
            nodeData.Health = health;
            nodeData.CpuUsage = cpuUsage;
            nodeData.MemoryUsage = memoryUsage;
            nodeData.ActiveTasks = activeTasks;
            nodeData.ResponseTime = responseTime;
            nodeData.LastUpdated = DateTime.UtcNow;
            
            if (oldHealth != health)
            {
                _logger.LogInformation("Node {NodeAddress} health changed from {OldHealth} to {NewHealth}", 
                    nodeAddress, oldHealth, health);
                
                // Recalculate cluster health
                await UpdateClusterHealthAsync();
            }
            
            _logger.LogDebug("Updated node {NodeAddress} data: Health={Health}, CPU={CpuUsage:F1}%, Memory={MemoryUsage:F1}%, Tasks={ActiveTasks}", 
                nodeAddress, health, cpuUsage, memoryUsage, activeTasks);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Performs a monitoring cycle to update cluster health and metrics.
    /// </summary>
    /// <param name="state">Timer state (not used).</param>
    private async void PerformMonitoringCycle(object? state)
    {
        try
        {
            _logger.LogDebug("Performing monitoring cycle");
            
            // Simulate monitoring updates for demonstration
            await SimulateNodeUpdates();
            
            // Update cluster health based on node statuses
            await UpdateClusterHealthAsync();
            
            // Update metrics
            var metrics = CalculateClusterMetrics();
            _lastMetrics = metrics;
            
            _logger.LogDebug("Monitoring cycle completed. Cluster health: {HealthStatus}, Total nodes: {TotalNodes}", 
                _healthStatus, metrics.TotalNodes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during monitoring cycle");
        }
    }

    /// <summary>
    /// Simulates node monitoring updates for demonstration purposes.
    /// </summary>
    /// <returns>Task representing the simulation operation.</returns>
    private async Task SimulateNodeUpdates()
    {
        var random = Random.Shared;
        
        foreach (var kvp in _monitoredNodes.ToList())
        {
            var nodeData = kvp.Value;
            if (!nodeData.IsMonitoring)
                continue;
            
            // Simulate varying metrics
            var cpuUsage = Math.Max(0, Math.Min(100, nodeData.CpuUsage + random.Next(-10, 11)));
            var memoryUsage = Math.Max(0, Math.Min(100, nodeData.MemoryUsage + random.Next(-5, 6)));
            var activeTasks = Math.Max(0, nodeData.ActiveTasks + random.Next(-2, 3));
            var responseTime = Math.Max(50, nodeData.ResponseTime + random.Next(-20, 21));
            
            // Determine health based on simulated metrics
            var health = DetermineNodeHealth(cpuUsage, memoryUsage, activeTasks, responseTime);
            
            await UpdateNodeDataAsync(kvp.Key, health, cpuUsage, memoryUsage, activeTasks, responseTime);
        }
    }

    /// <summary>
    /// Updates the overall cluster health status based on individual node health.
    /// </summary>
    /// <returns>Task representing the update operation.</returns>
    private async Task UpdateClusterHealthAsync()
    {
        var oldHealthStatus = _healthStatus;
        
        if (_monitoredNodes.Count == 0)
        {
            _healthStatus = ClusterHealthStatus.Unavailable;
        }
        else
        {
            var healthyNodes = _monitoredNodes.Values.Count(n => n.Health == NodeHealth.Healthy);
            var warningNodes = _monitoredNodes.Values.Count(n => n.Health == NodeHealth.Warning);
            var criticalNodes = _monitoredNodes.Values.Count(n => n.Health == NodeHealth.Critical);
            var unavailableNodes = _monitoredNodes.Values.Count(n => n.Health == NodeHealth.Unavailable);
            
            var totalNodes = _monitoredNodes.Count;
            var healthyRatio = (double)healthyNodes / totalNodes;
            
            if (unavailableNodes > totalNodes / 2)
            {
                _healthStatus = ClusterHealthStatus.Unavailable;
            }
            else if (criticalNodes > 0 || healthyRatio < 0.5)
            {
                _healthStatus = ClusterHealthStatus.Critical;
            }
            else if (warningNodes > totalNodes / 3)
            {
                _healthStatus = ClusterHealthStatus.Degraded;
            }
            else
            {
                _healthStatus = ClusterHealthStatus.Healthy;
            }
        }
        
        if (oldHealthStatus != _healthStatus)
        {
            _logger.LogInformation("Cluster health changed from {OldHealthStatus} to {NewHealthStatus}", 
                oldHealthStatus, _healthStatus);
            
            OnHealthChanged(oldHealthStatus, _healthStatus);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Calculates current cluster metrics from monitored nodes.
    /// </summary>
    /// <returns>Current cluster metrics.</returns>
    private ClusterMetrics CalculateClusterMetrics()
    {
        var totalNodes = _monitoredNodes.Count;
        
        if (totalNodes == 0)
        {
            return new ClusterMetrics
            {
                TotalNodes = 0,
                HealthyNodes = 0,
                AverageCpuUtilization = 0,
                AverageMemoryUtilization = 0,
                TotalActiveTasks = 0,
                AverageResponseTime = 0,
                MessagesPerSecond = 0
            };
        }
        
        var healthyNodes = _monitoredNodes.Values.Count(n => n.Health == NodeHealth.Healthy);
        var avgCpu = _monitoredNodes.Values.Average(n => n.CpuUsage);
        var avgMemory = _monitoredNodes.Values.Average(n => n.MemoryUsage);
        var totalTasks = _monitoredNodes.Values.Sum(n => n.ActiveTasks);
        var avgResponseTime = _monitoredNodes.Values.Average(n => n.ResponseTime);
        var messagesPerSecond = totalTasks * 0.1; // Rough estimate
        
        return new ClusterMetrics
        {
            TotalNodes = totalNodes,
            HealthyNodes = healthyNodes,
            AverageCpuUtilization = avgCpu,
            AverageMemoryUtilization = avgMemory,
            TotalActiveTasks = totalTasks,
            AverageResponseTime = avgResponseTime,
            MessagesPerSecond = messagesPerSecond
        };
    }

    /// <summary>
    /// Determines node health based on metrics.
    /// </summary>
    /// <param name="cpuUsage">CPU usage percentage.</param>
    /// <param name="memoryUsage">Memory usage percentage.</param>
    /// <param name="activeTasks">Number of active tasks.</param>
    /// <param name="responseTime">Response time in milliseconds.</param>
    /// <returns>Calculated node health.</returns>
    private static NodeHealth DetermineNodeHealth(double cpuUsage, double memoryUsage, int activeTasks, double responseTime)
    {
        if (cpuUsage > 90 || memoryUsage > 85 || activeTasks > 50 || responseTime > 5000)
            return NodeHealth.Critical;
        
        if (cpuUsage > 75 || memoryUsage > 70 || activeTasks > 25 || responseTime > 2000)
            return NodeHealth.Warning;
        
        return NodeHealth.Healthy;
    }

    /// <summary>
    /// Raises the HealthChanged event.
    /// </summary>
    /// <param name="oldHealthStatus">Previous health status.</param>
    /// <param name="newHealthStatus">New health status.</param>
    protected virtual void OnHealthChanged(ClusterHealthStatus oldHealthStatus, ClusterHealthStatus newHealthStatus)
    {
        HealthChanged?.Invoke(this, new ClusterHealthChangedEventArgs(oldHealthStatus, newHealthStatus));
    }

    /// <summary>
    /// Data structure for tracking monitored node information.
    /// </summary>
    private class NodeMonitoringData
    {
        public string NodeAddress { get; set; } = string.Empty;
        public NodeHealth Health { get; set; } = NodeHealth.Healthy;
        public double CpuUsage { get; set; } = 0.0;
        public double MemoryUsage { get; set; } = 0.0;
        public int ActiveTasks { get; set; } = 0;
        public double ResponseTime { get; set; } = 100.0;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public bool IsMonitoring { get; set; } = false;
    }
}
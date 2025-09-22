using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Clustering;

/// <summary>
/// Implementation of AI node manager for individual cluster nodes.
/// </summary>
public class AINodeManager : IAINodeManager
{
    private readonly ILogger<AINodeManager> _logger;
    private readonly string _nodeId;
    private NodeHealth _health = NodeHealth.Healthy;
    private readonly Dictionary<string, string> _capabilities = new();
    private double _cpuUsage = 0;
    private double _memoryUsage = 0;
    private int _activeTasks = 0;
    private bool _isRunning = false;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AINodeManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public AINodeManager(ILogger<AINodeManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _nodeId = $"ai-node-{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    /// <inheritdoc />
    public string NodeId => _nodeId;

    /// <inheritdoc />
    public NodeHealth Health => _health;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI Node Manager for node {NodeId}", _nodeId);
        
        // Initialize node-specific resources
        await Task.Delay(100, cancellationToken);
        
        // Register default capabilities
        await RegisterCapabilityAsync("text-processing", "TextProcessing", cancellationToken);
        await RegisterCapabilityAsync("ml-inference", "MLInference", cancellationToken);
        
        _logger.LogInformation("AI Node Manager initialized for node {NodeId}", _nodeId);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI Node Manager for node {NodeId}", _nodeId);
        
        // Start node services and health monitoring
        _health = NodeHealth.Healthy;
        await Task.Delay(50, cancellationToken);
        
        _isRunning = true;
        
        // Start periodic health checks
        _ = StartHealthMonitoringAsync(cancellationToken);
        
        _logger.LogInformation("AI Node Manager started for node {NodeId}", _nodeId);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AI Node Manager for node {NodeId}", _nodeId);
        
        _isRunning = false;
        _health = NodeHealth.Unavailable;
        await Task.Delay(50, cancellationToken);
        
        _logger.LogInformation("AI Node Manager stopped for node {NodeId}", _nodeId);
    }

    /// <inheritdoc />
    public async Task RegisterCapabilityAsync(string capabilityName, string capabilityType, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registering capability {CapabilityName} of type {CapabilityType} for node {NodeId}", 
            capabilityName, capabilityType, _nodeId);
        
        await Task.Delay(10, cancellationToken);
        
        _capabilities[capabilityName] = capabilityType;
        
        _logger.LogDebug("Capability {CapabilityName} registered successfully", capabilityName);
    }

    /// <inheritdoc />
    public async Task UpdateMetricsAsync(double cpuUsage, double memoryUsage, int activeTasks, CancellationToken cancellationToken = default)
    {
        _cpuUsage = cpuUsage;
        _memoryUsage = memoryUsage;
        _activeTasks = activeTasks;
        
        // Update health status based on metrics
        var oldHealth = _health;
        _health = DetermineHealthStatus(cpuUsage, memoryUsage, activeTasks);
        
        if (oldHealth != _health)
        {
            _logger.LogInformation("Node {NodeId} health changed from {OldHealth} to {NewHealth}", 
                _nodeId, oldHealth, _health);
        }
        
        _logger.LogDebug("Node {NodeId} metrics updated: CPU={CpuUsage}%, Memory={MemoryUsage}%, Tasks={ActiveTasks}", 
            _nodeId, cpuUsage, memoryUsage, activeTasks);
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            await StopAsync();
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
    /// Gets the current capabilities of this node.
    /// </summary>
    /// <returns>Read-only dictionary of capabilities.</returns>
    public IReadOnlyDictionary<string, string> GetCapabilities()
    {
        return _capabilities.AsReadOnly();
    }

    /// <summary>
    /// Starts background health monitoring for this node.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the monitoring operation.</returns>
    private async Task StartHealthMonitoringAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _health != NodeHealth.Unavailable && _isRunning)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                
                // Simulate health check - in real implementation, this would check actual system metrics
                var random = Random.Shared;
                var simulatedCpuUsage = random.NextDouble() * 80; // 0-80% CPU
                var simulatedMemoryUsage = random.NextDouble() * 70; // 0-70% Memory
                var simulatedActiveTasks = random.Next(0, 10);
                
                await UpdateMetricsAsync(simulatedCpuUsage, simulatedMemoryUsage, simulatedActiveTasks, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in health monitoring for node {NodeId}", _nodeId);
        }
    }

    /// <summary>
    /// Determines the health status based on current metrics.
    /// </summary>
    /// <param name="cpuUsage">CPU usage percentage.</param>
    /// <param name="memoryUsage">Memory usage percentage.</param>
    /// <param name="activeTasks">Number of active tasks.</param>
    /// <returns>Calculated health status.</returns>
    private static NodeHealth DetermineHealthStatus(double cpuUsage, double memoryUsage, int activeTasks)
    {
        // Critical thresholds
        if (cpuUsage > 90 || memoryUsage > 85 || activeTasks > 50)
            return NodeHealth.Critical;
        
        // Warning thresholds  
        if (cpuUsage > 75 || memoryUsage > 70 || activeTasks > 25)
            return NodeHealth.Warning;
        
        return NodeHealth.Healthy;
    }
}
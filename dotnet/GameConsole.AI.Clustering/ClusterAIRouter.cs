using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Clustering;

/// <summary>
/// Implementation of intelligent AI message router for cluster nodes.
/// </summary>
public class ClusterAIRouter : IClusterAIRouter
{
    private readonly ILogger<ClusterAIRouter> _logger;
    private readonly Dictionary<string, List<string>> _capabilityToNodes = new();
    private readonly Dictionary<string, NodeRoutingInfo> _nodeInfo = new();
    private readonly Random _random = new();
    private int _roundRobinIndex = 0;
    private bool _isRunning = false;
    private bool _disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterAIRouter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ClusterAIRouter(ILogger<ClusterAIRouter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Cluster AI Router");
        
        // Initialize routing tables and load balancing algorithms
        await Task.Delay(50, cancellationToken);
        
        _logger.LogInformation("Cluster AI Router initialized");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Cluster AI Router");
        
        // Start routing services and monitoring
        await Task.Delay(25, cancellationToken);
        
        _isRunning = true;
        
        // Initialize with some default nodes for demonstration
        await RegisterNodeAsync("akka://ai-cluster@127.0.0.1:2552", new[] { "text-processing", "ml-inference" });
        await RegisterNodeAsync("akka://ai-cluster@127.0.0.1:2553", new[] { "image-processing", "computer-vision" });
        
        _logger.LogInformation("Cluster AI Router started");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Cluster AI Router");
        
        _isRunning = false;
        
        // Clean up routing tables
        _capabilityToNodes.Clear();
        _nodeInfo.Clear();
        
        await Task.Delay(25, cancellationToken);
        
        _logger.LogInformation("Cluster AI Router stopped");
    }

    /// <inheritdoc />
    public async Task<string> RouteMessageAsync(string messageId, string capabilityType, int priority, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Routing message {MessageId} with capability {CapabilityType} and priority {Priority}", 
            messageId, capabilityType, priority);
        
        await Task.Delay(5, cancellationToken); // Simulate routing decision time
        
        if (!_capabilityToNodes.TryGetValue(capabilityType, out var availableNodes) || availableNodes.Count == 0)
        {
            _logger.LogWarning("No nodes available for capability {CapabilityType}", capabilityType);
            throw new InvalidOperationException($"No nodes available for capability: {capabilityType}");
        }
        
        // Route based on priority and load balancing strategy
        var selectedNode = SelectNodeForMessage(availableNodes, priority);
        
        _logger.LogInformation("Routed message {MessageId} to node {SelectedNode}", messageId, selectedNode);
        
        return selectedNode;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetAvailableNodesAsync(string capabilityType, int count = 5, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting available nodes for capability {CapabilityType} (max count: {Count})", 
            capabilityType, count);
        
        await Task.Delay(2, cancellationToken);
        
        if (!_capabilityToNodes.TryGetValue(capabilityType, out var availableNodes))
        {
            return Array.Empty<string>();
        }
        
        // Return nodes sorted by load (ascending) and health
        var sortedNodes = availableNodes
            .Where(node => _nodeInfo.ContainsKey(node) && _nodeInfo[node].IsHealthy)
            .OrderBy(node => _nodeInfo[node].LoadLevel)
            .Take(count)
            .ToList()
            .AsReadOnly();
        
        _logger.LogDebug("Found {NodeCount} available nodes for capability {CapabilityType}", 
            sortedNodes.Count, capabilityType);
        
        return sortedNodes;
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
    /// Registers a node with its capabilities for routing purposes.
    /// </summary>
    /// <param name="nodeAddress">Address of the node.</param>
    /// <param name="capabilities">Capabilities provided by the node.</param>
    /// <returns>Task representing the registration operation.</returns>
    public async Task RegisterNodeAsync(string nodeAddress, IEnumerable<string> capabilities)
    {
        _logger.LogInformation("Registering node {NodeAddress} with capabilities: {Capabilities}", 
            nodeAddress, string.Join(", ", capabilities));
        
        // Add node to capability mappings
        foreach (var capability in capabilities)
        {
            if (!_capabilityToNodes.ContainsKey(capability))
            {
                _capabilityToNodes[capability] = new List<string>();
            }
            
            if (!_capabilityToNodes[capability].Contains(nodeAddress))
            {
                _capabilityToNodes[capability].Add(nodeAddress);
            }
        }
        
        // Initialize node routing info
        _nodeInfo[nodeAddress] = new NodeRoutingInfo
        {
            Address = nodeAddress,
            Capabilities = capabilities.ToHashSet(),
            LoadLevel = 0.0,
            IsHealthy = true,
            LastSeen = DateTime.UtcNow
        };
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Updates the load and health information for a node.
    /// </summary>
    /// <param name="nodeAddress">Address of the node.</param>
    /// <param name="loadLevel">Current load level (0.0-1.0).</param>
    /// <param name="isHealthy">Whether the node is healthy.</param>
    /// <returns>Task representing the update operation.</returns>
    public async Task UpdateNodeStatusAsync(string nodeAddress, double loadLevel, bool isHealthy)
    {
        if (_nodeInfo.ContainsKey(nodeAddress))
        {
            var nodeInfo = _nodeInfo[nodeAddress];
            nodeInfo.LoadLevel = loadLevel;
            nodeInfo.IsHealthy = isHealthy;
            nodeInfo.LastSeen = DateTime.UtcNow;
            
            _logger.LogDebug("Updated node {NodeAddress} status: Load={LoadLevel:P2}, Healthy={IsHealthy}", 
                nodeAddress, loadLevel, isHealthy);
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Selects the best node for a message based on priority and load balancing.
    /// </summary>
    /// <param name="availableNodes">List of available nodes.</param>
    /// <param name="priority">Message priority (higher numbers = higher priority).</param>
    /// <returns>Selected node address.</returns>
    private string SelectNodeForMessage(List<string> availableNodes, int priority)
    {
        // Filter to healthy nodes only
        var healthyNodes = availableNodes
            .Where(node => _nodeInfo.ContainsKey(node) && _nodeInfo[node].IsHealthy)
            .ToList();
        
        if (healthyNodes.Count == 0)
        {
            // Fallback to any available node if no healthy nodes
            healthyNodes = availableNodes;
        }
        
        if (healthyNodes.Count == 1)
        {
            return healthyNodes[0];
        }
        
        // For high priority messages, use least loaded node
        if (priority >= 3)
        {
            return healthyNodes
                .OrderBy(node => _nodeInfo.ContainsKey(node) ? _nodeInfo[node].LoadLevel : 0.5)
                .First();
        }
        
        // For normal priority, use round-robin
        var selectedIndex = _roundRobinIndex % healthyNodes.Count;
        _roundRobinIndex++;
        
        return healthyNodes[selectedIndex];
    }

    /// <summary>
    /// Node routing information for load balancing decisions.
    /// </summary>
    private class NodeRoutingInfo
    {
        public string Address { get; set; } = string.Empty;
        public HashSet<string> Capabilities { get; set; } = new();
        public double LoadLevel { get; set; } = 0.0;
        public bool IsHealthy { get; set; } = true;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}
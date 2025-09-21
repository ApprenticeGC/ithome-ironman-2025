using Microsoft.Extensions.Logging;
using GameConsole.AI.Clustering.Interfaces;
using GameConsole.AI.Clustering.Models;

namespace GameConsole.AI.Clustering.Services;

/// <summary>
/// Default implementation of IClusterAIRouter for intelligent message routing across the cluster.
/// </summary>
public class ClusterAIRouter : IClusterAIRouter
{
    private readonly ILogger<ClusterAIRouter> _logger;
    private readonly IAIClusterManager _clusterManager;
    private bool _isRunning;
    private RoutingStrategy _strategy = new();
    private readonly Dictionary<string, double> _nodeWeights = new();
    private readonly Dictionary<string, NodeRoutingStats> _nodeStats = new();
    private long _totalRequests;
    private long _successfulRoutes;
    private long _failedRoutes;

    /// <summary>
    /// Initializes a new instance of the ClusterAIRouter.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="clusterManager">The cluster manager instance.</param>
    public ClusterAIRouter(ILogger<ClusterAIRouter> logger, IAIClusterManager clusterManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clusterManager = clusterManager ?? throw new ArgumentNullException(nameof(clusterManager));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<RoutingDecision>? RouteDecided;

    /// <inheritdoc />
    #pragma warning disable CS0067 // Event is declared but never used
    public event EventHandler<string>? NodeUnavailable;
    #pragma warning restore CS0067

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing ClusterAIRouter");
        
        // Basic initialization - will be enhanced when actor system foundation is available
        await Task.CompletedTask;
        
        _logger.LogInformation("ClusterAIRouter initialized");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ClusterAIRouter");
        
        _isRunning = true;
        
        _logger.LogInformation("ClusterAIRouter started");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping ClusterAIRouter");
        
        _isRunning = false;
        
        _logger.LogInformation("ClusterAIRouter stopped");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }
    }

    /// <inheritdoc />
    public async Task<RoutingDecision> RouteMessageAsync(RoutingRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Routing message {RequestId} for agent type {AgentType}", request.RequestId, request.AgentType);
        
        Interlocked.Increment(ref _totalRequests);
        
        var capableNodes = await GetCapableNodesAsync(request.AgentType, request.Operation, cancellationToken);
        
        if (capableNodes.Count == 0)
        {
            Interlocked.Increment(ref _failedRoutes);
            
            var failedDecision = new RoutingDecision
            {
                Request = request,
                SelectedNode = null,
                Reason = $"No nodes found capable of handling {request.AgentType} operations",
                Confidence = 0.0,
                AlternativeNodes = Array.Empty<ClusterNode>()
            };
            
            RouteDecided?.Invoke(this, failedDecision);
            _logger.LogWarning("No capable nodes found for request {RequestId}", request.RequestId);
            return failedDecision;
        }
        
        var selectedNode = SelectBestNode(capableNodes, request);
        Interlocked.Increment(ref _successfulRoutes);
        
        var decision = new RoutingDecision
        {
            Request = request,
            SelectedNode = selectedNode,
            Reason = $"Selected based on {_strategy.Algorithm} algorithm",
            Confidence = CalculateRoutingConfidence(selectedNode, capableNodes),
            AlternativeNodes = capableNodes.Where(n => n.NodeId != selectedNode.NodeId).ToList()
        };
        
        UpdateNodeStats(selectedNode.NodeId, true);
        RouteDecided?.Invoke(this, decision);
        
        _logger.LogDebug("Routed request {RequestId} to node {NodeId}", request.RequestId, selectedNode.NodeId);
        return decision;
    }

    /// <inheritdoc />
    public async Task<ClusterNode?> GetBestNodeForCapabilityAsync(string agentType, string operation, CancellationToken cancellationToken = default)
    {
        var capableNodes = await GetCapableNodesAsync(agentType, operation, cancellationToken);
        
        if (capableNodes.Count == 0)
        {
            _logger.LogDebug("No nodes found for capability {AgentType}/{Operation}", agentType, operation);
            return null;
        }
        
        var dummyRequest = new RoutingRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            AgentType = agentType,
            Operation = operation,
            MessagePayload = new object()
        };
        
        return SelectBestNode(capableNodes, dummyRequest);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ClusterNode>> GetCapableNodesAsync(string agentType, string operation, CancellationToken cancellationToken = default)
    {
        var allNodes = await _clusterManager.GetClusterNodesAsync(cancellationToken);
        
        var capableNodes = allNodes
            .Where(node => node.Health == NodeHealth.Healthy || node.Health == NodeHealth.Degraded)
            .Where(node => HasCapability(node, agentType, operation))
            .OrderBy(node => GetNodeScore(node))
            .ToList();
        
        _logger.LogDebug("Found {Count} capable nodes for {AgentType}/{Operation}", capableNodes.Count, agentType, operation);
        
        return capableNodes;
    }

    /// <inheritdoc />
    public async Task UpdateNodeWeightsAsync(IReadOnlyDictionary<string, double> nodeWeights, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating node weights for {Count} nodes", nodeWeights.Count);
        
        _nodeWeights.Clear();
        foreach (var kvp in nodeWeights)
        {
            _nodeWeights[kvp.Key] = kvp.Value;
        }
        
        _logger.LogInformation("Node weights updated");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<RoutingStatistics> GetRoutingStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new RoutingStatistics
        {
            TotalRequests = _totalRequests,
            SuccessfulRoutes = _successfulRoutes,
            FailedRoutes = _failedRoutes,
            AverageDecisionTimeMs = 5.0, // Placeholder - will be enhanced with actual timing
            PerNodeStats = new Dictionary<string, NodeRoutingStats>(_nodeStats),
            CapturedAt = DateTime.UtcNow
        };
        
        return await Task.FromResult(stats);
    }

    /// <inheritdoc />
    public async Task ConfigureRoutingStrategyAsync(RoutingStrategy strategy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring routing strategy: {Algorithm}", strategy.Algorithm);
        
        _strategy = strategy;
        
        _logger.LogInformation("Routing strategy configured");
        await Task.CompletedTask;
    }

    private bool HasCapability(ClusterNode node, string agentType, string operation)
    {
        return node.Capabilities.Any(cap => 
            cap.AgentType == agentType && 
            cap.SupportedOperations.Contains(operation));
    }

    private ClusterNode SelectBestNode(IReadOnlyList<ClusterNode> capableNodes, RoutingRequest request)
    {
        return _strategy.Algorithm switch
        {
            LoadBalancingAlgorithm.RoundRobin => SelectRoundRobin(capableNodes),
            LoadBalancingAlgorithm.WeightedRoundRobin => SelectWeightedRoundRobin(capableNodes),
            LoadBalancingAlgorithm.LeastConnections => SelectLeastConnections(capableNodes),
            LoadBalancingAlgorithm.CapabilityBased => SelectCapabilityBased(capableNodes, request),
            LoadBalancingAlgorithm.ConsistentHash => SelectConsistentHash(capableNodes, request),
            _ => capableNodes[0] // Default to first available
        };
    }

    private ClusterNode SelectRoundRobin(IReadOnlyList<ClusterNode> nodes)
    {
        var index = (int)(_totalRequests % nodes.Count);
        return nodes[index];
    }

    private ClusterNode SelectWeightedRoundRobin(IReadOnlyList<ClusterNode> nodes)
    {
        // Simple weighted selection - will be enhanced with proper weight distribution
        var weightedNodes = nodes
            .Select(n => new { Node = n, Weight = _nodeWeights.GetValueOrDefault(n.NodeId, 1.0) })
            .OrderByDescending(x => x.Weight)
            .ToList();
        
        return weightedNodes.Count > 0 ? weightedNodes[0].Node : nodes[0];
    }

    private ClusterNode SelectLeastConnections(IReadOnlyList<ClusterNode> nodes)
    {
        return nodes
            .OrderBy(n => _nodeStats.GetValueOrDefault(n.NodeId, new NodeRoutingStats()).RequestsRouted)
            .First();
    }

    private ClusterNode SelectCapabilityBased(IReadOnlyList<ClusterNode> nodes, RoutingRequest request)
    {
        return nodes
            .OrderByDescending(n => GetCapabilityScore(n, request.AgentType, request.Operation))
            .First();
    }

    private ClusterNode SelectConsistentHash(IReadOnlyList<ClusterNode> nodes, RoutingRequest request)
    {
        var hash = request.RequestId.GetHashCode();
        var index = Math.Abs(hash) % nodes.Count;
        return nodes[index];
    }

    private double GetNodeScore(ClusterNode node)
    {
        var healthScore = node.Health switch
        {
            NodeHealth.Healthy => 1.0,
            NodeHealth.Degraded => 0.7,
            NodeHealth.Unhealthy => 0.3,
            NodeHealth.Offline => 0.0,
            _ => 0.5
        };
        
        var weightScore = _nodeWeights.GetValueOrDefault(node.NodeId, 1.0);
        
        return healthScore * weightScore;
    }

    private double GetCapabilityScore(ClusterNode node, string agentType, string operation)
    {
        var capability = node.Capabilities.FirstOrDefault(c => 
            c.AgentType == agentType && 
            c.SupportedOperations.Contains(operation));
        
        return capability?.Performance.QualityScore ?? 0.0;
    }

    private double CalculateRoutingConfidence(ClusterNode selectedNode, IReadOnlyList<ClusterNode> alternatives)
    {
        // Simple confidence calculation - will be enhanced with more sophisticated metrics
        var nodeScore = GetNodeScore(selectedNode);
        var averageScore = alternatives.Average(GetNodeScore);
        
        return Math.Min(1.0, nodeScore / Math.Max(averageScore, 0.1));
    }

    private void UpdateNodeStats(string nodeId, bool success)
    {
        if (!_nodeStats.ContainsKey(nodeId))
        {
            _nodeStats[nodeId] = new NodeRoutingStats();
        }
        
        var stats = _nodeStats[nodeId];
        var newRequestsRouted = stats.RequestsRouted + 1;
        var newSuccessRate = success 
            ? (stats.SuccessRate * stats.RequestsRouted + 1.0) / newRequestsRouted
            : (stats.SuccessRate * stats.RequestsRouted) / newRequestsRouted;
        
        _nodeStats[nodeId] = stats with 
        { 
            RequestsRouted = newRequestsRouted,
            SuccessRate = newSuccessRate
        };
    }
}
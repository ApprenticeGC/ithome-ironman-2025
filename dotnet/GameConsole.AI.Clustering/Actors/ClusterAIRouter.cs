using Akka.Actor;
using Akka.Event;
using GameConsole.AI.Clustering.Configuration;
using GameConsole.AI.Clustering.Messages;

namespace GameConsole.AI.Clustering.Actors;

/// <summary>
/// Intelligent router for distributing messages across cluster nodes
/// </summary>
public class ClusterAIRouter : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly AIClusterConfig _config;
    private readonly Dictionary<string, List<NodeRouteInfo>> _capabilityRoutes = new();
    private readonly Dictionary<string, int> _roundRobinCounters = new();

    public ClusterAIRouter(AIClusterConfig config)
    {
        _config = config;

        Receive<ClusterMessages.RouteMessage>(HandleRouteMessage);
        Receive<ClusterMessages.NodeJoined>(HandleNodeJoined);
        Receive<ClusterMessages.NodeLeft>(HandleNodeLeft);
        Receive<ClusterMessages.HealthCheckResponse>(HandleHealthCheckResponse);
    }

    protected override void PreStart()
    {
        base.PreStart();
        _log.Info("ClusterAIRouter started with strategy: {0}", _config.LoadBalancingStrategy);
    }

    private void HandleRouteMessage(ClusterMessages.RouteMessage message)
    {
        _log.Debug("Routing message for capability: {0}, RequestId: {1}", 
                   message.RequiredCapability, message.RequestId);

        var targetNode = SelectTargetNode(message.RequiredCapability);
        
        if (targetNode != null)
        {
            _log.Debug("Routing to node {0} for capability {1}", 
                       targetNode.NodeId, message.RequiredCapability);
            
            // In a real implementation, we would forward the message to the target node
            // For now, we'll simulate successful routing
            Sender.Tell(new ClusterMessages.RouteResponse(message.RequestId, targetNode.NodeId, true));
            
            // Update routing metrics
            targetNode.IncrementRequestCount();
        }
        else
        {
            _log.Warning("No available node found for capability: {0}", message.RequiredCapability);
            Sender.Tell(new ClusterMessages.RouteResponse(message.RequestId, null, false));
        }
    }

    private void HandleNodeJoined(ClusterMessages.NodeJoined message)
    {
        _log.Info("Adding node {0} to routing tables with capabilities: {1}", 
                  message.NodeId, string.Join(", ", message.NodeCapabilities));

        foreach (var capability in message.NodeCapabilities)
        {
            if (!_capabilityRoutes.ContainsKey(capability))
            {
                _capabilityRoutes[capability] = new List<NodeRouteInfo>();
                _roundRobinCounters[capability] = 0;
            }

            var routeInfo = new NodeRouteInfo(message.NodeId, capability, DateTime.UtcNow);
            _capabilityRoutes[capability].Add(routeInfo);
        }
    }

    private void HandleNodeLeft(ClusterMessages.NodeLeft message)
    {
        _log.Info("Removing node {0} from routing tables", message.NodeId);

        foreach (var capabilityList in _capabilityRoutes.Values)
        {
            capabilityList.RemoveAll(r => r.NodeId == message.NodeId);
        }

        // Clean up empty capability lists
        var emptyCapabilities = _capabilityRoutes
            .Where(kvp => kvp.Value.Count == 0)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var capability in emptyCapabilities)
        {
            _capabilityRoutes.Remove(capability);
            _roundRobinCounters.Remove(capability);
        }
    }

    private void HandleHealthCheckResponse(ClusterMessages.HealthCheckResponse message)
    {
        // Update health status for all routes of this node
        foreach (var capabilityList in _capabilityRoutes.Values)
        {
            var nodeRoute = capabilityList.FirstOrDefault(r => r.NodeId == message.NodeId);
            if (nodeRoute != null)
            {
                nodeRoute.UpdateHealth(message.IsHealthy, message.LoadMetrics);
            }
        }
    }

    private NodeRouteInfo? SelectTargetNode(string capability)
    {
        if (!_capabilityRoutes.TryGetValue(capability, out var availableNodes))
            return null;

        var healthyNodes = availableNodes.Where(n => n.IsHealthy).ToList();
        if (healthyNodes.Count == 0)
            return null;

        return _config.LoadBalancingStrategy switch
        {
            LoadBalancingStrategies.RoundRobin => SelectRoundRobin(capability, healthyNodes),
            LoadBalancingStrategies.LeastConnections => SelectLeastConnections(healthyNodes),
            LoadBalancingStrategies.WeightedRoundRobin => SelectWeightedRoundRobin(healthyNodes),
            LoadBalancingStrategies.ConsistentHashing => SelectConsistentHashing(capability, healthyNodes),
            _ => SelectRoundRobin(capability, healthyNodes)
        };
    }

    private NodeRouteInfo SelectRoundRobin(string capability, List<NodeRouteInfo> nodes)
    {
        var counter = _roundRobinCounters[capability];
        var selectedNode = nodes[counter % nodes.Count];
        _roundRobinCounters[capability] = (counter + 1) % nodes.Count;
        return selectedNode;
    }

    private NodeRouteInfo SelectLeastConnections(List<NodeRouteInfo> nodes)
    {
        return nodes.OrderBy(n => n.ActiveConnections).First();
    }

    private NodeRouteInfo SelectWeightedRoundRobin(List<NodeRouteInfo> nodes)
    {
        // Simple implementation: weight inversely proportional to load
        var totalWeight = nodes.Sum(n => n.GetWeight());
        if (totalWeight == 0) return nodes.First();

        var random = new Random().NextDouble() * totalWeight;
        var currentWeight = 0.0;

        foreach (var node in nodes)
        {
            currentWeight += node.GetWeight();
            if (currentWeight >= random)
                return node;
        }

        return nodes.Last();
    }

    private NodeRouteInfo SelectConsistentHashing(string capability, List<NodeRouteInfo> nodes)
    {
        // Simple hash-based selection
        var hash = capability.GetHashCode();
        var index = Math.Abs(hash) % nodes.Count;
        return nodes[index];
    }

    public static Props Props(AIClusterConfig config) => Akka.Actor.Props.Create<ClusterAIRouter>(config);
}

/// <summary>
/// Routing information for a cluster node
/// </summary>
public class NodeRouteInfo
{
    public string NodeId { get; }
    public string Capability { get; }
    public DateTime LastSeen { get; private set; }
    public bool IsHealthy { get; private set; } = true;
    public int ActiveConnections { get; private set; } = 0;
    public Dictionary<string, double> LoadMetrics { get; private set; } = new();

    public NodeRouteInfo(string nodeId, string capability, DateTime lastSeen)
    {
        NodeId = nodeId;
        Capability = capability;
        LastSeen = lastSeen;
    }

    public void UpdateHealth(bool isHealthy, Dictionary<string, double> loadMetrics)
    {
        IsHealthy = isHealthy;
        LoadMetrics = loadMetrics ?? new Dictionary<string, double>();
        LastSeen = DateTime.UtcNow;
    }

    public void IncrementRequestCount()
    {
        ActiveConnections++;
    }

    public void DecrementRequestCount()
    {
        if (ActiveConnections > 0)
            ActiveConnections--;
    }

    public double GetWeight()
    {
        if (!IsHealthy) return 0.0;
        
        // Weight based on inverse of CPU load (assuming CPU load is in LoadMetrics)
        if (LoadMetrics.TryGetValue("cpu_usage", out var cpuUsage))
        {
            return Math.Max(0.1, 1.0 - (cpuUsage / 100.0));
        }
        
        // Default weight
        return 1.0;
    }
}
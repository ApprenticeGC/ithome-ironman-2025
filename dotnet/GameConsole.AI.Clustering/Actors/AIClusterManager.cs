using Akka.Actor;
using Akka.Cluster;
using Akka.Event;
using GameConsole.AI.Clustering.Configuration;
using GameConsole.AI.Clustering.Messages;

namespace GameConsole.AI.Clustering.Actors;

/// <summary>
/// Main cluster coordinator actor that manages the AI cluster
/// </summary>
public class AIClusterManager : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly AIClusterConfig _config;
    private readonly Cluster _cluster;
    private readonly Dictionary<string, ClusterNodeInfo> _clusterNodes = new();
    private readonly Dictionary<string, List<string>> _capabilityToNodes = new();
    private IActorRef? _router;
    private IActorRef? _monitor;

    public AIClusterManager(AIClusterConfig config)
    {
        _config = config;
        _cluster = Cluster.Get(Context.System);

        Receive<ClusterMessages.JoinCluster>(HandleJoinCluster);
        Receive<ClusterMessages.LeaveCluster>(HandleLeaveCluster);
        Receive<ClusterMessages.HealthCheckResponse>(HandleHealthCheckResponse);
        Receive<ClusterMessages.GetClusterState>(HandleGetClusterState);
        Receive<ClusterMessages.ScaleCluster>(HandleScaleCluster);
        Receive<ClusterMessages.RouteMessage>(HandleRouteMessage);
        Receive<ClusterEvent.MemberUp>(HandleMemberUp);
        Receive<ClusterEvent.MemberRemoved>(HandleMemberRemoved);
        Receive<ClusterEvent.UnreachableMember>(HandleUnreachableMember);
    }

    protected override void PreStart()
    {
        base.PreStart();
        
        // Subscribe to cluster events
        _cluster.Subscribe(Self, typeof(ClusterEvent.MemberUp), typeof(ClusterEvent.MemberRemoved), 
                          typeof(ClusterEvent.UnreachableMember));

        // Create child actors
        _router = Context.ActorOf(Props.Create<ClusterAIRouter>(_config), "cluster-router");
        _monitor = Context.ActorOf(Props.Create<AIClusterMonitor>(_config), "cluster-monitor");

        _log.Info("AIClusterManager started on node {0}", _cluster.SelfAddress);
    }

    protected override void PostStop()
    {
        _cluster.Unsubscribe(Self);
        base.PostStop();
    }

    private void HandleJoinCluster(ClusterMessages.JoinCluster message)
    {
        _log.Info("Node {0} requesting to join cluster with capabilities: {1}", 
                  message.NodeId, string.Join(", ", message.NodeCapabilities));

        var nodeInfo = new ClusterNodeInfo(
            message.NodeId,
            message.Address,
            message.NodeCapabilities,
            DateTime.UtcNow,
            true
        );

        _clusterNodes[message.NodeId] = nodeInfo;

        // Update capability mapping
        foreach (var capability in message.NodeCapabilities)
        {
            if (!_capabilityToNodes.ContainsKey(capability))
                _capabilityToNodes[capability] = new List<string>();
            
            if (!_capabilityToNodes[capability].Contains(message.NodeId))
                _capabilityToNodes[capability].Add(message.NodeId);
        }

        // Notify all nodes about the new member
        BroadcastToCluster(new ClusterMessages.NodeJoined(message.NodeId, message.NodeCapabilities));
        
        _log.Info("Node {0} joined cluster. Total nodes: {1}", message.NodeId, _clusterNodes.Count);
    }

    private void HandleLeaveCluster(ClusterMessages.LeaveCluster message)
    {
        _log.Info("Node {0} leaving cluster: {1}", message.NodeId, message.Reason);

        if (_clusterNodes.TryGetValue(message.NodeId, out var nodeInfo))
        {
            // Remove from capability mapping
            foreach (var capability in nodeInfo.Capabilities)
            {
                if (_capabilityToNodes.ContainsKey(capability))
                {
                    _capabilityToNodes[capability].Remove(message.NodeId);
                    if (_capabilityToNodes[capability].Count == 0)
                        _capabilityToNodes.Remove(capability);
                }
            }

            _clusterNodes.Remove(message.NodeId);
        }

        // Notify all nodes about the departure
        BroadcastToCluster(new ClusterMessages.NodeLeft(message.NodeId, message.Reason));
        
        _log.Info("Node {0} left cluster. Total nodes: {1}", message.NodeId, _clusterNodes.Count);
    }

    private void HandleHealthCheckResponse(ClusterMessages.HealthCheckResponse message)
    {
        if (_clusterNodes.TryGetValue(message.NodeId, out var nodeInfo))
        {
            var updatedInfo = nodeInfo with 
            { 
                IsHealthy = message.IsHealthy, 
                LastHeartbeat = DateTime.UtcNow,
                LoadMetrics = message.LoadMetrics
            };
            _clusterNodes[message.NodeId] = updatedInfo;

            if (!message.IsHealthy)
            {
                _log.Warning("Node {0} reported unhealthy status", message.NodeId);
            }
        }
    }

    private void HandleGetClusterState(ClusterMessages.GetClusterState message)
    {
        var nodes = _clusterNodes.Values
            .Where(n => n.IsHealthy)
            .ToDictionary(n => n.NodeId, n => n.Capabilities);

        Sender.Tell(new ClusterMessages.ClusterState(nodes, nodes.Count));
    }

    private void HandleScaleCluster(ClusterMessages.ScaleCluster message)
    {
        _log.Info("Scaling cluster to {0} nodes for capability: {1}", 
                  message.TargetSize, message.RequiredCapability ?? "all");

        var currentSize = _clusterNodes.Count;
        
        if (message.TargetSize > currentSize)
        {
            _log.Info("Cluster needs to scale up from {0} to {1} nodes", currentSize, message.TargetSize);
            // In a real implementation, this would trigger node provisioning
        }
        else if (message.TargetSize < currentSize)
        {
            _log.Info("Cluster needs to scale down from {0} to {1} nodes", currentSize, message.TargetSize);
            // In a real implementation, this would trigger graceful node removal
        }
    }

    private void HandleRouteMessage(ClusterMessages.RouteMessage message)
    {
        // Forward to the router actor
        _router?.Tell(message);
    }

    private void HandleMemberUp(ClusterEvent.MemberUp message)
    {
        _log.Info("Cluster member is up: {0}", message.Member.Address);
    }

    private void HandleMemberRemoved(ClusterEvent.MemberRemoved message)
    {
        _log.Info("Cluster member removed: {0}", message.Member.Address);
        
        // Find and remove the node from our tracking
        var nodeToRemove = _clusterNodes.Values.FirstOrDefault(n => n.Address == message.Member.Address.ToString());
        if (nodeToRemove != null)
        {
            HandleLeaveCluster(new ClusterMessages.LeaveCluster(nodeToRemove.NodeId, "Member removed from cluster"));
        }
    }

    private void HandleUnreachableMember(ClusterEvent.UnreachableMember message)
    {
        _log.Warning("Cluster member is unreachable: {0}", message.Member.Address);
        
        // Mark node as unhealthy
        var unreachableNode = _clusterNodes.Values.FirstOrDefault(n => n.Address == message.Member.Address.ToString());
        if (unreachableNode != null)
        {
            _clusterNodes[unreachableNode.NodeId] = unreachableNode with { IsHealthy = false };
        }
    }

    private void BroadcastToCluster(IClusterMessage message)
    {
        // In a real implementation, this would broadcast to all cluster members
        _log.Debug("Broadcasting message {0} to cluster", message.GetType().Name);
    }

    public static Props Props(AIClusterConfig config) => Akka.Actor.Props.Create<AIClusterManager>(config);
}

/// <summary>
/// Information about a cluster node
/// </summary>
/// <param name="NodeId">Unique node identifier</param>
/// <param name="Address">Network address</param>
/// <param name="Capabilities">Node capabilities</param>
/// <param name="LastHeartbeat">Last heartbeat timestamp</param>
/// <param name="IsHealthy">Health status</param>
/// <param name="LoadMetrics">Current load metrics</param>
public record ClusterNodeInfo(
    string NodeId,
    string Address,
    string[] Capabilities,
    DateTime LastHeartbeat,
    bool IsHealthy,
    Dictionary<string, double> LoadMetrics = null!)
{
    public Dictionary<string, double> LoadMetrics { get; init; } = LoadMetrics ?? new Dictionary<string, double>();
}
using Akka.Actor;

namespace GameConsole.AI.Clustering.Messages;

/// <summary>
/// Base interface for all cluster-related messages
/// </summary>
public interface IClusterMessage
{
}

/// <summary>
/// Messages for cluster coordination
/// </summary>
public static class ClusterMessages
{
    /// <summary>
    /// Request to join a cluster
    /// </summary>
    /// <param name="NodeId">Unique identifier for the node</param>
    /// <param name="NodeCapabilities">Capabilities this node provides</param>
    /// <param name="Address">Network address of the node</param>
    public record JoinCluster(string NodeId, string[] NodeCapabilities, string Address) : IClusterMessage;

    /// <summary>
    /// Notification that a node has joined the cluster
    /// </summary>
    /// <param name="NodeId">ID of the joined node</param>
    /// <param name="NodeCapabilities">Capabilities the node provides</param>
    public record NodeJoined(string NodeId, string[] NodeCapabilities) : IClusterMessage;

    /// <summary>
    /// Request to leave the cluster gracefully
    /// </summary>
    /// <param name="NodeId">ID of the node leaving</param>
    /// <param name="Reason">Reason for leaving</param>
    public record LeaveCluster(string NodeId, string Reason) : IClusterMessage;

    /// <summary>
    /// Notification that a node has left the cluster
    /// </summary>
    /// <param name="NodeId">ID of the node that left</param>
    /// <param name="Reason">Reason for leaving</param>
    public record NodeLeft(string NodeId, string Reason) : IClusterMessage;

    /// <summary>
    /// Health check request for cluster nodes
    /// </summary>
    /// <param name="NodeId">ID of the node to check</param>
    public record HealthCheck(string NodeId) : IClusterMessage;

    /// <summary>
    /// Health check response
    /// </summary>
    /// <param name="NodeId">ID of the responding node</param>
    /// <param name="IsHealthy">Health status</param>
    /// <param name="LoadMetrics">Current load information</param>
    public record HealthCheckResponse(string NodeId, bool IsHealthy, Dictionary<string, double> LoadMetrics) : IClusterMessage;

    /// <summary>
    /// Request to route a message to the best available node
    /// </summary>
    /// <param name="RequiredCapability">Capability required to handle the request</param>
    /// <param name="Message">Message to route</param>
    /// <param name="RequestId">Unique request identifier</param>
    /// <param name="Sender">Original sender for response routing</param>
    public record RouteMessage(string RequiredCapability, object Message, string RequestId, IActorRef Sender) : IClusterMessage;

    /// <summary>
    /// Response after routing a message
    /// </summary>
    /// <param name="RequestId">Original request identifier</param>
    /// <param name="TargetNodeId">Node that received the message</param>
    /// <param name="Success">Whether routing was successful</param>
    public record RouteResponse(string RequestId, string? TargetNodeId, bool Success) : IClusterMessage;

    /// <summary>
    /// Request for current cluster state
    /// </summary>
    public record GetClusterState : IClusterMessage;

    /// <summary>
    /// Current cluster state response
    /// </summary>
    /// <param name="Nodes">List of active nodes and their capabilities</param>
    /// <param name="ClusterSize">Current size of the cluster</param>
    public record ClusterState(Dictionary<string, string[]> Nodes, int ClusterSize) : IClusterMessage;

    /// <summary>
    /// Request to scale the cluster
    /// </summary>
    /// <param name="TargetSize">Desired cluster size</param>
    /// <param name="RequiredCapability">Capability that needs scaling</param>
    public record ScaleCluster(int TargetSize, string? RequiredCapability = null) : IClusterMessage;
}
namespace GameConsole.Core.Abstractions;

/// <summary>
/// Manages AI cluster coordination, including cluster formation, node discovery, and configuration management.
/// </summary>
public interface IAIClusterManager : IService
{
    /// <summary>
    /// Gets the current cluster status.
    /// </summary>
    ClusterStatus Status { get; }

    /// <summary>
    /// Forms a new cluster with the current node as the seed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the cluster formation operation.</returns>
    Task FormClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Joins an existing cluster.
    /// </summary>
    /// <param name="seedNodes">Seed nodes to join.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the cluster join operation.</returns>
    Task JoinClusterAsync(IEnumerable<string> seedNodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves the cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the cluster leave operation.</returns>
    Task LeaveClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when cluster membership changes.
    /// </summary>
    event EventHandler<ClusterMembershipChangedEventArgs>? MembershipChanged;
}

/// <summary>
/// Manages individual nodes within the AI cluster.
/// </summary>
public interface IAINodeManager : IService
{
    /// <summary>
    /// Gets the current node identifier.
    /// </summary>
    string NodeId { get; }

    /// <summary>
    /// Gets the node's current health status.
    /// </summary>
    NodeHealth Health { get; }

    /// <summary>
    /// Registers a capability that this node can provide.
    /// </summary>
    /// <param name="capabilityName">Name of the capability.</param>
    /// <param name="capabilityType">Type of AI capability.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the registration operation.</returns>
    Task RegisterCapabilityAsync(string capabilityName, string capabilityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the node's resource usage metrics.
    /// </summary>
    /// <param name="cpuUsage">CPU utilization percentage.</param>
    /// <param name="memoryUsage">Memory utilization percentage.</param>
    /// <param name="activeTasks">Number of active tasks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the metrics update operation.</returns>
    Task UpdateMetricsAsync(double cpuUsage, double memoryUsage, int activeTasks, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides intelligent routing of AI messages across cluster nodes.
/// </summary>
public interface IClusterAIRouter : IService
{
    /// <summary>
    /// Routes a message to the most appropriate node in the cluster.
    /// </summary>
    /// <param name="messageId">Unique identifier for the message.</param>
    /// <param name="capabilityType">Required capability type.</param>
    /// <param name="priority">Message priority.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task with the target node address.</returns>
    Task<string> RouteMessageAsync(string messageId, string capabilityType, int priority, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available nodes for a specific capability.
    /// </summary>
    /// <param name="capabilityType">Required capability type.</param>
    /// <param name="count">Maximum number of nodes to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task with available node addresses.</returns>
    Task<IReadOnlyList<string>> GetAvailableNodesAsync(string capabilityType, int count = 5, CancellationToken cancellationToken = default);
}

/// <summary>
/// Monitors the health and performance of the AI cluster.
/// </summary>
public interface IAIClusterMonitor : IService
{
    /// <summary>
    /// Gets the current cluster health status.
    /// </summary>
    ClusterHealthStatus HealthStatus { get; }

    /// <summary>
    /// Gets performance metrics for the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task with cluster performance metrics.</returns>
    Task<ClusterMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts monitoring a specific node.
    /// </summary>
    /// <param name="nodeAddress">Address of the node to monitor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the monitoring start operation.</returns>
    Task StartNodeMonitoringAsync(string nodeAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when cluster health changes.
    /// </summary>
    event EventHandler<ClusterHealthChangedEventArgs>? HealthChanged;
}

/// <summary>
/// Represents the current status of the cluster.
/// </summary>
public enum ClusterStatus
{
    /// <summary>
    /// Cluster is not initialized.
    /// </summary>
    Uninitialized,

    /// <summary>
    /// Node is joining the cluster.
    /// </summary>
    Joining,

    /// <summary>
    /// Node is an active member of the cluster.
    /// </summary>
    Up,

    /// <summary>
    /// Node is leaving the cluster.
    /// </summary>
    Leaving,

    /// <summary>
    /// Node has been removed from the cluster.
    /// </summary>
    Removed
}

/// <summary>
/// Health status enumeration.
/// </summary>
public enum NodeHealth
{
    /// <summary>
    /// Node is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// Node has warnings.
    /// </summary>
    Warning,

    /// <summary>
    /// Node is critical.
    /// </summary>
    Critical,

    /// <summary>
    /// Node is unavailable.
    /// </summary>
    Unavailable
}

/// <summary>
/// Cluster health status enumeration.
/// </summary>
public enum ClusterHealthStatus
{
    /// <summary>
    /// Cluster is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// Cluster has some issues but is functional.
    /// </summary>
    Degraded,

    /// <summary>
    /// Cluster is experiencing critical issues.
    /// </summary>
    Critical,

    /// <summary>
    /// Cluster is unavailable.
    /// </summary>
    Unavailable
}

/// <summary>
/// Cluster performance metrics.
/// </summary>
public class ClusterMetrics
{
    /// <summary>
    /// Total number of nodes in the cluster.
    /// </summary>
    public int TotalNodes { get; set; }

    /// <summary>
    /// Number of healthy nodes.
    /// </summary>
    public int HealthyNodes { get; set; }

    /// <summary>
    /// Average CPU utilization across all nodes.
    /// </summary>
    public double AverageCpuUtilization { get; set; }

    /// <summary>
    /// Average memory utilization across all nodes.
    /// </summary>
    public double AverageMemoryUtilization { get; set; }

    /// <summary>
    /// Total active tasks across all nodes.
    /// </summary>
    public int TotalActiveTasks { get; set; }

    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Messages processed per second.
    /// </summary>
    public double MessagesPerSecond { get; set; }
}

/// <summary>
/// Event arguments for cluster membership changes.
/// </summary>
public class ClusterMembershipChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterMembershipChangedEventArgs"/> class.
    /// </summary>
    /// <param name="nodeAddress">Address of the node that changed.</param>
    /// <param name="changeType">Type of change.</param>
    public ClusterMembershipChangedEventArgs(string nodeAddress, string changeType)
    {
        NodeAddress = nodeAddress;
        ChangeType = changeType;
    }

    /// <summary>
    /// Gets the address of the node that changed.
    /// </summary>
    public string NodeAddress { get; }

    /// <summary>
    /// Gets the type of change.
    /// </summary>
    public string ChangeType { get; }
}

/// <summary>
/// Event arguments for cluster health changes.
/// </summary>
public class ClusterHealthChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterHealthChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldStatus">Previous health status.</param>
    /// <param name="newStatus">New health status.</param>
    public ClusterHealthChangedEventArgs(ClusterHealthStatus oldStatus, ClusterHealthStatus newStatus)
    {
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }

    /// <summary>
    /// Gets the previous health status.
    /// </summary>
    public ClusterHealthStatus OldStatus { get; }

    /// <summary>
    /// Gets the new health status.
    /// </summary>
    public ClusterHealthStatus NewStatus { get; }
}
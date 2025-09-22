using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for managing cluster operations in a distributed actor system.
/// Provides cluster membership management, node discovery, and cluster monitoring.
/// </summary>
public interface IClusterService : IService
{
    /// <summary>
    /// Gets the current cluster member information for this node.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The cluster member information for this node.</returns>
    Task<ClusterMemberInfo> GetClusterMemberAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all cluster members currently in the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of all cluster member information.</returns>
    Task<IEnumerable<ClusterMemberInfo>> GetClusterMembersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Joins the cluster with the specified seed nodes.
    /// </summary>
    /// <param name="seedNodes">The seed nodes to join with.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the join operation.</returns>
    Task JoinClusterAsync(IEnumerable<string> seedNodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves the cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the leave operation.</returns>
    Task LeaveClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a node joins the cluster.
    /// </summary>
    event EventHandler<ClusterMemberJoinedEventArgs>? MemberJoined;

    /// <summary>
    /// Event raised when a node leaves the cluster.
    /// </summary>
    event EventHandler<ClusterMemberLeftEventArgs>? MemberLeft;

    /// <summary>
    /// Event raised when a node becomes unreachable.
    /// </summary>
    event EventHandler<ClusterMemberUnreachableEventArgs>? MemberUnreachable;
}

/// <summary>
/// Information about a cluster member node.
/// </summary>
public class ClusterMemberInfo
{
    public ClusterMemberInfo(string address, string nodeId, ClusterMemberStatus status, ISet<string> roles, DateTime joinedAt)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        Status = status;
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        JoinedAt = joinedAt;
    }

    public string Address { get; }
    public string NodeId { get; }
    public ClusterMemberStatus Status { get; }
    public ISet<string> Roles { get; }
    public DateTime JoinedAt { get; }
}

/// <summary>
/// Status of a cluster member.
/// </summary>
public enum ClusterMemberStatus
{
    Joining,
    WeaklyUp,
    Up,
    Leaving,
    Exiting,
    Down,
    Removed
}

/// <summary>
/// Event arguments for cluster member joined events.
/// </summary>
public class ClusterMemberJoinedEventArgs
{
    public ClusterMemberJoinedEventArgs(ClusterMemberInfo member)
    {
        Member = member ?? throw new ArgumentNullException(nameof(member));
    }

    public ClusterMemberInfo Member { get; }
}

/// <summary>
/// Event arguments for cluster member left events.
/// </summary>
public class ClusterMemberLeftEventArgs
{
    public ClusterMemberLeftEventArgs(ClusterMemberInfo member)
    {
        Member = member ?? throw new ArgumentNullException(nameof(member));
    }

    public ClusterMemberInfo Member { get; }
}

/// <summary>
/// Event arguments for cluster member unreachable events.
/// </summary>
public class ClusterMemberUnreachableEventArgs
{
    public ClusterMemberUnreachableEventArgs(ClusterMemberInfo member)
    {
        Member = member ?? throw new ArgumentNullException(nameof(member));
    }

    public ClusterMemberInfo Member { get; }
}
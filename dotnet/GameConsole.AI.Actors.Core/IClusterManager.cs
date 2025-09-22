namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Manages cluster operations and membership for the actor system.
/// </summary>
public interface IClusterManager
{
    /// <summary>
    /// Joins the cluster using the specified seed nodes.
    /// </summary>
    /// <param name="seedNodes">The seed node addresses to use for joining.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async join operation.</returns>
    Task JoinAsync(IEnumerable<string> seedNodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves the cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async leave operation.</returns>
    Task LeaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current cluster members.
    /// </summary>
    IEnumerable<ClusterNode> Members { get; }

    /// <summary>
    /// Gets the local cluster node information.
    /// </summary>
    ClusterNode? LocalNode { get; }

    /// <summary>
    /// Gets an actor reference for a sharded actor.
    /// </summary>
    /// <param name="entityId">The entity ID for sharding.</param>
    /// <param name="shardRegion">The shard region name.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the sharded actor reference.</returns>
    Task<IActorRef> GetShardActorAsync(string entityId, string shardRegion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when cluster membership changes.
    /// </summary>
    event EventHandler<ClusterMembershipChangedEventArgs>? MembershipChanged;
}

/// <summary>
/// Event arguments for cluster membership changes.
/// </summary>
public class ClusterMembershipChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the cluster node that changed.
    /// </summary>
    public required ClusterNode Node { get; init; }

    /// <summary>
    /// Gets the type of membership change.
    /// </summary>
    public ClusterMembershipChangeType ChangeType { get; init; }
}

/// <summary>
/// Types of cluster membership changes.
/// </summary>
public enum ClusterMembershipChangeType
{
    /// <summary>
    /// A node joined the cluster.
    /// </summary>
    NodeJoined,

    /// <summary>
    /// A node left the cluster.
    /// </summary>
    NodeLeft,

    /// <summary>
    /// A node became unreachable.
    /// </summary>
    NodeUnreachable,

    /// <summary>
    /// A node became reachable again.
    /// </summary>
    NodeReachable,

    /// <summary>
    /// A node was removed from the cluster.
    /// </summary>
    NodeRemoved
}
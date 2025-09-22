using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Information about a cluster member node.
/// </summary>
public class ClusterMember
{
    /// <summary>
    /// Gets or sets the unique identifier of the cluster member.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the hostname of the cluster member.
    /// </summary>
    public string Hostname { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the port of the cluster member.
    /// </summary>
    public int Port { get; set; }
    
    /// <summary>
    /// Gets or sets the current status of the cluster member.
    /// </summary>
    public ClusterMemberStatus Status { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp of the last heartbeat received from this member.
    /// </summary>
    public DateTimeOffset LastHeartbeat { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when this member joined the cluster.
    /// </summary>
    public DateTimeOffset JoinedAt { get; set; }
    
    /// <summary>
    /// Gets or sets additional metadata about the cluster member.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the actor system name of this cluster member.
    /// </summary>
    public string ActorSystemName { get; set; } = string.Empty;
}

/// <summary>
/// Status of a cluster member.
/// </summary>
public enum ClusterMemberStatus
{
    /// <summary>
    /// The member is joining the cluster.
    /// </summary>
    Joining,
    
    /// <summary>
    /// The member is active and participating in the cluster.
    /// </summary>
    Up,
    
    /// <summary>
    /// The member is leaving the cluster gracefully.
    /// </summary>
    Leaving,
    
    /// <summary>
    /// The member has left the cluster.
    /// </summary>
    Exited,
    
    /// <summary>
    /// The member is unreachable but not yet removed.
    /// </summary>
    Unreachable,
    
    /// <summary>
    /// The member has been removed from the cluster.
    /// </summary>
    Removed
}

/// <summary>
/// Event arguments for cluster membership events.
/// </summary>
public class ClusterMemberEventArgs : EventArgs
{
    /// <summary>
    /// Gets the cluster member information.
    /// </summary>
    public ClusterMember Member { get; }
    
    /// <summary>
    /// Gets the previous status of the member, if applicable.
    /// </summary>
    public ClusterMemberStatus? PreviousStatus { get; }

    /// <summary>
    /// Initializes a new instance of the ClusterMemberEventArgs class.
    /// </summary>
    /// <param name="member">The cluster member information.</param>
    /// <param name="previousStatus">The previous status of the member, if applicable.</param>
    public ClusterMemberEventArgs(ClusterMember member, ClusterMemberStatus? previousStatus = null)
    {
        Member = member ?? throw new ArgumentNullException(nameof(member));
        PreviousStatus = previousStatus;
    }
}

/// <summary>
/// Information about the cluster state and topology.
/// </summary>
public class ClusterState
{
    /// <summary>
    /// Gets or sets the cluster name.
    /// </summary>
    public string ClusterName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the current leader node, if any.
    /// </summary>
    public ClusterMember? Leader { get; set; }
    
    /// <summary>
    /// Gets or sets all members in the cluster.
    /// </summary>
    public List<ClusterMember> Members { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the unreachable members.
    /// </summary>
    public List<ClusterMember> UnreachableMembers { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the current node information.
    /// </summary>
    public ClusterMember? Self { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the cluster state was last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    
    /// <summary>
    /// Gets the number of active members in the cluster.
    /// </summary>
    public int ActiveMemberCount => Members.Count(m => m.Status == ClusterMemberStatus.Up);
    
    /// <summary>
    /// Gets whether this node is the cluster leader.
    /// </summary>
    public bool IsLeader => Leader != null && Self != null && Leader.NodeId == Self.NodeId;
}

/// <summary>
/// Tier 2: Actor cluster service interface for distributed actor system coordination.
/// Handles cluster membership, node discovery, and distributed messaging
/// to enable actor systems to work together across multiple nodes.
/// </summary>
public interface IActorCluster : IService, ICapabilityProvider
{
    /// <summary>
    /// Event raised when a new member joins the cluster.
    /// </summary>
    event EventHandler<ClusterMemberEventArgs>? MemberJoined;
    
    /// <summary>
    /// Event raised when a member leaves the cluster.
    /// </summary>
    event EventHandler<ClusterMemberEventArgs>? MemberLeft;
    
    /// <summary>
    /// Event raised when a member becomes unreachable.
    /// </summary>
    event EventHandler<ClusterMemberEventArgs>? MemberUnreachable;
    
    /// <summary>
    /// Event raised when a previously unreachable member becomes reachable again.
    /// </summary>
    event EventHandler<ClusterMemberEventArgs>? MemberReachable;
    
    /// <summary>
    /// Event raised when cluster leadership changes.
    /// </summary>
    event EventHandler<ClusterMemberEventArgs>? LeaderChanged;

    /// <summary>
    /// Gets the cluster configuration options.
    /// </summary>
    ClusterOptions Options { get; }
    
    /// <summary>
    /// Gets whether this node is currently connected to the cluster.
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Gets the current node information.
    /// </summary>
    ClusterMember? Self { get; }

    /// <summary>
    /// Joins the cluster using the configured seed nodes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async join operation.</returns>
    Task JoinClusterAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Leaves the cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async leave operation.</returns>
    Task LeaveClusterAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current cluster state and membership information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns cluster state.</returns>
    Task<ClusterState> GetClusterStateAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a message to an actor on a specific cluster member.
    /// </summary>
    /// <param name="targetMember">The target cluster member.</param>
    /// <param name="targetActor">The target actor address on that member.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="sender">The sender actor address, if any.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async send operation.</returns>
    Task SendToMemberAsync(ClusterMember targetMember, ActorAddress targetActor, IActorMessage message, ActorAddress? sender = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a message to an actor anywhere in the cluster using actor address resolution.
    /// </summary>
    /// <param name="targetActor">The target actor address.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="sender">The sender actor address, if any.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async send operation.</returns>
    Task SendToClusterAsync(ActorAddress targetActor, IActorMessage message, ActorAddress? sender = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Broadcasts a message to all members in the cluster.
    /// </summary>
    /// <param name="message">The message to broadcast.</param>
    /// <param name="sender">The sender actor address, if any.</param>
    /// <param name="excludeSelf">Whether to exclude this node from the broadcast.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async broadcast operation.</returns>
    Task BroadcastToClusterAsync(IActorMessage message, ActorAddress? sender = null, bool excludeSelf = true, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resolves which cluster member hosts the specified actor.
    /// </summary>
    /// <param name="actorAddress">The actor address to resolve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the hosting member, or null if not found.</returns>
    Task<ClusterMember?> ResolveActorLocationAsync(ActorAddress actorAddress, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers this node as hosting the specified actor for cluster-wide discovery.
    /// </summary>
    /// <param name="actorAddress">The actor address to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterActorAsync(ActorAddress actorAddress, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unregisters an actor from cluster-wide discovery.
    /// </summary>
    /// <param name="actorAddress">The actor address to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unregistration operation.</returns>
    Task UnregisterActorAsync(ActorAddress actorAddress, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all actors registered on the specified cluster member.
    /// </summary>
    /// <param name="member">The cluster member to query.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns actor addresses.</returns>
    Task<IEnumerable<ActorAddress>> GetMemberActorsAsync(ClusterMember member, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the metadata for this cluster member.
    /// </summary>
    /// <param name="metadata">The metadata to set.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateMemberMetadataAsync(Dictionary<string, object> metadata, CancellationToken cancellationToken = default);
}
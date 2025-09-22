using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Service for managing Akka.NET cluster functionality within the GameConsole architecture.
/// Handles cluster membership, member discovery, and cluster state management.
/// </summary>
public interface IClusterService : IService
{
    /// <summary>
    /// Gets the current cluster state.
    /// </summary>
    ClusterState State { get; }

    /// <summary>
    /// Gets a value indicating whether this node is part of a cluster.
    /// </summary>
    bool IsInCluster { get; }

    /// <summary>
    /// Gets a value indicating whether this node is the cluster leader.
    /// </summary>
    bool IsLeader { get; }

    /// <summary>
    /// Gets the cluster members currently known to this node.
    /// </summary>
    IReadOnlyCollection<ClusterMember> Members { get; }

    /// <summary>
    /// Joins the cluster at the specified address.
    /// </summary>
    /// <param name="seedNodes">The seed node addresses to join.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async join operation.</returns>
    Task JoinClusterAsync(IEnumerable<string> seedNodes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves the cluster gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async leave operation.</returns>
    Task LeaveClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the cluster state changes.
    /// </summary>
    event EventHandler<ClusterStateChangedEventArgs>? ClusterStateChanged;

    /// <summary>
    /// Event raised when a member joins the cluster.
    /// </summary>
    event EventHandler<ClusterMemberEventArgs>? MemberJoined;

    /// <summary>
    /// Event raised when a member leaves the cluster.
    /// </summary>
    event EventHandler<ClusterMemberEventArgs>? MemberLeft;

    /// <summary>
    /// Event raised when a member is unreachable.
    /// </summary>
    event EventHandler<ClusterMemberEventArgs>? MemberUnreachable;

    /// <summary>
    /// Event raised when a member becomes reachable again.
    /// </summary>
    event EventHandler<ClusterMemberEventArgs>? MemberReachable;
}

/// <summary>
/// Represents the state of a cluster.
/// </summary>
public enum ClusterState
{
    /// <summary>
    /// The node is not part of any cluster.
    /// </summary>
    NotInCluster,

    /// <summary>
    /// The node is joining a cluster.
    /// </summary>
    Joining,

    /// <summary>
    /// The node is an active member of the cluster.
    /// </summary>
    Up,

    /// <summary>
    /// The node is leaving the cluster.
    /// </summary>
    Leaving,

    /// <summary>
    /// The node has left the cluster.
    /// </summary>
    Exiting,

    /// <summary>
    /// The node is unreachable from other cluster members.
    /// </summary>
    Unreachable,

    /// <summary>
    /// The node has been removed from the cluster.
    /// </summary>
    Removed
}

/// <summary>
/// Represents a member of the cluster.
/// </summary>
public class ClusterMember
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterMember"/> class.
    /// </summary>
    /// <param name="address">The address of the member.</param>
    /// <param name="roles">The roles assigned to the member.</param>
    /// <param name="state">The current state of the member.</param>
    public ClusterMember(string address, IReadOnlyCollection<string> roles, ClusterState state)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        State = state;
    }

    /// <summary>
    /// Gets the address of the cluster member.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Gets the roles assigned to this cluster member.
    /// </summary>
    public IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Gets the current state of the cluster member.
    /// </summary>
    public ClusterState State { get; }

    /// <summary>
    /// Gets a value indicating whether this member has the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the member has the role; otherwise, false.</returns>
    public bool HasRole(string role) => Roles.Contains(role);
}

/// <summary>
/// Event arguments for cluster state change events.
/// </summary>
public class ClusterStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterStateChangedEventArgs"/> class.
    /// </summary>
    /// <param name="previousState">The previous cluster state.</param>
    /// <param name="newState">The new cluster state.</param>
    public ClusterStateChangedEventArgs(ClusterState previousState, ClusterState newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }

    /// <summary>
    /// Gets the previous cluster state.
    /// </summary>
    public ClusterState PreviousState { get; }

    /// <summary>
    /// Gets the new cluster state.
    /// </summary>
    public ClusterState NewState { get; }
}

/// <summary>
/// Event arguments for cluster member events.
/// </summary>
public class ClusterMemberEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterMemberEventArgs"/> class.
    /// </summary>
    /// <param name="member">The cluster member.</param>
    public ClusterMemberEventArgs(ClusterMember member)
    {
        Member = member ?? throw new ArgumentNullException(nameof(member));
    }

    /// <summary>
    /// Gets the cluster member associated with the event.
    /// </summary>
    public ClusterMember Member { get; }
}
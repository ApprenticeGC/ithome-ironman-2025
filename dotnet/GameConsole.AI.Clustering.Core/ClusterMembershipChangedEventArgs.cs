namespace GameConsole.AI.Clustering.Core;

/// <summary>
/// Event arguments for cluster membership change events.
/// </summary>
public class ClusterMembershipChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterMembershipChangedEventArgs"/> class.
    /// </summary>
    /// <param name="member">The cluster member that changed.</param>
    /// <param name="previousStatus">The previous status of the member.</param>
    /// <param name="newStatus">The new status of the member.</param>
    public ClusterMembershipChangedEventArgs(ClusterMember member, ClusterMemberStatus? previousStatus, ClusterMemberStatus newStatus)
    {
        Member = member ?? throw new ArgumentNullException(nameof(member));
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
    }
    
    /// <summary>
    /// Gets the cluster member that changed.
    /// </summary>
    public ClusterMember Member { get; }
    
    /// <summary>
    /// Gets the previous status of the member, or null if this is a new member.
    /// </summary>
    public ClusterMemberStatus? PreviousStatus { get; }
    
    /// <summary>
    /// Gets the new status of the member.
    /// </summary>
    public ClusterMemberStatus NewStatus { get; }
    
    /// <summary>
    /// Gets a value indicating whether this is a new member joining.
    /// </summary>
    public bool IsJoining => PreviousStatus == null && NewStatus == ClusterMemberStatus.Joining;
    
    /// <summary>
    /// Gets a value indicating whether this member became active.
    /// </summary>
    public bool IsUp => NewStatus == ClusterMemberStatus.Up;
    
    /// <summary>
    /// Gets a value indicating whether this member is leaving.
    /// </summary>
    public bool IsLeaving => NewStatus == ClusterMemberStatus.Leaving;
    
    /// <summary>
    /// Gets a value indicating whether this member was removed.
    /// </summary>
    public bool IsRemoved => NewStatus == ClusterMemberStatus.Removed;
}
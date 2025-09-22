namespace GameConsole.AI.Clustering.Core;

/// <summary>
/// Represents the current state of the AI agent actor cluster.
/// </summary>
public class ClusterState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterState"/> class.
    /// </summary>
    /// <param name="members">The current cluster members.</param>
    /// <param name="leader">The current leader of the cluster, if any.</param>
    /// <param name="isLeader">A value indicating whether this node is the cluster leader.</param>
    /// <param name="isClusterFormed">A value indicating whether the cluster is formed and ready.</param>
    /// <param name="unreachableCount">The number of unreachable members in the cluster.</param>
    /// <param name="lastUpdated">The timestamp when the cluster state was last updated.</param>
    public ClusterState(
        IReadOnlyList<ClusterMember> members,
        ClusterMember? leader,
        bool isLeader,
        bool isClusterFormed,
        int unreachableCount,
        DateTime lastUpdated)
    {
        Members = members ?? throw new ArgumentNullException(nameof(members));
        Leader = leader;
        IsLeader = isLeader;
        IsClusterFormed = isClusterFormed;
        UnreachableCount = unreachableCount;
        LastUpdated = lastUpdated;
    }
    
    /// <summary>
    /// Gets the current cluster members.
    /// </summary>
    public IReadOnlyList<ClusterMember> Members { get; }
    
    /// <summary>
    /// Gets the current leader of the cluster, if any.
    /// </summary>
    public ClusterMember? Leader { get; }
    
    /// <summary>
    /// Gets a value indicating whether this node is the cluster leader.
    /// </summary>
    public bool IsLeader { get; }
    
    /// <summary>
    /// Gets a value indicating whether the cluster is formed and ready.
    /// </summary>
    public bool IsClusterFormed { get; }
    
    /// <summary>
    /// Gets the number of unreachable members in the cluster.
    /// </summary>
    public int UnreachableCount { get; }
    
    /// <summary>
    /// Gets the timestamp when the cluster state was last updated.
    /// </summary>
    public DateTime LastUpdated { get; }
}
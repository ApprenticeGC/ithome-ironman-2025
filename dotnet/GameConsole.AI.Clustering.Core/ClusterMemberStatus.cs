namespace GameConsole.AI.Clustering.Core;

/// <summary>
/// Represents the status of a cluster member in the AI agent actor system.
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
    /// The member is leaving the cluster.
    /// </summary>
    Leaving,
    
    /// <summary>
    /// The member has been removed from the cluster.
    /// </summary>
    Removed,
    
    /// <summary>
    /// The member is unreachable from the current node.
    /// </summary>
    Unreachable
}
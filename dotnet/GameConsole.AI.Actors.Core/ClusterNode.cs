namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Represents information about a cluster node.
/// </summary>
public record ClusterNode
{
    /// <summary>
    /// Gets the unique address of the cluster node.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets the status of the cluster node.
    /// </summary>
    public ClusterNodeStatus Status { get; init; }

    /// <summary>
    /// Gets the roles assigned to this cluster node.
    /// </summary>
    public IReadOnlyList<string> Roles { get; init; } = new List<string>();

    /// <summary>
    /// Gets the timestamp when this node information was last updated.
    /// </summary>
    public DateTime LastSeen { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents the status of a cluster node.
/// </summary>
public enum ClusterNodeStatus
{
    /// <summary>
    /// Node is joining the cluster.
    /// </summary>
    Joining,

    /// <summary>
    /// Node is an active member of the cluster.
    /// </summary>
    Up,

    /// <summary>
    /// Node is down but may rejoin.
    /// </summary>
    Down,

    /// <summary>
    /// Node is leaving the cluster gracefully.
    /// </summary>
    Leaving,

    /// <summary>
    /// Node has left the cluster.
    /// </summary>
    Exiting,

    /// <summary>
    /// Node is unreachable and may have failed.
    /// </summary>
    Unreachable,

    /// <summary>
    /// Node has been removed from the cluster.
    /// </summary>
    Removed
}
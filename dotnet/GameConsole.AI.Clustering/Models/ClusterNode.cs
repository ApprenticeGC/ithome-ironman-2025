namespace GameConsole.AI.Clustering.Models;

/// <summary>
/// Represents a node in the AI cluster with its capabilities and status.
/// </summary>
public record ClusterNode
{
    /// <summary>
    /// Gets the unique identifier for this node.
    /// </summary>
    public required string NodeId { get; init; }

    /// <summary>
    /// Gets the network address of this node.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets the port number for cluster communication.
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// Gets the list of AI agent capabilities this node supports.
    /// </summary>
    public required IReadOnlyList<AgentCapability> Capabilities { get; init; }

    /// <summary>
    /// Gets the current health status of this node.
    /// </summary>
    public required NodeHealth Health { get; init; }

    /// <summary>
    /// Gets the timestamp when this node joined the cluster.
    /// </summary>
    public DateTime JoinedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the timestamp when this node was last seen.
    /// </summary>
    public DateTime LastSeenAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets additional metadata about this node.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents the health status of a cluster node.
/// </summary>
public enum NodeHealth
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy,
    Offline
}
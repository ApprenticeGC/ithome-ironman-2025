namespace GameConsole.Core.Abstractions;

/// <summary>
/// Configuration settings for cluster operations.
/// </summary>
public class ClusterConfiguration
{
    /// <summary>
    /// Gets or sets the cluster name.
    /// </summary>
    public string ClusterName { get; set; } = "GameConsole";

    /// <summary>
    /// Gets or sets the node hostname.
    /// </summary>
    public string Hostname { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the node port.
    /// </summary>
    public int Port { get; set; } = 2552;

    /// <summary>
    /// Gets or sets the seed nodes for cluster discovery.
    /// </summary>
    public IList<string> SeedNodes { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the roles this node should participate in.
    /// </summary>
    public ISet<string> Roles { get; set; } = new HashSet<string>();

    /// <summary>
    /// Gets or sets the minimum number of members required for the cluster to be operational.
    /// </summary>
    public int MinimumMembers { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timeout for cluster join operations.
    /// </summary>
    public TimeSpan JoinTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the interval for heartbeat messages.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the timeout for considering a node unreachable.
    /// </summary>
    public TimeSpan UnreachableTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets whether this node can be a cluster leader.
    /// </summary>
    public bool CanBeLeader { get; set; } = true;

    /// <summary>
    /// Gets or sets additional metadata for this cluster node.
    /// </summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}
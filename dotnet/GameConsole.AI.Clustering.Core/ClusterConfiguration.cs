namespace GameConsole.AI.Clustering.Core;

/// <summary>
/// Represents the configuration for AI agent actor clustering.
/// </summary>
public class ClusterConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterConfiguration"/> class.
    /// </summary>
    /// <param name="bindAddress">The address for this node to bind to.</param>
    /// <param name="bindPort">The port for this node to bind to.</param>
    /// <param name="seedNodes">The seed nodes for cluster formation.</param>
    /// <param name="roles">The roles for this node in the cluster.</param>
    /// <param name="clusterName">The cluster name.</param>
    /// <param name="minClusterSize">The minimum number of members required before the cluster is considered formed.</param>
    public ClusterConfiguration(
        string bindAddress = "127.0.0.1",
        int bindPort = 2551,
        IReadOnlyList<string>? seedNodes = null,
        IReadOnlyCollection<string>? roles = null,
        string clusterName = "GameConsole-AI-Cluster",
        int minClusterSize = 1)
    {
        BindAddress = bindAddress ?? throw new ArgumentNullException(nameof(bindAddress));
        BindPort = bindPort;
        SeedNodes = seedNodes ?? Array.Empty<string>();
        Roles = roles ?? Array.Empty<string>();
        ClusterName = clusterName ?? throw new ArgumentNullException(nameof(clusterName));
        MinClusterSize = minClusterSize;
    }
    
    /// <summary>
    /// Gets the address for this node to bind to.
    /// </summary>
    public string BindAddress { get; }
    
    /// <summary>
    /// Gets the port for this node to bind to.
    /// </summary>
    public int BindPort { get; }
    
    /// <summary>
    /// Gets the seed nodes for cluster formation.
    /// </summary>
    public IReadOnlyList<string> SeedNodes { get; }
    
    /// <summary>
    /// Gets the roles for this node in the cluster.
    /// </summary>
    public IReadOnlyCollection<string> Roles { get; }
    
    /// <summary>
    /// Gets the cluster name.
    /// </summary>
    public string ClusterName { get; }
    
    /// <summary>
    /// Gets the minimum number of members required before the cluster is considered formed.
    /// </summary>
    public int MinClusterSize { get; }
}
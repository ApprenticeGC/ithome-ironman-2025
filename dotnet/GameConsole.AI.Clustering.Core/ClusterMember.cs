namespace GameConsole.AI.Clustering.Core;

/// <summary>
/// Represents a member of the AI agent actor cluster.
/// </summary>
public class ClusterMember
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterMember"/> class.
    /// </summary>
    /// <param name="nodeId">The unique identifier for this cluster member.</param>
    /// <param name="address">The address of this cluster member.</param>
    /// <param name="port">The port of this cluster member.</param>
    /// <param name="status">The current status of this cluster member.</param>
    /// <param name="roles">The roles assigned to this cluster member.</param>
    /// <param name="joinedAt">The timestamp when this member joined the cluster.</param>
    public ClusterMember(string nodeId, string address, int port, ClusterMemberStatus status, IReadOnlyCollection<string> roles, DateTime joinedAt)
    {
        NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Port = port;
        Status = status;
        Roles = roles ?? throw new ArgumentNullException(nameof(roles));
        JoinedAt = joinedAt;
    }
    
    /// <summary>
    /// Gets the unique identifier for this cluster member.
    /// </summary>
    public string NodeId { get; }
    
    /// <summary>
    /// Gets the address of this cluster member.
    /// </summary>
    public string Address { get; }
    
    /// <summary>
    /// Gets the port of this cluster member.
    /// </summary>
    public int Port { get; }
    
    /// <summary>
    /// Gets the current status of this cluster member.
    /// </summary>
    public ClusterMemberStatus Status { get; }
    
    /// <summary>
    /// Gets the roles assigned to this cluster member.
    /// </summary>
    public IReadOnlyCollection<string> Roles { get; }
    
    /// <summary>
    /// Gets the timestamp when this member joined the cluster.
    /// </summary>
    public DateTime JoinedAt { get; }
}
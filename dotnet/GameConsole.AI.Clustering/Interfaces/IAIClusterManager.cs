using GameConsole.Core.Abstractions;
using GameConsole.AI.Clustering.Models;

namespace GameConsole.AI.Clustering.Interfaces;

/// <summary>
/// Interface for managing AI cluster coordination and configuration.
/// Handles cluster formation, node discovery, and cluster-wide configuration management.
/// </summary>
public interface IAIClusterManager : IService
{
    /// <summary>
    /// Gets the current cluster configuration.
    /// </summary>
    ClusterConfiguration Configuration { get; }

    /// <summary>
    /// Initializes the cluster with the specified configuration.
    /// </summary>
    /// <param name="configuration">The cluster configuration to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeClusterAsync(ClusterConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forms a new cluster or joins an existing one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster formation operation.</returns>
    Task FormClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gracefully leaves the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async leave operation.</returns>
    Task LeaveClusterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all nodes currently in the cluster.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the list of cluster nodes.</returns>
    Task<IReadOnlyList<ClusterNode>> GetClusterNodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new node to the cluster.
    /// </summary>
    /// <param name="node">The node to add to the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async add operation.</returns>
    Task AddNodeAsync(ClusterNode node, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a node from the cluster.
    /// </summary>
    /// <param name="nodeId">The ID of the node to remove.</param>
    /// <param name="graceful">Whether to perform a graceful removal.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async remove operation.</returns>
    Task RemoveNodeAsync(string nodeId, bool graceful = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the cluster configuration.
    /// </summary>
    /// <param name="configuration">The new cluster configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration update operation.</returns>
    Task UpdateConfigurationAsync(ClusterConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a node joins the cluster.
    /// </summary>
    event EventHandler<ClusterNode>? NodeJoined;

    /// <summary>
    /// Event raised when a node leaves the cluster.
    /// </summary>
    event EventHandler<string>? NodeLeft;

    /// <summary>
    /// Event raised when cluster configuration changes.
    /// </summary>
    event EventHandler<ClusterConfiguration>? ConfigurationChanged;
}
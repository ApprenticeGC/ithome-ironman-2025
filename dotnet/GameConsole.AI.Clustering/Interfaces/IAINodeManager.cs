using GameConsole.Core.Abstractions;
using GameConsole.AI.Clustering.Models;

namespace GameConsole.AI.Clustering.Interfaces;

/// <summary>
/// Interface for managing individual cluster nodes.
/// Handles node health, status, and local resource management.
/// </summary>
public interface IAINodeManager : IService
{
    /// <summary>
    /// Gets the current node information.
    /// </summary>
    ClusterNode CurrentNode { get; }

    /// <summary>
    /// Gets whether this node is currently part of a cluster.
    /// </summary>
    bool IsClusterMember { get; }

    /// <summary>
    /// Initializes this node with the specified configuration.
    /// </summary>
    /// <param name="nodeId">The unique identifier for this node.</param>
    /// <param name="address">The network address for this node.</param>
    /// <param name="port">The port for cluster communication.</param>
    /// <param name="capabilities">The AI capabilities this node provides.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeNodeAsync(string nodeId, string address, int port, IReadOnlyList<AgentCapability> capabilities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the capabilities of this node.
    /// </summary>
    /// <param name="capabilities">The new capabilities for this node.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateCapabilitiesAsync(IReadOnlyList<AgentCapability> capabilities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current health status of this node.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the node's health status.</returns>
    Task<NodeHealth> GetHealthStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on this node.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the health check results.</returns>
    Task<NodeHealthCheckResult> PerformHealthCheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current resource utilization of this node.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task containing the resource utilization metrics.</returns>
    Task<NodeResourceUtilization> GetResourceUtilizationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Prepares this node for graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async shutdown preparation.</returns>
    Task PrepareShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the node's health status changes.
    /// </summary>
    event EventHandler<NodeHealth>? HealthStatusChanged;

    /// <summary>
    /// Event raised when the node's capabilities change.
    /// </summary>
    event EventHandler<IReadOnlyList<AgentCapability>>? CapabilitiesChanged;

    /// <summary>
    /// Event raised when the node's resource utilization changes significantly.
    /// </summary>
    event EventHandler<NodeResourceUtilization>? ResourceUtilizationChanged;
}

/// <summary>
/// Represents the result of a node health check.
/// </summary>
public class NodeHealthCheckResult
{
    /// <summary>
    /// Gets the overall health status determined by the check.
    /// </summary>
    public required NodeHealth Status { get; init; }

    /// <summary>
    /// Gets any issues found during the health check.
    /// </summary>
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets performance metrics captured during the check.
    /// </summary>
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();

    /// <summary>
    /// Gets the timestamp when this health check was performed.
    /// </summary>
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents resource utilization metrics for a node.
/// </summary>
public class NodeResourceUtilization
{
    /// <summary>
    /// Gets the CPU utilization percentage (0-100).
    /// </summary>
    public double CpuUtilization { get; init; }

    /// <summary>
    /// Gets the memory utilization percentage (0-100).
    /// </summary>
    public double MemoryUtilization { get; init; }

    /// <summary>
    /// Gets the disk utilization percentage (0-100).
    /// </summary>
    public double DiskUtilization { get; init; }

    /// <summary>
    /// Gets the network utilization percentage (0-100).
    /// </summary>
    public double NetworkUtilization { get; init; }

    /// <summary>
    /// Gets the number of active AI agent instances.
    /// </summary>
    public int ActiveAgentInstances { get; init; }

    /// <summary>
    /// Gets the timestamp when these metrics were captured.
    /// </summary>
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
}
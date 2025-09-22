namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for a cluster of AI actors that coordinate behaviors.
/// </summary>
public interface ICluster
{
    /// <summary>
    /// Unique identifier for the cluster.
    /// </summary>
    string ClusterId { get; }

    /// <summary>
    /// Type of clustering strategy used (e.g., "spatial", "behavioral").
    /// </summary>
    string ClusterType { get; }

    /// <summary>
    /// List of AI actor IDs in this cluster.
    /// </summary>
    IReadOnlyList<string> ActorIds { get; }

    /// <summary>
    /// Center position of the cluster.
    /// </summary>
    (float X, float Y, float Z) CenterPosition { get; }

    /// <summary>
    /// Maximum radius of the cluster.
    /// </summary>
    float Radius { get; }

    /// <summary>
    /// Whether the cluster is currently active and processing.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Adds an AI actor to this cluster.
    /// </summary>
    /// <param name="actor">The AI actor to add.</param>
    /// <returns>True if the actor was successfully added, false otherwise.</returns>
    bool AddActor(IAIActor actor);

    /// <summary>
    /// Removes an AI actor from this cluster.
    /// </summary>
    /// <param name="actorId">ID of the actor to remove.</param>
    /// <returns>True if the actor was successfully removed, false otherwise.</returns>
    bool RemoveActor(string actorId);

    /// <summary>
    /// Updates the cluster's state based on current actor positions and behaviors.
    /// </summary>
    void UpdateCluster();

    /// <summary>
    /// Checks if the cluster should be dissolved (e.g., too few members, too spread out).
    /// </summary>
    /// <returns>True if the cluster should be dissolved, false otherwise.</returns>
    bool ShouldDissolve();
}
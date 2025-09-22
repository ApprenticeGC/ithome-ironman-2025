namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for AI-controlled actors that can participate in clustering.
/// </summary>
public interface IAIActor
{
    /// <summary>
    /// Unique identifier for the AI actor.
    /// </summary>
    string ActorId { get; }

    /// <summary>
    /// Current position of the AI actor in game world coordinates.
    /// </summary>
    (float X, float Y, float Z) Position { get; }

    /// <summary>
    /// Current behavior type of the AI actor (e.g., "patrol", "guard", "follow").
    /// </summary>
    string BehaviorType { get; }

    /// <summary>
    /// Whether this actor is currently available for clustering.
    /// </summary>
    bool IsClusteringEnabled { get; }

    /// <summary>
    /// Current cluster ID if the actor is part of a cluster, null otherwise.
    /// </summary>
    string? CurrentClusterId { get; set; }

    /// <summary>
    /// Called when the actor joins a cluster.
    /// </summary>
    /// <param name="clusterId">ID of the cluster being joined.</param>
    void OnJoinCluster(string clusterId);

    /// <summary>
    /// Called when the actor leaves a cluster.
    /// </summary>
    /// <param name="clusterId">ID of the cluster being left.</param>
    void OnLeaveCluster(string clusterId);
}
namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for the AI clustering service that manages clusters of AI actors.
/// </summary>
public interface IClusteringService : IService
{
    /// <summary>
    /// Registers an AI actor for potential clustering.
    /// </summary>
    /// <param name="actor">The AI actor to register.</param>
    void RegisterActor(IAIActor actor);

    /// <summary>
    /// Unregisters an AI actor from clustering.
    /// </summary>
    /// <param name="actorId">ID of the actor to unregister.</param>
    void UnregisterActor(string actorId);

    /// <summary>
    /// Gets all currently active clusters.
    /// </summary>
    /// <returns>Read-only collection of active clusters.</returns>
    IReadOnlyCollection<ICluster> GetActiveClusters();

    /// <summary>
    /// Gets the cluster that contains the specified actor, if any.
    /// </summary>
    /// <param name="actorId">ID of the actor to find.</param>
    /// <returns>The cluster containing the actor, or null if not clustered.</returns>
    ICluster? GetClusterForActor(string actorId);

    /// <summary>
    /// Forces a recalculation of all clusters.
    /// </summary>
    void RecalculateClusters();

    /// <summary>
    /// Gets clustering statistics for monitoring and debugging.
    /// </summary>
    /// <returns>Statistics about current clustering state.</returns>
    ClusteringStats GetStats();
}

/// <summary>
/// Statistics about the current clustering state.
/// </summary>
public record ClusteringStats(
    int TotalActors,
    int ClusteredActors,
    int ActiveClusters,
    int UnclusteredActors,
    double AverageClusterSize,
    TimeSpan LastUpdateDuration);
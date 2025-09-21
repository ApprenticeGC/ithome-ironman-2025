namespace GameConsole.ECS.Core;

/// <summary>
/// Interface for querying entities with specific component combinations.
/// Provides efficient access to entities that match certain criteria.
/// </summary>
public interface IEntityQuery : IAsyncDisposable
{
    /// <summary>
    /// Gets all entities that match this query.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the matching entities.</returns>
    Task<IReadOnlyList<IEntity>> GetEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of entities that match this query.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the count of matching entities.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the first entity that matches this query, or null if none found.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the first matching entity or null.</returns>
    Task<IEntity?> GetFirstAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entities match this query.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if any entities match.</returns>
    Task<bool> HasAnyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the component types that this query requires entities to have.
    /// </summary>
    IReadOnlySet<Type> RequiredComponents { get; }

    /// <summary>
    /// Gets the component types that this query requires entities to NOT have.
    /// </summary>
    IReadOnlySet<Type> ExcludedComponents { get; }
}
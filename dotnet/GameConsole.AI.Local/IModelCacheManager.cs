namespace GameConsole.AI.Local;

/// <summary>
/// Model cache manager for local model storage and retrieval.
/// Handles efficient storage, caching, and retrieval of AI models with
/// memory management and invalidation strategies.
/// </summary>
public interface IModelCacheManager
{
    /// <summary>
    /// Caches a model for efficient retrieval with optional compression and optimization.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="modelData">Model data to cache.</param>
    /// <param name="metadata">Model metadata including size, format, and optimization settings.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async caching operation.</returns>
    Task CacheModelAsync(string modelId, Stream modelData, ModelMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a cached model by identifier with automatic decompression if needed.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns cached model data.</returns>
    Task<CachedModel?> RetrieveModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates and removes a model from the cache to free up storage space.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async invalidation operation.</returns>
    Task InvalidateModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics including storage usage, hit rates, and model counts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns cache statistics.</returns>
    Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the entire cache and frees all storage space.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cache clearing operation.</returns>
    Task ClearCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs cache maintenance including cleanup of expired models and optimization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async maintenance operation.</returns>
    Task PerformMaintenanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model is currently cached and available for retrieval.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns availability status.</returns>
    Task<bool> IsModelCachedAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all cached model identifiers and their basic information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns cached model information.</returns>
    Task<IEnumerable<CachedModelInfo>> GetCachedModelsAsync(CancellationToken cancellationToken = default);
}
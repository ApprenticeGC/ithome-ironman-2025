using GameConsole.Core.Abstractions;

namespace GameConsole.ECS.Core;

/// <summary>
/// Capability interface for ECS worlds that support component pooling for memory efficiency.
/// </summary>
public interface IComponentPoolingCapability : ICapabilityProvider
{
    /// <summary>
    /// Configures pooling for a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to configure pooling for.</typeparam>
    /// <param name="initialSize">Initial pool size.</param>
    /// <param name="maxSize">Maximum pool size (0 for unlimited).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration operation.</returns>
    Task ConfigurePoolingAsync<T>(int initialSize = 16, int maxSize = 0, CancellationToken cancellationToken = default)
        where T : class, IComponent;

    /// <summary>
    /// Gets pooling statistics for a component type.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns pooling statistics.</returns>
    Task<ComponentPoolStats> GetPoolStatsAsync<T>(CancellationToken cancellationToken = default)
        where T : class, IComponent;

    /// <summary>
    /// Clears unused pooled components to free memory.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cleanup operation.</returns>
    Task CleanupPoolsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for ECS worlds that support performance profiling and benchmarking.
/// </summary>
public interface IECSProfilingCapability : ICapabilityProvider
{
    /// <summary>
    /// Enables or disables performance profiling.
    /// </summary>
    /// <param name="enabled">Whether profiling should be enabled.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetProfilingEnabledAsync(bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance statistics for the world.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns performance statistics.</returns>
    Task<ECSPerformanceStats> GetPerformanceStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance statistics for a specific system.
    /// </summary>
    /// <param name="system">The system to get stats for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns system performance statistics.</returns>
    Task<SystemPerformanceStats> GetSystemStatsAsync(ISystem system, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all collected performance statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async reset operation.</returns>
    Task ResetStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for ECS worlds that support serialization and persistence.
/// </summary>
public interface IECSSerializationCapability : ICapabilityProvider
{
    /// <summary>
    /// Serializes the world state to a stream.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="format">The serialization format.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async serialization operation.</returns>
    Task SerializeAsync(Stream stream, SerializationFormat format = SerializationFormat.Binary, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deserializes world state from a stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="format">The serialization format.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async deserialization operation.</returns>
    Task DeserializeAsync(Stream stream, SerializationFormat format = SerializationFormat.Binary, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the world state to a file.
    /// </summary>
    /// <param name="filePath">The file path to save to.</param>
    /// <param name="format">The serialization format.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async save operation.</returns>
    Task SaveWorldAsync(string filePath, SerializationFormat format = SerializationFormat.Binary, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads world state from a file.
    /// </summary>
    /// <param name="filePath">The file path to load from.</param>
    /// <param name="format">The serialization format.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async load operation.</returns>
    Task LoadWorldAsync(string filePath, SerializationFormat format = SerializationFormat.Binary, CancellationToken cancellationToken = default);
}
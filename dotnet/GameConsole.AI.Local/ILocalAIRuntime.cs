using GameConsole.Core.Abstractions;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GameConsole.AI.Local.Tests")]

namespace GameConsole.AI.Local;

/// <summary>
/// Interface for local AI runtime execution and management.
/// Provides unified access to local AI model execution with resource management.
/// </summary>
public interface ILocalAIRuntime : IService
{
    /// <summary>
    /// Gets the resource manager for GPU/CPU allocation and monitoring.
    /// </summary>
    IAIResourceManager ResourceManager { get; }

    /// <summary>
    /// Gets the model cache manager for local model storage.
    /// </summary>
    IModelCacheManager ModelCache { get; }

    /// <summary>
    /// Gets the inference engine for AI model execution.
    /// </summary>
    ILocalInferenceEngine InferenceEngine { get; }

    /// <summary>
    /// Gets the current execution provider being used.
    /// </summary>
    ExecutionProvider CurrentExecutionProvider { get; }

    /// <summary>
    /// Gets real-time performance metrics.
    /// </summary>
    InferenceMetrics CurrentMetrics { get; }

    /// <summary>
    /// Loads a model asynchronously with optional quantization.
    /// </summary>
    /// <param name="modelPath">Path to the model file.</param>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="quantizationConfig">Optional quantization configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async load operation.</returns>
    Task LoadModelAsync(string modelPath, string modelId, QuantizationConfig? quantizationConfig = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a model from memory.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model to unload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async unload operation.</returns>
    Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes inference on a loaded model.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="input">Input data for inference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the inference results.</returns>
    Task<object> InferAsync(string modelId, object input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes batch inference for improved throughput.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="inputs">Batch of input data for inference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the batch inference results.</returns>
    Task<IEnumerable<object>> InferBatchAsync(string modelId, IEnumerable<object> inputs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets resource constraints for AI operations.
    /// </summary>
    /// <param name="constraints">Resource constraints to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetResourceConstraintsAsync(ResourceConstraints constraints, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available execution providers on the current system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing available execution providers.</returns>
    Task<IEnumerable<ExecutionProvider>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for managing AI computational resources.
/// </summary>
public interface IAIResourceManager
{
    /// <summary>
    /// Gets the current resource utilization metrics.
    /// </summary>
    ResourceMetrics CurrentUtilization { get; }

    /// <summary>
    /// Gets the configured resource constraints.
    /// </summary>
    ResourceConstraints Constraints { get; }

    /// <summary>
    /// Allocates resources for an AI operation.
    /// </summary>
    /// <param name="requiredMemoryBytes">Required memory in bytes.</param>
    /// <param name="estimatedDurationMs">Estimated operation duration in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing resource allocation result.</returns>
    Task<ResourceAllocation> AllocateResourcesAsync(long requiredMemoryBytes, double estimatedDurationMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases previously allocated resources.
    /// </summary>
    /// <param name="allocation">Resource allocation to release.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task ReleaseResourcesAsync(ResourceAllocation allocation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors resource usage and triggers cleanup when necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the monitoring operation.</returns>
    Task MonitorResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes resource allocation based on usage patterns.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the optimization operation.</returns>
    Task OptimizeAllocationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for managing cached AI models.
/// </summary>
public interface IModelCacheManager
{
    /// <summary>
    /// Gets the total cache size in bytes.
    /// </summary>
    long TotalCacheSize { get; }

    /// <summary>
    /// Gets the maximum cache size in bytes.
    /// </summary>
    long MaxCacheSize { get; }

    /// <summary>
    /// Gets the number of cached models.
    /// </summary>
    int CachedModelCount { get; }

    /// <summary>
    /// Caches a model for fast access.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="modelPath">Path to the model file.</param>
    /// <param name="metadata">Optional model metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the caching operation.</returns>
    Task CacheModelAsync(string modelId, string modelPath, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a cached model.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the cached model data, or null if not found.</returns>
    Task<byte[]?> GetCachedModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a model from cache.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the removal operation.</returns>
    Task RemoveModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all cached models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the clear operation.</returns>
    Task ClearCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model exists in cache.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing true if model is cached, false otherwise.</returns>
    Task<bool> IsModelCachedAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets metadata for a cached model.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing model metadata, or null if not found.</returns>
    Task<Dictionary<string, object>?> GetModelMetadataAsync(string modelId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for local AI inference engine operations.
/// </summary>
public interface ILocalInferenceEngine
{
    /// <summary>
    /// Gets the supported model formats.
    /// </summary>
    IEnumerable<string> SupportedFormats { get; }

    /// <summary>
    /// Gets the current batch processing configuration.
    /// </summary>
    BatchConfiguration BatchConfig { get; }

    /// <summary>
    /// Creates an inference session for a model.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="modelData">Model data bytes.</param>
    /// <param name="executionProvider">Execution provider to use.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing the inference session.</returns>
    Task<IInferenceSession> CreateSessionAsync(string modelId, byte[] modelData, ExecutionProvider executionProvider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes inference using a session.
    /// </summary>
    /// <param name="session">Inference session to use.</param>
    /// <param name="input">Input data for inference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing inference results.</returns>
    Task<object> ExecuteAsync(IInferenceSession session, object input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes batch inference for improved throughput.
    /// </summary>
    /// <param name="session">Inference session to use.</param>
    /// <param name="inputs">Batch of inputs for inference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing batch inference results.</returns>
    Task<IEnumerable<object>> ExecuteBatchAsync(IInferenceSession session, IEnumerable<object> inputs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures batch processing parameters.
    /// </summary>
    /// <param name="config">Batch configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the configuration operation.</returns>
    Task ConfigureBatchingAsync(BatchConfiguration config, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents resource utilization metrics.
/// </summary>
public sealed class ResourceMetrics
{
    public double CpuUtilizationPercent { get; set; }
    public double GpuUtilizationPercent { get; set; }
    public long MemoryUsageBytes { get; set; }
    public long AvailableMemoryBytes { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a resource allocation.
/// </summary>
public sealed class ResourceAllocation
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public long AllocatedMemoryBytes { get; set; }
    public DateTime AllocatedAt { get; init; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Configuration for batch processing operations.
/// </summary>
public sealed class BatchConfiguration
{
    public int MaxBatchSize { get; set; } = 32;
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromMilliseconds(100);
    public bool EnableDynamicBatching { get; set; } = true;
    public int OptimalBatchSize { get; set; } = 8;
}

/// <summary>
/// Interface for inference session operations.
/// </summary>
public interface IInferenceSession : IAsyncDisposable
{
    string ModelId { get; }
    ExecutionProvider Provider { get; }
    DateTime CreatedAt { get; }
    InferenceMetrics Metrics { get; }
}
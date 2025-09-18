namespace GameConsole.AI.Local;

/// <summary>
/// Local inference engine for AI model execution.
/// Handles model loading, batching, scheduling, and execution with performance optimization
/// and support for multiple AI frameworks through ONNX Runtime.
/// </summary>
public interface ILocalInferenceEngine
{
    /// <summary>
    /// Loads a model for inference with specified configuration and optimization settings.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="modelPath">Path to the model file or cached model location.</param>
    /// <param name="configuration">Model configuration and optimization settings.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async model loading operation.</returns>
    Task LoadModelAsync(string modelId, string modelPath, ModelConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a batch of inference requests with automatic batching and optimization.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model to use.</param>
    /// <param name="requests">Batch of inference requests to execute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns batch results.</returns>
    Task<IEnumerable<InferenceResult>> ExecuteBatchAsync(string modelId, IEnumerable<InferenceRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a single inference request with the specified model.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model to use.</param>
    /// <param name="request">Inference request to execute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns inference result.</returns>
    Task<InferenceResult> ExecuteAsync(string modelId, InferenceRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a model from memory to free up resources.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model to unload.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async model unloading operation.</returns>
    Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for the inference engine including throughput and latency.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns performance metrics.</returns>
    Task<InferencePerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about currently loaded models and their status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns loaded model information.</returns>
    Task<IEnumerable<LoadedModelInfo>> GetLoadedModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes the inference engine configuration based on usage patterns and performance data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async optimization operation.</returns>
    Task OptimizePerformanceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model is currently loaded and ready for inference.
    /// </summary>
    /// <param name="modelId">Unique identifier for the model.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns model readiness status.</returns>
    Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default);
}
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Services;

/// <summary>
/// Core AI service interface for local AI deployment infrastructure.
/// Provides unified interface for AI model management and inference operations.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    #region Model Management
    
    /// <summary>
    /// Loads an AI model into the runtime.
    /// </summary>
    /// <param name="modelPath">Path to the model file.</param>
    /// <param name="framework">AI framework to use for inference.</param>
    /// <param name="config">Resource configuration for the model.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The loaded AI model.</returns>
    Task<AIModel> LoadModelAsync(string modelPath, AIFramework framework, ResourceConfiguration config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unloads an AI model from the runtime.
    /// </summary>
    /// <param name="modelId">Model identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets information about a loaded model.
    /// </summary>
    /// <param name="modelId">Model identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Model information or null if not found.</returns>
    Task<AIModel?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists all loaded models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of loaded models.</returns>
    Task<IEnumerable<AIModel>> ListModelsAsync(CancellationToken cancellationToken = default);
    
    #endregion

    #region Inference Operations
    
    /// <summary>
    /// Executes inference on a loaded model.
    /// </summary>
    /// <param name="request">Inference request containing input data.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Inference result with outputs and metadata.</returns>
    Task<InferenceResult> InferAsync(InferenceRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes batch inference on multiple requests.
    /// </summary>
    /// <param name="requests">Collection of inference requests.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of inference results.</returns>
    Task<IEnumerable<InferenceResult>> InferBatchAsync(IEnumerable<InferenceRequest> requests, CancellationToken cancellationToken = default);
    
    #endregion

    #region Resource Management
    
    /// <summary>
    /// Gets the resource manager capability.
    /// </summary>
    IResourceManagerCapability? ResourceManager { get; }
    
    /// <summary>
    /// Gets the model cache capability.
    /// </summary>
    IModelCacheCapability? ModelCache { get; }
    
    /// <summary>
    /// Gets the local inference engine capability.
    /// </summary>
    ILocalInferenceCapability? InferenceEngine { get; }
    
    /// <summary>
    /// Gets current resource usage statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current resource usage statistics.</returns>
    Task<ResourceStats> GetResourceStatsAsync(CancellationToken cancellationToken = default);
    
    #endregion
}

/// <summary>
/// Capability interface for AI resource management operations.
/// </summary>
public interface IResourceManagerCapability : ICapabilityProvider
{
    /// <summary>
    /// Allocates resources for AI inference.
    /// </summary>
    /// <param name="config">Resource configuration requirements.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if resources were successfully allocated.</returns>
    Task<bool> AllocateResourcesAsync(ResourceConfiguration config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Releases allocated resources.
    /// </summary>
    /// <param name="modelId">Model identifier that owned the resources.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ReleaseResourcesAsync(string modelId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets available execution devices.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of available execution devices.</returns>
    Task<IEnumerable<ExecutionDevice>> GetAvailableDevicesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets optimal device for given resource requirements.
    /// </summary>
    /// <param name="config">Resource configuration requirements.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Recommended execution device.</returns>
    Task<ExecutionDevice> GetOptimalDeviceAsync(ResourceConfiguration config, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for AI model cache management operations.
/// </summary>
public interface IModelCacheCapability : ICapabilityProvider
{
    /// <summary>
    /// Caches a model for faster loading.
    /// </summary>
    /// <param name="modelPath">Path to the model file.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Cache key for the model.</returns>
    Task<string> CacheModelAsync(string modelPath, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a cached model.
    /// </summary>
    /// <param name="cacheKey">Cache key for the model.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Cached model path or null if not cached.</returns>
    Task<string?> GetCachedModelAsync(string cacheKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a model from cache.
    /// </summary>
    /// <param name="cacheKey">Cache key for the model.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task EvictModelAsync(string cacheKey, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears all cached models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearCacheAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets cache usage statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Cache usage statistics.</returns>
    Task<(long UsedBytes, long AvailableBytes, int CachedModels)> GetCacheStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for local AI inference operations.
/// </summary>
public interface ILocalInferenceCapability : ICapabilityProvider
{
    /// <summary>
    /// Creates an inference session for a model.
    /// </summary>
    /// <param name="model">AI model to create session for.</param>
    /// <param name="config">Resource configuration for the session.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Session identifier.</returns>
    Task<string> CreateInferenceSessionAsync(AIModel model, ResourceConfiguration config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Destroys an inference session.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DestroyInferenceSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes inference using a session.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="inputs">Input data for inference.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Inference outputs.</returns>
    Task<Dictionary<string, object>> ExecuteInferenceAsync(string sessionId, Dictionary<string, object> inputs, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets inference session statistics.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Session statistics.</returns>
    Task<(int InferenceCount, TimeSpan TotalTime, TimeSpan AverageTime)> GetSessionStatsAsync(string sessionId, CancellationToken cancellationToken = default);
}
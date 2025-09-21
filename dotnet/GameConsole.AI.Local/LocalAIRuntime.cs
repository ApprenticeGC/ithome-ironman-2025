using GameConsole.AI.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Local;

/// <summary>
/// Local AI Runtime implementing ILocalAIRuntime for local AI execution.
/// Integrates resource management, model caching, and inference engine capabilities.
/// </summary>
[Service("AI", "Runtime", "Local")]
public class LocalAIRuntime : BaseLocalAIService
{
    private readonly ConcurrentDictionary<string, AIModel> _loadedModels = new();
    private readonly ConcurrentDictionary<string, string> _modelSessions = new();
    private readonly AIResourceManager _resourceManager;
    private readonly ModelCacheManager _modelCache;
    private readonly LocalInferenceEngine _inferenceEngine;

    public LocalAIRuntime(
        ILogger<LocalAIRuntime> logger,
        AIResourceManager? resourceManager = null,
        ModelCacheManager? modelCache = null,
        LocalInferenceEngine? inferenceEngine = null) 
        : base(logger)
    {
        _resourceManager = resourceManager ?? new AIResourceManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<AIResourceManager>.Instance);
        _modelCache = modelCache ?? new ModelCacheManager(Microsoft.Extensions.Logging.Abstractions.NullLogger<ModelCacheManager>.Instance);
        _inferenceEngine = inferenceEngine ?? new LocalInferenceEngine(Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalInferenceEngine>.Instance);
    }

    #region Capability Properties

    public override IResourceManagerCapability ResourceManager => _resourceManager;
    public override IModelCacheCapability ModelCache => _modelCache;
    public override ILocalInferenceCapability InferenceEngine => _inferenceEngine;

    #endregion

    #region Model Management Implementation

    public override async Task<AIModel> LoadModelAsync(string modelPath, AIFramework framework, ResourceConfiguration config, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException("Service is not running");

        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model file not found: {modelPath}");

        _logger.LogInformation("Loading model: {ModelPath} with framework: {Framework}", modelPath, framework);

        try
        {
            // Check if model is already loaded
            var modelId = GenerateModelId(modelPath, framework);
            if (_loadedModels.TryGetValue(modelId, out var existingModel))
            {
                _logger.LogDebug("Model already loaded: {ModelId}", modelId);
                return existingModel;
            }

            // Allocate resources first
            if (!await _resourceManager.AllocateResourcesAsync(config, cancellationToken))
            {
                throw new InvalidOperationException("Failed to allocate resources for model");
            }

            // Cache the model if not already cached
            var cacheKey = await _modelCache.CacheModelAsync(modelPath, cancellationToken);
            var cachedPath = await _modelCache.GetCachedModelAsync(cacheKey, cancellationToken);
            
            if (cachedPath == null)
            {
                await _resourceManager.ReleaseResourcesAsync(modelId, cancellationToken);
                throw new InvalidOperationException("Failed to cache model");
            }

            // Get model metadata
            var fileInfo = new FileInfo(modelPath);
            var model = new AIModel(
                Id: modelId,
                Name: Path.GetFileNameWithoutExtension(modelPath),
                Path: cachedPath,
                Framework: framework,
                SizeBytes: fileInfo.Length,
                Version: "1.0", // Could be extracted from model metadata
                Metadata: new Dictionary<string, object>
                {
                    ["CacheKey"] = cacheKey,
                    ["OriginalPath"] = modelPath,
                    ["Framework"] = framework.ToString()
                },
                LastAccessed: DateTime.UtcNow
            );

            // Create inference session
            var sessionId = await _inferenceEngine.CreateInferenceSessionAsync(model, config, cancellationToken);
            
            _loadedModels[modelId] = model;
            _modelSessions[modelId] = sessionId;

            _logger.LogInformation("Successfully loaded model: {ModelId}, Size: {SizeMB}MB", 
                modelId, fileInfo.Length / (1024.0 * 1024.0));

            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model: {ModelPath}", modelPath);
            throw;
        }
    }

    public override async Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (!_loadedModels.TryRemove(modelId, out var model))
        {
            _logger.LogWarning("Attempted to unload non-existent model: {ModelId}", modelId);
            return;
        }

        _logger.LogInformation("Unloading model: {ModelId}", modelId);

        try
        {
            // Destroy inference session
            if (_modelSessions.TryRemove(modelId, out var sessionId))
            {
                await _inferenceEngine.DestroyInferenceSessionAsync(sessionId, cancellationToken);
            }

            // Release resources
            await _resourceManager.ReleaseResourcesAsync(modelId, cancellationToken);

            // Optionally evict from cache (for memory pressure)
            if (model.Metadata.TryGetValue("CacheKey", out var cacheKeyObj) && cacheKeyObj is string cacheKey)
            {
                // Don't automatically evict from cache - let LRU handle it
                _logger.LogDebug("Model unloaded but kept in cache: {ModelId}", modelId);
            }

            _logger.LogInformation("Successfully unloaded model: {ModelId}", modelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unloading model: {ModelId}", modelId);
            throw;
        }
    }

    public override async Task<AIModel?> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_loadedModels.TryGetValue(modelId, out var model) ? model : null);
    }

    public override async Task<IEnumerable<AIModel>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_loadedModels.Values.ToList());
    }

    #endregion

    #region Inference Implementation

    public override async Task<InferenceResult> InferAsync(InferenceRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
            throw new InvalidOperationException("Service is not running");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Processing inference request: {RequestId} for model: {ModelId}", 
                request.RequestId, request.ModelId);

            // Check if model is loaded
            if (!_loadedModels.ContainsKey(request.ModelId))
            {
                return new InferenceResult(
                    request.RequestId,
                    new Dictionary<string, object>(),
                    stopwatch.Elapsed,
                    false,
                    $"Model not loaded: {request.ModelId}"
                );
            }

            // Get inference session
            if (!_modelSessions.TryGetValue(request.ModelId, out var sessionId))
            {
                return new InferenceResult(
                    request.RequestId,
                    new Dictionary<string, object>(),
                    stopwatch.Elapsed,
                    false,
                    $"No inference session for model: {request.ModelId}"
                );
            }

            // Execute inference
            var outputs = await _inferenceEngine.ExecuteInferenceAsync(sessionId, request.Inputs, cancellationToken);
            
            stopwatch.Stop();
            
            _logger.LogDebug("Inference completed: {RequestId} in {ElapsedMs}ms", 
                request.RequestId, stopwatch.ElapsedMilliseconds);

            return new InferenceResult(
                request.RequestId,
                outputs,
                stopwatch.Elapsed,
                true
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Inference failed: {RequestId}", request.RequestId);
            
            return new InferenceResult(
                request.RequestId,
                new Dictionary<string, object>(),
                stopwatch.Elapsed,
                false,
                ex.Message
            );
        }
    }

    public override async Task<IEnumerable<InferenceResult>> InferBatchAsync(IEnumerable<InferenceRequest> requests, CancellationToken cancellationToken = default)
    {
        var results = new List<InferenceResult>();
        
        // Group requests by model for batch processing
        var requestsByModel = requests.GroupBy(r => r.ModelId);
        
        foreach (var modelGroup in requestsByModel)
        {
            var modelRequests = modelGroup.ToList();
            _logger.LogDebug("Processing batch of {Count} requests for model: {ModelId}", 
                modelRequests.Count, modelGroup.Key);

            // Process requests sequentially for simplicity
            // In a production implementation, this could use more sophisticated batching
            foreach (var request in modelRequests)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var result = await InferAsync(request, cancellationToken);
                results.Add(result);
            }
        }

        return results;
    }

    #endregion

    #region Resource Management Implementation

    public override async Task<ResourceStats> GetResourceStatsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_resourceManager.GetCurrentStats());
    }

    #endregion

    #region Service Lifecycle

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing LocalAIRuntime components");
        
        // Components are already initialized in constructor
        await Task.CompletedTask;
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting LocalAIRuntime");
        
        // Perform any startup tasks
        var availableDevices = await _resourceManager.GetAvailableDevicesAsync(cancellationToken);
        _logger.LogInformation("Available execution devices: {Devices}", string.Join(", ", availableDevices));
        
        var cacheStats = await _modelCache.GetCacheStatsAsync(cancellationToken);
        _logger.LogInformation("Model cache: {CachedModels} models, {UsedMB}MB used", 
            cacheStats.CachedModels, cacheStats.UsedBytes / (1024.0 * 1024.0));
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping LocalAIRuntime");
        
        // Unload all models
        var modelIds = _loadedModels.Keys.ToList();
        foreach (var modelId in modelIds)
        {
            try
            {
                await UnloadModelAsync(modelId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading model during shutdown: {ModelId}", modelId);
            }
        }
    }

    protected override async Task OnDisposeAsync()
    {
        _logger.LogDebug("Disposing LocalAIRuntime components");
        
        await _inferenceEngine.DisposeAsync();
        await _modelCache.DisposeAsync();
        
        _loadedModels.Clear();
        _modelSessions.Clear();
    }

    #endregion

    #region Private Helpers

    private static string GenerateModelId(string modelPath, AIFramework framework)
    {
        var combined = $"{modelPath}:{framework}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined)))[..16];
    }

    #endregion
}
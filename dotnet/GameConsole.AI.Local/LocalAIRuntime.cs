using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Local;

/// <summary>
/// Implementation of the local AI runtime service for managing AI deployment and execution.
/// </summary>
public class LocalAIRuntime : ILocalAIRuntime
{
    private readonly ILogger<LocalAIRuntime> _logger;
    private readonly IAIResourceManager _resourceManager;
    private readonly IModelCacheManager _modelCacheManager;
    private readonly ILocalInferenceEngine _inferenceEngine;
    
    private LocalAIConfiguration _configuration = new();
    private bool _isRunning;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the LocalAIRuntime class.
    /// </summary>
    /// <param name="logger">Logger for the runtime.</param>
    /// <param name="resourceManager">Resource manager for GPU/CPU allocation.</param>
    /// <param name="modelCacheManager">Model cache manager for storage.</param>
    /// <param name="inferenceEngine">Inference engine for model execution.</param>
    public LocalAIRuntime(
        ILogger<LocalAIRuntime> logger,
        IAIResourceManager resourceManager,
        IModelCacheManager modelCacheManager,
        ILocalInferenceEngine inferenceEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _modelCacheManager = modelCacheManager ?? throw new ArgumentNullException(nameof(modelCacheManager));
        _inferenceEngine = inferenceEngine ?? throw new ArgumentNullException(nameof(inferenceEngine));
    }

    /// <inheritdoc />
    public bool IsRunning 
    { 
        get 
        { 
            lock (_lockObject) 
            { 
                return _isRunning; 
            } 
        } 
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Local AI Runtime");
        
        try
        {
            // Initialize resource capabilities
            var capabilities = await _resourceManager.GetResourceCapabilitiesAsync(cancellationToken);
            _logger.LogInformation("System capabilities: {CpuCores} CPU cores, {MemoryMB} MB memory, GPU: {HasGpu}", 
                capabilities.TotalCpuCores, capabilities.TotalMemoryMB, capabilities.HasGpu);

            // Initialize model cache
            await _modelCacheManager.PerformMaintenanceAsync(cancellationToken);
            var cacheStats = await _modelCacheManager.GetCacheStatisticsAsync(cancellationToken);
            _logger.LogInformation("Model cache initialized: {ModelCount} models, {CacheSizeMB} MB used", 
                cacheStats.TotalCachedModels, cacheStats.TotalCacheSizeBytes / (1024 * 1024));

            _logger.LogInformation("Local AI Runtime initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Local AI Runtime");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Local AI Runtime");
        
        lock (_lockObject)
        {
            if (_isRunning)
            {
                _logger.LogWarning("Local AI Runtime is already running");
                return;
            }
            _isRunning = true;
        }

        try
        {
            // Apply resource limits
            var limits = new ResourceLimits
            {
                MaxCpuUsagePercent = _configuration.Resources.MaxCpuUsagePercent,
                MaxMemoryUsageMB = _configuration.Resources.MaxGpuMemoryMB,
                MaxGpuMemoryUsageMB = _configuration.Resources.MaxGpuMemoryMB,
                MaxConcurrentAllocations = _configuration.MaxConcurrentInferences
            };
            await _resourceManager.SetResourceLimitsAsync(limits, cancellationToken);

            _logger.LogInformation("Local AI Runtime started successfully");
        }
        catch (Exception ex)
        {
            lock (_lockObject)
            {
                _isRunning = false;
            }
            _logger.LogError(ex, "Failed to start Local AI Runtime");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Local AI Runtime");
        
        lock (_lockObject)
        {
            if (!_isRunning)
            {
                _logger.LogWarning("Local AI Runtime is not running");
                return;
            }
            _isRunning = false;
        }

        try
        {
            // Cleanup resources and unload models
            var loadedModels = await _inferenceEngine.GetLoadedModelsAsync(cancellationToken);
            foreach (var model in loadedModels)
            {
                await _inferenceEngine.UnloadModelAsync(model.ModelId, cancellationToken);
            }

            _logger.LogInformation("Local AI Runtime stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping Local AI Runtime");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ConfigureRuntimeAsync(LocalAIConfiguration configuration, CancellationToken cancellationToken = default)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation("Local AI Runtime configured with {Strategy} resource allocation strategy", 
            configuration.Resources.AllocationStrategy);
        
        // Configuration is applied during next start or restart
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<AIRuntimeCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var resourceCapabilities = await _resourceManager.GetResourceCapabilitiesAsync(cancellationToken);
        
        return new AIRuntimeCapabilities
        {
            SupportedFrameworks = await GetSupportedFrameworksAsync(cancellationToken),
            AvailableProviders = resourceCapabilities.SupportedProviders,
            MaxModelSizeMB = _configuration.ModelCache.MaxCacheSizeMB / 4, // Conservative estimate
            HasGpuAcceleration = resourceCapabilities.HasGpu,
            TotalSystemMemoryMB = resourceCapabilities.TotalMemoryMB,
            TotalGpuMemoryMB = resourceCapabilities.TotalGpuMemoryMB,
            CpuCoreCount = resourceCapabilities.TotalCpuCores
        };
    }

    /// <inheritdoc />
    public async Task<AIInferenceResult> ExecuteInferenceAsync(AIInferenceRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Local AI Runtime is not running");
        }

        try
        {
            _logger.LogDebug("Executing inference request {RequestId} for model {ModelId}", 
                request.RequestId, request.ModelId);

            // Check if model is loaded, load if necessary
            if (!await _inferenceEngine.IsModelLoadedAsync(request.ModelId, cancellationToken))
            {
                await LoadModelIfNeededAsync(request.ModelId, cancellationToken);
            }

            // Execute inference
            var inferenceRequest = new InferenceRequest
            {
                RequestId = request.RequestId,
                InputData = request.InputData,
                Priority = request.Priority,
                TimeoutMs = request.TimeoutMs,
                Options = request.Options
            };

            var result = await _inferenceEngine.ExecuteAsync(request.ModelId, inferenceRequest, cancellationToken);
            
            return new AIInferenceResult
            {
                RequestId = result.RequestId,
                OutputData = result.OutputData,
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.ErrorMessage,
                ExecutionTimeMs = result.ExecutionTimeMs,
                ModelId = request.ModelId,
                Metadata = result.Metadata,
                CompletedAt = result.CompletedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing inference request {RequestId}", request.RequestId);
            return new AIInferenceResult
            {
                RequestId = request.RequestId,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ModelId = request.ModelId
            };
        }
    }

    /// <inheritdoc />
    public async Task<AIPerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        var inferenceMetrics = await _inferenceEngine.GetPerformanceMetricsAsync(cancellationToken);
        var resourceUsage = await _resourceManager.GetResourceUsageAsync(cancellationToken);

        return new AIPerformanceMetrics
        {
            TotalInferences = inferenceMetrics.TotalInferences,
            AverageInferenceTimeMs = inferenceMetrics.AverageInferenceTimeMs,
            ThroughputPerSecond = inferenceMetrics.ThroughputPerSecond,
            ResourceUsage = resourceUsage,
            SuccessfulInferences = inferenceMetrics.SuccessfulInferences,
            FailedInferences = inferenceMetrics.FailedInferences,
            LastUpdated = inferenceMetrics.LastUpdated
        };
    }

    /// <inheritdoc />
    public async Task OptimizeResourcesAsync(CancellationToken cancellationToken = default)
    {
        if (!_configuration.EnableAutoOptimization)
        {
            _logger.LogDebug("Auto-optimization is disabled");
            return;
        }

        _logger.LogInformation("Optimizing resource allocation");
        
        try
        {
            // Optimize resource allocation
            await _resourceManager.OptimizeAllocationAsync(cancellationToken);
            
            // Optimize inference engine
            await _inferenceEngine.OptimizePerformanceAsync(cancellationToken);
            
            // Perform cache maintenance
            await _modelCacheManager.PerformMaintenanceAsync(cancellationToken);
            
            _logger.LogInformation("Resource optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource optimization");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SupportedFramework>> GetSupportedFrameworksAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        return new[]
        {
            new SupportedFramework
            {
                Framework = AIFramework.ONNX,
                Version = "1.18.0",
                IsAvailable = true,
                SupportedFormats = [ModelFormat.ONNX],
                Notes = "Primary framework for cross-platform inference"
            }
        };
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsRunning)
        {
            await StopAsync();
        }
        
        _logger.LogInformation("Local AI Runtime disposed");
    }

    private async Task LoadModelIfNeededAsync(string modelId, CancellationToken cancellationToken)
    {
        // Check if model is cached
        var cachedModel = await _modelCacheManager.RetrieveModelAsync(modelId, cancellationToken);
        if (cachedModel == null)
        {
            throw new InvalidOperationException($"Model {modelId} is not available in cache");
        }

        // Load model with default configuration
        var configuration = new ModelConfiguration
        {
            ExecutionProvider = _configuration.Resources.PreferredProvider,
            EnableQuantization = _configuration.Inference.EnableQuantization,
            QuantizationMode = _configuration.Inference.QuantizationMode,
            MaxBatchSize = _configuration.Inference.MaxBatchSize
        };

        await _inferenceEngine.LoadModelAsync(modelId, cachedModel.FilePath ?? string.Empty, configuration, cancellationToken);
    }
}
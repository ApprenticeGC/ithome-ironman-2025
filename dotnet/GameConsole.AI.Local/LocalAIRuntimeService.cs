using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Local;

/// <summary>
/// Local AI runtime service for managing AI model execution and resources.
/// Provides unified access to local AI capabilities with resource optimization.
/// </summary>
[Service("Local AI Runtime", "1.0.0", "Local AI deployment infrastructure with resource management and optimization",
         Categories = new[] { "AI", "Local", "Runtime", "Inference" },
         Lifetime = ServiceLifetime.Singleton)]
public sealed class LocalAIRuntimeService : ILocalAIRuntime
{
    private readonly ILogger<LocalAIRuntimeService> _logger;
    private readonly Dictionary<string, IInferenceSession> _loadedModels = new();
    private readonly object _modelsLock = new();
    
    private IAIResourceManager? _resourceManager;
    private IModelCacheManager? _modelCache;
    private ILocalInferenceEngine? _inferenceEngine;
    private ResourceConstraints _constraints = new();
    private ExecutionProvider _currentProvider = ExecutionProvider.Auto;
    private InferenceMetrics _currentMetrics = new();
    
    private bool _isRunning = false;
    private bool _disposed = false;

    public LocalAIRuntimeService(ILogger<LocalAIRuntimeService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region ILocalAIRuntime Implementation

    public IAIResourceManager ResourceManager => 
        _resourceManager ?? throw new InvalidOperationException("Service not initialized");

    public IModelCacheManager ModelCache => 
        _modelCache ?? throw new InvalidOperationException("Service not initialized");

    public ILocalInferenceEngine InferenceEngine => 
        _inferenceEngine ?? throw new InvalidOperationException("Service not initialized");

    public ExecutionProvider CurrentExecutionProvider => _currentProvider;

    public InferenceMetrics CurrentMetrics => _currentMetrics;

    public async Task LoadModelAsync(string modelPath, string modelId, QuantizationConfig? quantizationConfig = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelPath))
            throw new ArgumentException("Model path cannot be null or empty", nameof(modelPath));
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        _logger.LogInformation("Loading model {ModelId} from {ModelPath}", modelId, modelPath);

        try
        {
            // Check if model is already loaded
            lock (_modelsLock)
            {
                if (_loadedModels.ContainsKey(modelId))
                {
                    _logger.LogWarning("Model {ModelId} is already loaded", modelId);
                    return;
                }
            }

            // Check resource availability
            var fileInfo = new FileInfo(modelPath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException($"Model file not found: {modelPath}");

            var allocation = await ResourceManager.AllocateResourcesAsync(
                fileInfo.Length * 2, // Estimate 2x file size for memory usage
                1000, // Estimate 1 second for loading
                cancellationToken);

            try
            {
                // Load model data
                byte[] modelData;
                var cachedData = await ModelCache.GetCachedModelAsync(modelId, cancellationToken);
                if (cachedData != null)
                {
                    _logger.LogDebug("Using cached model data for {ModelId}", modelId);
                    modelData = cachedData;
                }
                else
                {
                    _logger.LogDebug("Loading model data from file for {ModelId}", modelId);
                    modelData = await File.ReadAllBytesAsync(modelPath, cancellationToken);
                    
                    // Cache the model for future use
                    await ModelCache.CacheModelAsync(modelId, modelPath, new Dictionary<string, object>
                    {
                        ["FilePath"] = modelPath,
                        ["FileSize"] = fileInfo.Length,
                        ["LoadedAt"] = DateTime.UtcNow,
                        ["QuantizationConfig"] = quantizationConfig ?? new QuantizationConfig()
                    }, cancellationToken);
                }

                // Apply quantization if specified
                if (quantizationConfig?.Level != QuantizationLevel.None && quantizationConfig != null)
                {
                    _logger.LogDebug("Applying {QuantizationLevel} quantization to model {ModelId}", 
                        quantizationConfig.Level, modelId);
                    modelData = await ApplyQuantizationAsync(modelData, quantizationConfig, cancellationToken);
                }

                // Create inference session
                var session = await InferenceEngine.CreateSessionAsync(
                    modelId, modelData, _currentProvider, cancellationToken);

                lock (_modelsLock)
                {
                    _loadedModels[modelId] = session;
                }

                _logger.LogInformation("Successfully loaded model {ModelId}", modelId);
                UpdateMetrics();
            }
            finally
            {
                await ResourceManager.ReleaseResourcesAsync(allocation, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model {ModelId} from {ModelPath}", modelId, modelPath);
            throw;
        }
    }

    public async Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));

        _logger.LogInformation("Unloading model {ModelId}", modelId);

        IInferenceSession? session = null;
        lock (_modelsLock)
        {
            if (_loadedModels.TryGetValue(modelId, out session))
            {
                _loadedModels.Remove(modelId);
            }
        }

        if (session != null)
        {
            await session.DisposeAsync();
            _logger.LogInformation("Successfully unloaded model {ModelId}", modelId);
            UpdateMetrics();
        }
        else
        {
            _logger.LogWarning("Model {ModelId} was not loaded", modelId);
        }
    }

    public async Task<object> InferAsync(string modelId, object input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var startTime = DateTime.UtcNow;
        _logger.LogDebug("Starting inference for model {ModelId}", modelId);

        try
        {
            IInferenceSession session;
            lock (_modelsLock)
            {
                if (!_loadedModels.TryGetValue(modelId, out session!))
                    throw new InvalidOperationException($"Model {modelId} is not loaded");
            }

            var result = await InferenceEngine.ExecuteAsync(session, input, cancellationToken);
            
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _currentMetrics.InferenceTimeMs = duration;
            _currentMetrics.RecordedAt = DateTime.UtcNow;

            _logger.LogDebug("Completed inference for model {ModelId} in {Duration}ms", modelId, duration);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inference failed for model {ModelId}", modelId);
            throw;
        }
    }

    public async Task<IEnumerable<object>> InferBatchAsync(string modelId, IEnumerable<object> inputs, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (inputs == null)
            throw new ArgumentNullException(nameof(inputs));

        var inputList = inputs.ToList();
        var startTime = DateTime.UtcNow;
        _logger.LogDebug("Starting batch inference for model {ModelId} with {BatchSize} inputs", 
            modelId, inputList.Count);

        try
        {
            IInferenceSession session;
            lock (_modelsLock)
            {
                if (!_loadedModels.TryGetValue(modelId, out session!))
                    throw new InvalidOperationException($"Model {modelId} is not loaded");
            }

            var results = await InferenceEngine.ExecuteBatchAsync(session, inputList, cancellationToken);
            
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _currentMetrics.InferenceTimeMs = duration;
            _currentMetrics.OperationsPerSecond = inputList.Count / (duration / 1000.0);
            _currentMetrics.RecordedAt = DateTime.UtcNow;

            _logger.LogDebug("Completed batch inference for model {ModelId} with {BatchSize} inputs in {Duration}ms", 
                modelId, inputList.Count, duration);
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch inference failed for model {ModelId}", modelId);
            throw;
        }
    }

    public async Task SetResourceConstraintsAsync(ResourceConstraints constraints, CancellationToken cancellationToken = default)
    {
        if (constraints == null)
            throw new ArgumentNullException(nameof(constraints));

        _logger.LogInformation("Updating resource constraints: MaxMemory={MaxMemoryMB}MB, MaxCPU={MaxCpu}%, MaxGPU={MaxGpu}%", 
            constraints.MaxMemoryBytes / (1024 * 1024), constraints.MaxCpuUtilizationPercent, constraints.MaxGpuUtilizationPercent);

        _constraints = constraints;
        // Implementation would update the resource manager constraints
        await Task.CompletedTask;
    }

    public Task<IEnumerable<ExecutionProvider>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default)
    {
        var providers = new List<ExecutionProvider> { ExecutionProvider.Cpu };

        // Check for GPU availability
        try
        {
            // In a real implementation, this would check for actual GPU availability
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                providers.Add(ExecutionProvider.DirectMl);
            }
            
            // Add CUDA if available (simplified check)
            providers.Add(ExecutionProvider.Cuda);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking GPU availability");
        }

        _logger.LogDebug("Available execution providers: {Providers}", string.Join(", ", providers));
        return Task.FromResult<IEnumerable<ExecutionProvider>>(providers);
    }

    #endregion

    #region IService Implementation

    public bool IsRunning => _isRunning;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Local AI Runtime Service");

        try
        {
            // Initialize components
            _resourceManager = new AIResourceManagerService(_logger);
            _modelCache = new ModelCacheManagerService(_logger);
            _inferenceEngine = new LocalInferenceEngineService(_logger);

            // Initialize all components
            await _resourceManager.MonitorResourcesAsync(cancellationToken);
            
            // Determine best execution provider
            var availableProviders = await GetAvailableProvidersAsync(cancellationToken);
            _currentProvider = DetermineBestProvider(availableProviders);
            
            _logger.LogInformation("Local AI Runtime initialized with execution provider: {Provider}", _currentProvider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Local AI Runtime Service");
            throw;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Local AI Runtime Service");

        try
        {
            _isRunning = true;
            
            // Start resource monitoring
            _ = Task.Run(() => ResourceManager.MonitorResourcesAsync(cancellationToken), cancellationToken);
            
            // Small delay to ensure monitoring starts
            await Task.Delay(10, cancellationToken);
            
            _logger.LogInformation("Local AI Runtime Service started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Local AI Runtime Service");
            _isRunning = false;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Local AI Runtime Service");

        try
        {
            _isRunning = false;
            
            // Unload all models
            var modelIds = new List<string>();
            lock (_modelsLock)
            {
                modelIds.AddRange(_loadedModels.Keys);
            }

            foreach (var modelId in modelIds)
            {
                await UnloadModelAsync(modelId, cancellationToken);
            }

            _logger.LogInformation("Local AI Runtime Service stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Local AI Runtime Service");
            throw;
        }
    }

    #endregion

    #region Private Methods

    private Task<byte[]> ApplyQuantizationAsync(byte[] modelData, QuantizationConfig config, CancellationToken cancellationToken)
    {
        // In a real implementation, this would apply actual quantization
        // For now, return the original data as a placeholder
        _logger.LogDebug("Applying {QuantizationLevel} quantization (placeholder implementation)", config.Level);
        Task.Delay(50, cancellationToken); // Simulate processing time
        return Task.FromResult(modelData);
    }

    private ExecutionProvider DetermineBestProvider(IEnumerable<ExecutionProvider> availableProviders)
    {
        var providers = availableProviders.ToList();
        
        // Preference order: CUDA > DirectML > CPU
        if (providers.Contains(ExecutionProvider.Cuda))
            return ExecutionProvider.Cuda;
        if (providers.Contains(ExecutionProvider.DirectMl))
            return ExecutionProvider.DirectMl;
        
        return ExecutionProvider.Cpu;
    }

    private void UpdateMetrics()
    {
        _currentMetrics = new InferenceMetrics
        {
            LoadTimeMs = 0, // Would be updated during actual loading
            InferenceTimeMs = _currentMetrics.InferenceTimeMs,
            MemoryUsageBytes = GC.GetTotalMemory(false),
            OperationsPerSecond = _currentMetrics.OperationsPerSecond,
            GpuUtilizationPercent = 0, // Would be updated with actual GPU monitoring
            CpuUtilizationPercent = 0, // Would be updated with actual CPU monitoring
            RecordedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region IAsyncDisposable Implementation

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        try
        {
            if (_isRunning)
            {
                await StopAsync();
            }

            // Dispose of all loaded models
            lock (_modelsLock)
            {
                foreach (var session in _loadedModels.Values)
                {
                    if (session is IAsyncDisposable disposableSession)
                    {
                        _ = Task.Run(async () => await disposableSession.DisposeAsync());
                    }
                }
                _loadedModels.Clear();
            }

            // Dispose components
            if (_modelCache is IAsyncDisposable cacheDisposable)
                await cacheDisposable.DisposeAsync();

            _disposed = true;
            _logger.LogInformation("Local AI Runtime Service disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Local AI Runtime Service disposal");
        }
    }

    #endregion
}
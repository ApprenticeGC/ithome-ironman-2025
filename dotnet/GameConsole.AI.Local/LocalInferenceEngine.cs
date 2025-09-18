using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Concurrent;
using System.Numerics.Tensors;

namespace GameConsole.AI.Local;

/// <summary>
/// Implementation of local inference engine for AI model execution using ONNX Runtime.
/// </summary>
public class LocalInferenceEngine : ILocalInferenceEngine, IDisposable
{
    private readonly ILogger<LocalInferenceEngine> _logger;
    private readonly ConcurrentDictionary<string, LoadedModel> _loadedModels = new();
    private readonly ConcurrentQueue<InferenceRequest> _inferenceQueue = new();
    private readonly SemaphoreSlim _batchSemaphore = new(1, 1);
    private readonly Timer _batchTimer;
    private readonly InferenceConfiguration _configuration;
    
    private InferencePerformanceMetrics _performanceMetrics = new();
    private readonly object _metricsLock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the LocalInferenceEngine class.
    /// </summary>
    /// <param name="logger">Logger for the inference engine.</param>
    /// <param name="configuration">Inference configuration settings.</param>
    public LocalInferenceEngine(ILogger<LocalInferenceEngine> logger, InferenceConfiguration? configuration = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? new InferenceConfiguration();
        
        // Initialize batch processing timer
        _batchTimer = new Timer(ProcessBatchedInferences, null, 
            TimeSpan.FromMilliseconds(_configuration.BatchTimeoutMs), 
            TimeSpan.FromMilliseconds(_configuration.BatchTimeoutMs));
    }

    /// <inheritdoc />
    public async Task LoadModelAsync(string modelId, string modelPath, ModelConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(modelId))
            throw new ArgumentNullException(nameof(modelId));
        if (string.IsNullOrEmpty(modelPath))
            throw new ArgumentNullException(nameof(modelPath));
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Model file not found: {modelPath}");

        _logger.LogInformation("Loading model {ModelId} from {ModelPath}", modelId, modelPath);

        try
        {
            // Create session options based on configuration
            var sessionOptions = CreateSessionOptions(configuration);
            
            // Create ONNX inference session
            var session = new InferenceSession(modelPath, sessionOptions);
            
            var loadedModel = new LoadedModel
            {
                ModelId = modelId,
                ModelPath = modelPath,
                Session = session,
                Configuration = configuration,
                LoadedAt = DateTimeOffset.UtcNow,
                LastUsedAt = DateTimeOffset.UtcNow,
                InferenceCount = 0,
                IsInUse = false,
                MemoryUsageMB = EstimateModelMemoryUsage(session)
            };

            _loadedModels.AddOrUpdate(modelId, loadedModel, (_, _) => loadedModel);
            
            _logger.LogInformation("Model {ModelId} loaded successfully. Memory usage: {MemoryMB} MB", 
                modelId, loadedModel.MemoryUsageMB);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model {ModelId} from {ModelPath}", modelId, modelPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InferenceResult>> ExecuteBatchAsync(string modelId, IEnumerable<InferenceRequest> requests, CancellationToken cancellationToken = default)
    {
        if (!_loadedModels.TryGetValue(modelId, out var loadedModel))
        {
            throw new InvalidOperationException($"Model {modelId} is not loaded");
        }

        var requestList = requests.ToList();
        if (requestList.Count == 0)
        {
            return Enumerable.Empty<InferenceResult>();
        }

        _logger.LogDebug("Executing batch inference for model {ModelId} with {Count} requests", modelId, requestList.Count);

        var results = new List<InferenceResult>();
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            loadedModel.IsInUse = true;
            
            foreach (var request in requestList)
            {
                var result = await ExecuteSingleInferenceAsync(loadedModel, request, cancellationToken);
                results.Add(result);
            }

            // Update performance metrics
            var batchDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            UpdatePerformanceMetrics(requestList.Count, batchDuration, results.Count(r => r.IsSuccess));

            loadedModel.InferenceCount += requestList.Count;
            loadedModel.LastUsedAt = DateTimeOffset.UtcNow;

            return results;
        }
        finally
        {
            loadedModel.IsInUse = false;
        }
    }

    /// <inheritdoc />
    public async Task<InferenceResult> ExecuteAsync(string modelId, InferenceRequest request, CancellationToken cancellationToken = default)
    {
        if (_configuration.EnableDynamicBatching && _inferenceQueue.Count < _configuration.MaxBatchSize)
        {
            // Add to batch queue for dynamic batching
            _inferenceQueue.Enqueue(request);
            
            // Wait for batch processing or timeout
            var timeout = TimeSpan.FromMilliseconds(request.TimeoutMs);
            var completionSource = new TaskCompletionSource<InferenceResult>();
            
            // For simplicity, execute immediately in this implementation
            // In a full implementation, you'd have a batch processing system
        }

        var results = await ExecuteBatchAsync(modelId, [request], cancellationToken);
        return results.First();
    }

    /// <inheritdoc />
    public async Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        if (_loadedModels.TryRemove(modelId, out var loadedModel))
        {
            loadedModel.Session?.Dispose();
            _logger.LogInformation("Model {ModelId} unloaded successfully", modelId);
        }
        else
        {
            _logger.LogWarning("Attempted to unload unknown model: {ModelId}", modelId);
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<InferencePerformanceMetrics> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default)
    {
        InferencePerformanceMetrics metrics;
        lock (_metricsLock)
        {
            metrics = new InferencePerformanceMetrics
            {
                TotalInferences = _performanceMetrics.TotalInferences,
                SuccessfulInferences = _performanceMetrics.SuccessfulInferences,
                FailedInferences = _performanceMetrics.FailedInferences,
                AverageInferenceTimeMs = _performanceMetrics.AverageInferenceTimeMs,
                MinInferenceTimeMs = _performanceMetrics.MinInferenceTimeMs,
                MaxInferenceTimeMs = _performanceMetrics.MaxInferenceTimeMs,
                ThroughputPerSecond = _performanceMetrics.ThroughputPerSecond,
                BatchedInferences = _performanceMetrics.BatchedInferences,
                AverageBatchSize = _performanceMetrics.AverageBatchSize,
                LastUpdated = _performanceMetrics.LastUpdated
            };
        }
        return Task.FromResult(metrics);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<LoadedModelInfo>> GetLoadedModelsAsync(CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        
        return _loadedModels.Values.Select(m => new LoadedModelInfo
        {
            ModelId = m.ModelId,
            Name = Path.GetFileNameWithoutExtension(m.ModelPath),
            Format = ModelFormat.ONNX,
            ExecutionProvider = m.Configuration.ExecutionProvider,
            MemoryUsageMB = m.MemoryUsageMB,
            LoadedAt = m.LoadedAt,
            InferenceCount = m.InferenceCount,
            IsInUse = m.IsInUse,
            LastUsedAt = m.LastUsedAt
        }).ToList();
    }

    /// <inheritdoc />
    public async Task OptimizePerformanceAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Optimizing inference engine performance");

        // Unload unused models that haven't been used recently
        var modelsToUnload = _loadedModels.Values
            .Where(m => !m.IsInUse && (DateTimeOffset.UtcNow - m.LastUsedAt).TotalMinutes > 30)
            .ToList();

        foreach (var model in modelsToUnload)
        {
            await UnloadModelAsync(model.ModelId, cancellationToken);
        }

        // Trigger garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        _logger.LogInformation("Performance optimization completed. Loaded models: {Count}", _loadedModels.Count);
    }

    /// <inheritdoc />
    public Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_loadedModels.ContainsKey(modelId));
    }

    private Task<InferenceResult> ExecuteSingleInferenceAsync(LoadedModel loadedModel, InferenceRequest request, CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            // Convert input data to ONNX format
            var inputTensors = ConvertInputData(request.InputData);
            
            // Run inference
            using var results = loadedModel.Session.Run(inputTensors);
            
            // Convert output data from ONNX format
            var outputData = ConvertOutputData(results);
            
            var executionTime = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            
            return Task.FromResult(new InferenceResult
            {
                RequestId = request.RequestId,
                OutputData = outputData,
                IsSuccess = true,
                ExecutionTimeMs = executionTime,
                PreprocessingTimeMs = 0, // Would track separately in full implementation
                PostprocessingTimeMs = 0, // Would track separately in full implementation
                Metadata = new Dictionary<string, object>
                {
                    ["ModelId"] = loadedModel.ModelId,
                    ["ExecutionProvider"] = loadedModel.Configuration.ExecutionProvider.ToString()
                },
                CompletedAt = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing inference for request {RequestId}", request.RequestId);
            
            return Task.FromResult(new InferenceResult
            {
                RequestId = request.RequestId,
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds,
                CompletedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private static SessionOptions CreateSessionOptions(ModelConfiguration configuration)
    {
        var options = new SessionOptions();
        
        // Configure execution provider
        switch (configuration.ExecutionProvider)
        {
            case ExecutionProvider.CPU:
                // CPU is default, no additional configuration needed
                break;
            case ExecutionProvider.CUDA:
                try
                {
                    options.AppendExecutionProvider_CUDA();
                }
                catch
                {
                    // Fall back to CPU if CUDA is not available
                }
                break;
            case ExecutionProvider.DirectML:
                try
                {
                    options.AppendExecutionProvider_DML();
                }
                catch
                {
                    // Fall back to CPU if DirectML is not available
                }
                break;
        }

        // Configure optimization
        if (configuration.EnableOptimization)
        {
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        }

        return options;
    }

    private static List<NamedOnnxValue> ConvertInputData(Dictionary<string, object> inputData)
    {
        // Simplified conversion - in a full implementation, this would handle
        // proper tensor conversion based on model input specifications
        var inputs = new List<NamedOnnxValue>();
        
        foreach (var kvp in inputData)
        {
            // For this basic implementation, assume float arrays
            if (kvp.Value is float[] floatArray)
            {
                var tensor = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(floatArray, new[] { 1, floatArray.Length });
                inputs.Add(NamedOnnxValue.CreateFromTensor(kvp.Key, tensor));
            }
        }

        return inputs;
    }

    private static Dictionary<string, object> ConvertOutputData(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
    {
        // Simplified conversion - in a full implementation, this would handle
        // proper tensor conversion based on model output specifications
        var outputs = new Dictionary<string, object>();
        
        foreach (var result in results)
        {
            if (result.Value is Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> tensor)
            {
                outputs[result.Name] = tensor.ToArray();
            }
        }

        return outputs;
    }

    private static long EstimateModelMemoryUsage(InferenceSession session)
    {
        // Simplified estimation - in a full implementation, this would
        // calculate actual memory usage based on model parameters
        return 100; // Default 100 MB estimate
    }

    private void UpdatePerformanceMetrics(int requestCount, double batchDurationMs, int successCount)
    {
        lock (_metricsLock)
        {
            _performanceMetrics.TotalInferences += requestCount;
            _performanceMetrics.SuccessfulInferences += successCount;
            _performanceMetrics.FailedInferences += requestCount - successCount;
            _performanceMetrics.BatchedInferences += requestCount;

            var avgInferenceTime = batchDurationMs / requestCount;
            
            if (_performanceMetrics.MinInferenceTimeMs == 0 || avgInferenceTime < _performanceMetrics.MinInferenceTimeMs)
                _performanceMetrics.MinInferenceTimeMs = avgInferenceTime;
            
            if (avgInferenceTime > _performanceMetrics.MaxInferenceTimeMs)
                _performanceMetrics.MaxInferenceTimeMs = avgInferenceTime;

            // Update running average
            _performanceMetrics.AverageInferenceTimeMs = 
                (_performanceMetrics.AverageInferenceTimeMs + avgInferenceTime) / 2;

            // Calculate throughput (inferences per second)
            _performanceMetrics.ThroughputPerSecond = requestCount / (batchDurationMs / 1000.0);
            
            _performanceMetrics.AverageBatchSize = 
                _performanceMetrics.BatchedInferences / Math.Max(1, _performanceMetrics.TotalInferences / requestCount);

            _performanceMetrics.LastUpdated = DateTimeOffset.UtcNow;
        }
    }

    private void ProcessBatchedInferences(object? state)
    {
        // Simplified batch processing - in a full implementation, this would
        // collect queued requests and execute them in optimized batches
        if (_inferenceQueue.IsEmpty)
            return;

        // For now, just clear the queue to prevent memory issues
        while (_inferenceQueue.TryDequeue(out _))
        {
            // Process would happen here
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _batchTimer?.Dispose();
        
        foreach (var model in _loadedModels.Values)
        {
            model.Session?.Dispose();
        }
        
        _loadedModels.Clear();
        _batchSemaphore?.Dispose();
        
        _disposed = true;
    }

    private sealed class LoadedModel
    {
        public required string ModelId { get; init; }
        public required string ModelPath { get; init; }
        public required InferenceSession Session { get; init; }
        public required ModelConfiguration Configuration { get; init; }
        public required DateTimeOffset LoadedAt { get; init; }
        public DateTimeOffset LastUsedAt { get; set; }
        public long InferenceCount { get; set; }
        public bool IsInUse { get; set; }
        public long MemoryUsageMB { get; init; }
    }
}
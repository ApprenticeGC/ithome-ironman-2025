using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Concurrent;

namespace GameConsole.AI.Local;

/// <summary>
/// Local inference engine for AI model execution with batching and scheduling.
/// Supports ONNX Runtime with multiple execution providers.
/// </summary>
internal sealed class LocalInferenceEngineService : ILocalInferenceEngine, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, OnnxInferenceSession> _sessions = new();
    private readonly SemaphoreSlim _batchSemaphore;
    
    private BatchConfiguration _batchConfig = new();
    private bool _disposed = false;

    public LocalInferenceEngineService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _batchSemaphore = new SemaphoreSlim(_batchConfig.MaxBatchSize, _batchConfig.MaxBatchSize);
        
        _logger.LogDebug("LocalInferenceEngine initialized");
    }

    public IEnumerable<string> SupportedFormats { get; } = new[] { ".onnx" };
    public BatchConfiguration BatchConfig => _batchConfig;

    public Task<IInferenceSession> CreateSessionAsync(string modelId, byte[] modelData, ExecutionProvider executionProvider, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model ID cannot be null or empty", nameof(modelId));
        if (modelData == null || modelData.Length == 0)
            throw new ArgumentException("Model data cannot be null or empty", nameof(modelData));

        _logger.LogDebug("Creating inference session for model {ModelId} with provider {Provider}", 
            modelId, executionProvider);

        try
        {
            // Check if session already exists
            if (_sessions.ContainsKey(modelId))
            {
                _logger.LogWarning("Session for model {ModelId} already exists", modelId);
                throw new InvalidOperationException($"Session for model {modelId} already exists");
            }

            // Create session options
            var sessionOptions = CreateSessionOptions(executionProvider);
            
            // Create ONNX Runtime session
            var onnxSession = new InferenceSession(modelData, sessionOptions);

            // Create our wrapper session
            var session = new OnnxInferenceSession(modelId, executionProvider, onnxSession, _logger);

            _sessions[modelId] = session;
            
            _logger.LogInformation("Created inference session for model {ModelId} with {InputCount} inputs and {OutputCount} outputs", 
                modelId, onnxSession.InputMetadata.Count, onnxSession.OutputMetadata.Count);

            return Task.FromResult<IInferenceSession>(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create inference session for model {ModelId}", modelId);
            throw;
        }
    }

    public async Task<object> ExecuteAsync(IInferenceSession session, object input, CancellationToken cancellationToken = default)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (input == null) throw new ArgumentNullException(nameof(input));

        var onnxSession = (OnnxInferenceSession)session;
        
        _logger.LogTrace("Executing inference for model {ModelId}", onnxSession.ModelId);

        try
        {
            await _batchSemaphore.WaitAsync(cancellationToken);
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Convert input to ONNX format
                var onnxInputs = ConvertToOnnxInput(input, onnxSession.OnnxSession);
                
                // Execute inference
                using var results = onnxSession.OnnxSession.Run(onnxInputs);
                
                // Convert results back
                var output = ConvertFromOnnxOutput(results);
                
                // Update metrics
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
                onnxSession.UpdateMetrics(duration, 1);
                
                _logger.LogTrace("Completed inference for model {ModelId} in {Duration}ms", 
                    onnxSession.ModelId, duration);
                
                return output;
            }
            finally
            {
                _batchSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inference failed for model {ModelId}", onnxSession.ModelId);
            throw;
        }
    }

    public async Task<IEnumerable<object>> ExecuteBatchAsync(IInferenceSession session, IEnumerable<object> inputs, CancellationToken cancellationToken = default)
    {
        if (session == null) throw new ArgumentNullException(nameof(session));
        if (inputs == null) throw new ArgumentNullException(nameof(inputs));

        var onnxSession = (OnnxInferenceSession)session;
        var inputList = inputs.ToList();
        
        _logger.LogDebug("Executing batch inference for model {ModelId} with {BatchSize} inputs", 
            onnxSession.ModelId, inputList.Count);

        if (inputList.Count == 0)
            return Enumerable.Empty<object>();

        try
        {
            var startTime = DateTime.UtcNow;
            var results = new List<object>();

            // Process in optimal batch sizes
            var optimalBatchSize = Math.Min(_batchConfig.OptimalBatchSize, inputList.Count);
            var batches = inputList.Chunk(optimalBatchSize);

            foreach (var batch in batches)
            {
                await _batchSemaphore.WaitAsync(cancellationToken);
                try
                {
                    if (_batchConfig.EnableDynamicBatching && batch.Length > 1)
                    {
                        // Execute as a true batch
                        var batchResult = await ExecuteBatchInternal(onnxSession, batch, cancellationToken);
                        results.AddRange(batchResult);
                    }
                    else
                    {
                        // Execute sequentially for small batches
                        foreach (var input in batch)
                        {
                            var result = await ExecuteSingleInternal(onnxSession, input, cancellationToken);
                            results.Add(result);
                        }
                    }
                }
                finally
                {
                    _batchSemaphore.Release();
                }
            }

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            onnxSession.UpdateMetrics(duration, inputList.Count);

            _logger.LogDebug("Completed batch inference for model {ModelId} with {BatchSize} inputs in {Duration}ms", 
                onnxSession.ModelId, inputList.Count, duration);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch inference failed for model {ModelId}", onnxSession.ModelId);
            throw;
        }
    }

    public Task ConfigureBatchingAsync(BatchConfiguration config, CancellationToken cancellationToken = default)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        _logger.LogInformation("Updating batch configuration: MaxBatchSize={MaxBatch}, OptimalBatchSize={OptimalBatch}, Timeout={Timeout}ms, DynamicBatching={DynamicBatching}",
            config.MaxBatchSize, config.OptimalBatchSize, config.BatchTimeout.TotalMilliseconds, config.EnableDynamicBatching);

        _batchConfig = config;

        // Recreate semaphore with new limits
        var oldSemaphore = _batchSemaphore;
        var newSemaphore = new SemaphoreSlim(config.MaxBatchSize, config.MaxBatchSize);
        
        // Note: In production, you'd need to safely transition the semaphore
        // This is a simplified implementation

        return Task.CompletedTask;
    }

    #region Private Methods

    private SessionOptions CreateSessionOptions(ExecutionProvider provider)
    {
        var options = new SessionOptions();

        try
        {
            // Configure execution provider
            switch (provider)
            {
                case ExecutionProvider.Cpu:
                    // CPU is the default, no additional configuration needed
                    break;

                case ExecutionProvider.Cuda:
                    try
                    {
                        options.AppendExecutionProvider_CUDA();
                        _logger.LogDebug("Configured CUDA execution provider");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to configure CUDA, falling back to CPU");
                    }
                    break;

                case ExecutionProvider.DirectMl:
                    try
                    {
                        options.AppendExecutionProvider_DML();
                        _logger.LogDebug("Configured DirectML execution provider");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to configure DirectML, falling back to CPU");
                    }
                    break;

                case ExecutionProvider.Auto:
                    // Try providers in order of preference
                    try
                    {
                        options.AppendExecutionProvider_CUDA();
                        _logger.LogDebug("Auto-configured CUDA execution provider");
                    }
                    catch
                    {
                        try
                        {
                            options.AppendExecutionProvider_DML();
                            _logger.LogDebug("Auto-configured DirectML execution provider");
                        }
                        catch
                        {
                            _logger.LogDebug("Auto-configured CPU execution provider");
                        }
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown execution provider {Provider}, using CPU", provider);
                    break;
            }

            // Configure performance options
            options.EnableCpuMemArena = true;
            options.EnableMemoryPattern = true;
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            
            // Set intra-op parallelism
            options.IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error configuring session options");
            throw;
        }

        return options;
    }

    private IReadOnlyCollection<NamedOnnxValue> ConvertToOnnxInput(object input, InferenceSession session)
    {
        // This is a simplified conversion - in a real implementation, you'd need
        // proper type mapping based on the model's input metadata
        var inputs = new List<NamedOnnxValue>();

        try
        {
            // For demonstration, assume input is a dictionary or simple array
            if (input is Dictionary<string, object> inputDict)
            {
                foreach (var kvp in inputDict)
                {
                    if (kvp.Value is float[] floatArray)
                    {
                        var tensor = new DenseTensor<float>(floatArray, new[] { 1, floatArray.Length });
                        inputs.Add(NamedOnnxValue.CreateFromTensor(kvp.Key, tensor));
                    }
                    else if (kvp.Value is float[,] float2D)
                    {
                        var dimensions = new[] { float2D.GetLength(0), float2D.GetLength(1) };
                        var tensor = new DenseTensor<float>(float2D.Cast<float>().ToArray(), dimensions);
                        inputs.Add(NamedOnnxValue.CreateFromTensor(kvp.Key, tensor));
                    }
                }
            }
            else if (input is float[] singleArray)
            {
                // Use the first input name from the model metadata
                var firstInputName = session.InputMetadata.Keys.First();
                var tensor = new DenseTensor<float>(singleArray, new[] { 1, singleArray.Length });
                inputs.Add(NamedOnnxValue.CreateFromTensor(firstInputName, tensor));
            }

            if (inputs.Count == 0)
            {
                throw new ArgumentException($"Cannot convert input of type {input.GetType()} to ONNX format");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting input to ONNX format");
            throw;
        }

        return inputs;
    }

    private object ConvertFromOnnxOutput(IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results)
    {
        try
        {
            // Convert ONNX results back to a usable format
            var output = new Dictionary<string, object>();

            foreach (var result in results)
            {
                if (result.Value is Tensor<float> floatTensor)
                {
                    output[result.Name] = floatTensor.ToArray();
                }
                else if (result.Value is Tensor<int> intTensor)
                {
                    output[result.Name] = intTensor.ToArray();
                }
                else
                {
                    output[result.Name] = result.Value;
                }
            }

            // If there's only one output, return it directly
            if (output.Count == 1)
            {
                return output.Values.First();
            }

            return output;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting ONNX output");
            throw;
        }
    }

    private Task<IEnumerable<object>> ExecuteBatchInternal(OnnxInferenceSession session, object[] batch, CancellationToken cancellationToken)
    {
        // In a real implementation, this would create a batched tensor
        // For now, execute sequentially
        var results = new List<object>();
        foreach (var input in batch)
        {
            var result = ExecuteSingleInternal(session, input, cancellationToken);
            results.Add(result);
        }
        return Task.FromResult<IEnumerable<object>>(results);
    }

    private Task<object> ExecuteSingleInternal(OnnxInferenceSession session, object input, CancellationToken cancellationToken)
    {
        var onnxInputs = ConvertToOnnxInput(input, session.OnnxSession);
        using var results = session.OnnxSession.Run(onnxInputs);
        return Task.FromResult(ConvertFromOnnxOutput(results));
    }

    #endregion

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _logger.LogDebug("Disposing LocalInferenceEngine");

        try
        {
            _disposed = true;

            // Dispose all sessions
            var sessions = _sessions.Values.ToList();
            _sessions.Clear();

            foreach (var session in sessions)
            {
                _ = Task.Run(async () => await session.DisposeAsync());
            }

            _batchSemaphore.Dispose();

            _logger.LogDebug("LocalInferenceEngine disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing LocalInferenceEngine");
        }

        return ValueTask.CompletedTask;
    }

    #region OnnxInferenceSession Implementation

    private sealed class OnnxInferenceSession : IInferenceSession
    {
        private readonly ILogger _logger;
        private InferenceMetrics _metrics = new();
        private bool _disposed = false;

        public string ModelId { get; }
        public ExecutionProvider Provider { get; }
        public DateTime CreatedAt { get; }
        public InferenceMetrics Metrics => _metrics;
        public InferenceSession OnnxSession { get; }

        public OnnxInferenceSession(string modelId, ExecutionProvider provider, InferenceSession onnxSession, ILogger logger)
        {
            ModelId = modelId;
            Provider = provider;
            CreatedAt = DateTime.UtcNow;
            OnnxSession = onnxSession;
            _logger = logger;
        }

        public void UpdateMetrics(double durationMs, int operationCount)
        {
            _metrics = new InferenceMetrics
            {
                InferenceTimeMs = durationMs,
                OperationsPerSecond = operationCount / (durationMs / 1000.0),
                MemoryUsageBytes = GC.GetTotalMemory(false),
                RecordedAt = DateTime.UtcNow
            };
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;

            try
            {
                _disposed = true;
                OnnxSession.Dispose();
                _logger.LogDebug("Disposed inference session for model {ModelId}", ModelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing inference session for model {ModelId}", ModelId);
            }

            return ValueTask.CompletedTask;
        }
    }

    #endregion
}
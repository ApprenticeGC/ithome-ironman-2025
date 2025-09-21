using GameConsole.AI.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace GameConsole.AI.Local;

/// <summary>
/// Local Inference Engine for AI model execution with batching and scheduling.
/// Provides high-performance inference using ONNX Runtime with optimization support.
/// </summary>
[Service("AI", "Inference", "Local")]
public class LocalInferenceEngine : ILocalInferenceCapability, IAsyncDisposable
{
    private readonly ILogger<LocalInferenceEngine> _logger;
    private readonly ConcurrentDictionary<string, InferenceSession> _sessions = new();
    private readonly ConcurrentDictionary<string, SessionMetadata> _sessionMetadata = new();
    private readonly SemaphoreSlim _sessionSemaphore;
    private bool _disposed;

    public LocalInferenceEngine(ILogger<LocalInferenceEngine> logger, int maxConcurrentSessions = 4)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionSemaphore = new SemaphoreSlim(maxConcurrentSessions, maxConcurrentSessions);
        
        _logger.LogInformation("Initialized LocalInferenceEngine with max {MaxSessions} concurrent sessions", maxConcurrentSessions);
    }

    #region ILocalInferenceCapability Implementation

    public async Task<string> CreateInferenceSessionAsync(AIModel model, ResourceConfiguration config, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LocalInferenceEngine));

        var sessionId = Guid.NewGuid().ToString();
        
        try
        {
            await _sessionSemaphore.WaitAsync(cancellationToken);

            _logger.LogInformation("Creating inference session for model: {ModelId} -> {SessionId}", model.Id, sessionId);

            // Create session options with optimization
            var sessionOptions = CreateSessionOptions(config);
            
            // Create ONNX Runtime session
            var session = new InferenceSession(model.Path, sessionOptions);
            
            _sessions[sessionId] = session;
            _sessionMetadata[sessionId] = new SessionMetadata(
                sessionId,
                model.Id,
                config,
                DateTime.UtcNow,
                0,
                TimeSpan.Zero
            );

            _logger.LogInformation("Created inference session: {SessionId} for model: {ModelId}", sessionId, model.Id);
            return sessionId;
        }
        catch (Exception ex)
        {
            _sessionSemaphore.Release();
            _logger.LogError(ex, "Failed to create inference session for model: {ModelId}", model.Id);
            throw new InvalidOperationException($"Failed to create inference session: {ex.Message}", ex);
        }
    }

    public async Task DestroyInferenceSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LocalInferenceEngine));

        if (_sessions.TryRemove(sessionId, out var session))
        {
            try
            {
                session.Dispose();
                _sessionMetadata.TryRemove(sessionId, out _);
                _sessionSemaphore.Release();
                
                _logger.LogInformation("Destroyed inference session: {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error destroying inference session: {SessionId}", sessionId);
            }
        }
        else
        {
            _logger.LogWarning("Attempted to destroy non-existent session: {SessionId}", sessionId);
        }

        await Task.CompletedTask;
    }

    public async Task<Dictionary<string, object>> ExecuteInferenceAsync(string sessionId, Dictionary<string, object> inputs, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LocalInferenceEngine));

        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new ArgumentException($"Inference session not found: {sessionId}");

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogDebug("Executing inference: {SessionId}", sessionId);

            // Convert inputs to ONNX format
            var namedOnnxValues = ConvertInputsToOnnx(inputs);

            // Run inference
            using var results = session.Run(namedOnnxValues);
            
            // Convert outputs back to dictionary
            var outputs = ConvertOnnxOutputs(results);
            
            stopwatch.Stop();
            
            // Update session metadata
            UpdateSessionStats(sessionId, stopwatch.Elapsed);

            _logger.LogDebug("Inference completed: {SessionId} in {ElapsedMs}ms", 
                sessionId, stopwatch.ElapsedMilliseconds);

            return outputs;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Inference failed: {SessionId} after {ElapsedMs}ms", 
                sessionId, stopwatch.ElapsedMilliseconds);
            throw new InvalidOperationException($"Inference failed: {ex.Message}", ex);
        }
        finally
        {
            await Task.CompletedTask; // Keep async signature
        }
    }

    public async Task<(int InferenceCount, TimeSpan TotalTime, TimeSpan AverageTime)> GetSessionStatsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LocalInferenceEngine));

        if (_sessionMetadata.TryGetValue(sessionId, out var metadata))
        {
            var avgTime = metadata.InferenceCount > 0 
                ? TimeSpan.FromTicks(metadata.TotalExecutionTime.Ticks / metadata.InferenceCount)
                : TimeSpan.Zero;

            return await Task.FromResult((
                InferenceCount: metadata.InferenceCount,
                TotalTime: metadata.TotalExecutionTime,
                AverageTime: avgTime
            ));
        }

        return await Task.FromResult((0, TimeSpan.Zero, TimeSpan.Zero));
    }

    #endregion

    #region ICapabilityProvider Implementation

    public async Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new[] { typeof(ILocalInferenceCapability) });
    }

    public async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(typeof(T) == typeof(ILocalInferenceCapability));
    }

    public async Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(ILocalInferenceCapability))
            return await Task.FromResult(this as T);
        
        return await Task.FromResult<T?>(null);
    }

    #endregion

    #region Batch Inference Support

    public async Task<IEnumerable<Dictionary<string, object>>> ExecuteBatchInferenceAsync(
        string sessionId, 
        IEnumerable<Dictionary<string, object>> batchInputs, 
        CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(LocalInferenceEngine));

        var results = new List<Dictionary<string, object>>();
        
        // Simple sequential processing - could be optimized with parallel batching
        foreach (var inputs in batchInputs)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
                
            var result = await ExecuteInferenceAsync(sessionId, inputs, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    #endregion

    #region Private Helpers

    private static SessionOptions CreateSessionOptions(ResourceConfiguration config)
    {
        var options = new SessionOptions();
        
        // Set execution provider based on device
        switch (config.PreferredDevice)
        {
            case ExecutionDevice.CUDA:
                options.AppendExecutionProvider_CUDA(0);
                break;
            case ExecutionDevice.DirectML:
                options.AppendExecutionProvider_DML(0);
                break;
            case ExecutionDevice.CPU:
            default:
                // CPU is the default
                break;
        }

        // Set optimization level
        options.GraphOptimizationLevel = config.OptimizationLevel switch
        {
            OptimizationLevel.None => GraphOptimizationLevel.ORT_DISABLE_ALL,
            OptimizationLevel.Basic => GraphOptimizationLevel.ORT_ENABLE_BASIC,
            OptimizationLevel.Aggressive => GraphOptimizationLevel.ORT_ENABLE_EXTENDED,
            OptimizationLevel.Maximum => GraphOptimizationLevel.ORT_ENABLE_ALL,
            _ => GraphOptimizationLevel.ORT_ENABLE_BASIC
        };

        // Set thread options for CPU
        if (config.PreferredDevice == ExecutionDevice.CPU)
        {
            options.IntraOpNumThreads = Math.Min(Environment.ProcessorCount, config.MaxConcurrentInferences);
        }

        return options;
    }

    private static IReadOnlyCollection<NamedOnnxValue> ConvertInputsToOnnx(Dictionary<string, object> inputs)
    {
        var onnxInputs = new List<NamedOnnxValue>();
        
        foreach (var kvp in inputs)
        {
            // Simplified conversion - in a real implementation, this would handle various tensor types
            if (kvp.Value is float[] floatArray)
            {
                var tensor = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<float>(floatArray, new int[] { 1, floatArray.Length });
                onnxInputs.Add(NamedOnnxValue.CreateFromTensor(kvp.Key, tensor));
            }
            else if (kvp.Value is int[] intArray)
            {
                var tensor = new Microsoft.ML.OnnxRuntime.Tensors.DenseTensor<int>(intArray, new int[] { 1, intArray.Length });
                onnxInputs.Add(NamedOnnxValue.CreateFromTensor(kvp.Key, tensor));
            }
            // Add more type conversions as needed
        }

        return onnxInputs;
    }

    private static Dictionary<string, object> ConvertOnnxOutputs(IReadOnlyCollection<DisposableNamedOnnxValue> outputs)
    {
        var result = new Dictionary<string, object>();
        
        foreach (var output in outputs)
        {
            // Simplified conversion - in a real implementation, this would handle various tensor types
            if (output.Value is Microsoft.ML.OnnxRuntime.Tensors.Tensor<float> floatTensor)
            {
                result[output.Name] = floatTensor.ToArray();
            }
            else if (output.Value is Microsoft.ML.OnnxRuntime.Tensors.Tensor<int> intTensor)
            {
                result[output.Name] = intTensor.ToArray();
            }
            // Add more type conversions as needed
        }

        return result;
    }

    private void UpdateSessionStats(string sessionId, TimeSpan executionTime)
    {
        if (_sessionMetadata.TryGetValue(sessionId, out var metadata))
        {
            var updated = metadata with
            {
                InferenceCount = metadata.InferenceCount + 1,
                TotalExecutionTime = metadata.TotalExecutionTime + executionTime
            };
            _sessionMetadata[sessionId] = updated;
        }
    }

    #endregion

    #region IAsyncDisposable Implementation

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await Task.Yield(); // Make truly async
            _logger.LogDebug("Disposing LocalInferenceEngine");

            // Dispose all sessions
            foreach (var kvp in _sessions)
            {
                try
                {
                    kvp.Value.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing inference session: {SessionId}", kvp.Key);
                }
            }

            _sessions.Clear();
            _sessionMetadata.Clear();
            _sessionSemaphore.Dispose();
            
            _disposed = true;
            _logger.LogDebug("Disposed LocalInferenceEngine");
        }
        GC.SuppressFinalize(this);
    }

    #endregion

    private record SessionMetadata(
        string SessionId,
        string ModelId,
        ResourceConfiguration Config,
        DateTime CreatedAt,
        int InferenceCount,
        TimeSpan TotalExecutionTime
    );
}
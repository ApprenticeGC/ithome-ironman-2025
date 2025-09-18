namespace GameConsole.AI.Local;

/// <summary>
/// Configuration for model loading and optimization.
/// </summary>
public class ModelConfiguration
{
    /// <summary>
    /// Gets or sets the execution provider to use.
    /// </summary>
    public ExecutionProvider ExecutionProvider { get; set; } = ExecutionProvider.CPU;

    /// <summary>
    /// Gets or sets whether to enable quantization.
    /// </summary>
    public bool EnableQuantization { get; set; } = true;

    /// <summary>
    /// Gets or sets the quantization mode.
    /// </summary>
    public QuantizationMode QuantizationMode { get; set; } = QuantizationMode.Dynamic;

    /// <summary>
    /// Gets or sets the maximum batch size for the model.
    /// </summary>
    public int MaxBatchSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to enable model optimization.
    /// </summary>
    public bool EnableOptimization { get; set; } = true;

    /// <summary>
    /// Gets or sets custom configuration options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the memory pool size in MB.
    /// </summary>
    public long MemoryPoolSizeMB { get; set; } = 512;
}

/// <summary>
/// Inference request for model execution.
/// </summary>
public class InferenceRequest
{
    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the input data for inference.
    /// </summary>
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>
    /// Gets or sets the inference priority.
    /// </summary>
    public InferencePriority Priority { get; set; } = InferencePriority.Normal;

    /// <summary>
    /// Gets or sets the timeout in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets additional inference options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the request was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Result of an inference operation.
/// </summary>
public class InferenceResult
{
    /// <summary>
    /// Gets or sets the request identifier that generated this result.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the output data from inference.
    /// </summary>
    public Dictionary<string, object> OutputData { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the inference was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets any error message if inference failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the inference execution time in milliseconds.
    /// </summary>
    public double ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the preprocessing time in milliseconds.
    /// </summary>
    public double PreprocessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the postprocessing time in milliseconds.
    /// </summary>
    public double PostprocessingTimeMs { get; set; }

    /// <summary>
    /// Gets or sets additional result metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when inference was completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Performance metrics for the inference engine.
/// </summary>
public class InferencePerformanceMetrics
{
    /// <summary>
    /// Gets or sets the total number of inferences processed.
    /// </summary>
    public long TotalInferences { get; set; }

    /// <summary>
    /// Gets or sets the number of successful inferences.
    /// </summary>
    public long SuccessfulInferences { get; set; }

    /// <summary>
    /// Gets or sets the number of failed inferences.
    /// </summary>
    public long FailedInferences { get; set; }

    /// <summary>
    /// Gets or sets the average inference time in milliseconds.
    /// </summary>
    public double AverageInferenceTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the minimum inference time in milliseconds.
    /// </summary>
    public double MinInferenceTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the maximum inference time in milliseconds.
    /// </summary>
    public double MaxInferenceTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the current throughput in inferences per second.
    /// </summary>
    public double ThroughputPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the number of batched inferences.
    /// </summary>
    public long BatchedInferences { get; set; }

    /// <summary>
    /// Gets or sets the average batch size.
    /// </summary>
    public double AverageBatchSize { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when metrics were last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalInferences > 0 ? (double)SuccessfulInferences / TotalInferences * 100 : 0;
}

/// <summary>
/// Information about a loaded model.
/// </summary>
public class LoadedModelInfo
{
    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model format.
    /// </summary>
    public ModelFormat Format { get; set; }

    /// <summary>
    /// Gets or sets the execution provider being used.
    /// </summary>
    public ExecutionProvider ExecutionProvider { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in MB.
    /// </summary>
    public long MemoryUsageMB { get; set; }

    /// <summary>
    /// Gets or sets when the model was loaded.
    /// </summary>
    public DateTimeOffset LoadedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of inferences performed with this model.
    /// </summary>
    public long InferenceCount { get; set; }

    /// <summary>
    /// Gets or sets whether the model is currently in use.
    /// </summary>
    public bool IsInUse { get; set; }

    /// <summary>
    /// Gets or sets the last time the model was used.
    /// </summary>
    public DateTimeOffset LastUsedAt { get; set; }
}
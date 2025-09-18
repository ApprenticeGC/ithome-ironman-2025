namespace GameConsole.AI.Local;

/// <summary>
/// Runtime capabilities information for the local AI system.
/// </summary>
public class AIRuntimeCapabilities
{
    /// <summary>
    /// Gets or sets the supported AI frameworks.
    /// </summary>
    public IEnumerable<SupportedFramework> SupportedFrameworks { get; set; } = [];

    /// <summary>
    /// Gets or sets the available execution providers.
    /// </summary>
    public IEnumerable<ExecutionProvider> AvailableProviders { get; set; } = [];

    /// <summary>
    /// Gets or sets the maximum supported model size in MB.
    /// </summary>
    public long MaxModelSizeMB { get; set; }

    /// <summary>
    /// Gets or sets whether GPU acceleration is available.
    /// </summary>
    public bool HasGpuAcceleration { get; set; }

    /// <summary>
    /// Gets or sets the total system memory in MB.
    /// </summary>
    public long TotalSystemMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the total GPU memory in MB (if GPU available).
    /// </summary>
    public long TotalGpuMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the number of CPU cores available.
    /// </summary>
    public int CpuCoreCount { get; set; }
}

/// <summary>
/// Information about a supported AI framework.
/// </summary>
public class SupportedFramework
{
    /// <summary>
    /// Gets or sets the framework type.
    /// </summary>
    public AIFramework Framework { get; set; }

    /// <summary>
    /// Gets or sets the framework version.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the framework is currently available.
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Gets or sets the supported model formats.
    /// </summary>
    public IEnumerable<ModelFormat> SupportedFormats { get; set; } = [];

    /// <summary>
    /// Gets or sets any limitations or notes about the framework.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// AI inference request containing input data and configuration.
/// </summary>
public class AIInferenceRequest
{
    /// <summary>
    /// Gets or sets the model identifier to use for inference.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the input data for inference.
    /// </summary>
    public Dictionary<string, object> InputData { get; set; } = new();

    /// <summary>
    /// Gets or sets the inference priority.
    /// </summary>
    public InferencePriority Priority { get; set; } = InferencePriority.Normal;

    /// <summary>
    /// Gets or sets the timeout for the inference operation in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets additional inference options.
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the request identifier for tracking.
    /// </summary>
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// AI inference result containing output data and performance metrics.
/// </summary>
public class AIInferenceResult
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
    /// Gets or sets the model used for inference.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional metadata about the inference.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp when the inference was completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Performance metrics for the AI runtime.
/// </summary>
public class AIPerformanceMetrics
{
    /// <summary>
    /// Gets or sets the total number of inferences processed.
    /// </summary>
    public long TotalInferences { get; set; }

    /// <summary>
    /// Gets or sets the average inference time in milliseconds.
    /// </summary>
    public double AverageInferenceTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the current throughput in inferences per second.
    /// </summary>
    public double ThroughputPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the current resource usage statistics.
    /// </summary>
    public ResourceUsageStatistics ResourceUsage { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of successful inferences.
    /// </summary>
    public long SuccessfulInferences { get; set; }

    /// <summary>
    /// Gets or sets the number of failed inferences.
    /// </summary>
    public long FailedInferences { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when metrics were last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate => TotalInferences > 0 ? (double)SuccessfulInferences / TotalInferences * 100 : 0;
}
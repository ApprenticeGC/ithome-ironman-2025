namespace GameConsole.AI.Local;

/// <summary>
/// Configuration for the local AI runtime deployment.
/// </summary>
public class LocalAIConfiguration
{
    /// <summary>
    /// Gets or sets the resource management configuration.
    /// </summary>
    public ResourceConfiguration Resources { get; set; } = new();

    /// <summary>
    /// Gets or sets the model cache configuration.
    /// </summary>
    public ModelCacheConfiguration ModelCache { get; set; } = new();

    /// <summary>
    /// Gets or sets the inference engine configuration.
    /// </summary>
    public InferenceConfiguration Inference { get; set; } = new();

    /// <summary>
    /// Gets or sets the performance monitoring configuration.
    /// </summary>
    public PerformanceConfiguration Performance { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable automatic resource optimization.
    /// </summary>
    public bool EnableAutoOptimization { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of concurrent inference operations.
    /// </summary>
    public int MaxConcurrentInferences { get; set; } = 4;

    /// <summary>
    /// Gets or sets the default timeout for AI operations in seconds.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Configuration for resource management and allocation.
/// </summary>
public class ResourceConfiguration
{
    /// <summary>
    /// Gets or sets the maximum GPU memory usage in MB.
    /// </summary>
    public long MaxGpuMemoryMB { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the maximum CPU usage percentage.
    /// </summary>
    public int MaxCpuUsagePercent { get; set; } = 80;

    /// <summary>
    /// Gets or sets the preferred execution provider for ONNX Runtime.
    /// </summary>
    public ExecutionProvider PreferredProvider { get; set; } = ExecutionProvider.CPU;

    /// <summary>
    /// Gets or sets whether to enable GPU acceleration if available.
    /// </summary>
    public bool EnableGpuAcceleration { get; set; } = true;

    /// <summary>
    /// Gets or sets the resource allocation strategy.
    /// </summary>
    public ResourceAllocationStrategy AllocationStrategy { get; set; } = ResourceAllocationStrategy.Balanced;
}

/// <summary>
/// Configuration for model caching and storage.
/// </summary>
public class ModelCacheConfiguration
{
    /// <summary>
    /// Gets or sets the cache directory path.
    /// </summary>
    public string CacheDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameConsole", "AI", "Models");

    /// <summary>
    /// Gets or sets the maximum cache size in MB.
    /// </summary>
    public long MaxCacheSizeMB { get; set; } = 10240; // 10 GB

    /// <summary>
    /// Gets or sets whether to enable model compression.
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache eviction policy.
    /// </summary>
    public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.LeastRecentlyUsed;

    /// <summary>
    /// Gets or sets the cache cleanup interval in hours.
    /// </summary>
    public int CleanupIntervalHours { get; set; } = 24;
}

/// <summary>
/// Configuration for inference engine optimization.
/// </summary>
public class InferenceConfiguration
{
    /// <summary>
    /// Gets or sets the maximum batch size for inference operations.
    /// </summary>
    public int MaxBatchSize { get; set; } = 32;

    /// <summary>
    /// Gets or sets whether to enable dynamic batching.
    /// </summary>
    public bool EnableDynamicBatching { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch timeout in milliseconds.
    /// </summary>
    public int BatchTimeoutMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to enable model quantization.
    /// </summary>
    public bool EnableQuantization { get; set; } = true;

    /// <summary>
    /// Gets or sets the quantization mode.
    /// </summary>
    public QuantizationMode QuantizationMode { get; set; } = QuantizationMode.Dynamic;
}

/// <summary>
/// Configuration for performance monitoring and metrics.
/// </summary>
public class PerformanceConfiguration
{
    /// <summary>
    /// Gets or sets whether to enable detailed performance metrics collection.
    /// </summary>
    public bool EnableDetailedMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets the metrics collection interval in seconds.
    /// </summary>
    public int MetricsIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to enable performance profiling.
    /// </summary>
    public bool EnableProfiling { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of performance samples to retain.
    /// </summary>
    public int MaxPerformanceSamples { get; set; } = 1000;
}
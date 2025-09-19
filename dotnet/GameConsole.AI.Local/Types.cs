namespace GameConsole.AI.Local;

/// <summary>
/// Represents performance metrics for AI model inference operations.
/// </summary>
public sealed class InferenceMetrics
{
    /// <summary>
    /// Gets or sets the time taken for model loading in milliseconds.
    /// </summary>
    public double LoadTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the time taken for inference execution in milliseconds.
    /// </summary>
    public double InferenceTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the memory usage in bytes during inference.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of operations processed per second.
    /// </summary>
    public double OperationsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the GPU utilization percentage (0-100).
    /// </summary>
    public double GpuUtilizationPercent { get; set; }

    /// <summary>
    /// Gets or sets the CPU utilization percentage (0-100).
    /// </summary>
    public double CpuUtilizationPercent { get; set; }

    /// <summary>
    /// Gets the timestamp when these metrics were recorded.
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents resource constraints for AI model execution.
/// </summary>
public sealed class ResourceConstraints
{
    /// <summary>
    /// Gets or sets the maximum memory allocation in bytes.
    /// </summary>
    public long MaxMemoryBytes { get; set; } = 2L * 1024 * 1024 * 1024; // 2GB default

    /// <summary>
    /// Gets or sets the maximum CPU utilization percentage (0-100).
    /// </summary>
    public double MaxCpuUtilizationPercent { get; set; } = 80.0;

    /// <summary>
    /// Gets or sets the maximum GPU utilization percentage (0-100).
    /// </summary>
    public double MaxGpuUtilizationPercent { get; set; } = 90.0;

    /// <summary>
    /// Gets or sets the maximum number of concurrent inference operations.
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 4;

    /// <summary>
    /// Gets or sets the timeout for inference operations.
    /// </summary>
    public TimeSpan InferenceTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Represents configuration for model quantization.
/// </summary>
public sealed class QuantizationConfig
{
    /// <summary>
    /// Gets or sets the quantization level.
    /// </summary>
    public QuantizationLevel Level { get; set; } = QuantizationLevel.Dynamic;

    /// <summary>
    /// Gets or sets whether to use GPU acceleration for quantization.
    /// </summary>
    public bool UseGpuAcceleration { get; set; } = true;

    /// <summary>
    /// Gets or sets the calibration dataset size for static quantization.
    /// </summary>
    public int CalibrationDatasetSize { get; set; } = 100;
}

/// <summary>
/// Defines the level of quantization to apply to models.
/// </summary>
public enum QuantizationLevel
{
    /// <summary>
    /// No quantization applied - full precision.
    /// </summary>
    None,

    /// <summary>
    /// Dynamic quantization at runtime.
    /// </summary>
    Dynamic,

    /// <summary>
    /// Static quantization using calibration data.
    /// </summary>
    Static,

    /// <summary>
    /// Aggressive quantization for maximum performance.
    /// </summary>
    Aggressive
}

/// <summary>
/// Represents the execution provider for AI inference.
/// </summary>
public enum ExecutionProvider
{
    /// <summary>
    /// CPU-based execution.
    /// </summary>
    Cpu,

    /// <summary>
    /// CUDA GPU execution (NVIDIA).
    /// </summary>
    Cuda,

    /// <summary>
    /// DirectML execution (Windows).
    /// </summary>
    DirectMl,

    /// <summary>
    /// OpenVINO execution (Intel).
    /// </summary>
    OpenVino,

    /// <summary>
    /// Automatic selection based on available hardware.
    /// </summary>
    Auto
}
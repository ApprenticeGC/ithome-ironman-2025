namespace GameConsole.AI.Local;

/// <summary>
/// Execution provider types for ONNX Runtime.
/// </summary>
public enum ExecutionProvider
{
    /// <summary>
    /// CPU execution provider.
    /// </summary>
    CPU,

    /// <summary>
    /// CUDA GPU execution provider.
    /// </summary>
    CUDA,

    /// <summary>
    /// DirectML execution provider for Windows.
    /// </summary>
    DirectML,

    /// <summary>
    /// CoreML execution provider for macOS.
    /// </summary>
    CoreML,

    /// <summary>
    /// OpenVINO execution provider for Intel hardware.
    /// </summary>
    OpenVINO
}

/// <summary>
/// Resource allocation strategies for optimizing performance.
/// </summary>
public enum ResourceAllocationStrategy
{
    /// <summary>
    /// Balanced allocation between performance and memory usage.
    /// </summary>
    Balanced,

    /// <summary>
    /// Prioritize performance over memory usage.
    /// </summary>
    Performance,

    /// <summary>
    /// Prioritize memory efficiency over performance.
    /// </summary>
    MemoryEfficient,

    /// <summary>
    /// Conservative allocation with minimal resource usage.
    /// </summary>
    Conservative
}

/// <summary>
/// Cache eviction policies for model management.
/// </summary>
public enum CacheEvictionPolicy
{
    /// <summary>
    /// Least recently used models are evicted first.
    /// </summary>
    LeastRecentlyUsed,

    /// <summary>
    /// Least frequently used models are evicted first.
    /// </summary>
    LeastFrequentlyUsed,

    /// <summary>
    /// First in, first out eviction policy.
    /// </summary>
    FirstInFirstOut,

    /// <summary>
    /// Largest models are evicted first to free up space.
    /// </summary>
    LargestFirst
}

/// <summary>
/// Model quantization modes for performance optimization.
/// </summary>
public enum QuantizationMode
{
    /// <summary>
    /// No quantization applied.
    /// </summary>
    None,

    /// <summary>
    /// Dynamic quantization applied during inference.
    /// </summary>
    Dynamic,

    /// <summary>
    /// Static quantization with calibration data.
    /// </summary>
    Static,

    /// <summary>
    /// Mixed precision quantization.
    /// </summary>
    MixedPrecision
}

/// <summary>
/// Resource types for allocation and monitoring.
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// CPU processing resources.
    /// </summary>
    CPU,

    /// <summary>
    /// GPU processing resources.
    /// </summary>
    GPU,

    /// <summary>
    /// System memory resources.
    /// </summary>
    Memory,

    /// <summary>
    /// GPU memory resources.
    /// </summary>
    GpuMemory,

    /// <summary>
    /// Storage resources for model caching.
    /// </summary>
    Storage
}

/// <summary>
/// AI framework types supported by the runtime.
/// </summary>
public enum AIFramework
{
    /// <summary>
    /// ONNX Runtime framework.
    /// </summary>
    ONNX,

    /// <summary>
    /// TensorFlow Lite framework.
    /// </summary>
    TensorFlowLite,

    /// <summary>
    /// PyTorch Mobile framework.
    /// </summary>
    PyTorchMobile,

    /// <summary>
    /// DirectML framework.
    /// </summary>
    DirectML,

    /// <summary>
    /// OpenVINO framework.
    /// </summary>
    OpenVINO
}

/// <summary>
/// Model format types for inference engines.
/// </summary>
public enum ModelFormat
{
    /// <summary>
    /// ONNX model format.
    /// </summary>
    ONNX,

    /// <summary>
    /// TensorFlow Lite model format.
    /// </summary>
    TFLite,

    /// <summary>
    /// PyTorch model format.
    /// </summary>
    PyTorch,

    /// <summary>
    /// CoreML model format.
    /// </summary>
    CoreML,

    /// <summary>
    /// OpenVINO Intermediate Representation format.
    /// </summary>
    OpenVINO
}

/// <summary>
/// Inference priority levels for scheduling.
/// </summary>
public enum InferencePriority
{
    /// <summary>
    /// Low priority inference that can be delayed.
    /// </summary>
    Low,

    /// <summary>
    /// Normal priority inference.
    /// </summary>
    Normal,

    /// <summary>
    /// High priority inference that should be processed quickly.
    /// </summary>
    High,

    /// <summary>
    /// Critical priority inference that should be processed immediately.
    /// </summary>
    Critical
}
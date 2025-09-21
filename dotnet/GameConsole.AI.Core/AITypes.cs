namespace GameConsole.AI.Services;

/// <summary>
/// Represents the available AI inference frameworks for local deployment.
/// </summary>
public enum AIFramework
{
    /// <summary>
    /// ONNX Runtime for cross-platform inference.
    /// </summary>
    OnnxRuntime,
    
    /// <summary>
    /// PyTorch inference engine.
    /// </summary>
    PyTorch,
    
    /// <summary>
    /// TensorFlow Lite for mobile/edge deployment.
    /// </summary>
    TensorFlowLite,
    
    /// <summary>
    /// Custom inference implementation.
    /// </summary>
    Custom
}

/// <summary>
/// Defines the execution device for AI inference.
/// </summary>
public enum ExecutionDevice
{
    /// <summary>
    /// CPU execution (fallback default).
    /// </summary>
    CPU,
    
    /// <summary>
    /// NVIDIA GPU with CUDA.
    /// </summary>
    CUDA,
    
    /// <summary>
    /// AMD GPU with ROCm.
    /// </summary>
    ROCm,
    
    /// <summary>
    /// Apple Silicon GPU (Metal Performance Shaders).
    /// </summary>
    Metal,
    
    /// <summary>
    /// DirectX GPU acceleration on Windows.
    /// </summary>
    DirectML
}

/// <summary>
/// Configuration for AI model optimization.
/// </summary>
public enum OptimizationLevel
{
    /// <summary>
    /// No optimization - use original model.
    /// </summary>
    None,
    
    /// <summary>
    /// Basic optimizations that preserve accuracy.
    /// </summary>
    Basic,
    
    /// <summary>
    /// Aggressive optimizations that may slightly reduce accuracy.
    /// </summary>
    Aggressive,
    
    /// <summary>
    /// Maximum optimization for best performance.
    /// </summary>
    Maximum
}

/// <summary>
/// Represents an AI model resource with metadata.
/// </summary>
public record AIModel(
    string Id,
    string Name,
    string Path,
    AIFramework Framework,
    long SizeBytes,
    string Version,
    Dictionary<string, object> Metadata,
    DateTime LastAccessed
);

/// <summary>
/// Configuration for AI resource allocation.
/// </summary>
public record ResourceConfiguration(
    ExecutionDevice PreferredDevice,
    long MaxMemoryMB,
    int MaxConcurrentInferences,
    TimeSpan InferenceTimeoutMs,
    OptimizationLevel OptimizationLevel
);

/// <summary>
/// Result of an AI inference operation.
/// </summary>
public record InferenceResult(
    string RequestId,
    Dictionary<string, object> Outputs,
    TimeSpan ExecutionTime,
    bool Success,
    string? ErrorMessage = null
);

/// <summary>
/// Request for AI inference.
/// </summary>
public record InferenceRequest(
    string RequestId,
    string ModelId,
    Dictionary<string, object> Inputs,
    ResourceConfiguration? OverrideConfig = null
);

/// <summary>
/// Statistics about resource usage.
/// </summary>
public record ResourceStats(
    long MemoryUsedMB,
    long MemoryAvailableMB,
    double CpuUsagePercent,
    double GpuUsagePercent,
    int ActiveInferences,
    int QueuedInferences
);
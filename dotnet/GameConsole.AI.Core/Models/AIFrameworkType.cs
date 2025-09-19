namespace GameConsole.AI.Models;

/// <summary>
/// Defines the supported AI framework types for agent compatibility.
/// </summary>
public enum AIFrameworkType
{
    /// <summary>
    /// Open Neural Network Exchange (ONNX) framework.
    /// </summary>
    ONNX,

    /// <summary>
    /// TensorFlow framework.
    /// </summary>
    TensorFlow,

    /// <summary>
    /// PyTorch framework.
    /// </summary>
    PyTorch,

    /// <summary>
    /// OpenVINO framework for Intel hardware optimization.
    /// </summary>
    OpenVINO,

    /// <summary>
    /// DirectML framework for Windows ML acceleration.
    /// </summary>
    DirectML,

    /// <summary>
    /// Core ML framework for Apple platforms.
    /// </summary>
    CoreML,

    /// <summary>
    /// Custom or proprietary AI framework.
    /// </summary>
    Custom
}

/// <summary>
/// Defines the types of processing units available for AI computation.
/// </summary>
public enum AIProcessingUnit
{
    /// <summary>
    /// Central Processing Unit (CPU).
    /// </summary>
    CPU,

    /// <summary>
    /// Graphics Processing Unit (GPU).
    /// </summary>
    GPU,

    /// <summary>
    /// Neural Processing Unit (NPU) or AI accelerator.
    /// </summary>
    NPU,

    /// <summary>
    /// Tensor Processing Unit (TPU).
    /// </summary>
    TPU,

    /// <summary>
    /// Field-Programmable Gate Array (FPGA).
    /// </summary>
    FPGA
}
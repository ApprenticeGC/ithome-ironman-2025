namespace GameConsole.AI.Core;

/// <summary>
/// Represents different AI framework types supported by the system.
/// Used for framework compatibility and provider selection.
/// </summary>
public enum AIFrameworkType
{
    /// <summary>
    /// Unknown or unspecified framework type.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Open Neural Network Exchange (ONNX) runtime.
    /// </summary>
    ONNX = 1,

    /// <summary>
    /// TensorFlow framework.
    /// </summary>
    TensorFlow = 2,

    /// <summary>
    /// PyTorch framework.
    /// </summary>
    PyTorch = 3,

    /// <summary>
    /// OpenAI API-based services.
    /// </summary>
    OpenAI = 4,

    /// <summary>
    /// Local language models (e.g., Ollama).
    /// </summary>
    LocalLLM = 5,

    /// <summary>
    /// Azure Cognitive Services.
    /// </summary>
    Azure = 6,

    /// <summary>
    /// Custom or proprietary AI frameworks.
    /// </summary>
    Custom = 99
}
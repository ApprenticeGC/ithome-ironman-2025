namespace GameConsole.AI.Core;

/// <summary>
/// Represents training data for an AI agent.
/// </summary>
public interface IAIAgentTrainingData
{
    /// <summary>
    /// Gets the type of training data (e.g., "supervised", "reinforcement", "feedback").
    /// </summary>
    string DataType { get; }

    /// <summary>
    /// Gets the training data content.
    /// </summary>
    object Data { get; }

    /// <summary>
    /// Gets additional parameters for the training process.
    /// </summary>
    IReadOnlyDictionary<string, object> Parameters { get; }

    /// <summary>
    /// Gets the source or origin of the training data.
    /// </summary>
    string Source { get; }

    /// <summary>
    /// Gets the quality or confidence score of the training data.
    /// </summary>
    double QualityScore { get; }
}
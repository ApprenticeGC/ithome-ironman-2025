namespace GameConsole.AI.Core;

/// <summary>
/// Defines the capabilities and characteristics of an AI agent.
/// This provides metadata about what the agent can do and how it operates.
/// </summary>
public interface IAIAgentCapabilities
{
    /// <summary>
    /// Gets the types of decisions this AI agent can make.
    /// Examples: "pathfinding", "combat", "dialogue", "strategy"
    /// </summary>
    IReadOnlyList<string> DecisionTypes { get; }

    /// <summary>
    /// Gets whether this AI agent supports learning and adaptation.
    /// </summary>
    bool SupportsLearning { get; }

    /// <summary>
    /// Gets whether this AI agent can operate autonomously.
    /// </summary>
    bool SupportsAutonomousMode { get; }

    /// <summary>
    /// Gets the maximum number of simultaneous inputs this agent can process.
    /// </summary>
    int MaxConcurrentInputs { get; }

    /// <summary>
    /// Gets the processing priority level for this agent.
    /// Higher values indicate higher priority (0 = lowest, 100 = highest).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets additional capability metadata as key-value pairs.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Represents the current operational state of an AI agent.
/// </summary>
public interface IAIAgentState
{
    /// <summary>
    /// Gets whether the AI agent is currently active and processing inputs.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets whether the AI agent is currently learning from feedback.
    /// </summary>
    bool IsLearning { get; }

    /// <summary>
    /// Gets the number of decisions made by this agent since initialization.
    /// </summary>
    long DecisionCount { get; }

    /// <summary>
    /// Gets the timestamp of the last decision made by this agent.
    /// </summary>
    DateTime? LastDecisionTime { get; }

    /// <summary>
    /// Gets performance metrics for this agent.
    /// </summary>
    IAIAgentMetrics Metrics { get; }

    /// <summary>
    /// Gets current configuration and parameter values.
    /// </summary>
    IReadOnlyDictionary<string, object> Configuration { get; }
}

/// <summary>
/// Provides performance and operational metrics for an AI agent.
/// </summary>
public interface IAIAgentMetrics
{
    /// <summary>
    /// Gets the average processing time for decisions in milliseconds.
    /// </summary>
    double AverageProcessingTimeMs { get; }

    /// <summary>
    /// Gets the success rate of agent decisions (0.0 to 1.0).
    /// </summary>
    double SuccessRate { get; }

    /// <summary>
    /// Gets the current memory usage in bytes.
    /// </summary>
    long MemoryUsageBytes { get; }

    /// <summary>
    /// Gets additional metrics as key-value pairs.
    /// </summary>
    IReadOnlyDictionary<string, object> AdditionalMetrics { get; }
}

/// <summary>
/// Represents input data provided to an AI agent for processing.
/// </summary>
public interface IAIAgentInput
{
    /// <summary>
    /// Gets the type of input (e.g., "sensory", "query", "event").
    /// </summary>
    string InputType { get; }

    /// <summary>
    /// Gets the input data payload.
    /// </summary>
    object Data { get; }

    /// <summary>
    /// Gets the context in which this input was generated.
    /// </summary>
    IReadOnlyDictionary<string, object> Context { get; }

    /// <summary>
    /// Gets the priority of this input (0 = lowest, 100 = highest).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the timestamp when this input was created.
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
/// Represents the response or decision from an AI agent.
/// </summary>
public interface IAIAgentResponse
{
    /// <summary>
    /// Gets whether the agent successfully processed the input.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Gets the type of response (e.g., "action", "decision", "analysis").
    /// </summary>
    string ResponseType { get; }

    /// <summary>
    /// Gets the response data payload.
    /// </summary>
    object Data { get; }

    /// <summary>
    /// Gets the confidence level of this response (0.0 to 1.0).
    /// </summary>
    double Confidence { get; }

    /// <summary>
    /// Gets additional response metadata.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets error information if the response was unsuccessful.
    /// </summary>
    string? Error { get; }
}

/// <summary>
/// Represents feedback provided to an AI agent for learning and adaptation.
/// </summary>
public interface IAIAgentFeedback
{
    /// <summary>
    /// Gets the ID of the original response this feedback relates to.
    /// </summary>
    string ResponseId { get; }

    /// <summary>
    /// Gets the type of feedback (e.g., "reward", "correction", "evaluation").
    /// </summary>
    string FeedbackType { get; }

    /// <summary>
    /// Gets the feedback score or rating.
    /// Positive values indicate good performance, negative values indicate poor performance.
    /// </summary>
    double Score { get; }

    /// <summary>
    /// Gets detailed feedback data.
    /// </summary>
    object Data { get; }

    /// <summary>
    /// Gets additional feedback context and metadata.
    /// </summary>
    IReadOnlyDictionary<string, object> Context { get; }
}
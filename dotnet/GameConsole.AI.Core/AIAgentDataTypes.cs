namespace GameConsole.AI.Core;

/// <summary>
/// Default implementation of IAIAgentCapabilityRequirements.
/// </summary>
public class AIAgentCapabilityRequirements : IAIAgentCapabilityRequirements
{
    /// <inheritdoc />
    public IReadOnlyList<string> RequiredDecisionTypes { get; init; } = Array.Empty<string>();

    /// <inheritdoc />
    public bool RequiresLearning { get; init; }

    /// <inheritdoc />
    public bool RequiresAutonomousMode { get; init; }

    /// <inheritdoc />
    public int MinimumPriority { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> AdditionalRequirements { get; init; } = 
        new Dictionary<string, object>();
}

/// <summary>
/// Default implementation of IAIAgentInput.
/// </summary>
public class AIAgentInput : IAIAgentInput
{
    /// <inheritdoc />
    public string InputType { get; init; } = string.Empty;

    /// <inheritdoc />
    public object Data { get; init; } = new object();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Context { get; init; } = 
        new Dictionary<string, object>();

    /// <inheritdoc />
    public int Priority { get; init; } = 50;

    /// <inheritdoc />
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Default implementation of IAIAgentResponse.
/// </summary>
public class AIAgentResponse : IAIAgentResponse
{
    /// <inheritdoc />
    public bool Success { get; init; }

    /// <inheritdoc />
    public string ResponseType { get; init; } = string.Empty;

    /// <inheritdoc />
    public object Data { get; init; } = new object();

    /// <inheritdoc />
    public double Confidence { get; init; } = 1.0;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = 
        new Dictionary<string, object>();

    /// <inheritdoc />
    public string? Error { get; init; }
}

/// <summary>
/// Default implementation of IAIAgentFeedback.
/// </summary>
public class AIAgentFeedback : IAIAgentFeedback
{
    /// <inheritdoc />
    public string ResponseId { get; init; } = string.Empty;

    /// <inheritdoc />
    public string FeedbackType { get; init; } = string.Empty;

    /// <inheritdoc />
    public double Score { get; init; }

    /// <inheritdoc />
    public object Data { get; init; } = new object();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Context { get; init; } = 
        new Dictionary<string, object>();
}

/// <summary>
/// Default implementation of IAIAgentCapabilities.
/// </summary>
public class AIAgentCapabilities : IAIAgentCapabilities
{
    /// <inheritdoc />
    public IReadOnlyList<string> DecisionTypes { get; init; } = Array.Empty<string>();

    /// <inheritdoc />
    public bool SupportsLearning { get; init; }

    /// <inheritdoc />
    public bool SupportsAutonomousMode { get; init; }

    /// <inheritdoc />
    public int MaxConcurrentInputs { get; init; } = 1;

    /// <inheritdoc />
    public int Priority { get; init; } = 50;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = 
        new Dictionary<string, object>();
}

/// <summary>
/// Default implementation of IAIAgentMetrics.
/// </summary>
public class AIAgentMetrics : IAIAgentMetrics
{
    /// <inheritdoc />
    public double AverageProcessingTimeMs { get; init; }

    /// <inheritdoc />
    public double SuccessRate { get; init; } = 1.0;

    /// <inheritdoc />
    public long MemoryUsageBytes { get; init; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> AdditionalMetrics { get; init; } = 
        new Dictionary<string, object>();
}

/// <summary>
/// Default implementation of IAIAgentState.
/// </summary>
public class AIAgentState : IAIAgentState
{
    /// <inheritdoc />
    public bool IsActive { get; init; }

    /// <inheritdoc />
    public bool IsLearning { get; init; }

    /// <inheritdoc />
    public long DecisionCount { get; init; }

    /// <inheritdoc />
    public DateTime? LastDecisionTime { get; init; }

    /// <inheritdoc />
    public IAIAgentMetrics Metrics { get; init; } = new AIAgentMetrics();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Configuration { get; init; } = 
        new Dictionary<string, object>();
}
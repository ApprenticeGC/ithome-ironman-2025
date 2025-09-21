namespace GameConsole.AI.Core;

/// <summary>
/// Specifies the AI framework backend used by an agent.
/// </summary>
public enum AIFrameworkType
{
    /// <summary>
    /// Open Neural Network Exchange format.
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
    /// Custom or proprietary framework.
    /// </summary>
    Custom,
    
    /// <summary>
    /// Framework-agnostic or pure algorithmic agent.
    /// </summary>
    Native
}

/// <summary>
/// Specifies the type of computational resource required by an agent.
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// CPU computational resources.
    /// </summary>
    CPU,
    
    /// <summary>
    /// GPU computational resources.
    /// </summary>
    GPU,
    
    /// <summary>
    /// System memory resources.
    /// </summary>
    Memory,
    
    /// <summary>
    /// Storage/disk resources.
    /// </summary>
    Storage,
    
    /// <summary>
    /// Network bandwidth resources.
    /// </summary>
    Network
}

/// <summary>
/// Represents the current operational status of an AI agent.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent is not initialized.
    /// </summary>
    Uninitialized,
    
    /// <summary>
    /// Agent is initializing.
    /// </summary>
    Initializing,
    
    /// <summary>
    /// Agent is ready to process requests.
    /// </summary>
    Ready,
    
    /// <summary>
    /// Agent is currently processing a request.
    /// </summary>
    Processing,
    
    /// <summary>
    /// Agent encountered an error.
    /// </summary>
    Error,
    
    /// <summary>
    /// Agent is shutting down.
    /// </summary>
    ShuttingDown,
    
    /// <summary>
    /// Agent has been disposed.
    /// </summary>
    Disposed
}

/// <summary>
/// Specifies the security level requirements for agent execution.
/// </summary>
public enum SecurityLevel
{
    /// <summary>
    /// No special security restrictions.
    /// </summary>
    None,
    
    /// <summary>
    /// Basic security restrictions applied.
    /// </summary>
    Basic,
    
    /// <summary>
    /// Agent runs in a restricted sandbox environment.
    /// </summary>
    Sandboxed,
    
    /// <summary>
    /// Agent runs in a fully isolated environment.
    /// </summary>
    Isolated
}

/// <summary>
/// Represents resource allocation requirements for an AI agent.
/// </summary>
public readonly record struct ResourceRequirement
{
    /// <summary>
    /// Gets the type of resource.
    /// </summary>
    public ResourceType Type { get; init; }
    
    /// <summary>
    /// Gets the minimum required amount.
    /// </summary>
    public long MinimumAmount { get; init; }
    
    /// <summary>
    /// Gets the recommended amount.
    /// </summary>
    public long RecommendedAmount { get; init; }
    
    /// <summary>
    /// Gets the maximum amount that can be utilized.
    /// </summary>
    public long MaximumAmount { get; init; }
    
    /// <summary>
    /// Gets the unit of measurement (e.g., "MB", "cores", "ms").
    /// </summary>
    public string Unit { get; init; }
}

/// <summary>
/// Represents performance metrics for agent operations.
/// </summary>
public readonly record struct PerformanceMetric
{
    /// <summary>
    /// Gets the name of the metric.
    /// </summary>
    public string Name { get; init; }
    
    /// <summary>
    /// Gets the current value of the metric.
    /// </summary>
    public double Value { get; init; }
    
    /// <summary>
    /// Gets the unit of measurement.
    /// </summary>
    public string Unit { get; init; }
    
    /// <summary>
    /// Gets the timestamp when the metric was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}
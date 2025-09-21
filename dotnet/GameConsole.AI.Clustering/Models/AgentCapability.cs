namespace GameConsole.AI.Clustering.Models;

/// <summary>
/// Represents an AI agent capability that can be provided by cluster nodes.
/// </summary>
public class AgentCapability
{
    /// <summary>
    /// Gets the unique identifier for this capability.
    /// </summary>
    public required string CapabilityId { get; init; }

    /// <summary>
    /// Gets the type of AI agent (e.g., "dialogue", "analysis", "code-generation").
    /// </summary>
    public required string AgentType { get; init; }

    /// <summary>
    /// Gets the list of operations this capability supports.
    /// </summary>
    public required IReadOnlyList<string> SupportedOperations { get; init; }

    /// <summary>
    /// Gets the performance characteristics of this capability.
    /// </summary>
    public required CapabilityPerformance Performance { get; init; }

    /// <summary>
    /// Gets the resource requirements for this capability.
    /// </summary>
    public required ResourceRequirements Resources { get; init; }

    /// <summary>
    /// Gets additional configuration parameters for this capability.
    /// </summary>
    public IReadOnlyDictionary<string, string> Configuration { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents performance characteristics of an agent capability.
/// </summary>
public class CapabilityPerformance
{
    /// <summary>
    /// Gets the expected latency in milliseconds.
    /// </summary>
    public double ExpectedLatencyMs { get; init; }

    /// <summary>
    /// Gets the maximum throughput per second.
    /// </summary>
    public double MaxThroughputPerSecond { get; init; }

    /// <summary>
    /// Gets the current utilization percentage (0-100).
    /// </summary>
    public double CurrentUtilization { get; init; }

    /// <summary>
    /// Gets the quality score (0-1) of this capability.
    /// </summary>
    public double QualityScore { get; init; } = 1.0;
}

/// <summary>
/// Represents resource requirements for an agent capability.
/// </summary>
public class ResourceRequirements
{
    /// <summary>
    /// Gets the minimum CPU cores required.
    /// </summary>
    public double MinCpuCores { get; init; }

    /// <summary>
    /// Gets the minimum memory in MB required.
    /// </summary>
    public long MinMemoryMb { get; init; }

    /// <summary>
    /// Gets whether GPU acceleration is required.
    /// </summary>
    public bool RequiresGpu { get; init; }

    /// <summary>
    /// Gets the minimum GPU memory in MB if GPU is required.
    /// </summary>
    public long MinGpuMemoryMb { get; init; }
}
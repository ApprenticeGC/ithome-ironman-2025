namespace GameConsole.AI.Models;

/// <summary>
/// Represents resource requirements for AI operations.
/// </summary>
public class AIResourceRequirements
{
    /// <summary>
    /// Gets or sets the minimum memory required in bytes.
    /// </summary>
    public long MinimumMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets the recommended memory in bytes for optimal performance.
    /// </summary>
    public long RecommendedMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets the minimum number of CPU cores required.
    /// </summary>
    public int MinimumCpuCores { get; set; } = 1;

    /// <summary>
    /// Gets or sets the recommended number of CPU cores for optimal performance.
    /// </summary>
    public int RecommendedCpuCores { get; set; } = 1;

    /// <summary>
    /// Gets or sets the preferred processing units for this operation.
    /// </summary>
    public IList<AIProcessingUnit> PreferredProcessingUnits { get; set; } = new List<AIProcessingUnit> { AIProcessingUnit.CPU };

    /// <summary>
    /// Gets or sets the minimum GPU memory required in bytes (if GPU is used).
    /// </summary>
    public long MinimumGpuMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets whether the operation requires dedicated GPU resources.
    /// </summary>
    public bool RequiresDedicatedGpu { get; set; }

    /// <summary>
    /// Gets or sets the estimated execution time in milliseconds.
    /// </summary>
    public TimeSpan EstimatedExecutionTime { get; set; }

    /// <summary>
    /// Gets or sets additional resource-specific requirements.
    /// </summary>
    public IDictionary<string, object> AdditionalRequirements { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents actual resource allocation for an AI context.
/// </summary>
public class AIResourceAllocation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIResourceAllocation"/> class.
    /// </summary>
    /// <param name="allocationId">The unique identifier for this allocation.</param>
    public AIResourceAllocation(string allocationId)
    {
        AllocationId = allocationId ?? throw new ArgumentNullException(nameof(allocationId));
    }

    /// <summary>
    /// Gets the unique identifier for this resource allocation.
    /// </summary>
    public string AllocationId { get; }

    /// <summary>
    /// Gets or sets the allocated memory in bytes.
    /// </summary>
    public long AllocatedMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets the allocated CPU cores.
    /// </summary>
    public int AllocatedCpuCores { get; set; }

    /// <summary>
    /// Gets or sets the allocated GPU memory in bytes.
    /// </summary>
    public long AllocatedGpuMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets the processing units allocated for this context.
    /// </summary>
    public IList<AIProcessingUnit> AllocatedProcessingUnits { get; set; } = new List<AIProcessingUnit>();

    /// <summary>
    /// Gets or sets the timestamp when this allocation was created.
    /// </summary>
    public DateTimeOffset AllocationTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets additional allocation-specific properties.
    /// </summary>
    public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}
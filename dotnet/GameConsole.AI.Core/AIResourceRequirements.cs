namespace GameConsole.AI.Core;

/// <summary>
/// Specifies resource requirements for AI agent execution.
/// Used for resource allocation and environment setup.
/// </summary>
public class AIResourceRequirements
{
    /// <summary>
    /// Gets or sets the minimum CPU cores required.
    /// </summary>
    public int MinCpuCores { get; set; } = 1;

    /// <summary>
    /// Gets or sets the minimum RAM required in megabytes.
    /// </summary>
    public long MinMemoryMB { get; set; } = 512;

    /// <summary>
    /// Gets or sets whether GPU acceleration is required.
    /// </summary>
    public bool RequiresGpu { get; set; } = false;

    /// <summary>
    /// Gets or sets the minimum GPU memory required in megabytes.
    /// Only relevant if RequiresGpu is true.
    /// </summary>
    public long MinGpuMemoryMB { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum execution time allowed in milliseconds.
    /// Used for timeout and resource management.
    /// </summary>
    public int MaxExecutionTimeMs { get; set; } = 30000; // 30 seconds default

    /// <summary>
    /// Gets or sets additional resource properties.
    /// Can be used for framework-specific requirements.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; set; } = 
        new Dictionary<string, object>();
}
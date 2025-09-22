namespace GameConsole.AI.Discovery;

/// <summary>
/// Attribute for decorating AI agent types with metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AIAgentMetadataAttribute : Attribute
{
    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Human-readable name of the agent.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of the agent's functionality.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Version of the agent.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Priority of the agent (higher values = higher priority).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Tags associated with the agent for categorization.
    /// </summary>
    public string[]? Tags { get; set; }
}

/// <summary>
/// Attribute for specifying resource requirements of an AI agent.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AIAgentResourceRequirementsAttribute : Attribute
{
    /// <summary>
    /// Minimum required memory in bytes.
    /// </summary>
    public long MinMemoryBytes { get; set; } = 0;

    /// <summary>
    /// Maximum memory the agent might use in bytes.
    /// </summary>
    public long MaxMemoryBytes { get; set; } = long.MaxValue;

    /// <summary>
    /// Required CPU cores (0 = any available).
    /// </summary>
    public int RequiredCpuCores { get; set; } = 0;

    /// <summary>
    /// Whether GPU acceleration is required.
    /// </summary>
    public bool RequiresGpu { get; set; } = false;

    /// <summary>
    /// Network access requirements.
    /// </summary>
    public NetworkAccessLevel NetworkAccess { get; set; } = NetworkAccessLevel.None;

    /// <summary>
    /// Initialization timeout in milliseconds.
    /// </summary>
    public int InitializationTimeoutMs { get; set; } = 30000;
}
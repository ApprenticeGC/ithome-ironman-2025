namespace GameConsole.AI.Core;

/// <summary>
/// Configuration for AI service profiles.
/// </summary>
public class AIProfile
{
    /// <summary>
    /// Profile name (e.g., "Editor", "Runtime").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of agents that can run concurrently.
    /// </summary>
    public int MaxConcurrentAgents { get; set; } = 10;

    /// <summary>
    /// Default timeout for agent operations in milliseconds.
    /// </summary>
    public int DefaultTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Whether clustering is enabled for distributed processing.
    /// </summary>
    public bool ClusteringEnabled { get; set; } = false;

    /// <summary>
    /// Cluster seed nodes for distributed operation.
    /// </summary>
    public List<string> ClusterSeeds { get; set; } = new();

    /// <summary>
    /// Port for cluster communication.
    /// </summary>
    public int ClusterPort { get; set; } = 4053;

    /// <summary>
    /// Additional configuration properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Configuration for individual AI agents.
/// </summary>
public class AgentConfig
{
    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the agent (e.g., "DialogueAgent", "CodexAgent").
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Maximum processing time for requests in milliseconds.
    /// </summary>
    public int TimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Agent-specific configuration properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Metadata about an AI agent.
/// </summary>
public class AgentMetadata
{
    /// <summary>
    /// Agent unique identifier.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Agent type.
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the agent.
    /// </summary>
    public AgentStatus Status { get; set; } = AgentStatus.Unknown;

    /// <summary>
    /// Timestamp when the agent was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Number of requests processed by this agent.
    /// </summary>
    public long ProcessedRequests { get; set; }

    /// <summary>
    /// Average processing time in milliseconds.
    /// </summary>
    public double AverageProcessingTimeMs { get; set; }

    /// <summary>
    /// Current cluster node (if clustering is enabled).
    /// </summary>
    public string? ClusterNode { get; set; }
}

/// <summary>
/// Status of an AI agent.
/// </summary>
public enum AgentStatus
{
    Unknown,
    Initializing,
    Running,
    Busy,
    Stopping,
    Stopped,
    Failed
}
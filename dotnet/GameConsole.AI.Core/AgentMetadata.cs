namespace GameConsole.AI.Services;

/// <summary>
/// Metadata information about an AI agent in the system.
/// </summary>
public class AgentMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier of the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the agent.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the agent's capabilities and purpose.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the agent.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the supported task kinds for this agent.
    /// </summary>
    public TaskKind[] SupportedTaskKinds { get; set; } = Array.Empty<TaskKind>();

    /// <summary>
    /// Gets or sets the capabilities provided by this agent.
    /// </summary>
    public string[] Capabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets a value indicating whether the agent is currently available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Gets or sets the current status of the agent.
    /// </summary>
    public AgentStatus Status { get; set; } = AgentStatus.Ready;

    /// <summary>
    /// Gets or sets additional configuration properties for the agent.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the timestamp when the agent was last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Represents the current status of an AI agent.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent is ready to accept new tasks.
    /// </summary>
    Ready,

    /// <summary>
    /// Agent is currently busy processing a task.
    /// </summary>
    Busy,

    /// <summary>
    /// Agent is initializing and not yet ready.
    /// </summary>
    Initializing,

    /// <summary>
    /// Agent encountered an error and is not available.
    /// </summary>
    Error,

    /// <summary>
    /// Agent is offline or disconnected.
    /// </summary>
    Offline
}
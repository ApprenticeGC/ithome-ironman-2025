namespace GameConsole.AI.Discovery;

/// <summary>
/// Represents the metadata and capabilities of an AI agent.
/// Used for discovery, registration, and matching purposes.
/// </summary>
public class AgentMetadata
{
    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name of the agent.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of the agent's functionality.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Version of the agent.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// The agent's implementation type.
    /// </summary>
    public required Type AgentType { get; init; }

    /// <summary>
    /// Capabilities provided by this agent.
    /// </summary>
    public IReadOnlyList<Type> Capabilities { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Tags associated with the agent for categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Priority of the agent (higher values = higher priority).
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Whether the agent is currently available for use.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Resource requirements for the agent.
    /// </summary>
    public AgentResourceRequirements ResourceRequirements { get; init; } = new();
}
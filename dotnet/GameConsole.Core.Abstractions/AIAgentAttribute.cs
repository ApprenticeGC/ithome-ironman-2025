namespace GameConsole.Core.Abstractions;

/// <summary>
/// Attribute used to declaratively define AI agent metadata.
/// Apply this attribute to AI agent classes to provide metadata for agent discovery,
/// capability matching, and runtime management.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AIAgentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the AI agent.</param>
    /// <param name="name">The human-readable name of the AI agent.</param>
    /// <param name="version">The version of the AI agent.</param>
    /// <param name="description">A description of what the AI agent does.</param>
    /// <param name="author">The author or organization that created the AI agent.</param>
    public AIAgentAttribute(string id, string name, string version, string description, string author)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Author = author ?? throw new ArgumentNullException(nameof(author));
    }

    /// <summary>
    /// Gets the unique identifier for the AI agent.
    /// This should be a stable identifier that doesn't change across versions.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the AI agent.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the AI agent.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets a description of what the AI agent does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the author or organization that created the AI agent.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Gets or sets the list of AI agent IDs that this agent depends on.
    /// Dependencies must be loaded and running before this agent can be initialized.
    /// </summary>
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the list of capability type names that this AI agent provides.
    /// Used for capability-based agent discovery and selection.
    /// </summary>
    public string[] ProvidedCapabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the list of capability type names that this AI agent requires.
    /// Required capabilities must be available for the agent to function properly.
    /// </summary>
    public string[] RequiredCapabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets tags that categorize or describe the AI agent's functionality.
    /// This can be used for agent filtering and organization.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the maximum execution timeout in milliseconds for agent operations.
    /// If not specified, a default timeout will be used.
    /// </summary>
    public int ExecutionTimeoutMs { get; set; } = 30000; // 30 seconds default

    /// <summary>
    /// Gets or sets the maximum memory allocation in bytes allowed for this agent.
    /// If not specified, no memory limit is enforced.
    /// </summary>
    public long MaxMemoryBytes { get; set; } = 0; // 0 means no limit
}
namespace GameConsole.AI.Models;

/// <summary>
/// Provides metadata information about an AI agent including identity, model information, and requirements.
/// Used for agent discovery, version management, and resource allocation.
/// </summary>
public class AIAgentMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentMetadata"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the agent.</param>
    /// <param name="name">The human-readable name of the agent.</param>
    /// <param name="version">The version of the agent.</param>
    /// <param name="description">A description of what the agent does.</param>
    /// <param name="author">The author or organization that created the agent.</param>
    /// <param name="modelInfo">Information about the AI model used by this agent.</param>
    public AIAgentMetadata(
        string id,
        string name,
        Version version,
        string description,
        string author,
        AIModelInfo modelInfo)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Author = author ?? throw new ArgumentNullException(nameof(author));
        ModelInfo = modelInfo ?? throw new ArgumentNullException(nameof(modelInfo));
        Dependencies = new List<string>();
        Properties = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the unique identifier for the agent.
    /// This should be a stable identifier that doesn't change across versions.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the agent.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the agent.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Gets a description of what the agent does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the author or organization that created the agent.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Gets information about the AI model used by this agent.
    /// </summary>
    public AIModelInfo ModelInfo { get; }

    /// <summary>
    /// Gets the list of agent IDs that this agent depends on.
    /// Dependencies must be loaded before this agent can be initialized.
    /// </summary>
    public IList<string> Dependencies { get; }

    /// <summary>
    /// Gets additional properties and metadata for the agent.
    /// This can include custom configuration, feature flags, or other agent-specific data.
    /// </summary>
    public IDictionary<string, object> Properties { get; }

    /// <summary>
    /// Gets the resource requirements for this agent.
    /// </summary>
    public AIResourceRequirements ResourceRequirements { get; init; } = new AIResourceRequirements();

    /// <summary>
    /// Gets the supported AI frameworks for this agent.
    /// </summary>
    public IReadOnlyList<AIFrameworkType> SupportedFrameworks { get; init; } = Array.Empty<AIFrameworkType>();

    /// <summary>
    /// Gets the minimum framework versions required for this agent.
    /// </summary>
    public IReadOnlyDictionary<AIFrameworkType, Version> MinimumFrameworkVersions { get; init; } = 
        new Dictionary<AIFrameworkType, Version>();
}
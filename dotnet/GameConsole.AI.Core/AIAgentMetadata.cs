using GameConsole.Plugins.Core;

namespace GameConsole.AI.Core;

/// <summary>
/// Default implementation of AI agent metadata.
/// </summary>
public class AIAgentMetadata : IAIAgentMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentMetadata"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the agent.</param>
    /// <param name="name">The human-readable name of the agent.</param>
    /// <param name="version">The version of the agent.</param>
    /// <param name="description">A description of what the agent does.</param>
    /// <param name="author">The author or organization that created the agent.</param>
    /// <param name="agentType">The type of AI agent.</param>
    /// <param name="capabilities">The capabilities that this AI agent provides.</param>
    /// <param name="resourceRequirements">The resource requirements for this AI agent.</param>
    /// <param name="supportedProtocols">The supported communication protocols.</param>
    /// <param name="supportsLearning">Whether this agent can learn and adapt.</param>
    /// <param name="dependencies">The list of plugin IDs that this agent depends on.</param>
    /// <param name="properties">Additional properties and metadata for the agent.</param>
    public AIAgentMetadata(
        string id,
        string name,
        Version version,
        string description,
        string author,
        string agentType,
        IReadOnlyList<string> capabilities,
        IAIAgentResourceRequirements resourceRequirements,
        IReadOnlyList<string> supportedProtocols,
        bool supportsLearning = false,
        IReadOnlyList<string>? dependencies = null,
        IReadOnlyDictionary<string, object>? properties = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Author = author ?? throw new ArgumentNullException(nameof(author));
        AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        Capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        ResourceRequirements = resourceRequirements ?? throw new ArgumentNullException(nameof(resourceRequirements));
        SupportedProtocols = supportedProtocols ?? throw new ArgumentNullException(nameof(supportedProtocols));
        SupportsLearning = supportsLearning;
        Dependencies = dependencies ?? Array.Empty<string>();
        Properties = properties ?? new Dictionary<string, object>();
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Version Version { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public string Author { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Dependencies { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Properties { get; }

    /// <inheritdoc />
    public string AgentType { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Capabilities { get; }

    /// <inheritdoc />
    public IAIAgentResourceRequirements ResourceRequirements { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedProtocols { get; }

    /// <inheritdoc />
    public bool SupportsLearning { get; }
}
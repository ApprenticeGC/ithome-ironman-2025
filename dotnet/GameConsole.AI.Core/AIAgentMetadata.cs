using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Metadata information for AI agents, extending the base service metadata
/// with AI-specific information such as model details and framework requirements.
/// </summary>
public class AIAgentMetadata : IServiceMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentMetadata"/> class.
    /// </summary>
    /// <param name="name">The human-readable name of the AI agent.</param>
    /// <param name="version">The version of the AI agent implementation.</param>
    /// <param name="description">A brief description of what the AI agent does.</param>
    /// <param name="modelName">The name of the underlying AI model.</param>
    /// <param name="frameworkType">The AI framework type used by this agent.</param>
    public AIAgentMetadata(
        string name, 
        string version, 
        string description, 
        string modelName, 
        AIFrameworkType frameworkType)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        FrameworkType = frameworkType;
        Categories = new[] { "AI", "Agent", frameworkType.ToString() };
        Properties = new Dictionary<string, object>();
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Version { get; }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public IEnumerable<string> Categories { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Properties { get; private set; }

    /// <summary>
    /// Gets the name of the underlying AI model.
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Gets the version of the underlying AI model.
    /// </summary>
    public string? ModelVersion { get; init; }

    /// <summary>
    /// Gets the AI framework type used by this agent.
    /// </summary>
    public AIFrameworkType FrameworkType { get; }

    /// <summary>
    /// Gets the resource requirements for this AI agent.
    /// </summary>
    public AIResourceRequirements? ResourceRequirements { get; init; }

    /// <summary>
    /// Gets the supported input types for this AI agent.
    /// </summary>
    public IEnumerable<string> SupportedInputTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the supported output types for this AI agent.
    /// </summary>
    public IEnumerable<string> SupportedOutputTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Sets additional properties for the metadata.
    /// </summary>
    /// <param name="properties">The additional properties to set.</param>
    /// <returns>This instance for method chaining.</returns>
    public AIAgentMetadata WithProperties(IReadOnlyDictionary<string, object> properties)
    {
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        return this;
    }
}
namespace GameConsole.AI.Core.Models;

/// <summary>
/// Provides comprehensive metadata information about an AI agent including
/// identity, version, model information, dependencies, and resource requirements.
/// </summary>
public class AIAgentMetadata
{
    /// <summary>
    /// Gets the unique identifier for the agent.
    /// This should be a stable identifier that doesn't change across versions.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human-readable name of the agent.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the version of the agent using semantic versioning.
    /// </summary>
    public required Version Version { get; init; }

    /// <summary>
    /// Gets a description of what the agent does and its capabilities.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the author or organization that created the agent.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Gets information about the AI model or framework used by this agent.
    /// </summary>
    public required AIModelInfo ModelInfo { get; init; }

    /// <summary>
    /// Gets the list of agent IDs or capabilities that this agent depends on.
    /// Dependencies must be available before this agent can be initialized.
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the resource requirements for this agent.
    /// </summary>
    public IReadOnlyList<ResourceRequirement> ResourceRequirements { get; init; } = Array.Empty<ResourceRequirement>();

    /// <summary>
    /// Gets the security level requirements for agent execution.
    /// </summary>
    public SecurityLevel SecurityLevel { get; init; } = SecurityLevel.Basic;

    /// <summary>
    /// Gets additional properties and metadata for the agent.
    /// This can include custom configuration, feature flags, or other agent-specific data.
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets the supported input formats or content types that this agent can process.
    /// </summary>
    public IReadOnlyList<string> SupportedInputTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the output formats or content types that this agent can generate.
    /// </summary>
    public IReadOnlyList<string> SupportedOutputTypes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the tags or categories that describe the agent's domain or purpose.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Provides information about the AI model or framework used by an agent.
/// </summary>
public class AIModelInfo
{
    /// <summary>
    /// Gets the AI framework type used by the agent.
    /// </summary>
    public required AIFrameworkType FrameworkType { get; init; }

    /// <summary>
    /// Gets the version of the AI framework or runtime.
    /// </summary>
    public Version? FrameworkVersion { get; init; }

    /// <summary>
    /// Gets the name or identifier of the specific model used.
    /// </summary>
    public string? ModelName { get; init; }

    /// <summary>
    /// Gets the version of the model.
    /// </summary>
    public Version? ModelVersion { get; init; }

    /// <summary>
    /// Gets the path or URI to the model file(s), if applicable.
    /// </summary>
    public string? ModelPath { get; init; }

    /// <summary>
    /// Gets the size of the model in bytes, if known.
    /// </summary>
    public long? ModelSize { get; init; }

    /// <summary>
    /// Gets additional model-specific configuration or parameters.
    /// </summary>
    public IReadOnlyDictionary<string, object> Configuration { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets the license information for the model, if applicable.
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Gets the source or origin of the model (e.g., "Hugging Face", "OpenAI", "Custom").
    /// </summary>
    public string? Source { get; init; }
}
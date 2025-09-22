namespace GameConsole.Core.Abstractions;

/// <summary>
/// Provides metadata information about an AI agent including identity, version, and capabilities.
/// Used for agent discovery, capability matching, and runtime management.
/// </summary>
public interface IAIAgentMetadata
{
    /// <summary>
    /// Gets the unique identifier for the AI agent.
    /// This should be a stable identifier that doesn't change across versions.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the AI agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the AI agent.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Gets a description of what the AI agent does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the author or organization that created the AI agent.
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Gets the list of AI agent IDs that this agent depends on.
    /// Dependencies must be loaded and running before this agent can be initialized.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Gets the list of capability types that this AI agent provides.
    /// Used for capability-based agent discovery and selection.
    /// </summary>
    IReadOnlyList<Type> ProvidedCapabilities { get; }

    /// <summary>
    /// Gets the list of capability types that this AI agent requires.
    /// Required capabilities must be available for the agent to function properly.
    /// </summary>
    IReadOnlyList<Type> RequiredCapabilities { get; }

    /// <summary>
    /// Gets additional properties and metadata for the AI agent.
    /// This can include custom configuration, feature flags, performance metrics, or other agent-specific data.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}
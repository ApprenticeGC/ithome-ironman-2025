namespace GameConsole.Core.Abstractions;

/// <summary>
/// Interface for providing metadata information about an AI agent.
/// This allows agents to expose descriptive information for documentation,
/// logging, and agent discovery purposes.
/// </summary>
public interface IAgentMetadata
{
    /// <summary>
    /// Gets the human-readable name of the agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the agent implementation.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets a brief description of what the agent does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the categories or tags associated with this agent.
    /// Used for grouping and filtering agents in agent discovery.
    /// </summary>
    IEnumerable<string> Categories { get; }

    /// <summary>
    /// Gets the agent's capabilities as a collection of capability names.
    /// Used to determine what the agent can do.
    /// </summary>
    IEnumerable<string> Capabilities { get; }

    /// <summary>
    /// Gets additional metadata properties as key-value pairs.
    /// Can be used to store agent-specific configuration or information.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}
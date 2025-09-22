namespace GameConsole.AI.Core;

/// <summary>
/// Represents a descriptor for an AI agent, containing metadata and instantiation information.
/// </summary>
public interface IAIAgentDescriptor
{
    /// <summary>
    /// Gets the metadata for the AI agent.
    /// </summary>
    IAIAgentMetadata Metadata { get; }

    /// <summary>
    /// Gets the type information for creating instances of this AI agent.
    /// </summary>
    Type AgentType { get; }

    /// <summary>
    /// Gets the assembly that contains this AI agent.
    /// </summary>
    System.Reflection.Assembly Assembly { get; }

    /// <summary>
    /// Gets a value indicating whether this agent can be instantiated multiple times.
    /// </summary>
    bool AllowMultipleInstances { get; }

    /// <summary>
    /// Gets the timestamp when this descriptor was created or last updated.
    /// </summary>
    DateTimeOffset RegisteredAt { get; }

    /// <summary>
    /// Creates a new instance of the AI agent.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    /// <returns>A new instance of the AI agent.</returns>
    IAIAgent CreateInstance(IServiceProvider serviceProvider);

    /// <summary>
    /// Validates that this agent descriptor is properly configured.
    /// </summary>
    /// <returns>True if the descriptor is valid, false otherwise.</returns>
    bool IsValid();
}
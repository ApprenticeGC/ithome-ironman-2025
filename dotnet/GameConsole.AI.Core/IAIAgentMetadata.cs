using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core
{

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
    /// Gets the category or type of AI agent (e.g., "Language Model", "Computer Vision", "Game AI").
    /// </summary>
    string Category { get; }

    /// <summary>
    /// Gets the list of AI agent IDs that this agent depends on.
    /// Dependencies must be available before this agent can be initialized.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Gets the types of requests that this agent can handle.
    /// </summary>
    IReadOnlyList<Type> SupportedRequestTypes { get; }

    /// <summary>
    /// Gets the types of capabilities that this agent provides.
    /// </summary>
    IReadOnlyList<Type> ProvidedCapabilities { get; }

    /// <summary>
    /// Gets the priority level of this agent for load balancing and selection.
    /// Higher values indicate higher priority.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets the maximum number of concurrent requests this agent can handle.
    /// A value of -1 indicates no limit.
    /// </summary>
    int MaxConcurrentRequests { get; }

    /// <summary>
    /// Gets additional properties and metadata for the agent.
    /// This can include custom configuration, feature flags, or other agent-specific data.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}
}
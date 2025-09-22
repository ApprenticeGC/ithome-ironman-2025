using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core;

/// <summary>
/// Represents metadata about an AI agent in the system.
/// Contains information about the agent's identity, capabilities, and configuration.
/// </summary>
public sealed class AgentMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentMetadata"/> class.
    /// </summary>
    /// <param name="id">Unique identifier for the agent.</param>
    /// <param name="name">Human-readable name of the agent.</param>
    /// <param name="version">Version of the agent implementation.</param>
    /// <param name="description">Description of what the agent does.</param>
    public AgentMetadata(string id, string name, string version, string description)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Capabilities = Array.Empty<AgentCapability>();
        Tags = Array.Empty<string>();
    }

    /// <summary>
    /// Gets the unique identifier for the agent.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the human-readable name of the agent.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the agent implementation.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the description of what the agent does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets the capabilities provided by this agent.
    /// </summary>
    public AgentCapability[] Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with this agent for categorization.
    /// </summary>
    public string[] Tags { get; set; }

    /// <summary>
    /// Gets or sets the priority level of this agent (higher values have higher priority).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether this agent is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Represents a capability that an AI agent can provide.
/// </summary>
public sealed class AgentCapability
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentCapability"/> class.
    /// </summary>
    /// <param name="name">Name of the capability.</param>
    /// <param name="type">Type that defines the capability interface.</param>
    /// <param name="description">Description of what the capability does.</param>
    public AgentCapability(string name, Type type, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Parameters = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the name of the capability.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type that defines the capability interface.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the description of what the capability does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets configuration parameters for this capability.
    /// </summary>
    public Dictionary<string, object> Parameters { get; }
}

/// <summary>
/// Represents the result of an agent discovery operation.
/// </summary>
public sealed class AgentDiscoveryResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentDiscoveryResult"/> class.
    /// </summary>
    /// <param name="agents">The discovered agents.</param>
    /// <param name="totalCount">Total number of agents that match the criteria.</param>
    /// <param name="isPartialResult">Indicates if this is a partial result due to pagination.</param>
    public AgentDiscoveryResult(IReadOnlyList<AgentMetadata> agents, int totalCount, bool isPartialResult = false)
    {
        Agents = agents ?? throw new ArgumentNullException(nameof(agents));
        TotalCount = totalCount;
        IsPartialResult = isPartialResult;
    }

    /// <summary>
    /// Gets the discovered agents.
    /// </summary>
    public IReadOnlyList<AgentMetadata> Agents { get; }

    /// <summary>
    /// Gets the total number of agents that match the discovery criteria.
    /// </summary>
    public int TotalCount { get; }

    /// <summary>
    /// Gets a value indicating whether this is a partial result due to pagination.
    /// </summary>
    public bool IsPartialResult { get; }
}

/// <summary>
/// Represents criteria for discovering AI agents.
/// </summary>
public sealed class AgentDiscoveryCriteria
{
    /// <summary>
    /// Gets or sets the capability type to filter by.
    /// </summary>
    public Type? CapabilityType { get; set; }

    /// <summary>
    /// Gets or sets the tags to filter by (any of the tags must match).
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Gets or sets the minimum priority level.
    /// </summary>
    public int? MinimumPriority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include only enabled agents.
    /// </summary>
    public bool EnabledOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int? MaxResults { get; set; }
}
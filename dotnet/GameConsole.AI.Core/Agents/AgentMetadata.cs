using System;
using GameConsole.AI.Core.Configuration;

namespace GameConsole.AI.Core.Agents;

/// <summary>
/// Metadata describing an AI agent's capabilities and configuration.
/// Used for agent discovery and selection.
/// </summary>
public class AgentMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable name of the agent.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the agent does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the agent implementation.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets the task kinds this agent supports.
    /// </summary>
    public TaskKind[] SupportedTaskKinds { get; set; } = Array.Empty<TaskKind>();

    /// <summary>
    /// Gets or sets the capabilities provided by this agent.
    /// </summary>
    public string[] Capabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the categories or tags associated with this agent.
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether the agent is currently available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Gets or sets the recommended resource requirements for this agent.
    /// </summary>
    public ResourceRequirements? ResourceRequirements { get; set; }
}

/// <summary>
/// Describes the resource requirements for an AI agent.
/// </summary>
public class ResourceRequirements
{
    /// <summary>
    /// Gets or sets the minimum memory requirements in MB.
    /// </summary>
    public int MinMemoryMB { get; set; }

    /// <summary>
    /// Gets or sets the recommended CPU cores.
    /// </summary>
    public int RecommendedCores { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether GPU acceleration is required.
    /// </summary>
    public bool RequiresGPU { get; set; } = false;

    /// <summary>
    /// Gets or sets the estimated tokens per second throughput.
    /// </summary>
    public int EstimatedTokensPerSecond { get; set; }
}
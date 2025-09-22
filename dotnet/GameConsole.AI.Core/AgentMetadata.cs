using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core;

/// <summary>
/// Metadata information about an AI agent, including its capabilities and configuration.
/// </summary>
public class AgentMetadata
{
    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the agent.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the agent's purpose and capabilities.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Agent type (e.g., "DirectorAgent", "DialogueAgent", "CodexAgent").
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Version of the agent implementation.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// List of capabilities provided by this agent.
    /// </summary>
    public IReadOnlyList<string> Capabilities { get; set; } = new string[0];

    /// <summary>
    /// Configuration options specific to this agent.
    /// </summary>
    public IReadOnlyDictionary<string, object> Configuration { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Indicates whether the agent is currently available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Task kinds that this agent supports.
    /// </summary>
    public IReadOnlyList<string> SupportedTaskKinds { get; set; } = new string[0];
}
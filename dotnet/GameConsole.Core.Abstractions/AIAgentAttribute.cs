namespace GameConsole.Core.Abstractions;

/// <summary>
/// Attribute used to declaratively define AI agent metadata.
/// Apply this attribute to AI agent classes to provide metadata for agent discovery,
/// registration, and runtime management.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AIAgentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentAttribute"/> class.
    /// </summary>
    /// <param name="agentId">The unique identifier for the AI agent.</param>
    /// <param name="agentType">The type or category of the AI agent.</param>
    /// <param name="name">The human-readable name of the AI agent.</param>
    /// <param name="version">The version of the AI agent.</param>
    /// <param name="description">A description of what the AI agent does.</param>
    public AIAgentAttribute(string agentId, string agentType, string name, string version = "1.0.0", string description = "")
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Gets the unique identifier for the AI agent.
    /// This should be a stable identifier that doesn't change across versions.
    /// </summary>
    public string AgentId { get; }

    /// <summary>
    /// Gets the type or category of the AI agent (e.g., "ChatBot", "Orchestrator", "Assistant").
    /// </summary>
    public string AgentType { get; }

    /// <summary>
    /// Gets the human-readable name of the AI agent.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the AI agent.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets a description of what the AI agent does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets the priority level for this agent when multiple agents of the same type are available.
    /// Higher values indicate higher priority. Default is 0.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets the capabilities provided by this AI agent.
    /// Used for capability-based discovery and filtering.
    /// </summary>
    public string[] Capabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the service lifetime for dependency injection.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets tags that categorize or describe the agent's functionality.
    /// This can be used for agent filtering and organization.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
}
using GameConsole.Plugins.Core;

namespace GameConsole.AI.Core;

/// <summary>
/// Attribute used to declaratively define AI agent metadata.
/// Extends PluginAttribute with AI-specific properties for discovery and registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AIAgentAttribute : PluginAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the AI agent.</param>
    /// <param name="name">The human-readable name of the AI agent.</param>
    /// <param name="version">The version of the AI agent.</param>
    /// <param name="description">A description of what the AI agent does.</param>
    /// <param name="author">The author or organization that created the AI agent.</param>
    /// <param name="capabilities">The capabilities that this AI agent provides.</param>
    public AIAgentAttribute(string id, string name, string version, string description, string author, AIAgentCapability capabilities = AIAgentCapability.None)
        : base(id, name, version, description, author)
    {
        Capabilities = capabilities;
    }

    /// <summary>
    /// Gets the capabilities that this AI agent provides.
    /// </summary>
    public AIAgentCapability Capabilities { get; }

    /// <summary>
    /// Gets or sets the behavior type category for this AI agent.
    /// Used for organizing and filtering AI agents by their primary function.
    /// </summary>
    public string? BehaviorType { get; set; }

    /// <summary>
    /// Gets or sets the priority for this AI agent when multiple agents 
    /// provide the same capabilities. Higher values indicate higher priority.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether this AI agent requires 
    /// exclusive access to resources when active.
    /// </summary>
    public bool RequiresExclusiveAccess { get; set; } = false;
}
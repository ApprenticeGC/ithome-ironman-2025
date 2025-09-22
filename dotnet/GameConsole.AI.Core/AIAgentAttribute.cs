using GameConsole.Plugins.Core;

namespace GameConsole.AI.Core;

/// <summary>
/// Attribute for marking classes as AI agents and providing metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AIAgentAttribute : PluginAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the agent.</param>
    /// <param name="name">The human-readable name of the agent.</param>
    /// <param name="version">The version of the agent.</param>
    /// <param name="description">A description of what the agent does.</param>
    /// <param name="author">The author or organization that created the agent.</param>
    /// <param name="agentType">The type of AI agent.</param>
    public AIAgentAttribute(string id, string name, string version, string description, string author, string agentType)
        : base(id, name, version, description, author)
    {
        AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        Capabilities = Array.Empty<string>();
        SupportedProtocols = Array.Empty<string>();
    }

    /// <summary>
    /// Gets or sets the type of AI agent.
    /// </summary>
    public string AgentType { get; set; }

    /// <summary>
    /// Gets or sets the capabilities that this AI agent provides.
    /// </summary>
    public string[] Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the supported communication protocols for this AI agent.
    /// </summary>
    public string[] SupportedProtocols { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this agent can learn and adapt during runtime.
    /// </summary>
    public bool SupportsLearning { get; set; }

    /// <summary>
    /// Gets or sets the minimum amount of memory required in MB.
    /// </summary>
    public int MinMemoryMB { get; set; } = 64;

    /// <summary>
    /// Gets or sets the recommended amount of memory in MB.
    /// </summary>
    public int RecommendedMemoryMB { get; set; } = 256;

    /// <summary>
    /// Gets or sets a value indicating whether GPU acceleration is required.
    /// </summary>
    public bool RequiresGPU { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether network connectivity is required.
    /// </summary>
    public bool RequiresNetwork { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent instances supported.
    /// </summary>
    public int MaxConcurrentInstances { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether this agent can be instantiated multiple times.
    /// </summary>
    public bool AllowMultipleInstances { get; set; } = true;
}
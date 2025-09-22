namespace GameConsole.Plugins.Core;

/// <summary>
/// Attribute used to declaratively define AI agent metadata.
/// Apply this attribute to AI agent classes to provide metadata for agent discovery,
/// capability identification, and runtime management.
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
    /// <param name="aiModel">The AI model or framework this agent is based on.</param>
    public AIAgentAttribute(string id, string name, string version, string description, string author, string aiModel)
        : base(id, name, version, description, author)
    {
        AIModel = aiModel ?? throw new ArgumentNullException(nameof(aiModel));
    }

    /// <summary>
    /// Gets the AI model or framework this agent is based on.
    /// Examples: "GPT-4", "Claude", "Llama", "Custom Neural Network", etc.
    /// </summary>
    public string AIModel { get; }

    /// <summary>
    /// Gets or sets the list of capability names that this AI agent supports.
    /// These should correspond to <see cref="IAIAgentCapability.CapabilityName"/> values.
    /// </summary>
    public string[] SupportedCapabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the performance rating of this AI agent (1-10 scale).
    /// This can be used for agent selection when multiple agents support the same capability.
    /// </summary>
    public int PerformanceRating { get; set; } = 5;

    /// <summary>
    /// Gets or sets the resource requirements level for this AI agent.
    /// Values: "Low", "Medium", "High", "Very High"
    /// </summary>
    public string ResourceRequirements { get; set; } = "Medium";

    /// <summary>
    /// Gets or sets a value indicating whether this AI agent requires internet connectivity.
    /// </summary>
    public bool RequiresInternetConnection { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of concurrent requests this agent can handle.
    /// Set to 0 for unlimited (subject to system resources).
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 1;
}
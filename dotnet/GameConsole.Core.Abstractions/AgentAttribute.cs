namespace GameConsole.Core.Abstractions;

/// <summary>
/// Attribute used to decorate agent implementations with metadata.
/// This attribute enables declarative agent registration and provides
/// metadata for agent discovery and documentation purposes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AgentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AgentAttribute"/> class.
    /// </summary>
    /// <param name="name">The human-readable name of the agent.</param>
    /// <param name="version">The version of the agent implementation.</param>
    /// <param name="description">A brief description of what the agent does.</param>
    public AgentAttribute(string name, string version = "1.0.0", string description = "")
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Gets the human-readable name of the agent.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the agent implementation.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets a brief description of what the agent does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets the categories or tags associated with this agent.
    /// Used for grouping and filtering agents in agent discovery.
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the capabilities that this agent provides.
    /// Used to determine what the agent can do during discovery.
    /// </summary>
    public string[] Capabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the agent lifetime for dependency injection.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}
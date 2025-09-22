namespace GameConsole.AI.Core;

/// <summary>
/// Attribute used to mark classes as AI agents for automatic discovery and registration.
/// This extends the plugin attribute pattern specifically for AI agents.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AIAgentAttribute : Attribute
{
    /// <summary>
    /// Gets the unique name of the AI agent.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the AI agent.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the description of the AI agent.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets the categories this AI agent belongs to.
    /// Default is an empty array.
    /// </summary>
    public string[] Categories { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the decision types this AI agent supports.
    /// Default is an empty array.
    /// </summary>
    public string[] DecisionTypes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether this AI agent supports learning.
    /// Default is false.
    /// </summary>
    public bool SupportsLearning { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this AI agent supports autonomous operation.
    /// Default is false.
    /// </summary>
    public bool SupportsAutonomousMode { get; set; } = false;

    /// <summary>
    /// Gets or sets the priority level for this AI agent.
    /// Higher values indicate higher priority (0 = lowest, 100 = highest).
    /// Default is 50.
    /// </summary>
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum number of concurrent inputs this agent can process.
    /// Default is 1.
    /// </summary>
    public int MaxConcurrentInputs { get; set; } = 1;

    /// <summary>
    /// Initializes a new instance of the AIAgentAttribute class.
    /// </summary>
    /// <param name="name">The unique name of the AI agent.</param>
    /// <param name="version">The version of the AI agent.</param>
    /// <param name="description">The description of the AI agent.</param>
    public AIAgentAttribute(string name, string version, string description)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

/// <summary>
/// Provides extension methods for working with AI agent attributes.
/// </summary>
public static class AIAgentAttributeExtensions
{
    /// <summary>
    /// Gets the AIAgentAttribute from a type, if present.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>The AIAgentAttribute if found, null otherwise.</returns>
    public static AIAgentAttribute? GetAIAgentAttribute(this Type type)
    {
        return type.GetCustomAttributes(typeof(AIAgentAttribute), false)
                  .FirstOrDefault() as AIAgentAttribute;
    }

    /// <summary>
    /// Checks if a type has the AIAgentAttribute.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type has the AIAgentAttribute, false otherwise.</returns>
    public static bool HasAIAgentAttribute(this Type type)
    {
        return type.GetAIAgentAttribute() != null;
    }

    /// <summary>
    /// Converts an AIAgentAttribute to IAIAgentTypeMetadata.
    /// </summary>
    /// <param name="attribute">The attribute to convert.</param>
    /// <returns>The metadata representation of the attribute.</returns>
    public static IAIAgentTypeMetadata ToMetadata(this AIAgentAttribute attribute)
    {
        return new AIAgentTypeMetadata
        {
            Name = attribute.Name,
            Version = attribute.Version,
            Description = attribute.Description,
            Categories = attribute.Categories,
            Properties = new Dictionary<string, object>
            {
                [nameof(attribute.DecisionTypes)] = attribute.DecisionTypes,
                [nameof(attribute.SupportsLearning)] = attribute.SupportsLearning,
                [nameof(attribute.SupportsAutonomousMode)] = attribute.SupportsAutonomousMode,
                [nameof(attribute.Priority)] = attribute.Priority,
                [nameof(attribute.MaxConcurrentInputs)] = attribute.MaxConcurrentInputs
            }
        };
    }
}

/// <summary>
/// Default implementation of IAIAgentTypeMetadata.
/// </summary>
public class AIAgentTypeMetadata : IAIAgentTypeMetadata
{
    /// <inheritdoc />
    public string Name { get; init; } = string.Empty;

    /// <inheritdoc />
    public string Version { get; init; } = string.Empty;

    /// <inheritdoc />
    public string Description { get; init; } = string.Empty;

    /// <inheritdoc />
    public IReadOnlyList<string> Categories { get; init; } = Array.Empty<string>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Properties { get; init; } = 
        new Dictionary<string, object>();
}
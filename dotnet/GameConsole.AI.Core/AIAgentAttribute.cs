using System;

namespace GameConsole.AI.Core
{

/// <summary>
/// Attribute used to declaratively define AI agent metadata.
/// Apply this attribute to AI agent classes to provide metadata for agent discovery,
/// capability matching, and runtime management.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AIAgentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentAttribute"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for the AI agent.</param>
    /// <param name="name">The human-readable name of the AI agent.</param>
    /// <param name="version">The version of the AI agent.</param>
    /// <param name="description">A description of what the AI agent does.</param>
    /// <param name="author">The author or organization that created the AI agent.</param>
    /// <param name="category">The category or type of AI agent.</param>
    public AIAgentAttribute(string id, string name, string version, string description, string author, string category)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Author = author ?? throw new ArgumentNullException(nameof(author));
        Category = category ?? throw new ArgumentNullException(nameof(category));
    }

    /// <summary>
    /// Gets the unique identifier for the AI agent.
    /// This should be a stable identifier that doesn't change across versions.
    /// </summary>
    public string Id { get; }

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
    /// Gets the author or organization that created the AI agent.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Gets the category or type of AI agent (e.g., "Language Model", "Computer Vision", "Game AI").
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets or sets the list of AI agent IDs that this agent depends on.
    /// Dependencies must be available before this agent can be initialized.
    /// </summary>
    public string[] Dependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the priority level of this agent for load balancing and selection.
    /// Higher values indicate higher priority. Default is 100.
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of concurrent requests this agent can handle.
    /// A value of -1 indicates no limit. Default is 1.
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 1;

    /// <summary>
    /// Gets or sets tags that categorize or describe the agent's functionality.
    /// This can be used for agent filtering and organization.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
}
}
namespace GameConsole.AI.Actors.Core;

/// <summary>
/// Represents the state of an AI agent.
/// </summary>
public class AIAgentState
{
    /// <summary>
    /// Gets or sets the unique identifier for the agent.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the agent.
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets custom properties for the agent.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the current status of the agent.
    /// </summary>
    public AIAgentStatus Status { get; set; } = AIAgentStatus.Idle;

    /// <summary>
    /// Gets a property value of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="key">The property key.</param>
    /// <returns>The property value, or default if not found.</returns>
    public T? GetProperty<T>(string key)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Sets a property value.
    /// </summary>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    public void SetProperty(string key, object value)
    {
        Properties[key] = value;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a copy of the current state.
    /// </summary>
    /// <returns>A new instance with the same property values.</returns>
    public AIAgentState Clone()
    {
        return new AIAgentState
        {
            AgentId = AgentId,
            AgentType = AgentType,
            Properties = new Dictionary<string, object>(Properties),
            CreatedAt = CreatedAt,
            LastUpdatedAt = DateTime.UtcNow,
            Status = Status
        };
    }
}

/// <summary>
/// Defines the status of an AI agent.
/// </summary>
public enum AIAgentStatus
{
    /// <summary>
    /// Agent is idle and waiting for tasks.
    /// </summary>
    Idle,

    /// <summary>
    /// Agent is actively processing a task.
    /// </summary>
    Processing,

    /// <summary>
    /// Agent is busy and cannot accept new tasks.
    /// </summary>
    Busy,

    /// <summary>
    /// Agent has encountered an error.
    /// </summary>
    Error,

    /// <summary>
    /// Agent is offline or unavailable.
    /// </summary>
    Offline
}
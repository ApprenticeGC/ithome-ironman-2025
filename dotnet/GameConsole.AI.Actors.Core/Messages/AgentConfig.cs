namespace GameConsole.AI.Actors.Core.Messages;

/// <summary>
/// Configuration settings for AI agents.
/// </summary>
public record AgentConfig
{
    /// <summary>
    /// Gets the unique identifier for this agent instance.
    /// </summary>
    public string AgentId { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the type of AI agent (dialogue, analysis, codegen, etc.).
    /// </summary>
    public string AgentType { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the maximum number of concurrent requests this agent can handle.
    /// </summary>
    public int MaxConcurrentRequests { get; init; } = 5;
    
    /// <summary>
    /// Gets the timeout for processing requests.
    /// </summary>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(2);
    
    /// <summary>
    /// Gets the maximum number of retries for failed requests.
    /// </summary>
    public int MaxRetries { get; init; } = 3;
    
    /// <summary>
    /// Gets the preferred cluster role for this agent (for cluster routing).
    /// </summary>
    public string? ClusterRole { get; init; }
    
    /// <summary>
    /// Gets additional configuration properties specific to the agent type.
    /// </summary>
    public Dictionary<string, object> Properties { get; init; } = new();
    
    /// <summary>
    /// Gets the backend service configuration for this agent.
    /// </summary>
    public BackendConfig Backend { get; init; } = new();
}

/// <summary>
/// Configuration for AI backend services.
/// </summary>
public record BackendConfig
{
    /// <summary>
    /// Gets the name of the backend service.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the endpoint URL for the backend service.
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets the API key for authentication (if required).
    /// </summary>
    public string? ApiKey { get; init; }
    
    /// <summary>
    /// Gets the model name to use for this backend.
    /// </summary>
    public string Model { get; init; } = string.Empty;
    
    /// <summary>
    /// Gets additional backend-specific configuration.
    /// </summary>
    public Dictionary<string, object> Settings { get; init; } = new();
}
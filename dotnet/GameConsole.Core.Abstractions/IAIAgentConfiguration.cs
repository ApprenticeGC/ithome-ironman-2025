namespace GameConsole.Core.Abstractions;

/// <summary>
/// Provides configuration settings for AI agents.
/// Contains runtime configuration, dependency access, and environment settings.
/// </summary>
public interface IAIAgentConfiguration
{
    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets configuration values specific to this AI agent.
    /// </summary>
    IReadOnlyDictionary<string, object> Settings { get; }

    /// <summary>
    /// Gets the maximum execution timeout for agent operations.
    /// </summary>
    TimeSpan ExecutionTimeout { get; }

    /// <summary>
    /// Gets the maximum memory allocation allowed for this agent.
    /// </summary>
    long MaxMemoryBytes { get; }

    /// <summary>
    /// Gets a value indicating whether the agent should operate in debug mode.
    /// </summary>
    bool IsDebugMode { get; }

    /// <summary>
    /// Gets environment-specific settings for the agent.
    /// </summary>
    IReadOnlyDictionary<string, string> Environment { get; }
}

/// <summary>
/// Represents the health status of an AI agent.
/// </summary>
public class AIAgentHealthStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentHealthStatus"/> class.
    /// </summary>
    /// <param name="isHealthy">Whether the agent is healthy.</param>
    /// <param name="statusMessage">Optional status message.</param>
    /// <param name="lastCheckTime">When the health check was performed.</param>
    public AIAgentHealthStatus(bool isHealthy, string statusMessage = "", DateTime? lastCheckTime = null)
    {
        IsHealthy = isHealthy;
        StatusMessage = statusMessage ?? string.Empty;
        LastCheckTime = lastCheckTime ?? DateTime.UtcNow;
        Diagnostics = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets a value indicating whether the agent is healthy.
    /// </summary>
    public bool IsHealthy { get; }

    /// <summary>
    /// Gets the status message providing details about the agent's health.
    /// </summary>
    public string StatusMessage { get; }

    /// <summary>
    /// Gets the timestamp when the health check was performed.
    /// </summary>
    public DateTime LastCheckTime { get; }

    /// <summary>
    /// Gets additional diagnostic information about the agent's health.
    /// </summary>
    public IReadOnlyDictionary<string, object> Diagnostics { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentHealthStatus"/> class with diagnostics.
    /// </summary>
    /// <param name="isHealthy">Whether the agent is healthy.</param>
    /// <param name="statusMessage">Optional status message.</param>
    /// <param name="lastCheckTime">When the health check was performed.</param>
    /// <param name="diagnostics">Additional diagnostic information.</param>
    public AIAgentHealthStatus(bool isHealthy, string statusMessage, DateTime? lastCheckTime, IReadOnlyDictionary<string, object> diagnostics)
        : this(isHealthy, statusMessage, lastCheckTime)
    {
        Diagnostics = diagnostics ?? new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates a healthy status instance.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    /// <returns>A healthy status instance.</returns>
    public static AIAgentHealthStatus Healthy(string message = "Agent is operating normally")
        => new(true, message);

    /// <summary>
    /// Creates an unhealthy status instance.
    /// </summary>
    /// <param name="message">Error or warning message.</param>
    /// <returns>An unhealthy status instance.</returns>
    public static AIAgentHealthStatus Unhealthy(string message)
        => new(false, message);
}
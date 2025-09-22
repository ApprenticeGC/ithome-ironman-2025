using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Registry;

/// <summary>
/// Default implementation of <see cref="IAIAgentConfiguration"/>.
/// Provides basic configuration settings for AI agents.
/// </summary>
public class DefaultAIAgentConfiguration : IAIAgentConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAIAgentConfiguration"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="settings">Configuration values specific to the AI agent.</param>
    /// <param name="executionTimeout">The maximum execution timeout for agent operations.</param>
    /// <param name="maxMemoryBytes">The maximum memory allocation allowed for the agent.</param>
    /// <param name="isDebugMode">Whether the agent should operate in debug mode.</param>
    /// <param name="environment">Environment-specific settings for the agent.</param>
    public DefaultAIAgentConfiguration(
        IServiceProvider serviceProvider,
        IReadOnlyDictionary<string, object>? settings = null,
        TimeSpan? executionTimeout = null,
        long maxMemoryBytes = 0,
        bool isDebugMode = false,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Settings = settings ?? new Dictionary<string, object>();
        ExecutionTimeout = executionTimeout ?? TimeSpan.FromSeconds(30);
        MaxMemoryBytes = maxMemoryBytes;
        IsDebugMode = isDebugMode;
        Environment = environment ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets configuration values specific to this AI agent.
    /// </summary>
    public IReadOnlyDictionary<string, object> Settings { get; }

    /// <summary>
    /// Gets the maximum execution timeout for agent operations.
    /// </summary>
    public TimeSpan ExecutionTimeout { get; }

    /// <summary>
    /// Gets the maximum memory allocation allowed for this agent.
    /// </summary>
    public long MaxMemoryBytes { get; }

    /// <summary>
    /// Gets a value indicating whether the agent should operate in debug mode.
    /// </summary>
    public bool IsDebugMode { get; }

    /// <summary>
    /// Gets environment-specific settings for the agent.
    /// </summary>
    public IReadOnlyDictionary<string, string> Environment { get; }
}
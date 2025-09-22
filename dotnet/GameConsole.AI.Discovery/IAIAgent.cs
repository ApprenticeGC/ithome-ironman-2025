using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Discovery;

/// <summary>
/// Basic interface for AI agents in the system.
/// Provides minimal contract for agent lifecycle and capability reporting.
/// </summary>
public interface IAIAgent : ICapabilityProvider
{
    /// <summary>
    /// Unique identifier for this agent instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable name of the agent.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Current status of the agent.
    /// </summary>
    AgentStatus Status { get; }

    /// <summary>
    /// Initializes the agent with the provided context.
    /// </summary>
    /// <param name="context">Initialization context containing configuration and dependencies.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async initialization.</returns>
    Task InitializeAsync(AgentInitializationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down the agent gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async shutdown.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async health check, returning true if healthy.</returns>
    Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Status of an AI agent.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// Agent is not yet initialized.
    /// </summary>
    Uninitialized = 0,

    /// <summary>
    /// Agent is currently initializing.
    /// </summary>
    Initializing = 1,

    /// <summary>
    /// Agent is ready and available for use.
    /// </summary>
    Ready = 2,

    /// <summary>
    /// Agent is busy processing a request.
    /// </summary>
    Busy = 3,

    /// <summary>
    /// Agent has encountered an error.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Agent is shutting down.
    /// </summary>
    ShuttingDown = 5,

    /// <summary>
    /// Agent has been shut down.
    /// </summary>
    Shutdown = 6
}

/// <summary>
/// Context provided to agents during initialization.
/// </summary>
public class AgentInitializationContext
{
    /// <summary>
    /// Service provider for dependency injection.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Configuration data for the agent.
    /// </summary>
    public IReadOnlyDictionary<string, object> Configuration { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Working directory for the agent.
    /// </summary>
    public string? WorkingDirectory { get; init; }
}
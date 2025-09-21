using GameConsole.AI.Core.Models;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Services;

/// <summary>
/// Defines the base interface for all AI agents in the GameConsole system.
/// Provides capability discovery, lifecycle management, and execution operations.
/// </summary>
public interface IAIAgent : ICapabilityProvider, IAsyncDisposable
{
    /// <summary>
    /// Gets the metadata information for this agent.
    /// This includes identity, version, model information, and requirements.
    /// </summary>
    AIAgentMetadata Metadata { get; }

    /// <summary>
    /// Gets or sets the execution context for this agent.
    /// The context provides access to resources, security constraints, and configuration.
    /// This is typically set by the agent manager before initialization.
    /// </summary>
    IAIContext? Context { get; set; }

    /// <summary>
    /// Gets the current operational status of the agent.
    /// </summary>
    GameConsole.AI.Core.AgentStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether the agent is currently processing a request.
    /// </summary>
    bool IsBusy { get; }

    /// <summary>
    /// Gets the list of capabilities that this agent provides.
    /// </summary>
    IReadOnlyList<IAICapability> Capabilities { get; }

    /// <summary>
    /// Initializes the agent with its execution context.
    /// This method is called before the agent can process any requests.
    /// </summary>
    /// <param name="context">The execution context for the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(IAIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a request and returns the result.
    /// This method provides synchronous-style request processing.
    /// </summary>
    /// <param name="input">The input data or prompt for the agent.</param>
    /// <param name="options">Optional execution parameters and configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the agent's response.</returns>
    Task<string> ExecuteAsync(string input, AgentExecutionOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a request and streams the results as they become available.
    /// This method is ideal for long-running operations or real-time interactions.
    /// </summary>
    /// <param name="input">The input data or prompt for the agent.</param>
    /// <param name="options">Optional execution parameters and configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable that yields response chunks as they are generated.</returns>
    IAsyncEnumerable<string> StreamAsync(string input, AgentExecutionOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether the agent can process a specific type of input or perform a specific operation.
    /// </summary>
    /// <param name="inputType">The type or format of input to validate.</param>
    /// <param name="operation">The specific operation to validate, if applicable.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the input type and operation are supported.</returns>
    Task<bool> CanProcessAsync(string inputType, string? operation = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current performance metrics for this agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the current performance metrics.</returns>
    Task<IReadOnlyList<GameConsole.AI.Core.PerformanceMetric>> GetPerformanceMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests the agent to perform cleanup and prepare for shutdown.
    /// This method should be called before disposing the agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cleanup operation.</returns>
    Task CleanupAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies the agent that its configuration has changed and it should reload settings.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration reload operation.</returns>
    Task ReloadConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides execution options and parameters for agent operations.
/// </summary>
public class AgentExecutionOptions
{
    /// <summary>
    /// Gets or sets the maximum execution time for the operation.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens or response length.
    /// </summary>
    public int? MaxResponseLength { get; set; }

    /// <summary>
    /// Gets or sets the temperature or randomness parameter for generation (0.0 to 1.0).
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Gets or sets additional parameters specific to the agent or model.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the priority level for this execution request.
    /// </summary>
    public ExecutionPriority Priority { get; set; } = ExecutionPriority.Normal;

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed performance tracking for this request.
    /// </summary>
    public bool EnableProfiling { get; set; } = false;

    /// <summary>
    /// Gets or sets context information that should be maintained across related requests.
    /// </summary>
    public string? ConversationId { get; set; }
}

/// <summary>
/// Specifies the priority level for agent execution requests.
/// </summary>
public enum ExecutionPriority
{
    /// <summary>
    /// Low priority, can be delayed or throttled.
    /// </summary>
    Low,
    
    /// <summary>
    /// Normal priority, standard processing.
    /// </summary>
    Normal,
    
    /// <summary>
    /// High priority, should be processed quickly.
    /// </summary>
    High,
    
    /// <summary>
    /// Critical priority, process immediately.
    /// </summary>
    Critical
}
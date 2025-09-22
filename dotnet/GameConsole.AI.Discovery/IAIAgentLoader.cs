namespace GameConsole.AI.Discovery;

/// <summary>
/// Interface for safely loading and initializing AI agents.
/// Provides validation, resource checks, and lifecycle management.
/// </summary>
public interface IAIAgentLoader
{
    /// <summary>
    /// Loads and initializes an AI agent instance.
    /// </summary>
    /// <param name="metadata">Metadata of the agent to load.</param>
    /// <param name="context">Initialization context for the agent.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning the loaded and initialized agent instance.</returns>
    Task<IAIAgent> LoadAgentAsync(AgentMetadata metadata, AgentInitializationContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that an agent can be safely loaded.
    /// </summary>
    /// <param name="metadata">Metadata of the agent to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning validation results.</returns>
    Task<AgentValidationResult> ValidateAgentAsync(AgentMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if system resources are sufficient for loading an agent.
    /// </summary>
    /// <param name="requirements">Resource requirements to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning resource availability check results.</returns>
    Task<ResourceAvailabilityResult> CheckResourceAvailabilityAsync(AgentResourceRequirements requirements, CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely shuts down and disposes an agent instance.
    /// </summary>
    /// <param name="agent">Agent instance to shut down.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async shutdown operation.</returns>
    Task ShutdownAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Results of agent validation.
/// </summary>
public class AgentValidationResult
{
    /// <summary>
    /// Whether the agent is valid and can be loaded.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation error messages, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Validation warning messages, if any.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether the agent type is instantiable.
    /// </summary>
    public bool IsInstantiable { get; init; }

    /// <summary>
    /// Whether the agent implements required interfaces.
    /// </summary>
    public bool ImplementsRequiredInterfaces { get; init; }

    /// <summary>
    /// Whether the agent's assembly is loadable.
    /// </summary>
    public bool IsAssemblyLoadable { get; init; }
}

/// <summary>
/// Results of resource availability check.
/// </summary>
public class ResourceAvailabilityResult
{
    /// <summary>
    /// Whether sufficient resources are available.
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// Available memory in bytes.
    /// </summary>
    public long AvailableMemoryBytes { get; init; }

    /// <summary>
    /// Available CPU cores.
    /// </summary>
    public int AvailableCpuCores { get; init; }

    /// <summary>
    /// Whether GPU is available if required.
    /// </summary>
    public bool IsGpuAvailable { get; init; }

    /// <summary>
    /// Whether network access is available if required.
    /// </summary>
    public bool IsNetworkAvailable { get; init; }

    /// <summary>
    /// Resource availability messages.
    /// </summary>
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();
}
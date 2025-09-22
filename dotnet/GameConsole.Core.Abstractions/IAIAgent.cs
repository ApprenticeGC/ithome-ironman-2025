namespace GameConsole.Core.Abstractions;

/// <summary>
/// Defines the base interface for all AI agents in the GameConsole system.
/// Extends the base <see cref="IService"/> interface with AI-specific functionality
/// including capability provision, metadata access, and enhanced lifecycle operations.
/// </summary>
public interface IAIAgent : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the metadata information for this AI agent.
    /// This includes identity, version, capabilities, and other agent-specific information.
    /// </summary>
    IAIAgentMetadata Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether the agent is currently ready to accept requests.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Configures the AI agent with runtime settings and dependencies.
    /// This method is called before <see cref="IService.InitializeAsync"/> to provide
    /// the agent with access to its runtime configuration and dependencies.
    /// </summary>
    /// <param name="configuration">The runtime configuration for the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration operation.</returns>
    Task ConfigureAsync(IAIAgentConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the agent is properly configured and ready to start.
    /// This method is called after configuration but before initialization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent is valid and ready to start.</returns>
    Task<bool> ValidateConfigurationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the AI agent to ensure it's operating correctly.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the health check result.</returns>
    Task<AIAgentHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
}
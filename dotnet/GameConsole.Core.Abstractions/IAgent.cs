namespace GameConsole.Core.Abstractions;

/// <summary>
/// Base interface for all AI agents in the GameConsole 4-tier architecture.
/// Defines the fundamental lifecycle operations and capabilities that all agents must support.
/// </summary>
public interface IAgent : IAsyncDisposable
{
    /// <summary>
    /// Initializes the agent asynchronously.
    /// This method is called once during agent setup before the agent is activated.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Activates the agent asynchronously.
    /// This method is called to begin agent operations after initialization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async activation operation.</returns>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates the agent asynchronously.
    /// This method is called to gracefully shut down agent operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async deactivation operation.</returns>
    Task DeactivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a value indicating whether the agent is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the agent's unique identifier.
    /// </summary>
    string AgentId { get; }

    /// <summary>
    /// Gets the agent's capabilities and metadata.
    /// </summary>
    IAgentMetadata Metadata { get; }
}
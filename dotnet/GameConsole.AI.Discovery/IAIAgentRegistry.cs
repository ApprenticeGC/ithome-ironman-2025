namespace GameConsole.AI.Discovery;

/// <summary>
/// Registry for managing discovered AI agents.
/// Provides registration, lookup, and persistence capabilities.
/// </summary>
public interface IAIAgentRegistry
{
    /// <summary>
    /// Registers an agent with the registry.
    /// </summary>
    /// <param name="metadata">Metadata for the agent to register.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async registration.</returns>
    Task RegisterAgentAsync(AgentMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an agent from the registry.
    /// </summary>
    /// <param name="agentId">ID of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async unregistration, returning true if successful.</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning a collection of all registered agent metadata.</returns>
    Task<IEnumerable<AgentMetadata>> GetAllAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agents that provide a specific capability.
    /// </summary>
    /// <param name="capabilityType">The capability type to search for.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning a collection of agents with the specified capability.</returns>
    Task<IEnumerable<AgentMetadata>> GetAgentsByCapabilityAsync(Type capabilityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets agents that match the specified tags.
    /// </summary>
    /// <param name="tags">Tags to search for.</param>
    /// <param name="requireAll">If true, agents must have all tags; if false, agents need only one tag.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning a collection of agents with matching tags.</returns>
    Task<IEnumerable<AgentMetadata>> GetAgentsByTagsAsync(IEnumerable<string> tags, bool requireAll = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific agent by ID.
    /// </summary>
    /// <param name="agentId">ID of the agent to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning the agent metadata, or null if not found.</returns>
    Task<AgentMetadata?> GetAgentByIdAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the availability status of an agent.
    /// </summary>
    /// <param name="agentId">ID of the agent to update.</param>
    /// <param name="isAvailable">New availability status.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async update, returning true if successful.</returns>
    Task<bool> UpdateAgentAvailabilityAsync(string agentId, bool isAvailable, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the registry state to storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async save operation.</returns>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the registry state from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async load operation.</returns>
    Task LoadAsync(CancellationToken cancellationToken = default);
}
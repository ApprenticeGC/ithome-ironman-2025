namespace GameConsole.AI.Discovery;

/// <summary>
/// Interface for discovering AI agents in the system.
/// Provides methods to scan directories and assemblies for agents.
/// </summary>
public interface IAIAgentDiscovery
{
    /// <summary>
    /// Discovers AI agents in the specified directory.
    /// </summary>
    /// <param name="searchPath">Path to search for agent assemblies.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning a collection of discovered agent metadata.</returns>
    Task<IEnumerable<AgentMetadata>> DiscoverAgentsAsync(string searchPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents in the specified assembly.
    /// </summary>
    /// <param name="assemblyPath">Path to the assembly to scan.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning a collection of discovered agent metadata.</returns>
    Task<IEnumerable<AgentMetadata>> DiscoverAgentsInAssemblyAsync(string assemblyPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that an agent type implements the required interfaces.
    /// </summary>
    /// <param name="agentType">The type to validate.</param>
    /// <returns>True if the type is a valid agent, false otherwise.</returns>
    bool IsValidAgentType(Type agentType);

    /// <summary>
    /// Extracts metadata from an agent type using reflection.
    /// </summary>
    /// <param name="agentType">The agent type to analyze.</param>
    /// <returns>Agent metadata extracted from the type.</returns>
    AgentMetadata ExtractAgentMetadata(Type agentType);
}
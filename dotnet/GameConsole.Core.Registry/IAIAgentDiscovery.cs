using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Registry;

/// <summary>
/// Interface for discovering AI agents within the GameConsole system.
/// Provides capability-based discovery and dependency resolution for AI agents.
/// </summary>
public interface IAIAgentDiscovery
{
    /// <summary>
    /// Discovers all available AI agents in loaded assemblies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of discovered AI agent descriptors.</returns>
    Task<IEnumerable<AIAgentDescriptor>> DiscoverAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents that provide specific capabilities.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to search for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of AI agent descriptors that provide the specified capability.</returns>
    Task<IEnumerable<AIAgentDescriptor>> DiscoverAgentsByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents that match specific tags or categories.
    /// </summary>
    /// <param name="tags">The tags to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of AI agent descriptors that match the specified tags.</returns>
    Task<IEnumerable<AIAgentDescriptor>> DiscoverAgentsByTagsAsync(string[] tags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an AI agent descriptor by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The AI agent descriptor, or null if not found.</returns>
    Task<AIAgentDescriptor?> GetAgentDescriptorAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans assemblies for AI agents decorated with <see cref="AIAgentAttribute"/>.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of discovered AI agent descriptors.</returns>
    Task<IEnumerable<AIAgentDescriptor>> ScanAssembliesAsync(System.Reflection.Assembly[] assemblies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the dependencies of discovered AI agents and returns them in dependency order.
    /// </summary>
    /// <param name="agents">The AI agent descriptors to validate and order.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>AI agent descriptors ordered by dependencies, or throws if circular dependencies are found.</returns>
    Task<IEnumerable<AIAgentDescriptor>> ValidateAndOrderDependenciesAsync(IEnumerable<AIAgentDescriptor> agents, CancellationToken cancellationToken = default);
}
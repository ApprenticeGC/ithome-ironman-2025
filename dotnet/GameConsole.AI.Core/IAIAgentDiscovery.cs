using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core
{

/// <summary>
/// Service interface for discovering AI agents from various sources.
/// This service scans assemblies, plugins, and other sources to find available AI agents.
/// </summary>
public interface IAIAgentDiscovery : IService
{
    /// <summary>
    /// Discovers AI agents from a specific assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for AI agents.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of discovered agent metadata.</returns>
    Task<IEnumerable<IAIAgentMetadata>> DiscoverFromAssemblyAsync(System.Reflection.Assembly assembly, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents from all loaded assemblies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of discovered agent metadata.</returns>
    Task<IEnumerable<IAIAgentMetadata>> DiscoverAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents that provide a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to search for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of discovered agent metadata that provide the specified capability.</returns>
    Task<IEnumerable<IAIAgentMetadata>> DiscoverByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default) where TCapability : class;

    /// <summary>
    /// Discovers AI agents by category.
    /// </summary>
    /// <param name="category">The category to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of discovered agent metadata in the specified category.</returns>
    Task<IEnumerable<IAIAgentMetadata>> DiscoverByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers AI agents that can handle a specific request type.
    /// </summary>
    /// <param name="requestType">The type of request to search for handlers.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of discovered agent metadata that can handle the specified request type.</returns>
    Task<IEnumerable<IAIAgentMetadata>> DiscoverByRequestTypeAsync(Type requestType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when new AI agents are discovered.
    /// </summary>
    event EventHandler<AIAgentDiscoveredEventArgs> AgentDiscovered;

    /// <summary>
    /// Event raised when AI agents are removed or become unavailable.
    /// </summary>
    event EventHandler<AIAgentRemovedEventArgs> AgentRemoved;
}
}
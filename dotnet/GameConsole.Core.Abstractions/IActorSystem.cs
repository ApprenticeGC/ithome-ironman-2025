using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Actor system execution mode settings.
/// </summary>
public enum ActorSystemMode
{
    /// <summary>
    /// Single-node mode for local development and testing.
    /// </summary>
    SingleNode,
    
    /// <summary>
    /// Clustered mode for distributed actor systems.
    /// </summary>
    Clustered,
    
    /// <summary>
    /// Hybrid mode that can operate in both single-node and clustered configurations.
    /// </summary>
    Hybrid
}

/// <summary>
/// Arguments for actor system-related events.
/// </summary>
public class ActorSystemEventArgs : EventArgs
{
    /// <summary>
    /// The name of the actor system.
    /// </summary>
    public string SystemName { get; }
    
    /// <summary>
    /// The current mode of the actor system.
    /// </summary>
    public ActorSystemMode Mode { get; }
    
    /// <summary>
    /// Optional additional data about the actor system event.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new instance of the ActorSystemEventArgs class.
    /// </summary>
    /// <param name="systemName">The name of the actor system.</param>
    /// <param name="mode">The current mode of the actor system.</param>
    /// <param name="data">Optional additional data about the actor system event.</param>
    public ActorSystemEventArgs(string systemName, ActorSystemMode mode, object? data = null)
    {
        SystemName = systemName ?? throw new ArgumentNullException(nameof(systemName));
        Mode = mode;
        Data = data;
    }
}

/// <summary>
/// Tier 1: Core abstraction for the actor system that manages AI agents and clustering.
/// Provides the foundational infrastructure for creating, managing, and coordinating
/// distributed AI agents across single-node and clustered configurations.
/// </summary>
public interface IActorSystem : IService, ICapabilityProvider
{
    /// <summary>
    /// Event raised when the actor system mode changes.
    /// </summary>
    event EventHandler<ActorSystemEventArgs>? ModeChanged;
    
    /// <summary>
    /// Event raised when a new cluster is created in the system.
    /// </summary>
    event EventHandler<ClusterEventArgs>? ClusterCreated;
    
    /// <summary>
    /// Event raised when a cluster is removed from the system.
    /// </summary>
    event EventHandler<ClusterEventArgs>? ClusterRemoved;

    /// <summary>
    /// Gets the name of this actor system instance.
    /// </summary>
    string SystemName { get; }
    
    /// <summary>
    /// Gets the current execution mode of the actor system.
    /// </summary>
    ActorSystemMode Mode { get; }

    /// <summary>
    /// Creates a new AI agent with the specified type and configuration.
    /// </summary>
    /// <param name="agentType">The type identifier for the agent.</param>
    /// <param name="agentId">Optional specific identifier for the agent. If null, a unique ID will be generated.</param>
    /// <param name="configuration">Optional configuration data for the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created agent.</returns>
    Task<IAIAgent> CreateAgentAsync(string agentType, string? agentId = null, object? configuration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys an AI agent and cleans up its resources.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to destroy.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async agent destruction operation.</returns>
    Task DestroyAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new agent cluster with the specified identifier.
    /// </summary>
    /// <param name="clusterId">The unique identifier for the cluster.</param>
    /// <param name="configuration">Optional configuration data for the cluster.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the created cluster.</returns>
    Task<IAgentCluster> CreateClusterAsync(string clusterId, object? configuration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys an agent cluster and removes all its agents.
    /// </summary>
    /// <param name="clusterId">The identifier of the cluster to destroy.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cluster destruction operation.</returns>
    Task DestroyClusterAsync(string clusterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available clusters in the actor system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns cluster identifiers.</returns>
    Task<IEnumerable<string>> GetClusterIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific cluster by its identifier.
    /// </summary>
    /// <param name="clusterId">The identifier of the cluster to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the cluster, or null if not found.</returns>
    Task<IAgentCluster?> GetClusterAsync(string clusterId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all agents across all clusters in the actor system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns all agent identifiers.</returns>
    Task<IEnumerable<string>> GetAllAgentIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds agents of a specific type across all clusters.
    /// </summary>
    /// <param name="agentType">The type of agents to find.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns matching agent identifiers.</returns>
    Task<IEnumerable<string>> FindAgentsByTypeAsync(string agentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific agent from any cluster in the system.
    /// </summary>
    /// <param name="agentId">The identifier of the agent to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the agent, or null if not found.</returns>
    Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the execution mode of the actor system.
    /// </summary>
    /// <param name="mode">The new execution mode to set.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async mode change operation.</returns>
    Task SetModeAsync(ActorSystemMode mode, CancellationToken cancellationToken = default);
}
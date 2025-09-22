using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core
{

/// <summary>
/// Service interface for registering and managing AI agents.
/// This service maintains a registry of available AI agents and provides access to them.
/// </summary>
public interface IAIAgentRegistry : IService
{
    /// <summary>
    /// Registers an AI agent in the registry.
    /// </summary>
    /// <param name="agent">The AI agent to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    Task RegisterAsync(IAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the registry.
    /// </summary>
    /// <param name="agentId">The ID of the AI agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent was unregistered, false if it was not found.</returns>
    Task<bool> UnregisterAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of all registered AI agents.</returns>
    Task<IEnumerable<IAIAgent>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific AI agent by ID.
    /// </summary>
    /// <param name="agentId">The ID of the AI agent to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the AI agent if found, or null if not found.</returns>
    Task<IAIAgent> GetByIdAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AI agents by category.
    /// </summary>
    /// <param name="category">The category to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of AI agents in the specified category.</returns>
    Task<IEnumerable<IAIAgent>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets AI agents that provide a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to search for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of AI agents that provide the specified capability.</returns>
    Task<IEnumerable<IAIAgent>> GetByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default) where TCapability : class;

    /// <summary>
    /// Gets AI agents that can handle a specific request type.
    /// </summary>
    /// <param name="requestType">The type of request to search for handlers.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of AI agents that can handle the specified request type.</returns>
    Task<IEnumerable<IAIAgent>> GetByRequestTypeAsync(Type requestType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available AI agents that can handle a specific request type.
    /// This filters out agents that are not available (e.g., overloaded, in maintenance, etc.).
    /// </summary>
    /// <param name="requestType">The type of request to search for handlers.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of available AI agents that can handle the specified request type.</returns>
    Task<IEnumerable<IAIAgent>> GetAvailableByRequestTypeAsync(Type requestType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an AI agent is registered.
    /// </summary>
    /// <param name="agentId">The ID of the AI agent to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent is registered, false otherwise.</returns>
    Task<bool> IsRegisteredAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the number of registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the count of registered AI agents.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when an AI agent is registered.
    /// </summary>
    event EventHandler<AIAgentRegisteredEventArgs> AgentRegistered;

    /// <summary>
    /// Event raised when an AI agent is unregistered.
    /// </summary>
    event EventHandler<AIAgentUnregisteredEventArgs> AgentUnregistered;

    /// <summary>
    /// Event raised when an AI agent's status changes.
    /// </summary>
    event EventHandler<AIAgentStatusChangedEventArgs> AgentStatusChanged;
}
}
using GameConsole.Core.Abstractions;
using System.Collections.Concurrent;

namespace GameConsole.AI.Core;

/// <summary>
/// Concrete implementation of the AI agent registry that manages discovery, registration, and lifecycle of AI agents.
/// Thread-safe implementation using concurrent collections for high-performance agent management.
/// </summary>
public class AIAgentRegistry : IAIAgentRegistry
{
    private readonly ConcurrentDictionary<string, IAIAgent> _agents = new();
    private volatile bool _isRunning = false;

    /// <summary>
    /// Event raised when an AI agent is registered.
    /// </summary>
    public event EventHandler<IAIAgent>? AgentRegistered;

    /// <summary>
    /// Event raised when an AI agent is unregistered.
    /// </summary>
    public event EventHandler<string>? AgentUnregistered;

    /// <summary>
    /// Gets a value indicating whether the service is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Initializes the AI agent registry asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the AI agent registry service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the AI agent registry service asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registers an AI agent with the registry.
    /// </summary>
    /// <param name="agent">The AI agent to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async registration operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the agent is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an agent with the same ID is already registered.</exception>
    public Task RegisterAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        if (string.IsNullOrWhiteSpace(agent.AgentId))
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agent));

        if (!_agents.TryAdd(agent.AgentId, agent))
            throw new ArgumentException($"Agent with ID '{agent.AgentId}' is already registered", nameof(agent));

        AgentRegistered?.Invoke(this, agent);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Unregisters an AI agent from the registry.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async unregistration operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the agentId is null or empty.</exception>
    public Task UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            throw new ArgumentNullException(nameof(agentId));

        if (_agents.TryRemove(agentId, out _))
        {
            AgentUnregistered?.Invoke(this, agentId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all registered AI agents.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of all registered AI agents.</returns>
    public Task<IEnumerable<IAIAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<IAIAgent>>(_agents.Values.ToList());
    }

    /// <summary>
    /// Gets an AI agent by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the AI agent if found, or null if not found.</returns>
    public Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            return Task.FromResult<IAIAgent?>(null);

        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult<IAIAgent?>(agent);
    }

    /// <summary>
    /// Discovers AI agents that can handle a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to discover agents for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of agents that provide the specified capability, ordered by priority.</returns>
    public async Task<IEnumerable<IAIAgent>> DiscoverAgentsByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default)
        where TCapability : class
    {
        var matchingAgents = new List<IAIAgent>();

        foreach (var agent in _agents.Values)
        {
            if (await agent.HasCapabilityAsync<TCapability>(cancellationToken))
            {
                matchingAgents.Add(agent);
            }
        }

        return matchingAgents.OrderByDescending(a => a.Priority).ToList();
    }

    /// <summary>
    /// Discovers AI agents that can handle a specific request type.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to discover agents for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns a collection of agents that can handle the specified request type, ordered by priority.</returns>
    public async Task<IEnumerable<IAIAgent>> DiscoverAgentsByRequestAsync<TRequest>(CancellationToken cancellationToken = default)
        where TRequest : class
    {
        var matchingAgents = new List<IAIAgent>();

        foreach (var agent in _agents.Values)
        {
            if (await agent.CanHandleAsync<TRequest>(cancellationToken))
            {
                matchingAgents.Add(agent);
            }
        }

        return matchingAgents.OrderByDescending(a => a.Priority).ToList();
    }

    /// <summary>
    /// Gets the best AI agent for handling a specific capability based on priority and availability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to find the best agent for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the best AI agent for the capability, or null if none are available.</returns>
    public async Task<IAIAgent?> GetBestAgentForCapabilityAsync<TCapability>(CancellationToken cancellationToken = default)
        where TCapability : class
    {
        var agents = await DiscoverAgentsByCapabilityAsync<TCapability>(cancellationToken);
        return agents.FirstOrDefault();
    }

    /// <summary>
    /// Gets the best AI agent for handling a specific request type based on priority and availability.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to find the best agent for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the best AI agent for the request type, or null if none are available.</returns>
    public async Task<IAIAgent?> GetBestAgentForRequestAsync<TRequest>(CancellationToken cancellationToken = default)
        where TRequest : class
    {
        var agents = await DiscoverAgentsByRequestAsync<TRequest>(cancellationToken);
        return agents.FirstOrDefault();
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        _agents.Clear();
        GC.SuppressFinalize(this);
    }
}
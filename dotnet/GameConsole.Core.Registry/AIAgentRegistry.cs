using System.Collections.Concurrent;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Core.Registry;

/// <summary>
/// Default implementation of the AI agent registry.
/// Provides thread-safe registration and management of AI agents.
/// </summary>
[Service("AIAgentRegistry", "1.0.0", "Manages registration and lifecycle of AI agents")]
public sealed class AIAgentRegistry : IAIAgentRegistry
{
    private readonly ConcurrentDictionary<string, IAIAgent> _agents = new();
    private readonly ILogger<AIAgentRegistry>? _logger;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentRegistry"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for registry operations.</param>
    public AIAgentRegistry(ILogger<AIAgentRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event AsyncEventHandler<AIAgentRegisteredEventArgs>? AgentRegistered;

    /// <inheritdoc />
    public event AsyncEventHandler<AIAgentUnregisteredEventArgs>? AgentUnregistered;

    /// <inheritdoc />
#pragma warning disable CS0067 // Event is part of interface contract but not used in this implementation
    public event AsyncEventHandler<AIAgentStatusChangedEventArgs>? AgentStatusChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Initializing AI Agent Registry");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting AI Agent Registry");
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Stopping AI Agent Registry");
        _isRunning = false;

        // Stop all registered agents
        var stopTasks = _agents.Values.Select(agent => StopAgentSafely(agent, cancellationToken));
        await Task.WhenAll(stopTasks);
    }

    /// <inheritdoc />
    public async Task<bool> RegisterAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ThrowIfDisposed();

        if (_agents.TryAdd(agent.Id, agent))
        {
            _logger?.LogInformation("Registered AI agent '{AgentId}' ({AgentName})", agent.Id, agent.Name);
            
            // Initialize and start the agent if the registry is running
            if (_isRunning && !agent.IsRunning)
            {
                try
                {
                    await agent.InitializeAsync(cancellationToken);
                    await agent.StartAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to initialize/start agent '{AgentId}' during registration", agent.Id);
                    _agents.TryRemove(agent.Id, out _);
                    return false;
                }
            }

            // Raise registration event
            if (AgentRegistered != null)
            {
                var eventArgs = new AIAgentRegisteredEventArgs(agent);
                await AgentRegistered.Invoke(this, eventArgs);
            }

            return true;
        }

        _logger?.LogWarning("Failed to register AI agent '{AgentId}' - already registered", agent.Id);
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> UnregisterAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ThrowIfDisposed();

        if (_agents.TryRemove(agentId, out var agent))
        {
            _logger?.LogInformation("Unregistered AI agent '{AgentId}' ({AgentName})", agentId, agent.Name);
            
            // Stop the agent
            await StopAgentSafely(agent, cancellationToken);

            // Raise unregistration event
            if (AgentUnregistered != null)
            {
                var eventArgs = new AIAgentUnregisteredEventArgs(agentId, agent);
                await AgentUnregistered.Invoke(this, eventArgs);
            }

            return true;
        }

        _logger?.LogWarning("Failed to unregister AI agent '{AgentId}' - not found", agentId);
        return false;
    }

    /// <inheritdoc />
    public Task<bool> UnregisterAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        return UnregisterAsync(agent.Id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IAIAgent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Task.FromResult<IEnumerable<IAIAgent>>(_agents.Values.ToList());
    }

    /// <inheritdoc />
    public Task<IAIAgent?> GetByIdAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ThrowIfDisposed();
        
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<bool> IsRegisteredAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ThrowIfDisposed();
        
        return Task.FromResult(_agents.ContainsKey(agentId));
    }

    /// <inheritdoc />
    public Task<bool> IsRegisteredAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        return IsRegisteredAsync(agent.Id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return Task.FromResult(_agents.Count);
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger?.LogInformation("Clearing all registered AI agents");

        var agents = _agents.Values.ToList();
        _agents.Clear();

        // Stop all agents
        var stopTasks = agents.Select(agent => StopAgentSafely(agent, cancellationToken));
        await Task.WhenAll(stopTasks);

        // Raise unregistration events
        if (AgentUnregistered != null)
        {
            var eventTasks = agents.Select(agent => AgentUnregistered.Invoke(this, new AIAgentUnregisteredEventArgs(agent.Id, agent)));
            await Task.WhenAll(eventTasks);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _logger?.LogInformation("Disposing AI Agent Registry");
        await StopAsync();
        await ClearAsync();
        _disposed = true;
    }

    private async Task StopAgentSafely(IAIAgent agent, CancellationToken cancellationToken)
    {
        try
        {
            if (agent.IsRunning)
            {
                await agent.StopAsync(cancellationToken);
            }
            await agent.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping/disposing agent '{AgentId}'", agent.Id);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AIAgentRegistry));
        }
    }
}
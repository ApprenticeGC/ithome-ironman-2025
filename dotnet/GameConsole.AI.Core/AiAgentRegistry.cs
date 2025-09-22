using System.Collections.Concurrent;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Core;

/// <summary>
/// Default implementation of the AI agent registry service.
/// Manages registration and retrieval of AI agents in the system.
/// </summary>
[Service("AI Agent Registry", "1.0.0", "Manages AI agent registration and lifecycle")]
public sealed class AiAgentRegistry : IAiAgentRegistry
{
    private readonly ConcurrentDictionary<string, IAiAgent> _agents = new();
    private readonly ILogger<AiAgentRegistry>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentRegistry"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for registry operations.</param>
    public AiAgentRegistry(ILogger<AiAgentRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <inheritdoc />
    public event EventHandler<AiAgentRegisteredEventArgs>? AgentRegistered;

    /// <inheritdoc />
    public event EventHandler<AiAgentUnregisteredEventArgs>? AgentUnregistered;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger?.LogInformation("Initializing AI Agent Registry");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger?.LogInformation("Starting AI Agent Registry");
        IsRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning) return;

        _logger?.LogInformation("Stopping AI Agent Registry");
        
        // Stop all registered agents
        var stopTasks = _agents.Values.Select(agent => agent.StopAsync(cancellationToken));
        await Task.WhenAll(stopTasks);
        
        IsRunning = false;
        _logger?.LogInformation("AI Agent Registry stopped");
    }

    /// <inheritdoc />
    public async Task<bool> RegisterAsync(IAiAgent agent, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(agent);

        if (_agents.TryAdd(agent.AgentId, agent))
        {
            _logger?.LogInformation("Registered AI agent: {AgentId} ({Name})", agent.AgentId, agent.Name);
            
            // Initialize and start the agent if the registry is running
            if (IsRunning && !agent.IsRunning)
            {
                await agent.InitializeAsync(cancellationToken);
                await agent.StartAsync(cancellationToken);
            }
            
            AgentRegistered?.Invoke(this, new AiAgentRegisteredEventArgs(agent));
            return true;
        }

        _logger?.LogWarning("Failed to register AI agent: {AgentId} - Agent already exists", agent.AgentId);
        return false;
    }

    /// <inheritdoc />
    public async Task<bool> UnregisterAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        if (_agents.TryRemove(agentId, out var agent))
        {
            _logger?.LogInformation("Unregistering AI agent: {AgentId} ({Name})", agentId, agent.Name);
            
            // Stop the agent if it's running
            if (agent.IsRunning)
            {
                await agent.StopAsync(cancellationToken);
            }
            
            AgentUnregistered?.Invoke(this, new AiAgentUnregisteredEventArgs(agentId, "Explicitly unregistered"));
            return true;
        }

        _logger?.LogWarning("Failed to unregister AI agent: {AgentId} - Agent not found", agentId);
        return false;
    }

    /// <inheritdoc />
    public Task<IAiAgent?> GetAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IAiAgent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        IReadOnlyList<IAiAgent> agents = _agents.Values.ToList().AsReadOnly();
        return Task.FromResult(agents);
    }

    /// <inheritdoc />
    public Task<bool> IsRegisteredAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(agentId);

        return Task.FromResult(_agents.ContainsKey(agentId));
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsRunning)
        {
            await StopAsync();
        }

        // Dispose all registered agents
        var disposeTasks = _agents.Values.Select(agent => agent.DisposeAsync().AsTask());
        await Task.WhenAll(disposeTasks);

        _agents.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AiAgentRegistry));
    }
}
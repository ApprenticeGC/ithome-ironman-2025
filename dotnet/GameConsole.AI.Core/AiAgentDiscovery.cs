using System.Reflection;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Core;

/// <summary>
/// Default implementation of the AI agent discovery service.
/// Discovers AI agents through the registry and provides filtering capabilities.
/// </summary>
[Service("AI Agent Discovery", "1.0.0", "Discovers and filters available AI agents")]
public sealed class AiAgentDiscovery : IAiAgentDiscovery
{
    private readonly IAiAgentRegistry _registry;
    private readonly ILogger<AiAgentDiscovery>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentDiscovery"/> class.
    /// </summary>
    /// <param name="registry">The AI agent registry to discover agents from.</param>
    /// <param name="logger">Optional logger for discovery operations.</param>
    public AiAgentDiscovery(IAiAgentRegistry registry, ILogger<AiAgentDiscovery>? logger = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsRunning { get; private set; }

    /// <inheritdoc />
    public event EventHandler<AiAgentDiscoveredEventArgs>? AgentDiscovered;

    /// <inheritdoc />
    public event EventHandler<AiAgentUnavailableEventArgs>? AgentUnavailable;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger?.LogInformation("Initializing AI Agent Discovery Service");
        
        // Subscribe to registry events to forward discovery events
        _registry.AgentRegistered += OnAgentRegistered;
        _registry.AgentUnregistered += OnAgentUnregistered;
        
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger?.LogInformation("Starting AI Agent Discovery Service");
        IsRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning) return Task.CompletedTask;

        _logger?.LogInformation("Stopping AI Agent Discovery Service");
        
        // Unsubscribe from registry events
        _registry.AgentRegistered -= OnAgentRegistered;
        _registry.AgentUnregistered -= OnAgentUnregistered;
        
        IsRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IAiAgent>> DiscoverAllAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger?.LogTrace("Discovering all AI agents");
        var agents = await _registry.GetAllAsync(cancellationToken);
        _logger?.LogTrace("Discovered {Count} AI agents", agents.Count);
        
        return agents;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IAiAgent>> DiscoverByCapabilityAsync(string capability, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(capability);
        
        _logger?.LogTrace("Discovering AI agents with capability: {Capability}", capability);
        
        var allAgents = await _registry.GetAllAsync(cancellationToken);
        var matchingAgents = allAgents.Where(agent => agent.Capabilities.Contains(capability)).ToList().AsReadOnly();
        
        _logger?.LogTrace("Found {Count} AI agents with capability: {Capability}", matchingAgents.Count, capability);
        return matchingAgents;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IAiAgent>> DiscoverByStatusAsync(AiAgentStatus status, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger?.LogTrace("Discovering AI agents with status: {Status}", status);
        
        var allAgents = await _registry.GetAllAsync(cancellationToken);
        var matchingAgents = allAgents.Where(agent => agent.Status == status).ToList().AsReadOnly();
        
        _logger?.LogTrace("Found {Count} AI agents with status: {Status}", matchingAgents.Count, status);
        return matchingAgents;
    }

    /// <inheritdoc />
    public async Task<IAiAgent?> DiscoverByIdAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(agentId);
        
        _logger?.LogTrace("Discovering AI agent with ID: {AgentId}", agentId);
        
        var agent = await _registry.GetAsync(agentId, cancellationToken);
        _logger?.LogTrace(agent != null 
            ? "Found AI agent: {AgentId} ({Name})" 
            : "AI agent not found: {AgentId}", agentId, agent?.Name);
        
        return agent;
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
    }

    private void OnAgentRegistered(object? sender, AiAgentRegisteredEventArgs e)
    {
        _logger?.LogDebug("AI agent registered and discovered: {AgentId} ({Name})", e.Agent.AgentId, e.Agent.Name);
        AgentDiscovered?.Invoke(this, new AiAgentDiscoveredEventArgs(e.Agent));
    }

    private void OnAgentUnregistered(object? sender, AiAgentUnregisteredEventArgs e)
    {
        _logger?.LogDebug("AI agent unregistered and became unavailable: {AgentId}", e.AgentId);
        AgentUnavailable?.Invoke(this, new AiAgentUnavailableEventArgs(e.AgentId, e.Reason));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AiAgentDiscovery));
    }
}
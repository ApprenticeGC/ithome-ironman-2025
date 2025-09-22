using GameConsole.Core.Abstractions;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;

namespace GameConsole.Core.Registry;

/// <summary>
/// Concrete implementation of <see cref="IAIAgentRegistry"/> that provides AI agent registration and discovery services.
/// This service manages the lifecycle of AI agents and provides capability-based discovery.
/// </summary>
[Service("AI Agent Discovery Service", "1.0.0", "Service for discovering and managing AI agents", 
    Categories = new[] { "AI" }, Lifetime = ServiceLifetime.Singleton)]
public class AIAgentDiscoveryService : IAIAgentRegistry, IService
{
    private readonly ILogger<AIAgentDiscoveryService> _logger;
    private readonly ConcurrentDictionary<string, IAIAgent> _registeredAgents;
    private readonly object _lock = new object();
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDiscoveryService"/> class.
    /// </summary>
    /// <param name="logger">Logger for service operations.</param>
    public AIAgentDiscoveryService(ILogger<AIAgentDiscoveryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registeredAgents = new ConcurrentDictionary<string, IAIAgent>();
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI Agent Discovery Service");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogWarning("AI Agent Discovery Service is already running");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Starting AI Agent Discovery Service");
            _isRunning = true;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_isRunning)
            {
                _logger.LogWarning("AI Agent Discovery Service is not running");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Stopping AI Agent Discovery Service");
            _isRunning = false;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        _logger.LogInformation("Disposing AI Agent Discovery Service");
        _registeredAgents.Clear();
    }

    /// <inheritdoc />
    public Task RegisterAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        var agentId = agent.Metadata.Id;
        
        if (_registeredAgents.TryAdd(agentId, agent))
        {
            _logger.LogInformation("Registered AI agent: {AgentId} ({AgentName})", agentId, agent.Metadata.Name);
            return Task.CompletedTask;
        }

        _logger.LogWarning("AI agent {AgentId} is already registered", agentId);
        throw new InvalidOperationException($"AI agent with ID '{agentId}' is already registered");
    }

    /// <inheritdoc />
    public Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));

        if (_registeredAgents.TryRemove(agentId, out var agent))
        {
            _logger.LogInformation("Unregistered AI agent: {AgentId} ({AgentName})", agentId, agent.Metadata.Name);
            return Task.FromResult(true);
        }

        _logger.LogWarning("AI agent {AgentId} not found for unregistration", agentId);
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IAIAgent>> DiscoverAgentsAsync(CancellationToken cancellationToken = default)
    {
        var agents = _registeredAgents.Values.ToList();
        _logger.LogDebug("Discovered {AgentCount} AI agents", agents.Count);
        return Task.FromResult<IEnumerable<IAIAgent>>(agents);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> FindAgentsByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default)
        where TCapability : IAIAgentCapability
    {
        var capabilityType = typeof(TCapability);
        var matchingAgents = new List<IAIAgent>();

        foreach (var agent in _registeredAgents.Values)
        {
            try
            {
                var capabilities = await agent.GetAICapabilitiesAsync(cancellationToken);
                if (capabilities.Any(c => c.GetType() == capabilityType || capabilityType.IsAssignableFrom(c.GetType())))
                {
                    matchingAgents.Add(agent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting capabilities from agent {AgentId}", agent.Metadata.Id);
            }
        }

        _logger.LogDebug("Found {AgentCount} agents supporting capability {CapabilityType}", 
            matchingAgents.Count, capabilityType.Name);

        return matchingAgents;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> FindAgentsByCapabilityNameAsync(string capabilityName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(capabilityName))
            throw new ArgumentException("Capability name cannot be null or empty", nameof(capabilityName));

        var matchingAgents = new List<IAIAgent>();

        foreach (var agent in _registeredAgents.Values)
        {
            try
            {
                var capabilities = await agent.GetAICapabilitiesAsync(cancellationToken);
                if (capabilities.Any(c => string.Equals(c.CapabilityName, capabilityName, StringComparison.OrdinalIgnoreCase)))
                {
                    matchingAgents.Add(agent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting capabilities from agent {AgentId}", agent.Metadata.Id);
            }
        }

        _logger.LogDebug("Found {AgentCount} agents supporting capability '{CapabilityName}'", 
            matchingAgents.Count, capabilityName);

        return matchingAgents;
    }

    /// <inheritdoc />
    public Task<IAIAgent?> GetAgentByIdAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));

        _registeredAgents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IAIAgent>> GetRunningAgentsAsync(CancellationToken cancellationToken = default)
    {
        var runningAgents = _registeredAgents.Values.Where(a => a.IsRunning).ToList();
        _logger.LogDebug("Found {RunningCount} running AI agents out of {TotalCount} registered", 
            runningAgents.Count, _registeredAgents.Count);
        return Task.FromResult<IEnumerable<IAIAgent>>(runningAgents);
    }

    /// <inheritdoc />
    public async Task<int> RegisterFromAssemblyAsync(Assembly assembly, CancellationToken cancellationToken = default)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var registeredCount = 0;
        _logger.LogInformation("Scanning assembly {AssemblyName} for AI agents", assembly.GetName().Name);

        try
        {
            var aiAgentTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IAIAgent).IsAssignableFrom(t))
                .Where(t => t.GetCustomAttribute<AIAgentAttribute>() != null)
                .ToList();

            foreach (var agentType in aiAgentTypes)
            {
                try
                {
                    if (Activator.CreateInstance(agentType) is IAIAgent agent)
                    {
                        await RegisterAgentAsync(agent, cancellationToken);
                        registeredCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create and register AI agent of type {AgentType}", agentType.Name);
                }
            }

            _logger.LogInformation("Registered {RegisteredCount} AI agents from assembly {AssemblyName}", 
                registeredCount, assembly.GetName().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning assembly {AssemblyName} for AI agents", assembly.GetName().Name);
        }

        return registeredCount;
    }

    /// <inheritdoc />
    public Task<int> GetRegisteredAgentCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_registeredAgents.Count);
    }
}
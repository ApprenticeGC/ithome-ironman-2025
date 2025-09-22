using GameConsole.Core.Abstractions;
using System.Collections.Concurrent;
using System.Reflection;

namespace GameConsole.Core.Registry;

/// <summary>
/// Default implementation of <see cref="IAIAgentRegistry"/>.
/// Provides in-memory AI agent registration and discovery capabilities.
/// </summary>
[Service("AIAgentRegistry", "1.0.0", "Registry for AI agent discovery and management", Categories = new[] { "AI", "Registry" }, Lifetime = ServiceLifetime.Singleton)]
public class AIAgentRegistry : IAIAgentRegistry, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, IAIAgent> _agents = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed = false;

    /// <inheritdoc />
    public async Task RegisterAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_agents.ContainsKey(agent.AgentId))
            {
                throw new InvalidOperationException($"AI agent with ID '{agent.AgentId}' is already registered.");
            }

            _agents[agent.AgentId] = agent;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _agents.TryRemove(agentId, out _);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<IAIAgent>> GetAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_agents.Values.AsEnumerable());
    }

    /// <inheritdoc />
    public Task<IEnumerable<IAIAgent>> GetAgentsByTypeAsync(string agentType, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentType);
        
        var matchingAgents = _agents.Values
            .Where(agent => string.Equals(agent.AgentType, agentType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(agent => agent.Priority)
            .AsEnumerable();

        return Task.FromResult(matchingAgents);
    }

    /// <inheritdoc />
    public Task<IAIAgent?> GetAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        
        _agents.TryGetValue(agentId, out var agent);
        return Task.FromResult(agent);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> FindCapableAgentsAsync(object request, string? agentType = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var candidateAgents = agentType != null 
            ? await GetAgentsByTypeAsync(agentType, cancellationToken).ConfigureAwait(false)
            : await GetAllAgentsAsync(cancellationToken).ConfigureAwait(false);

        var capableAgents = new List<IAIAgent>();
        
        foreach (var agent in candidateAgents)
        {
            try
            {
                if (await agent.CanHandleAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    capableAgents.Add(agent);
                }
            }
            catch
            {
                // Agent failed capability check, skip it
                continue;
            }
        }

        return capableAgents.OrderByDescending(agent => agent.Priority);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> FindAgentsByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default)
    {
        var capableAgents = new List<IAIAgent>();
        
        foreach (var agent in _agents.Values)
        {
            try
            {
                if (await agent.HasCapabilityAsync<TCapability>(cancellationToken).ConfigureAwait(false))
                {
                    capableAgents.Add(agent);
                }
            }
            catch
            {
                // Agent failed capability check, skip it
                continue;
            }
        }

        return capableAgents.OrderByDescending(agent => agent.Priority);
    }

    /// <inheritdoc />
    public Task<bool> IsRegisteredAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        
        return Task.FromResult(_agents.ContainsKey(agentId));
    }

    /// <inheritdoc />
    public async Task ScanAndRegisterAsync(Assembly assembly, IServiceRegistry serviceRegistry, string[]? agentTypes = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(serviceRegistry);

        var aiAgentTypes = assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Where(type => typeof(IAIAgent).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttribute<AIAgentAttribute>() != null);

        foreach (var agentType in aiAgentTypes)
        {
            var attribute = agentType.GetCustomAttribute<AIAgentAttribute>()!;
            
            // Filter by agent types if specified
            if (agentTypes != null && agentTypes.Length > 0 && 
                !agentTypes.Any(filterType => string.Equals(attribute.AgentType, filterType, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Register the agent type in the service registry
            var serviceDescriptor = new ServiceDescriptor(typeof(IAIAgent), agentType, attribute.Lifetime);
            serviceRegistry.Register(serviceDescriptor);

            // If it's a singleton, create and register the instance immediately
            if (attribute.Lifetime == ServiceLifetime.Singleton)
            {
                var serviceProvider = serviceRegistry as ServiceProvider ?? 
                    throw new InvalidOperationException("Service registry must support service resolution for singleton AI agents.");
                
                var agentInstance = serviceProvider.GetService<IAIAgent>();
                if (agentInstance != null)
                {
                    await RegisterAgentAsync(agentInstance, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                _agents.Clear();
                _disposed = true;
            }
            finally
            {
                _semaphore.Release();
                _semaphore.Dispose();
            }
        }
    }
}
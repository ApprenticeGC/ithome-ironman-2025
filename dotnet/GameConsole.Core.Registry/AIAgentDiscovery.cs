using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Core.Registry;

/// <summary>
/// Default implementation of the AI agent discovery service.
/// Discovers AI agents through the registry and provides filtering capabilities.
/// </summary>
[Service("AIAgentDiscovery", "1.0.0", "Discovers and filters AI agents based on various criteria")]
public sealed class AIAgentDiscovery : IAIAgentDiscovery
{
    private readonly IAIAgentRegistry _registry;
    private readonly ILogger<AIAgentDiscovery>? _logger;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDiscovery"/> class.
    /// </summary>
    /// <param name="registry">The AI agent registry to discover agents from.</param>
    /// <param name="logger">Optional logger for discovery operations.</param>
    public AIAgentDiscovery(IAIAgentRegistry registry, ILogger<AIAgentDiscovery>? logger = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Initializing AI Agent Discovery");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting AI Agent Discovery");
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Stopping AI Agent Discovery");
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> DiscoverAllAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _logger?.LogDebug("Discovering all AI agents");
        
        var agents = await _registry.GetAllAsync(cancellationToken);
        _logger?.LogDebug("Discovered {Count} AI agents", agents.Count());
        
        return agents;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> DiscoverAsync(AIAgentDiscoveryCriteria criteria, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        ThrowIfDisposed();
        
        _logger?.LogDebug("Discovering AI agents with criteria");

        var allAgents = await _registry.GetAllAsync(cancellationToken);
        var filteredAgents = await FilterAgents(allAgents, criteria, cancellationToken);

        _logger?.LogDebug("Discovered {Count} AI agents matching criteria", filteredAgents.Count());
        
        return filteredAgents;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> DiscoverByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default) where TCapability : class
    {
        ThrowIfDisposed();
        _logger?.LogDebug("Discovering AI agents with capability {CapabilityType}", typeof(TCapability).Name);

        var allAgents = await _registry.GetAllAsync(cancellationToken);
        var agentsWithCapability = new List<IAIAgent>();

        foreach (var agent in allAgents)
        {
            try
            {
                if (await agent.HasCapabilityAsync<TCapability>(cancellationToken))
                {
                    agentsWithCapability.Add(agent);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error checking capability {CapabilityType} for agent '{AgentId}'", typeof(TCapability).Name, agent.Id);
            }
        }

        _logger?.LogDebug("Discovered {Count} AI agents with capability {CapabilityType}", agentsWithCapability.Count, typeof(TCapability).Name);
        
        return agentsWithCapability;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> DiscoverByCategoryAsync(IEnumerable<string> categories, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(categories);
        ThrowIfDisposed();

        var categoryList = categories.ToList();
        _logger?.LogDebug("Discovering AI agents in categories: {Categories}", string.Join(", ", categoryList));

        var allAgents = await _registry.GetAllAsync(cancellationToken);
        var agentsInCategories = allAgents.Where(agent => 
            agent.Categories.Any(agentCategory => categoryList.Contains(agentCategory, StringComparer.OrdinalIgnoreCase)));

        _logger?.LogDebug("Discovered {Count} AI agents in specified categories", agentsInCategories.Count());
        
        return agentsInCategories;
    }

    /// <inheritdoc />
    public async Task<IAIAgent?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ThrowIfDisposed();
        
        _logger?.LogDebug("Finding AI agent by ID: {AgentId}", id);

        var agent = await _registry.GetByIdAsync(id, cancellationToken);
        
        if (agent != null)
        {
            _logger?.LogDebug("Found AI agent '{AgentId}' ({AgentName})", agent.Id, agent.Name);
        }
        else
        {
            _logger?.LogDebug("AI agent '{AgentId}' not found", id);
        }

        return agent;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> FindByNameAsync(string name, bool exactMatch = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ThrowIfDisposed();
        
        _logger?.LogDebug("Finding AI agents by name: {Name} (exact: {ExactMatch})", name, exactMatch);

        var allAgents = await _registry.GetAllAsync(cancellationToken);
        var matchingAgents = exactMatch 
            ? allAgents.Where(agent => string.Equals(agent.Name, name, StringComparison.OrdinalIgnoreCase))
            : allAgents.Where(agent => agent.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

        _logger?.LogDebug("Found {Count} AI agents matching name criteria", matchingAgents.Count());
        
        return matchingAgents;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _logger?.LogInformation("Disposing AI Agent Discovery");
        _disposed = true;
        
        return ValueTask.CompletedTask;
    }

    private async Task<IEnumerable<IAIAgent>> FilterAgents(IEnumerable<IAIAgent> agents, AIAgentDiscoveryCriteria criteria, CancellationToken cancellationToken)
    {
        var filteredAgents = agents.AsEnumerable();

        // Filter by categories
        if (criteria.Categories.Count > 0)
        {
            filteredAgents = filteredAgents.Where(agent =>
                agent.Categories.Any(agentCategory => 
                    criteria.Categories.Contains(agentCategory, StringComparer.OrdinalIgnoreCase)));
        }

        // Filter by minimum status
        if (criteria.MinimumStatus.HasValue)
        {
            filteredAgents = filteredAgents.Where(agent => agent.Status >= criteria.MinimumStatus.Value);
        }

        // Filter by name
        if (!string.IsNullOrWhiteSpace(criteria.NameFilter))
        {
            filteredAgents = filteredAgents.Where(agent =>
                agent.Name.Contains(criteria.NameFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by description
        if (!string.IsNullOrWhiteSpace(criteria.DescriptionFilter))
        {
            filteredAgents = filteredAgents.Where(agent =>
                agent.Description.Contains(criteria.DescriptionFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by version
        if (!string.IsNullOrWhiteSpace(criteria.VersionFilter))
        {
            filteredAgents = filteredAgents.Where(agent =>
                string.Equals(agent.Version, criteria.VersionFilter, StringComparison.OrdinalIgnoreCase));
        }

        // Convert to list to avoid multiple enumeration for capability filtering
        var agentList = filteredAgents.ToList();

        // Filter by required capabilities
        if (criteria.RequiredCapabilities.Count > 0)
        {
            var agentsWithCapabilities = new List<IAIAgent>();
            
            foreach (var agent in agentList)
            {
                bool hasAllCapabilities = true;
                
                foreach (var requiredCapability in criteria.RequiredCapabilities)
                {
                    try
                    {
                        var capabilities = await agent.GetCapabilitiesAsync(cancellationToken);
                        if (!capabilities.Any(cap => requiredCapability.IsAssignableFrom(cap)))
                        {
                            hasAllCapabilities = false;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Error checking capabilities for agent '{AgentId}'", agent.Id);
                        hasAllCapabilities = false;
                        break;
                    }
                }

                if (hasAllCapabilities)
                {
                    agentsWithCapabilities.Add(agent);
                }
            }

            return agentsWithCapabilities;
        }

        return agentList;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AIAgentDiscovery));
        }
    }
}
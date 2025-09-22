using GameConsole.AI.Core;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services;

/// <summary>
/// Default implementation of the AI agent discovery service.
/// Provides capability-based agent discovery and selection functionality.
/// </summary>
public class AIAgentDiscoveryService : IAIAgentDiscovery
{
    private readonly ILogger<AIAgentDiscoveryService> _logger;
    private readonly IAIAgentRegistry _registry;
    private volatile bool _isRunning = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDiscoveryService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="registry">The AI agent registry.</param>
    public AIAgentDiscoveryService(ILogger<AIAgentDiscoveryService> logger, IAIAgentRegistry registry)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public IServiceMetadata Metadata => new ServiceMetadata
    {
        Name = "AI Agent Discovery",
        Version = "1.0.0",
        Description = "Discovery service for finding and selecting AI agents based on capabilities",
        Categories = new[] { "AI", "Discovery", "Capability" },
        Properties = new Dictionary<string, object>
        {
            ["SupportedDiscoveryTypes"] = new[] { "RequestType", "Capabilities", "SkillDomains" }
        }
    };

    /// <inheritdoc />
    public event EventHandler<AIAgentDiscoveredEventArgs>? AgentDiscovered;

    /// <inheritdoc />
    public event EventHandler<AIAgentUnavailableEventArgs>? AgentUnavailable;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI Agent Discovery Service");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI Agent Discovery Service");
        _isRunning = true;
        
        // Subscribe to registry events to track agent availability
        _registry.AgentRegistered += OnAgentRegistered;
        _registry.AgentUnregistered += OnAgentUnregistered;
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AI Agent Discovery Service");
        _isRunning = false;
        
        // Unsubscribe from registry events
        _registry.AgentRegistered -= OnAgentRegistered;
        _registry.AgentUnregistered -= OnAgentUnregistered;
        
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> DiscoverAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Discovering all available AI agents");
        
        var allAgents = await _registry.GetAllAgentsAsync(cancellationToken);
        
        _logger.LogDebug("Found {Count} AI agents", allAgents.Count());
        return allAgents;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> DiscoverAgentsByRequestTypeAsync(Type requestType, CancellationToken cancellationToken = default)
    {
        if (requestType == null) throw new ArgumentNullException(nameof(requestType));
        
        _logger.LogDebug("Discovering AI agents that can handle request type: {RequestType}", requestType.Name);
        
        var allAgents = await _registry.GetAllAgentsAsync(cancellationToken);
        var suitableAgents = new List<IAIAgent>();

        foreach (var agent in allAgents)
        {
            try
            {
                if (agent.Capabilities.SupportedRequestTypes.Contains(requestType) || 
                    await agent.CanHandleRequestAsync(requestType, cancellationToken))
                {
                    suitableAgents.Add(agent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking if agent {AgentId} can handle request type {RequestType}", 
                    agent.Metadata.Name, requestType.Name);
            }
        }

        _logger.LogDebug("Found {Count} agents that can handle request type {RequestType}", 
            suitableAgents.Count, requestType.Name);
        
        return suitableAgents;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> DiscoverAgentsByCapabilitiesAsync(IEnumerable<string> capabilities, CancellationToken cancellationToken = default)
    {
        if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));
        
        var capabilityList = capabilities.ToList();
        if (!capabilityList.Any())
        {
            return Enumerable.Empty<IAIAgent>();
        }

        _logger.LogDebug("Discovering AI agents with capabilities: {Capabilities}", string.Join(", ", capabilityList));
        
        var allAgents = await _registry.GetAllAgentsAsync(cancellationToken);
        var suitableAgents = allAgents.Where(agent =>
            capabilityList.All(capability => agent.Capabilities.HasCapability(capability))
        ).ToList();

        _logger.LogDebug("Found {Count} agents with required capabilities", suitableAgents.Count);
        return suitableAgents;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAIAgent>> DiscoverAgentsBySkillDomainsAsync(IEnumerable<string> skillDomains, CancellationToken cancellationToken = default)
    {
        if (skillDomains == null) throw new ArgumentNullException(nameof(skillDomains));
        
        var domainList = skillDomains.ToList();
        if (!domainList.Any())
        {
            return Enumerable.Empty<IAIAgent>();
        }

        _logger.LogDebug("Discovering AI agents with skill domains: {SkillDomains}", string.Join(", ", domainList));
        
        var allAgents = await _registry.GetAllAgentsAsync(cancellationToken);
        var suitableAgents = allAgents.Where(agent =>
            domainList.Any(domain => agent.Capabilities.SkillDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
        ).ToList();

        _logger.LogDebug("Found {Count} agents with required skill domains", suitableAgents.Count);
        return suitableAgents;
    }

    /// <inheritdoc />
    public async Task<IAIAgent?> FindBestAgentAsync(IAIAgentRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        
        _logger.LogDebug("Finding best AI agent for request: {RequestId}", request.RequestId);
        
        var rankedAgents = await RankAgentsAsync(request, cancellationToken);
        var bestAgent = rankedAgents.FirstOrDefault().Agent;
        
        if (bestAgent != null)
        {
            _logger.LogDebug("Selected best agent {AgentId} for request {RequestId}", 
                bestAgent.Metadata.Name, request.RequestId);
        }
        else
        {
            _logger.LogWarning("No suitable agent found for request {RequestId}", request.RequestId);
        }
        
        return bestAgent;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<(IAIAgent Agent, int Score)>> RankAgentsAsync(IAIAgentRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        
        _logger.LogDebug("Ranking AI agents for request: {RequestId}", request.RequestId);
        
        var availableAgents = await _registry.GetAvailableAgentsAsync(cancellationToken);
        var rankedAgents = new List<(IAIAgent Agent, int Score)>();

        foreach (var agent in availableAgents)
        {
            try
            {
                var priority = await agent.GetPriorityAsync(request, cancellationToken);
                var confidence = agent.Capabilities.GetConfidenceLevel(request.GetType());
                
                // Calculate composite score (priority * confidence with availability bonus)
                var availabilityBonus = agent.Status == AIAgentStatus.Ready ? 10 : 0;
                var score = (priority * confidence / 100) + availabilityBonus;
                
                if (score > 0)
                {
                    rankedAgents.Add((agent, score));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error ranking agent {AgentId} for request {RequestId}", 
                    agent.Metadata.Name, request.RequestId);
            }
        }

        var sortedAgents = rankedAgents
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Agent.Metadata.Name) // Consistent tie-breaking
            .ToList();

        _logger.LogDebug("Ranked {Count} agents for request {RequestId}", sortedAgents.Count, request.RequestId);
        return sortedAgents;
    }

    #region ICapabilityProvider Implementation

    /// <inheritdoc />
    public async Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[]
        {
            typeof(IAIAgentDiscovery),
            typeof(ICapabilityProvider)
        };
        
        return await Task.FromResult(capabilities);
    }

    /// <inheritdoc />
    public async Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        var requestedType = typeof(T);
        var capabilities = await GetCapabilitiesAsync(cancellationToken);
        return capabilities.Contains(requestedType);
    }

    /// <inheritdoc />
    public async Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        var requestedType = typeof(T);
        
        if (requestedType == typeof(IAIAgentDiscovery))
        {
            return this as T;
        }
        
        if (requestedType == typeof(ICapabilityProvider))
        {
            return this as T;
        }
        
        return await Task.FromResult<T?>(null);
    }

    #endregion

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing AI Agent Discovery Service");
        
        // Unsubscribe from registry events
        _registry.AgentRegistered -= OnAgentRegistered;
        _registry.AgentUnregistered -= OnAgentUnregistered;
        
        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }

    private void OnAgentRegistered(object? sender, AIAgentRegisteredEventArgs e)
    {
        _logger.LogDebug("New agent discovered: {AgentId}", e.Agent.Metadata.Name);
        AgentDiscovered?.Invoke(this, new AIAgentDiscoveredEventArgs(e.Agent));
    }

    private void OnAgentUnregistered(object? sender, AIAgentUnregisteredEventArgs e)
    {
        _logger.LogDebug("Agent became unavailable: {AgentId}", e.AgentId);
        
        // We don't have the agent instance here, but we can still notify about unavailability
        // Create a placeholder event for the unavailable agent
        var unavailableEvent = new AIAgentUnavailableEventArgs(null!, e.Reason);
        AgentUnavailable?.Invoke(this, unavailableEvent);
    }
}
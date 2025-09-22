using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Registry;

/// <summary>
/// Default implementation of <see cref="IAIAgentRegistry"/> that manages AI agent registration
/// and instantiation within the GameConsole dependency injection system.
/// </summary>
[Service("AIAgentRegistry", "1.0.0", "Manages AI agent registration and lifecycle", 
    Categories = new[] { "AI", "Registry", "Core" })]
public class AIAgentRegistryService : IAIAgentRegistry
{
    private readonly Dictionary<string, AIAgentDescriptor> _registeredAgents = new();
    private readonly Dictionary<Type, List<string>> _capabilityIndex = new();
    private readonly object _registryLock = new();
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentRegistryService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    public AIAgentRegistryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Registers an AI agent descriptor for later instantiation.
    /// </summary>
    /// <param name="descriptor">The AI agent descriptor to register.</param>
    /// <returns>True if the agent was registered successfully, false if already exists.</returns>
    public bool RegisterAgent(AIAgentDescriptor descriptor)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        lock (_registryLock)
        {
            if (_registeredAgents.ContainsKey(descriptor.Metadata.Id))
                return false;

            _registeredAgents[descriptor.Metadata.Id] = descriptor;
            
            // Update capability index
            foreach (var capability in descriptor.Metadata.ProvidedCapabilities)
            {
                if (!_capabilityIndex.TryGetValue(capability, out var agentIds))
                {
                    agentIds = new List<string>();
                    _capabilityIndex[capability] = agentIds;
                }
                agentIds.Add(descriptor.Metadata.Id);
            }

            return true;
        }
    }

    /// <summary>
    /// Registers multiple AI agent descriptors.
    /// </summary>
    /// <param name="descriptors">The AI agent descriptors to register.</param>
    /// <returns>The number of agents successfully registered.</returns>
    public int RegisterAgents(IEnumerable<AIAgentDescriptor> descriptors)
    {
        if (descriptors == null)
            throw new ArgumentNullException(nameof(descriptors));

        int registeredCount = 0;
        foreach (var descriptor in descriptors)
        {
            if (RegisterAgent(descriptor))
                registeredCount++;
        }

        return registeredCount;
    }

    /// <summary>
    /// Creates and configures an AI agent instance from a registered descriptor.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent to create.</param>
    /// <param name="configuration">The configuration for the AI agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The configured AI agent instance, or null if not found.</returns>
    public async Task<IAIAgent?> CreateAgentAsync(string agentId, IAIAgentConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(agentId))
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        AIAgentDescriptor? descriptor;
        lock (_registryLock)
        {
            if (!_registeredAgents.TryGetValue(agentId, out descriptor))
                return null;
        }

        if (!descriptor.CanCreate)
            throw new InvalidOperationException($"Cannot create AI agent {agentId} due to unsatisfied dependencies: {string.Join(", ", descriptor.UnsatisfiedDependencies)}");

        try
        {
            var agent = descriptor.CreateFactory(_serviceProvider)(_serviceProvider);
            
            // Configure the agent
            await agent.ConfigureAsync(configuration, cancellationToken);
            
            // Validate configuration
            var isValid = await agent.ValidateConfigurationAsync(cancellationToken);
            if (!isValid)
            {
                throw new InvalidOperationException($"AI agent {agentId} failed configuration validation");
            }

            return agent;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create AI agent {agentId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Creates and configures an AI agent instance that provides a specific capability.
    /// If multiple agents provide the capability, returns the first available one.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability required.</typeparam>
    /// <param name="configuration">The configuration for the AI agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The configured AI agent instance that provides the capability, or null if not found.</returns>
    public async Task<IAIAgent?> CreateAgentForCapabilityAsync<TCapability>(IAIAgentConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var capabilityType = typeof(TCapability);
        List<string>? candidateAgentIds;
        
        lock (_registryLock)
        {
            if (!_capabilityIndex.TryGetValue(capabilityType, out var agentIds))
                return null;

            candidateAgentIds = agentIds
                .Where(id => _registeredAgents.TryGetValue(id, out var descriptor) && descriptor.CanCreate)
                .ToList();
        }

        foreach (var agentId in candidateAgentIds)
        {
            // Try to create the first available agent that provides the capability
            var agent = await CreateAgentAsync(agentId, configuration, cancellationToken);
            if (agent != null)
                return agent;
        }

        return null;
    }

    /// <summary>
    /// Unregisters an AI agent descriptor.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent to unregister.</param>
    /// <returns>True if the agent was unregistered successfully.</returns>
    public bool UnregisterAgent(string agentId)
    {
        if (string.IsNullOrEmpty(agentId))
            return false;

        lock (_registryLock)
        {
            if (!_registeredAgents.TryGetValue(agentId, out var descriptor))
                return false;

            _registeredAgents.Remove(agentId);
            
            // Update capability index
            foreach (var capability in descriptor.Metadata.ProvidedCapabilities)
            {
                if (_capabilityIndex.TryGetValue(capability, out var agentIds))
                {
                    agentIds.Remove(agentId);
                    if (agentIds.Count == 0)
                        _capabilityIndex.Remove(capability);
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Checks if an AI agent is registered.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent.</param>
    /// <returns>True if the agent is registered.</returns>
    public bool IsAgentRegistered(string agentId)
    {
        if (string.IsNullOrEmpty(agentId))
            return false;

        lock (_registryLock)
        {
            return _registeredAgents.ContainsKey(agentId);
        }
    }

    /// <summary>
    /// Gets all registered AI agent descriptors.
    /// </summary>
    /// <returns>An enumerable of registered AI agent descriptors.</returns>
    public IEnumerable<AIAgentDescriptor> GetRegisteredAgents()
    {
        lock (_registryLock)
        {
            return _registeredAgents.Values.ToArray();
        }
    }

    /// <summary>
    /// Gets registered AI agent descriptors that provide a specific capability.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to search for.</typeparam>
    /// <returns>An enumerable of AI agent descriptors that provide the specified capability.</returns>
    public IEnumerable<AIAgentDescriptor> GetAgentsByCapability<TCapability>()
    {
        var capabilityType = typeof(TCapability);
        
        lock (_registryLock)
        {
            if (!_capabilityIndex.TryGetValue(capabilityType, out var agentIds))
                return Array.Empty<AIAgentDescriptor>();

            return agentIds
                .Where(id => _registeredAgents.ContainsKey(id))
                .Select(id => _registeredAgents[id])
                .ToArray();
        }
    }

    /// <summary>
    /// Gets a registered AI agent descriptor by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent.</param>
    /// <returns>The AI agent descriptor, or null if not found.</returns>
    public AIAgentDescriptor? GetAgentDescriptor(string agentId)
    {
        if (string.IsNullOrEmpty(agentId))
            return null;

        lock (_registryLock)
        {
            _registeredAgents.TryGetValue(agentId, out var descriptor);
            return descriptor;
        }
    }

    /// <summary>
    /// Clears all registered AI agent descriptors.
    /// </summary>
    public void ClearAgents()
    {
        lock (_registryLock)
        {
            _registeredAgents.Clear();
            _capabilityIndex.Clear();
        }
    }
}
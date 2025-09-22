using System.Collections.Concurrent;
using System.Reflection;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace GameConsole.AI.Core;

/// <summary>
/// Default implementation of the AI Agent Registry.
/// Provides discovery, registration, and management of AI agents in the GameConsole system.
/// </summary>
public class AIAgentRegistry : IAIAgentRegistry
{
    private readonly ConcurrentDictionary<Type, AIAgentTypeRegistration> _registeredTypes = new();
    private readonly ConcurrentDictionary<string, IAIAgent> _activeAgents = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIAgentRegistry> _logger;
    private readonly object _lock = new object();
    private volatile bool _isInitialized;
    private volatile bool _isStarted;
    private volatile bool _isDisposed;

    /// <inheritdoc />
    public bool IsRunning => _isStarted && !_isDisposed;

    /// <inheritdoc />
    public event EventHandler<AIAgentRegisteredEventArgs>? AgentRegistered;

    /// <inheritdoc />
    public event EventHandler<AIAgentRemovedEventArgs>? AgentRemoved;

    /// <summary>
    /// Initializes a new instance of the AIAgentRegistry class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public AIAgentRegistry(IServiceProvider serviceProvider, ILogger<AIAgentRegistry> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (_isInitialized)
            return Task.CompletedTask;

        lock (_lock)
        {
            if (_isInitialized)
                return Task.CompletedTask;

            _logger.LogInformation("Initializing AI Agent Registry");
            _isInitialized = true;
            _logger.LogInformation("AI Agent Registry initialized");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();

        if (_isStarted)
            return Task.CompletedTask;

        lock (_lock)
        {
            if (_isStarted)
                return Task.CompletedTask;

            _logger.LogInformation("Starting AI Agent Registry");
            _isStarted = true;
            _logger.LogInformation("AI Agent Registry started");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted || _isDisposed)
            return Task.CompletedTask;

        lock (_lock)
        {
            if (!_isStarted || _isDisposed)
                return Task.CompletedTask;

            _logger.LogInformation("Stopping AI Agent Registry");
            _isStarted = false;
            _logger.LogInformation("AI Agent Registry stopped");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        await StopAsync();

        lock (_lock)
        {
            if (_isDisposed)
                return;

            _logger.LogInformation("Disposing AI Agent Registry");

            // Dispose all active agents
            foreach (var agent in _activeAgents.Values)
            {
                if (agent is IAsyncDisposable asyncDisposable)
                    _ = asyncDisposable.DisposeAsync();
                else if (agent is IDisposable disposable)
                    disposable.Dispose();
            }

            _activeAgents.Clear();
            _registeredTypes.Clear();
            _isDisposed = true;
            _logger.LogInformation("AI Agent Registry disposed");
        }
    }

    /// <inheritdoc />
    public void RegisterAgentType<TAgent>(IAIAgentTypeMetadata? metadata = null) where TAgent : class, IAIAgent
    {
        ThrowIfDisposed();
        
        var agentType = typeof(TAgent);
        var effectiveMetadata = metadata ?? CreateMetadataFromAttribute(agentType);

        var registration = new AIAgentTypeRegistration(agentType, effectiveMetadata, null);
        
        if (_registeredTypes.TryAdd(agentType, registration))
        {
            _logger.LogInformation("Registered AI agent type: {AgentType} (Name: {Name})", 
                agentType.Name, effectiveMetadata.Name);
        }
        else
        {
            _logger.LogWarning("AI agent type {AgentType} is already registered", agentType.Name);
        }
    }

    /// <inheritdoc />
    public void RegisterAgentType<TAgent>(Func<IServiceProvider, TAgent> factory, IAIAgentTypeMetadata? metadata = null) 
        where TAgent : class, IAIAgent
    {
        ThrowIfDisposed();
        
        var agentType = typeof(TAgent);
        var effectiveMetadata = metadata ?? CreateMetadataFromAttribute(agentType);

        var registration = new AIAgentTypeRegistration(agentType, effectiveMetadata, 
            provider => factory(provider));
        
        if (_registeredTypes.TryAdd(agentType, registration))
        {
            _logger.LogInformation("Registered AI agent type with factory: {AgentType} (Name: {Name})", 
                agentType.Name, effectiveMetadata.Name);
        }
        else
        {
            _logger.LogWarning("AI agent type {AgentType} is already registered", agentType.Name);
        }
    }

    /// <inheritdoc />
    public void RegisterAgentInstance<TAgent>(TAgent instance) where TAgent : class, IAIAgent
    {
        ThrowIfDisposed();
        
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        var agentId = instance.Metadata.Name + "_" + Guid.NewGuid().ToString("N")[..8];
        
        if (_activeAgents.TryAdd(agentId, instance))
        {
            _logger.LogInformation("Registered AI agent instance: {AgentId} (Type: {AgentType})", 
                agentId, typeof(TAgent).Name);

            var metadata = CreateMetadataFromAttribute(typeof(TAgent));
            OnAgentRegistered(instance, metadata);
        }
        else
        {
            _logger.LogWarning("Failed to register AI agent instance: {AgentId}", agentId);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IAIAgentTypeInfo>> DiscoverAgentTypesAsync(
        IAIAgentCapabilityRequirements? requiredCapabilities = null, 
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var results = new List<IAIAgentTypeInfo>();

        foreach (var registration in _registeredTypes.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var capabilityPreview = CreateCapabilityPreview(registration.Metadata);
            
            if (requiredCapabilities == null || MatchesRequirements(capabilityPreview, requiredCapabilities))
            {
                results.Add(new AIAgentTypeInfo
                {
                    AgentType = registration.AgentType,
                    Metadata = registration.Metadata,
                    CapabilityPreview = capabilityPreview
                });
            }
        }

        _logger.LogDebug("Discovered {Count} AI agent types", results.Count);
        return Task.FromResult<IReadOnlyList<IAIAgentTypeInfo>>(results);
    }

    /// <inheritdoc />
    public async Task<TAgent> CreateAgentAsync<TAgent>(CancellationToken cancellationToken = default) 
        where TAgent : class, IAIAgent
    {
        ThrowIfDisposed();

        var agentType = typeof(TAgent);
        
        if (!_registeredTypes.TryGetValue(agentType, out var registration))
            throw new InvalidOperationException($"AI agent type {agentType.Name} is not registered");

        var agent = await CreateAgentInstanceAsync<TAgent>(registration, cancellationToken);
        
        var agentId = agent.Metadata.Name + "_" + Guid.NewGuid().ToString("N")[..8];
        
        if (_activeAgents.TryAdd(agentId, agent))
        {
            _logger.LogInformation("Created AI agent instance: {AgentId} (Type: {AgentType})", 
                agentId, agentType.Name);
            OnAgentRegistered(agent, registration.Metadata);
        }

        return agent;
    }

    /// <inheritdoc />
    public async Task<IAIAgent> CreateAgentAsync(string agentTypeName, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var registration = _registeredTypes.Values
            .FirstOrDefault(r => r.Metadata.Name == agentTypeName);

        if (registration == null)
            throw new InvalidOperationException($"AI agent type '{agentTypeName}' is not registered");

        var agent = await CreateAgentInstanceAsync<IAIAgent>(registration, cancellationToken);
        
        var agentId = agent.Metadata.Name + "_" + Guid.NewGuid().ToString("N")[..8];
        
        if (_activeAgents.TryAdd(agentId, agent))
        {
            _logger.LogInformation("Created AI agent instance: {AgentId} (Name: {AgentName})", 
                agentId, agentTypeName);
            OnAgentRegistered(agent, registration.Metadata);
        }

        return agent;
    }

    /// <inheritdoc />
    public IReadOnlyList<IAIAgent> GetActiveAgents()
    {
        ThrowIfDisposed();
        return _activeAgents.Values.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<IAIAgent> GetActiveAgents(IAIAgentCapabilityRequirements requiredCapabilities)
    {
        ThrowIfDisposed();

        return _activeAgents.Values
            .Where(agent => MatchesRequirements(agent.Capabilities, requiredCapabilities))
            .ToList();
    }

    /// <inheritdoc />
    public IAIAgent? GetAgent(string agentId)
    {
        ThrowIfDisposed();
        _activeAgents.TryGetValue(agentId, out var agent);
        return agent;
    }

    /// <inheritdoc />
    public async Task RemoveAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (agent == null)
            throw new ArgumentNullException(nameof(agent));

        var agentId = _activeAgents.FirstOrDefault(kvp => ReferenceEquals(kvp.Value, agent)).Key;
        
        if (agentId != null)
        {
            await RemoveAgentAsync(agentId, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_activeAgents.TryRemove(agentId, out var agent))
        {
            _logger.LogInformation("Removing AI agent: {AgentId}", agentId);

            try
            {
                await agent.StopAsync(cancellationToken);
                
                if (agent is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync();
                else if (agent is IDisposable disposable)
                    disposable.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing AI agent: {AgentId}", agentId);
            }

            OnAgentRemoved(agentId, agent.Metadata.Name);
        }
    }

    /// <inheritdoc />
    public Task DiscoverAndRegisterAsync(IEnumerable<Assembly> assemblies, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var discoveredCount = 0;

        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var agentTypes = assembly.GetTypes()
                    .Where(type => type.IsClass && !type.IsAbstract && 
                                  typeof(IAIAgent).IsAssignableFrom(type) &&
                                  type.HasAIAgentAttribute());

                foreach (var agentType in agentTypes)
                {
                    var attribute = agentType.GetAIAgentAttribute();
                    if (attribute != null)
                    {
                        var metadata = attribute.ToMetadata();
                        var registration = new AIAgentTypeRegistration(agentType, metadata, null);
                        
                        if (_registeredTypes.TryAdd(agentType, registration))
                        {
                            discoveredCount++;
                            _logger.LogDebug("Auto-registered AI agent type: {AgentType} from assembly {Assembly}", 
                                agentType.Name, assembly.GetName().Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan assembly {Assembly} for AI agents", assembly.GetName().Name);
            }
        }

        _logger.LogInformation("Auto-discovered and registered {Count} AI agent types", discoveredCount);
        return Task.CompletedTask;
    }

    private Task<T> CreateAgentInstanceAsync<T>(AIAgentTypeRegistration registration, CancellationToken cancellationToken) 
        where T : class, IAIAgent
    {
        if (registration.Factory != null)
        {
            var instance = registration.Factory(_serviceProvider) as T;
            if (instance == null)
                throw new InvalidOperationException($"Factory did not produce expected type {typeof(T).Name}");
            return Task.FromResult(instance);
        }
        else
        {
            // Use service provider to create instance with dependency injection
            var instance = ActivatorUtilities.CreateInstance(_serviceProvider, registration.AgentType) as T;
            if (instance == null)
                throw new InvalidOperationException($"Could not create instance of {registration.AgentType.Name}");
            return Task.FromResult(instance);
        }
    }

    private IAIAgentTypeMetadata CreateMetadataFromAttribute(Type agentType)
    {
        var attribute = agentType.GetAIAgentAttribute();
        
        if (attribute != null)
            return attribute.ToMetadata();

        // Create default metadata if no attribute is present
        return new AIAgentTypeMetadata
        {
            Name = agentType.Name,
            Version = "1.0.0",
            Description = $"AI Agent: {agentType.Name}",
            Categories = Array.Empty<string>(),
            Properties = new Dictionary<string, object>()
        };
    }

    private IAIAgentCapabilities CreateCapabilityPreview(IAIAgentTypeMetadata metadata)
    {
        // Extract capability information from metadata properties
        var decisionTypes = metadata.Properties.GetValueOrDefault(nameof(AIAgentAttribute.DecisionTypes)) as string[] ?? Array.Empty<string>();
        var supportsLearning = metadata.Properties.GetValueOrDefault(nameof(AIAgentAttribute.SupportsLearning)) as bool? ?? false;
        var supportsAutonomousMode = metadata.Properties.GetValueOrDefault(nameof(AIAgentAttribute.SupportsAutonomousMode)) as bool? ?? false;
        var priority = metadata.Properties.GetValueOrDefault(nameof(AIAgentAttribute.Priority)) as int? ?? 50;
        var maxConcurrentInputs = metadata.Properties.GetValueOrDefault(nameof(AIAgentAttribute.MaxConcurrentInputs)) as int? ?? 1;

        return new AIAgentCapabilityPreview
        {
            DecisionTypes = decisionTypes,
            SupportsLearning = supportsLearning,
            SupportsAutonomousMode = supportsAutonomousMode,
            Priority = priority,
            MaxConcurrentInputs = maxConcurrentInputs,
            Metadata = new Dictionary<string, object>(metadata.Properties)
        };
    }

    private static bool MatchesRequirements(IAIAgentCapabilities capabilities, IAIAgentCapabilityRequirements requirements)
    {
        // Check required decision types
        if (requirements.RequiredDecisionTypes.Any() && 
            !requirements.RequiredDecisionTypes.All(required => capabilities.DecisionTypes.Contains(required)))
            return false;

        // Check learning requirement
        if (requirements.RequiresLearning && !capabilities.SupportsLearning)
            return false;

        // Check autonomous mode requirement
        if (requirements.RequiresAutonomousMode && !capabilities.SupportsAutonomousMode)
            return false;

        // Check minimum priority
        if (capabilities.Priority < requirements.MinimumPriority)
            return false;

        return true;
    }

    private void OnAgentRegistered(IAIAgent agent, IAIAgentTypeMetadata metadata)
    {
        AgentRegistered?.Invoke(this, new AIAgentRegisteredEventArgs(agent, metadata));
    }

    private void OnAgentRemoved(string agentId, string agentTypeName)
    {
        AgentRemoved?.Invoke(this, new AIAgentRemovedEventArgs(agentId, agentTypeName));
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(AIAgentRegistry));
    }

    private void ThrowIfNotInitialized()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("AI Agent Registry must be initialized before use");
    }

    /// <summary>
    /// Internal class for tracking registered agent types.
    /// </summary>
    private class AIAgentTypeRegistration
    {
        public Type AgentType { get; }
        public IAIAgentTypeMetadata Metadata { get; }
        public Func<IServiceProvider, IAIAgent>? Factory { get; }

        public AIAgentTypeRegistration(Type agentType, IAIAgentTypeMetadata metadata, Func<IServiceProvider, IAIAgent>? factory)
        {
            AgentType = agentType;
            Metadata = metadata;
            Factory = factory;
        }
    }

    /// <summary>
    /// Implementation of IAIAgentTypeInfo.
    /// </summary>
    private class AIAgentTypeInfo : IAIAgentTypeInfo
    {
        public Type AgentType { get; init; } = null!;
        public IAIAgentTypeMetadata Metadata { get; init; } = null!;
        public IAIAgentCapabilities CapabilityPreview { get; init; } = null!;
    }

    /// <summary>
    /// Implementation of IAIAgentCapabilities for capability previews.
    /// </summary>
    private class AIAgentCapabilityPreview : IAIAgentCapabilities
    {
        public IReadOnlyList<string> DecisionTypes { get; init; } = Array.Empty<string>();
        public bool SupportsLearning { get; init; }
        public bool SupportsAutonomousMode { get; init; }
        public int MaxConcurrentInputs { get; init; }
        public int Priority { get; init; }
        public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
    }
}
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Core;

/// <summary>
/// Default implementation of IAIAgentDiscovery.
/// Scans assemblies for types that implement IAIAgent and have the AIAgentAttribute.
/// </summary>
public class AIAgentDiscovery : IAIAgentDiscovery
{
    private readonly ILogger<AIAgentDiscovery> _logger;
    private bool _isInitialized = false;
    private bool _isRunning = false;
    private bool _isDisposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentDiscovery"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public AIAgentDiscovery(ILogger<AIAgentDiscovery> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        _logger.LogInformation("Initializing AI Agent Discovery Service");
        
        // No specific initialization needed for discovery service
        _isInitialized = true;
        
        _logger.LogInformation("AI Agent Discovery Service initialized successfully");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Service must be initialized before starting");

        _logger.LogInformation("Starting AI Agent Discovery Service");
        
        // Discovery service is stateless and always ready
        _isRunning = true;
        
        _logger.LogInformation("AI Agent Discovery Service started successfully");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AI Agent Discovery Service");
        
        // Discovery service has no background operations to stop
        _isRunning = false;
        
        _logger.LogInformation("AI Agent Discovery Service stopped successfully");
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<DiscoveredAIAgent> DiscoverAgents(Assembly assembly)
    {
        if (assembly == null) throw new ArgumentNullException(nameof(assembly));

        _logger.LogDebug("Discovering AI agents in assembly: {AssemblyName}", assembly.FullName);

        var discoveredAgents = new List<DiscoveredAIAgent>();
        
        try
        {
            var types = assembly.GetTypes();
            
            foreach (var type in types)
            {
                if (TryDiscoverAgent(type, out var discoveredAgent))
                {
                    discoveredAgents.Add(discoveredAgent);
                    _logger.LogDebug("Discovered AI agent: {AgentId} ({AgentType})", discoveredAgent.Id, type.Name);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            _logger.LogWarning("Failed to load some types from assembly {AssemblyName}: {Error}", 
                assembly.FullName, string.Join(", ", ex.LoaderExceptions.Where(e => e != null).Select(e => e!.Message)));
            
            // Process the types that did load successfully
            foreach (var type in ex.Types.Where(t => t != null))
            {
                if (TryDiscoverAgent(type!, out var discoveredAgent))
                {
                    discoveredAgents.Add(discoveredAgent);
                    _logger.LogDebug("Discovered AI agent: {AgentId} ({AgentType})", discoveredAgent.Id, type!.Name);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering AI agents in assembly {AssemblyName}", assembly.FullName);
        }

        _logger.LogInformation("Discovered {Count} AI agents in assembly {AssemblyName}", discoveredAgents.Count, assembly.FullName);
        return discoveredAgents;
    }

    /// <inheritdoc />
    public IEnumerable<DiscoveredAIAgent> DiscoverAgents(IEnumerable<Assembly> assemblies)
    {
        if (assemblies == null) throw new ArgumentNullException(nameof(assemblies));

        var allDiscoveredAgents = new List<DiscoveredAIAgent>();
        
        foreach (var assembly in assemblies)
        {
            allDiscoveredAgents.AddRange(DiscoverAgents(assembly));
        }

        // Remove duplicates based on agent ID
        var uniqueAgents = allDiscoveredAgents
            .GroupBy(a => a.Id)
            .Select(g => g.First())
            .ToList();

        if (uniqueAgents.Count != allDiscoveredAgents.Count)
        {
            _logger.LogWarning("Found duplicate AI agent IDs, keeping first occurrence of each");
        }

        return uniqueAgents;
    }

    /// <inheritdoc />
    public IEnumerable<DiscoveredAIAgent> DiscoverAllAgents()
    {
        _logger.LogDebug("Discovering AI agents in all loaded assemblies");
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic) // Skip dynamic assemblies
            .ToList();

        return DiscoverAgents(assemblies);
    }

    /// <inheritdoc />
    public bool ValidateAgentType(Type type)
    {
        return ValidateAgentTypeDetailed(type).IsValid;
    }

    /// <inheritdoc />
    public AIAgentValidationResult ValidateAgentTypeDetailed(Type type)
    {
        if (type == null) 
            return AIAgentValidationResult.Failure("Type cannot be null");

        var errors = new List<string>();

        // Check if type implements IAIAgent
        if (!typeof(IAIAgent).IsAssignableFrom(type))
        {
            errors.Add($"Type {type.Name} does not implement IAIAgent interface");
        }

        // Check if type has AIAgentAttribute
        var attribute = type.GetCustomAttribute<AIAgentAttribute>();
        if (attribute == null)
        {
            errors.Add($"Type {type.Name} does not have AIAgentAttribute");
        }

        // Check if type is concrete (not abstract or interface)
        if (type.IsAbstract || type.IsInterface)
        {
            errors.Add($"Type {type.Name} must be a concrete class (not abstract or interface)");
        }

        // Check if type has a parameterless constructor or constructor injectable by DI
        var constructors = type.GetConstructors();
        if (!constructors.Any(c => c.GetParameters().Length == 0) && 
            !constructors.Any(c => c.GetParameters().All(p => p.ParameterType.IsInterface)))
        {
            errors.Add($"Type {type.Name} must have a parameterless constructor or a constructor with only interface parameters for dependency injection");
        }

        return errors.Count == 0 ? AIAgentValidationResult.Success() : AIAgentValidationResult.Failure(errors.ToArray());
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;

        _logger.LogInformation("Disposing AI Agent Discovery Service");
        
        // No resources to dispose for this stateless service
        
        _isDisposed = true;
        
        _logger.LogInformation("AI Agent Discovery Service disposed");
        await ValueTask.CompletedTask;
    }

    /// <summary>
    /// Attempts to discover an AI agent from the specified type.
    /// </summary>
    /// <param name="type">The type to examine.</param>
    /// <param name="discoveredAgent">The discovered agent, if successful.</param>
    /// <returns>True if an AI agent was discovered, false otherwise.</returns>
    private bool TryDiscoverAgent(Type type, out DiscoveredAIAgent discoveredAgent)
    {
        discoveredAgent = null!;

        // Quick check for IAIAgent interface before doing expensive validation
        if (!typeof(IAIAgent).IsAssignableFrom(type))
            return false;

        var attribute = type.GetCustomAttribute<AIAgentAttribute>();
        if (attribute == null)
            return false;

        var validationResult = ValidateAgentTypeDetailed(type);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid AI agent type {TypeName}: {Errors}", 
                type.Name, string.Join(", ", validationResult.Errors));
            return false;
        }

        discoveredAgent = new DiscoveredAIAgent(type, attribute);
        return true;
    }
}
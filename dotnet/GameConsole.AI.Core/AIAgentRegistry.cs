using System.Collections.Concurrent;
using System.Reflection;

namespace GameConsole.AI.Core;

/// <summary>
/// Default implementation of AI agent registry for discovering and managing AI agents.
/// </summary>
public class AIAgentRegistry : IAIAgentRegistry
{
    private readonly ConcurrentDictionary<string, IAIAgentDescriptor> _registeredAgents = new();
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentRegistry"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public AIAgentRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IAIAgentDescriptor>> DiscoverAgentsAsync(Assembly assembly, CancellationToken cancellationToken = default)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        var agents = new List<IAIAgentDescriptor>();

        await Task.Run(() =>
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IAIAgent).IsAssignableFrom(t))
                .ToList();

            foreach (var type in types)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var attribute = type.GetCustomAttribute<AIAgentAttribute>();
                if (attribute != null)
                {
                    var metadata = CreateMetadataFromAttribute(attribute);
                    var descriptor = new AIAgentDescriptor(metadata, type, assembly, attribute.AllowMultipleInstances);

                    if (descriptor.IsValid())
                    {
                        agents.Add(descriptor);
                    }
                }
            }
        }, cancellationToken);

        return agents;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IAIAgentDescriptor>> DiscoverAllAgentsAsync(CancellationToken cancellationToken = default)
    {
        var allAgents = new List<IAIAgentDescriptor>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var agents = await DiscoverAgentsAsync(assembly, cancellationToken);
                allAgents.AddRange(agents);
            }
            catch (Exception)
            {
                // Skip assemblies that can't be scanned (e.g., system assemblies)
                continue;
            }
        }

        return allAgents;
    }

    /// <inheritdoc />
    public Task RegisterAgentAsync(IAIAgentDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (!descriptor.IsValid())
        {
            throw new ArgumentException($"Invalid agent descriptor: {descriptor.Metadata.Id}", nameof(descriptor));
        }

        _registeredAgents.TryAdd(descriptor.Metadata.Id, descriptor);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        }

        var removed = _registeredAgents.TryRemove(agentId, out _);
        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public IReadOnlyList<IAIAgentDescriptor> GetRegisteredAgents()
    {
        return _registeredAgents.Values.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<IAIAgentDescriptor> FindAgentsByCapabilities(params string[] capabilities)
    {
        if (capabilities == null || capabilities.Length == 0)
        {
            return Array.Empty<IAIAgentDescriptor>();
        }

        return _registeredAgents.Values
            .Where(descriptor => capabilities.All(capability => 
                descriptor.Metadata.Capabilities.Contains(capability)))
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<IAIAgentDescriptor> FindAgentsByType(string agentType)
    {
        if (string.IsNullOrEmpty(agentType))
        {
            return Array.Empty<IAIAgentDescriptor>();
        }

        return _registeredAgents.Values
            .Where(descriptor => string.Equals(descriptor.Metadata.AgentType, agentType, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <inheritdoc />
    public IAIAgentDescriptor? GetAgent(string agentId)
    {
        if (string.IsNullOrEmpty(agentId))
        {
            return null;
        }

        _registeredAgents.TryGetValue(agentId, out var descriptor);
        return descriptor;
    }

    /// <inheritdoc />
    public bool IsAgentRegistered(string agentId)
    {
        if (string.IsNullOrEmpty(agentId))
        {
            return false;
        }

        return _registeredAgents.ContainsKey(agentId);
    }

    /// <inheritdoc />
    public async Task<IAIAgent?> CreateAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        var descriptor = GetAgent(agentId);
        if (descriptor == null)
        {
            return null;
        }

        return await Task.Run(() => descriptor.CreateInstance(_serviceProvider), cancellationToken);
    }

    private static AIAgentMetadata CreateMetadataFromAttribute(AIAgentAttribute attribute)
    {
        var resourceRequirements = new AIAgentResourceRequirements(
            attribute.MinMemoryMB,
            attribute.RecommendedMemoryMB,
            attribute.RequiresGPU,
            attribute.RequiresNetwork,
            attribute.MaxConcurrentInstances);

        return new AIAgentMetadata(
            attribute.Id,
            attribute.Name,
            Version.Parse(attribute.Version),
            attribute.Description,
            attribute.Author,
            attribute.AgentType,
            attribute.Capabilities,
            resourceRequirements,
            attribute.SupportedProtocols,
            attribute.SupportsLearning,
            attribute.Dependencies);
    }
}
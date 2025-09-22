using GameConsole.Core.Abstractions;
using System.Reflection;

namespace GameConsole.Core.Registry;

/// <summary>
/// Default implementation of <see cref="IAIAgentDiscovery"/> that discovers AI agents
/// through assembly scanning and attribute-based metadata.
/// </summary>
[Service("AIAgentDiscovery", "1.0.0", "Discovers AI agents within the GameConsole system", 
    Categories = new[] { "AI", "Discovery" })]
public class AIAgentDiscoveryService : IAIAgentDiscovery
{
    private readonly Dictionary<string, AIAgentDescriptor> _cachedAgents = new();
    private readonly object _cacheLock = new();

    /// <summary>
    /// Discovers all available AI agents in loaded assemblies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of discovered AI agent descriptors.</returns>
    public async Task<IEnumerable<AIAgentDescriptor>> DiscoverAgentsAsync(CancellationToken cancellationToken = default)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .ToArray();

        return await ScanAssembliesAsync(assemblies, cancellationToken);
    }

    /// <summary>
    /// Discovers AI agents that provide specific capabilities.
    /// </summary>
    /// <typeparam name="TCapability">The type of capability to search for.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of AI agent descriptors that provide the specified capability.</returns>
    public async Task<IEnumerable<AIAgentDescriptor>> DiscoverAgentsByCapabilityAsync<TCapability>(CancellationToken cancellationToken = default)
    {
        var allAgents = await DiscoverAgentsAsync(cancellationToken);
        return allAgents.Where(agent => agent.Metadata.ProvidedCapabilities.Contains(typeof(TCapability)));
    }

    /// <summary>
    /// Discovers AI agents that match specific tags or categories.
    /// </summary>
    /// <param name="tags">The tags to search for.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of AI agent descriptors that match the specified tags.</returns>
    public async Task<IEnumerable<AIAgentDescriptor>> DiscoverAgentsByTagsAsync(string[] tags, CancellationToken cancellationToken = default)
    {
        if (tags == null || tags.Length == 0)
            return Array.Empty<AIAgentDescriptor>();

        var allAgents = await DiscoverAgentsAsync(cancellationToken);
        return allAgents.Where(agent =>
        {
            if (agent.Metadata.Properties.TryGetValue("Tags", out var tagsValue) 
                && tagsValue is string[] agentTags)
            {
                return tags.Any(tag => agentTags.Contains(tag, StringComparer.OrdinalIgnoreCase));
            }
            return false;
        });
    }

    /// <summary>
    /// Gets an AI agent descriptor by its unique identifier.
    /// </summary>
    /// <param name="agentId">The unique identifier of the AI agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The AI agent descriptor, or null if not found.</returns>
    public async Task<AIAgentDescriptor?> GetAgentDescriptorAsync(string agentId, CancellationToken cancellationToken = default)
    {
        lock (_cacheLock)
        {
            if (_cachedAgents.TryGetValue(agentId, out var cachedAgent))
                return cachedAgent;
        }

        var allAgents = await DiscoverAgentsAsync(cancellationToken);
        return allAgents.FirstOrDefault(agent => agent.Metadata.Id == agentId);
    }

    /// <summary>
    /// Scans assemblies for AI agents decorated with <see cref="AIAgentAttribute"/>.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A collection of discovered AI agent descriptors.</returns>
    public async Task<IEnumerable<AIAgentDescriptor>> ScanAssembliesAsync(Assembly[] assemblies, CancellationToken cancellationToken = default)
    {
        var agents = new List<AIAgentDescriptor>();

        await Task.Run(() =>
        {
            foreach (var assembly in assemblies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(IAIAgent).IsAssignableFrom(t));

                    foreach (var type in types)
                    {
                        var descriptor = AIAgentDescriptor.FromAttributedType(type);
                        if (descriptor != null)
                        {
                            agents.Add(descriptor);

                            // Cache the descriptor
                            lock (_cacheLock)
                            {
                                _cachedAgents[descriptor.Metadata.Id] = descriptor;
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Handle assemblies that cannot be fully loaded
                    // Log the exception but continue with other assemblies
                    Console.WriteLine($"Warning: Failed to load types from assembly {assembly.FullName}: {ex.Message}");
                }
            }
        }, cancellationToken);

        return agents;
    }

    /// <summary>
    /// Validates the dependencies of discovered AI agents and returns them in dependency order.
    /// </summary>
    /// <param name="agents">The AI agent descriptors to validate and order.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>AI agent descriptors ordered by dependencies, or throws if circular dependencies are found.</returns>
    public async Task<IEnumerable<AIAgentDescriptor>> ValidateAndOrderDependenciesAsync(IEnumerable<AIAgentDescriptor> agents, CancellationToken cancellationToken = default)
    {
        var agentList = agents.ToList();
        var agentDict = agentList.ToDictionary(a => a.Metadata.Id, a => a);
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();
        var ordered = new List<AIAgentDescriptor>();

        await Task.Run(() =>
        {
            foreach (var agent in agentList)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                if (!visited.Contains(agent.Metadata.Id))
                {
                    Visit(agent, agentDict, visited, visiting, ordered);
                }
            }
        }, cancellationToken);

        // Update CanCreate flags based on dependency satisfaction
        foreach (var agent in ordered)
        {
            var unsatisfiedDeps = agent.Metadata.Dependencies
                .Where(depId => !agentDict.ContainsKey(depId))
                .ToList();

            agent.UnsatisfiedDependencies = unsatisfiedDeps.AsReadOnly();
            agent.CanCreate = unsatisfiedDeps.Count == 0;
        }

        return ordered;
    }

    private static void Visit(
        AIAgentDescriptor agent,
        Dictionary<string, AIAgentDescriptor> agentDict,
        HashSet<string> visited,
        HashSet<string> visiting,
        List<AIAgentDescriptor> ordered)
    {
        if (visiting.Contains(agent.Metadata.Id))
        {
            throw new InvalidOperationException($"Circular dependency detected involving AI agent: {agent.Metadata.Id}");
        }

        if (visited.Contains(agent.Metadata.Id))
            return;

        visiting.Add(agent.Metadata.Id);

        foreach (var dependencyId in agent.Metadata.Dependencies)
        {
            if (agentDict.TryGetValue(dependencyId, out var dependency))
            {
                Visit(dependency, agentDict, visited, visiting, ordered);
            }
        }

        visiting.Remove(agent.Metadata.Id);
        visited.Add(agent.Metadata.Id);
        ordered.Add(agent);
    }
}
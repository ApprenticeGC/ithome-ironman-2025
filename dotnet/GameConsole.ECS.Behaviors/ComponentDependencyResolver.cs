using Microsoft.Extensions.Logging;

namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Basic implementation of component dependency resolver.
/// Provides dependency analysis and validation for ECS components.
/// </summary>
public class ComponentDependencyResolver : IComponentDependencyResolver
{
    private readonly ILogger<ComponentDependencyResolver> _logger;
    private readonly Dictionary<Type, ComponentDependencyMetadata> _componentMetadata = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentDependencyResolver"/> class.
    /// </summary>
    /// <param name="logger">Logger for the resolver.</param>
    public ComponentDependencyResolver(ILogger<ComponentDependencyResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ComponentDependencyGraph> ResolveDependenciesAsync(IEnumerable<Type> components, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Resolving dependencies for {ComponentCount} components", components.Count());

        var componentList = components.ToList();
        var nodes = new List<ComponentDependencyNode>();
        var edges = new List<ComponentDependencyEdge>();

        // Create nodes for all components
        foreach (var componentType in componentList)
        {
            var metadata = await GetComponentMetadataAsync(componentType);
            nodes.Add(new ComponentDependencyNode
            {
                ComponentType = componentType,
                Metadata = metadata
            });
        }

        // Create edges based on dependencies
        foreach (var node in nodes)
        {
            var metadata = node.Metadata;

            // Required dependencies
            foreach (var dependency in metadata.RequiredDependencies)
            {
                edges.Add(new ComponentDependencyEdge
                {
                    Source = node.ComponentType,
                    Target = dependency,
                    DependencyType = DependencyType.Required
                });
            }

            // Optional dependencies
            foreach (var dependency in metadata.OptionalDependencies)
            {
                edges.Add(new ComponentDependencyEdge
                {
                    Source = node.ComponentType,
                    Target = dependency,
                    DependencyType = DependencyType.Optional
                });
            }

            // Conflicts
            foreach (var conflict in metadata.Conflicts)
            {
                edges.Add(new ComponentDependencyEdge
                {
                    Source = node.ComponentType,
                    Target = conflict,
                    DependencyType = DependencyType.Conflict
                });
            }

            // Provides relationships
            foreach (var provides in metadata.Provides)
            {
                edges.Add(new ComponentDependencyEdge
                {
                    Source = provides,
                    Target = node.ComponentType,
                    DependencyType = DependencyType.Provides
                });
            }
        }

        // Detect cycles
        var cycles = DetectCycles(componentList, edges);
        var initializationOrder = TopologicalSort(componentList, edges);

        var graph = new ComponentDependencyGraph
        {
            Nodes = nodes,
            Edges = edges,
            HasCycles = cycles.Any(),
            Cycles = cycles,
            InitializationOrder = initializationOrder
        };

        _logger.LogDebug("Resolved dependency graph with {NodeCount} nodes, {EdgeCount} edges, {CycleCount} cycles",
            graph.Nodes.Count, graph.Edges.Count, graph.Cycles.Count);

        return graph;
    }

    /// <inheritdoc />
    public async Task<ComponentCompatibilityResult> ValidateCompatibilityAsync(IEnumerable<Type> components, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating compatibility for {ComponentCount} components", components.Count());

        var errors = new List<string>();
        var warnings = new List<string>();
        var missingDependencies = new List<Type>();
        var conflictingComponents = new List<Type>();

        var componentSet = components.ToHashSet();

        foreach (var componentType in components)
        {
            var metadata = await GetComponentMetadataAsync(componentType);

            // Check required dependencies
            foreach (var dependency in metadata.RequiredDependencies)
            {
                if (!componentSet.Contains(dependency) && !componentSet.Any(c => dependency.IsAssignableFrom(c)))
                {
                    errors.Add($"Component {componentType.Name} requires {dependency.Name} but it is not present");
                    missingDependencies.Add(dependency);
                }
            }

            // Check conflicts
            foreach (var conflict in metadata.Conflicts)
            {
                if (componentSet.Contains(conflict) || componentSet.Any(c => conflict.IsAssignableFrom(c)))
                {
                    errors.Add($"Component {componentType.Name} conflicts with {conflict.Name}");
                    conflictingComponents.Add(conflict);
                }
            }

            // Check optional dependencies for warnings
            foreach (var optionalDep in metadata.OptionalDependencies)
            {
                if (!componentSet.Contains(optionalDep) && !componentSet.Any(c => optionalDep.IsAssignableFrom(c)))
                {
                    warnings.Add($"Component {componentType.Name} would benefit from optional dependency {optionalDep.Name}");
                }
            }
        }

        var isCompatible = errors.Count == 0;
        _logger.LogDebug("Compatibility validation result: {IsCompatible} ({ErrorCount} errors, {WarningCount} warnings)",
            isCompatible, errors.Count, warnings.Count);

        return isCompatible
            ? ComponentCompatibilityResult.Success()
            : ComponentCompatibilityResult.Failure(errors, warnings, missingDependencies, conflictingComponents);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Type>> GetMinimumComponentSetAsync(IEnumerable<Type> desiredComponents, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Computing minimum component set for {ComponentCount} desired components", desiredComponents.Count());

        var result = new HashSet<Type>(desiredComponents);
        var toProcess = new Queue<Type>(desiredComponents);

        while (toProcess.Count > 0)
        {
            var componentType = toProcess.Dequeue();
            var metadata = await GetComponentMetadataAsync(componentType);

            foreach (var dependency in metadata.RequiredDependencies)
            {
                if (!result.Contains(dependency) && !result.Any(c => dependency.IsAssignableFrom(c)))
                {
                    result.Add(dependency);
                    toProcess.Enqueue(dependency);
                }
            }
        }

        _logger.LogDebug("Minimum component set contains {ComponentCount} components", result.Count);
        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ComponentSuggestion>> SuggestEnhancementsAsync(IEnumerable<Type> existingComponents, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Suggesting enhancements for {ComponentCount} existing components", existingComponents.Count());

        var suggestions = new List<ComponentSuggestion>();
        var componentSet = existingComponents.ToHashSet();

        foreach (var componentType in existingComponents)
        {
            var metadata = await GetComponentMetadataAsync(componentType);

            // Suggest optional dependencies
            foreach (var optionalDep in metadata.OptionalDependencies)
            {
                if (!componentSet.Contains(optionalDep) && !componentSet.Any(c => optionalDep.IsAssignableFrom(c)))
                {
                    suggestions.Add(new ComponentSuggestion
                    {
                        ComponentType = optionalDep,
                        Reason = $"Would enhance {componentType.Name} functionality",
                        BenefitLevel = SuggestionBenefitLevel.Medium,
                        BeneficiaryComponents = new[] { componentType }
                    });
                }
            }
        }

        _logger.LogDebug("Generated {SuggestionCount} enhancement suggestions", suggestions.Count);
        return suggestions;
    }

    /// <inheritdoc />
    public Task RegisterComponentMetadataAsync(Type componentType, ComponentDependencyMetadata metadata, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Registering metadata for component {ComponentType}", componentType.Name);
        _componentMetadata[componentType] = metadata;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets or creates metadata for a component type.
    /// </summary>
    /// <param name="componentType">The component type to get metadata for.</param>
    /// <returns>The component metadata.</returns>
    private Task<ComponentDependencyMetadata> GetComponentMetadataAsync(Type componentType)
    {
        if (_componentMetadata.TryGetValue(componentType, out var metadata))
        {
            return Task.FromResult(metadata);
        }

        // Create default metadata if none is registered
        var defaultMetadata = new ComponentDependencyMetadata
        {
            RequiredDependencies = Array.Empty<Type>(),
            OptionalDependencies = Array.Empty<Type>(),
            Conflicts = Array.Empty<Type>(),
            Provides = Array.Empty<Type>(),
            InitializationPriority = 0,
            Tags = Array.Empty<string>()
        };

        return Task.FromResult(defaultMetadata);
    }

    /// <summary>
    /// Detects cycles in the dependency graph.
    /// </summary>
    /// <param name="components">The components to check for cycles.</param>
    /// <param name="edges">The dependency edges.</param>
    /// <returns>Any cycles found in the dependency graph.</returns>
    private static List<IReadOnlyCollection<Type>> DetectCycles(List<Type> components, List<ComponentDependencyEdge> edges)
    {
        var cycles = new List<IReadOnlyCollection<Type>>();
        var visited = new HashSet<Type>();
        var recursionStack = new HashSet<Type>();
        var adjacencyList = BuildAdjacencyList(edges);

        foreach (var component in components)
        {
            if (!visited.Contains(component))
            {
                var path = new List<Type>();
                if (HasCycleRecursive(component, adjacencyList, visited, recursionStack, path))
                {
                    cycles.Add(path.ToList());
                }
            }
        }

        return cycles;
    }

    /// <summary>
    /// Performs topological sorting to determine initialization order.
    /// </summary>
    /// <param name="components">The components to sort.</param>
    /// <param name="edges">The dependency edges.</param>
    /// <returns>The topologically sorted component order.</returns>
    private static List<Type> TopologicalSort(List<Type> components, List<ComponentDependencyEdge> edges)
    {
        var adjacencyList = BuildAdjacencyList(edges);
        var inDegree = components.ToDictionary(c => c, _ => 0);
        
        // Calculate in-degrees
        foreach (var edge in edges.Where(e => e.DependencyType == DependencyType.Required))
        {
            if (inDegree.ContainsKey(edge.Target))
            {
                inDegree[edge.Target]++;
            }
        }

        var queue = new Queue<Type>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));
        var result = new List<Type>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            if (adjacencyList.TryGetValue(current, out var neighbors))
            {
                foreach (var neighbor in neighbors.Where(n => inDegree.ContainsKey(n)))
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Builds an adjacency list from dependency edges.
    /// </summary>
    /// <param name="edges">The dependency edges.</param>
    /// <returns>The adjacency list.</returns>
    private static Dictionary<Type, List<Type>> BuildAdjacencyList(List<ComponentDependencyEdge> edges)
    {
        var adjacencyList = new Dictionary<Type, List<Type>>();

        foreach (var edge in edges.Where(e => e.DependencyType == DependencyType.Required))
        {
            if (!adjacencyList.TryGetValue(edge.Source, out var neighbors))
            {
                neighbors = new List<Type>();
                adjacencyList[edge.Source] = neighbors;
            }
            neighbors.Add(edge.Target);
        }

        return adjacencyList;
    }

    /// <summary>
    /// Recursive helper for cycle detection using DFS.
    /// </summary>
    /// <param name="node">The current node being visited.</param>
    /// <param name="adjacencyList">The adjacency list.</param>
    /// <param name="visited">Set of visited nodes.</param>
    /// <param name="recursionStack">Set of nodes in current recursion stack.</param>
    /// <param name="path">Current path being explored.</param>
    /// <returns>True if a cycle is detected.</returns>
    private static bool HasCycleRecursive(Type node, Dictionary<Type, List<Type>> adjacencyList, 
        HashSet<Type> visited, HashSet<Type> recursionStack, List<Type> path)
    {
        visited.Add(node);
        recursionStack.Add(node);
        path.Add(node);

        if (adjacencyList.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    if (HasCycleRecursive(neighbor, adjacencyList, visited, recursionStack, path))
                        return true;
                }
                else if (recursionStack.Contains(neighbor))
                {
                    // Found a cycle
                    var cycleStart = path.IndexOf(neighbor);
                    path.RemoveRange(0, cycleStart);
                    return true;
                }
            }
        }

        recursionStack.Remove(node);
        if (path.Count > 0 && path[^1] == node)
        {
            path.RemoveAt(path.Count - 1);
        }

        return false;
    }
}
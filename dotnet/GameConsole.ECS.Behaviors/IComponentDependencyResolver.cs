namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Resolves and validates component dependencies for behavior composition.
/// Ensures component compatibility and manages dependency relationships.
/// </summary>
public interface IComponentDependencyResolver
{
    /// <summary>
    /// Resolves the dependency graph for a set of components.
    /// </summary>
    /// <param name="components">The components to analyze for dependencies.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the dependency graph.</returns>
    Task<ComponentDependencyGraph> ResolveDependenciesAsync(IEnumerable<Type> components, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a set of components have compatible dependencies.
    /// </summary>
    /// <param name="components">The components to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns validation results.</returns>
    Task<ComponentCompatibilityResult> ValidateCompatibilityAsync(IEnumerable<Type> components, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the minimum set of components required to satisfy all dependencies.
    /// </summary>
    /// <param name="desiredComponents">The components that are desired.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the minimum component set.</returns>
    Task<IEnumerable<Type>> GetMinimumComponentSetAsync(IEnumerable<Type> desiredComponents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests additional components that would enhance the given component set.
    /// </summary>
    /// <param name="existingComponents">The existing components.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns suggested components with their benefits.</returns>
    Task<IEnumerable<ComponentSuggestion>> SuggestEnhancementsAsync(IEnumerable<Type> existingComponents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers component dependency metadata with the resolver.
    /// </summary>
    /// <param name="componentType">The component type to register metadata for.</param>
    /// <param name="metadata">The dependency metadata.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RegisterComponentMetadataAsync(Type componentType, ComponentDependencyMetadata metadata, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a dependency graph between components.
/// </summary>
public class ComponentDependencyGraph
{
    /// <summary>
    /// Gets the nodes in the dependency graph.
    /// </summary>
    public IReadOnlyCollection<ComponentDependencyNode> Nodes { get; init; } = Array.Empty<ComponentDependencyNode>();

    /// <summary>
    /// Gets the edges representing dependencies between nodes.
    /// </summary>
    public IReadOnlyCollection<ComponentDependencyEdge> Edges { get; init; } = Array.Empty<ComponentDependencyEdge>();

    /// <summary>
    /// Gets a value indicating whether the dependency graph has cycles.
    /// </summary>
    public bool HasCycles { get; init; }

    /// <summary>
    /// Gets any cycles detected in the dependency graph.
    /// </summary>
    public IReadOnlyCollection<IReadOnlyCollection<Type>> Cycles { get; init; } = Array.Empty<IReadOnlyCollection<Type>>();

    /// <summary>
    /// Gets the topologically sorted component order for initialization.
    /// </summary>
    public IReadOnlyCollection<Type> InitializationOrder { get; init; } = Array.Empty<Type>();
}

/// <summary>
/// Represents a node in the component dependency graph.
/// </summary>
public class ComponentDependencyNode
{
    /// <summary>
    /// Gets the component type for this node.
    /// </summary>
    public required Type ComponentType { get; init; }

    /// <summary>
    /// Gets the metadata for this component.
    /// </summary>
    public required ComponentDependencyMetadata Metadata { get; init; }
}

/// <summary>
/// Represents an edge in the component dependency graph.
/// </summary>
public class ComponentDependencyEdge
{
    /// <summary>
    /// Gets the source component type (the one that has the dependency).
    /// </summary>
    public required Type Source { get; init; }

    /// <summary>
    /// Gets the target component type (the one being depended on).
    /// </summary>
    public required Type Target { get; init; }

    /// <summary>
    /// Gets the type of dependency relationship.
    /// </summary>
    public required DependencyType DependencyType { get; init; }
}

/// <summary>
/// Metadata about a component's dependencies and relationships.
/// </summary>
public class ComponentDependencyMetadata
{
    /// <summary>
    /// Gets the component types that this component requires to function.
    /// </summary>
    public IReadOnlyCollection<Type> RequiredDependencies { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Gets the component types that enhance this component but are not required.
    /// </summary>
    public IReadOnlyCollection<Type> OptionalDependencies { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Gets the component types that cannot be used with this component.
    /// </summary>
    public IReadOnlyCollection<Type> Conflicts { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Gets the component types that this component provides functionality for.
    /// </summary>
    public IReadOnlyCollection<Type> Provides { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Gets the initialization priority for this component.
    /// Higher values initialize first.
    /// </summary>
    public int InitializationPriority { get; init; } = 0;

    /// <summary>
    /// Gets the tags associated with this component for categorization.
    /// </summary>
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Results of component compatibility validation.
/// </summary>
public class ComponentCompatibilityResult
{
    /// <summary>
    /// Gets a value indicating whether the components are compatible.
    /// </summary>
    public bool IsCompatible { get; init; }

    /// <summary>
    /// Gets validation errors if the components are not compatible.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets validation warnings that don't prevent usage.
    /// </summary>
    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets missing component types that are required but not provided.
    /// </summary>
    public IReadOnlyCollection<Type> MissingDependencies { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Gets conflicting component types that cannot be used together.
    /// </summary>
    public IReadOnlyCollection<Type> ConflictingComponents { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Creates a successful compatibility result.
    /// </summary>
    public static ComponentCompatibilityResult Success() => new() { IsCompatible = true };

    /// <summary>
    /// Creates a failed compatibility result with errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <param name="warnings">Optional validation warnings.</param>
    /// <param name="missingDependencies">Missing dependency types.</param>
    /// <param name="conflictingComponents">Conflicting component types.</param>
    public static ComponentCompatibilityResult Failure(
        IEnumerable<string> errors,
        IEnumerable<string>? warnings = null,
        IEnumerable<Type>? missingDependencies = null,
        IEnumerable<Type>? conflictingComponents = null) => new()
        {
            IsCompatible = false,
            Errors = errors?.ToList() ?? new List<string>(),
            Warnings = warnings?.ToList() ?? new List<string>(),
            MissingDependencies = missingDependencies?.ToList() ?? new List<Type>(),
            ConflictingComponents = conflictingComponents?.ToList() ?? new List<Type>()
        };
}

/// <summary>
/// Suggests a component that would enhance an existing component set.
/// </summary>
public class ComponentSuggestion
{
    /// <summary>
    /// Gets the component type being suggested.
    /// </summary>
    public required Type ComponentType { get; init; }

    /// <summary>
    /// Gets the reason why this component is being suggested.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets the benefit level of adding this component.
    /// </summary>
    public required SuggestionBenefitLevel BenefitLevel { get; init; }

    /// <summary>
    /// Gets the components that would benefit from adding this suggestion.
    /// </summary>
    public IReadOnlyCollection<Type> BeneficiaryComponents { get; init; } = Array.Empty<Type>();
}

/// <summary>
/// Represents the type of dependency between components.
/// </summary>
public enum DependencyType
{
    /// <summary>
    /// A hard requirement that must be satisfied.
    /// </summary>
    Required,

    /// <summary>
    /// An optional dependency that enhances functionality.
    /// </summary>
    Optional,

    /// <summary>
    /// A conflicting relationship that prevents usage together.
    /// </summary>
    Conflict,

    /// <summary>
    /// A providing relationship where one component provides functionality for another.
    /// </summary>
    Provides
}

/// <summary>
/// Represents the benefit level of a component suggestion.
/// </summary>
public enum SuggestionBenefitLevel
{
    /// <summary>
    /// Low benefit, nice to have but not important.
    /// </summary>
    Low,

    /// <summary>
    /// Medium benefit, would improve functionality noticeably.
    /// </summary>
    Medium,

    /// <summary>
    /// High benefit, would significantly enhance functionality.
    /// </summary>
    High,

    /// <summary>
    /// Critical benefit, almost required for proper functioning.
    /// </summary>
    Critical
}
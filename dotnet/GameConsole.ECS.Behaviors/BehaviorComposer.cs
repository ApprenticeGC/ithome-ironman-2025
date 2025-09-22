using Microsoft.Extensions.Logging;

namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Basic implementation of behavior composer that combines components into behaviors.
/// </summary>
public class BehaviorComposer : IBehaviorComposer
{
    private readonly ILogger<BehaviorComposer> _logger;
    private readonly IComponentDependencyResolver _dependencyResolver;
    private readonly Dictionary<Type, Func<IEnumerable<object>, IBehavior>> _behaviorFactories = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BehaviorComposer"/> class.
    /// </summary>
    /// <param name="logger">Logger for the composer.</param>
    /// <param name="dependencyResolver">Dependency resolver for component validation.</param>
    public BehaviorComposer(ILogger<BehaviorComposer> logger, IComponentDependencyResolver dependencyResolver)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dependencyResolver = dependencyResolver ?? throw new ArgumentNullException(nameof(dependencyResolver));
    }

    /// <inheritdoc />
    public async Task<T> ComposeBehaviorAsync<T>(IEnumerable<object> components, CancellationToken cancellationToken = default) 
        where T : class, IBehavior
    {
        _logger.LogDebug("Composing behavior of type {BehaviorType} from {ComponentCount} components", 
            typeof(T).Name, components.Count());

        var componentList = components.ToList();
        var behaviorType = typeof(T);

        // Validate composition first
        var isValid = await ValidateCompositionAsync(behaviorType, componentList, cancellationToken);
        if (!isValid)
        {
            var error = $"Cannot compose behavior {behaviorType.Name} from provided components";
            _logger.LogError(error);
            throw new InvalidOperationException(error);
        }

        // Get or create factory for this behavior type
        if (!_behaviorFactories.TryGetValue(behaviorType, out var factory))
        {
            factory = CreateDefaultFactory<T>();
            _behaviorFactories[behaviorType] = factory;
        }

        try
        {
            var behavior = factory(componentList);
            if (behavior is not T typedBehavior)
            {
                throw new InvalidOperationException($"Factory for {behaviorType.Name} returned incorrect type");
            }

            _logger.LogInformation("Successfully composed behavior {BehaviorType} ({BehaviorId})", 
                behaviorType.Name, typedBehavior.Id);

            return typedBehavior;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compose behavior {BehaviorType}", behaviorType.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateCompositionAsync(Type behaviorType, IEnumerable<object> components, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Validating composition of {BehaviorType} with {ComponentCount} components", 
            behaviorType.Name, components.Count());

        try
        {
            var componentTypes = components.Select(c => c.GetType()).ToList();
            
            // Use dependency resolver to validate component compatibility
            var compatibilityResult = await _dependencyResolver.ValidateCompatibilityAsync(componentTypes, cancellationToken);
            
            if (!compatibilityResult.IsCompatible)
            {
                _logger.LogWarning("Component compatibility validation failed for {BehaviorType}: {Errors}", 
                    behaviorType.Name, string.Join(", ", compatibilityResult.Errors));
                return false;
            }

            // Additional behavior-specific validation can be added here
            // For now, we accept any compatible component set

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating composition for {BehaviorType}", behaviorType.Name);
            return false;
        }
    }

    /// <inheritdoc />
    public Task<IEnumerable<object>> DecomposeBehaviorAsync(IBehavior behavior, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Decomposing behavior {BehaviorName} ({BehaviorId})", behavior.Name, behavior.Id);

        try
        {
            // Return the components that make up this behavior
            var components = behavior.Components.ToList();
            
            _logger.LogDebug("Decomposed behavior {BehaviorName} into {ComponentCount} components", 
                behavior.Name, components.Count);

            return Task.FromResult<IEnumerable<object>>(components);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decompose behavior {BehaviorName} ({BehaviorId})", 
                behavior.Name, behavior.Id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IBehavior> ModifyBehaviorAsync(IBehavior behavior, BehaviorModificationType modificationType, object component, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Modifying behavior {BehaviorName} ({BehaviorId}) with {ModificationType} of {ComponentType}", 
            behavior.Name, behavior.Id, modificationType, component.GetType().Name);

        try
        {
            // Get current components
            var currentComponents = behavior.Components.ToList();
            var newComponents = new List<object>(currentComponents);

            // Apply modification
            switch (modificationType)
            {
                case BehaviorModificationType.AddComponent:
                    newComponents.Add(component);
                    break;

                case BehaviorModificationType.RemoveComponent:
                    var componentType = component.GetType();
                    newComponents.RemoveAll(c => c.GetType() == componentType || componentType.IsAssignableFrom(c.GetType()));
                    break;

                case BehaviorModificationType.UpdateComponent:
                    var updateType = component.GetType();
                    for (int i = 0; i < newComponents.Count; i++)
                    {
                        if (newComponents[i].GetType() == updateType || updateType.IsAssignableFrom(newComponents[i].GetType()))
                        {
                            newComponents[i] = component;
                            break;
                        }
                    }
                    break;

                case BehaviorModificationType.ReplaceComponent:
                    // For replace, we assume the component parameter is the new component
                    // and we need to find what to replace based on type
                    var replaceType = component.GetType();
                    for (int i = 0; i < newComponents.Count; i++)
                    {
                        if (replaceType.IsAssignableFrom(newComponents[i].GetType()))
                        {
                            newComponents[i] = component;
                            break;
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(modificationType));
            }

            // Validate the new composition
            var behaviorType = behavior.GetType();
            var isValid = await ValidateCompositionAsync(behaviorType, newComponents, cancellationToken);
            if (!isValid)
            {
                throw new InvalidOperationException($"Modification would result in invalid behavior composition");
            }

            // For now, we'll create a new behavior instance with the modified components
            // In a more sophisticated implementation, we might modify the existing behavior in-place
            if (!_behaviorFactories.TryGetValue(behaviorType, out var factory))
            {
                throw new InvalidOperationException($"No factory available for behavior type {behaviorType.Name}");
            }

            var modifiedBehavior = factory(newComponents);
            
            _logger.LogInformation("Successfully modified behavior {BehaviorName}, created new instance {NewBehaviorId}", 
                behavior.Name, modifiedBehavior.Id);

            return modifiedBehavior;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to modify behavior {BehaviorName} ({BehaviorId})", 
                behavior.Name, behavior.Id);
            throw;
        }
    }

    /// <summary>
    /// Registers a factory function for creating behaviors of a specific type.
    /// </summary>
    /// <typeparam name="T">The behavior type.</typeparam>
    /// <param name="factory">The factory function.</param>
    public void RegisterBehaviorFactory<T>(Func<IEnumerable<object>, T> factory) where T : class, IBehavior
    {
        var behaviorType = typeof(T);
        _logger.LogDebug("Registering behavior factory for {BehaviorType}", behaviorType.Name);
        _behaviorFactories[behaviorType] = components => factory(components);
    }

    /// <summary>
    /// Creates a default factory for a behavior type.
    /// </summary>
    /// <typeparam name="T">The behavior type.</typeparam>
    /// <returns>A default factory function.</returns>
    private Func<IEnumerable<object>, IBehavior> CreateDefaultFactory<T>() where T : class, IBehavior
    {
        var behaviorType = typeof(T);
        _logger.LogDebug("Creating default factory for {BehaviorType}", behaviorType.Name);

        return components =>
        {
            // Try to find a constructor that takes an IEnumerable<object> or similar
            var constructors = behaviorType.GetConstructors();
            
            foreach (var constructor in constructors.OrderByDescending(c => c.GetParameters().Length))
            {
                var parameters = constructor.GetParameters();
                
                if (parameters.Length == 0)
                {
                    // Parameterless constructor
                    var instance = (T)constructor.Invoke(Array.Empty<object>());
                    return instance;
                }
                
                if (parameters.Length == 1)
                {
                    var paramType = parameters[0].ParameterType;
                    
                    // Constructor that takes components
                    if (paramType.IsAssignableFrom(typeof(IEnumerable<object>)))
                    {
                        var instance = (T)constructor.Invoke(new object[] { components });
                        return instance;
                    }
                    
                    // Constructor that takes logger
                    if (paramType.IsAssignableFrom(typeof(ILogger)) || paramType.IsAssignableFrom(typeof(ILogger<T>)))
                    {
                        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
                        var logger = loggerFactory.CreateLogger<T>();
                        var instance = (T)constructor.Invoke(new object[] { logger });
                        return instance;
                    }
                }
            }

            throw new InvalidOperationException($"Cannot create instance of {behaviorType.Name}: no suitable constructor found");
        };
    }
}

/// <summary>
/// A simple concrete behavior implementation for testing and basic use cases.
/// </summary>
public class CompositeBehavior : BaseBehavior
{
    private readonly List<object> _components;
    private readonly BehaviorMetadata _metadata;

    /// <inheritdoc />
    public override string Name { get; }

    /// <inheritdoc />
    public override IReadOnlyCollection<object> Components => _components;

    /// <inheritdoc />
    public override IBehaviorMetadata Metadata => _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeBehavior"/> class.
    /// </summary>
    /// <param name="name">The name of the behavior.</param>
    /// <param name="components">The components that make up this behavior.</param>
    /// <param name="logger">Logger for this behavior.</param>
    /// <param name="metadata">Optional metadata for this behavior.</param>
    public CompositeBehavior(string name, IEnumerable<object> components, ILogger logger, IBehaviorMetadata? metadata = null) 
        : base(logger)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _components = components?.ToList() ?? throw new ArgumentNullException(nameof(components));
        _metadata = (BehaviorMetadata)(metadata ?? new BehaviorMetadata
        {
            RequiredComponents = _components.Select(c => c.GetType()).ToList(),
            Tags = new[] { "composite" }
        });
    }

    /// <inheritdoc />
    protected override Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Activating composite behavior {BehaviorName} with {ComponentCount} components", 
            Name, Components.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task OnDeactivateAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("Deactivating composite behavior {BehaviorName}", Name);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task OnUpdateAsync(TimeSpan deltaTime, CancellationToken cancellationToken = default)
    {
        // Basic composite behavior just logs updates
        Logger.LogTrace("Updating composite behavior {BehaviorName} (delta: {DeltaTime}ms)", 
            Name, deltaTime.TotalMilliseconds);
        return Task.CompletedTask;
    }
}
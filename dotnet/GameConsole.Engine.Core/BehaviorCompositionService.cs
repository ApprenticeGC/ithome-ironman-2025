using GameConsole.Core.Abstractions;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GameConsole.Engine.Core;

/// <summary>
/// Base abstract component implementation that provides capability provider integration.
/// Concrete components should inherit from this class to integrate with the ECS framework.
/// </summary>
public abstract class BaseComponent : IComponent
{
    /// <inheritdoc />
    public int EntityId { get; set; }

    /// <inheritdoc />
    public virtual IEnumerable<string> GetCapabilities()
    {
        // Base implementation returns the component type name as a capability
        return new[] { GetType().Name };
    }

    /// <inheritdoc />
    public virtual bool HasCapability(string capability)
    {
        return GetCapabilities().Contains(capability, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// System execution context that provides additional information during system execution.
/// </summary>
public class SystemExecutionContext
{
    /// <summary>
    /// Gets the current frame delta time in milliseconds.
    /// </summary>
    public float DeltaTime { get; init; }

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public long FrameNumber { get; init; }

    /// <summary>
    /// Gets additional execution data.
    /// </summary>
    public IReadOnlyDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Tier 3 implementation of behavior composition service.
/// Orchestrates system execution and manages system lifecycle with error isolation and recovery.
/// </summary>
[Service(ServiceLifetime.Singleton)]
public class BehaviorCompositionService : IBehaviorCompositionService
{
    private readonly IEntityManager _entityManager;
    private readonly ILogger<BehaviorCompositionService>? _logger;
    private readonly ConcurrentDictionary<Type, ISystem> _systems = new();
    private volatile bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the BehaviorCompositionService class.
    /// </summary>
    /// <param name="entityManager">The entity manager service.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public BehaviorCompositionService(IEntityManager entityManager, ILogger<BehaviorCompositionService>? logger = null)
    {
        _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Initializing BehaviorCompositionService");
        _systems.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting BehaviorCompositionService");
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Stopping BehaviorCompositionService");
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RegisterSystemAsync<T>(T system) where T : class, ISystem
    {
        if (!_isRunning)
            throw new InvalidOperationException("BehaviorCompositionService is not running");

        if (system == null)
            throw new ArgumentNullException(nameof(system));

        _systems.AddOrUpdate(typeof(T), system, (_, _) => system);
        _logger?.LogDebug("Registered system: {SystemType}", typeof(T).Name);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterSystemAsync<T>() where T : class, ISystem
    {
        if (!_isRunning)
            throw new InvalidOperationException("BehaviorCompositionService is not running");

        if (_systems.TryRemove(typeof(T), out _))
        {
            _logger?.LogDebug("Unregistered system: {SystemType}", typeof(T).Name);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ExecuteSystemsAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("BehaviorCompositionService is not running");

        // Get all entities once for all systems
        var allEntities = await _entityManager.GetAllEntitiesAsync(cancellationToken);
        var entityArray = allEntities.ToArray();

        // Execute systems in priority order
        var systemsToExecute = _systems.Values
            .OrderBy(s => s.Priority)
            .ToArray();

        foreach (var system in systemsToExecute)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Filter entities that this system can process
                var relevantEntities = entityArray.Where(system.CanProcess).ToArray();
                
                if (relevantEntities.Length > 0)
                {
                    _logger?.LogTrace("Executing system {SystemType} on {EntityCount} entities", 
                        system.GetType().Name, relevantEntities.Length);
                    
                    await system.ExecuteAsync(relevantEntities, cancellationToken);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Error isolation - don't let one system failure break the entire execution
                _logger?.LogError(ex, "Error executing system {SystemType}: {Error}", 
                    system.GetType().Name, ex.Message);
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<ISystem> GetRegisteredSystems()
    {
        return _systems.Values.ToArray();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        _systems.Clear();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Base abstract system implementation that provides common functionality.
/// Concrete systems should inherit from this class to implement specific behaviors.
/// </summary>
public abstract class BaseSystem : ISystem
{
    /// <summary>
    /// Gets the logger for this system.
    /// </summary>
    protected ILogger? Logger { get; }

    /// <summary>
    /// Initializes a new instance of the BaseSystem class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostics.</param>
    protected BaseSystem(ILogger? logger = null)
    {
        Logger = logger;
    }

    /// <inheritdoc />
    public abstract Task ExecuteAsync(IEnumerable<IEntity> entities, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract bool CanProcess(IEntity entity);

    /// <inheritdoc />
    public virtual int Priority => 0;
}

/// <summary>
/// Validation helper for component composition rules.
/// Provides methods to validate component combinations and detect conflicts.
/// </summary>
public static class ComponentValidation
{
    /// <summary>
    /// Validates that an entity's component composition is valid.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <returns>A collection of validation errors, or empty if valid.</returns>
    public static IEnumerable<string> ValidateEntityComposition(IEntity entity)
    {
        if (entity == null)
        {
            yield return "Entity cannot be null";
            yield break;
        }

        var components = entity.Components.ToArray();
        var componentTypes = components.Select(c => c.GetType()).ToArray();

        // Check for duplicate component types
        var duplicateTypes = componentTypes
            .GroupBy(t => t)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicateType in duplicateTypes)
        {
            yield return $"Duplicate component type: {duplicateType.Name}";
        }

        // Validate individual components
        foreach (var component in components)
        {
            if (component.EntityId != entity.Id)
            {
                yield return $"Component {component.GetType().Name} has incorrect EntityId: expected {entity.Id}, got {component.EntityId}";
            }
        }
    }

    /// <summary>
    /// Checks if two component types conflict with each other.
    /// </summary>
    /// <param name="type1">The first component type.</param>
    /// <param name="type2">The second component type.</param>
    /// <returns>True if the types conflict, false otherwise.</returns>
    public static bool DoComponentTypesConflict(Type type1, Type type2)
    {
        // Basic conflict detection - components of the same type conflict
        return type1 == type2;
    }
}
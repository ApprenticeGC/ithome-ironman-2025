using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Core entity interface for the ECS framework.
/// Represents a lightweight entity with component composition capabilities.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets all components attached to this entity.
    /// </summary>
    IEnumerable<IComponent> Components { get; }

    /// <summary>
    /// Gets a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to retrieve.</typeparam>
    /// <returns>The component instance, or null if not found.</returns>
    T? GetComponent<T>() where T : class, IComponent;

    /// <summary>
    /// Adds a component to this entity.
    /// </summary>
    /// <param name="component">The component to add.</param>
    void AddComponent(IComponent component);

    /// <summary>
    /// Removes a component of the specified type from this entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    void RemoveComponent<T>() where T : class, IComponent;

    /// <summary>
    /// Checks if the entity has a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <returns>True if the component exists, false otherwise.</returns>
    bool HasComponent<T>() where T : class, IComponent;
}

/// <summary>
/// Base component interface for ECS framework.
/// Components are data-only containers that implement capability providers for tier integration.
/// </summary>
public interface IComponent : ICapabilityProvider
{
    /// <summary>
    /// Gets or sets the ID of the entity this component belongs to.
    /// </summary>
    int EntityId { get; set; }
}

/// <summary>
/// System interface for ECS behavior execution.
/// Systems contain the logic that operates on entities with specific component combinations.
/// </summary>
public interface ISystem
{
    /// <summary>
    /// Executes the system logic on a collection of entities.
    /// </summary>
    /// <param name="entities">The entities to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ExecuteAsync(IEnumerable<IEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if this system can process the given entity.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the system can process this entity, false otherwise.</returns>
    bool CanProcess(IEntity entity);

    /// <summary>
    /// Gets the execution priority of this system. Lower values execute first.
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Tier 3 service interface for entity lifecycle management.
/// Handles entity creation, destruction, and querying within the ECS framework.
/// </summary>
public interface IEntityManager : IService
{
    /// <summary>
    /// Creates a new entity asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the new entity.</returns>
    Task<IEntity> CreateEntityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="entityId">The entity ID to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the entity, or null if not found.</returns>
    Task<IEntity?> GetEntityAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity by its ID.
    /// </summary>
    /// <param name="entityId">The entity ID to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RemoveEntityAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities that have a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The component type to filter by.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns matching entities.</returns>
    Task<IEnumerable<IEntity>> GetEntitiesWithComponentsAsync<T>(CancellationToken cancellationToken = default) 
        where T : class, IComponent;

    /// <summary>
    /// Gets all entities that have components of the specified types.
    /// </summary>
    /// <param name="componentTypes">The component types that entities must have.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns matching entities.</returns>
    Task<IEnumerable<IEntity>> GetEntitiesWithComponentsAsync(IEnumerable<Type> componentTypes, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently active entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns all entities.</returns>
    Task<IEnumerable<IEntity>> GetAllEntitiesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Tier 3 service interface for behavior composition and system execution orchestration.
/// Manages system registration and coordinates execution of all registered systems.
/// </summary>
public interface IBehaviorCompositionService : IService
{
    /// <summary>
    /// Registers a system for execution.
    /// </summary>
    /// <typeparam name="T">The system type.</typeparam>
    /// <param name="system">The system instance to register.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RegisterSystemAsync<T>(T system) where T : class, ISystem;

    /// <summary>
    /// Unregisters a system from execution.
    /// </summary>
    /// <typeparam name="T">The system type.</typeparam>
    /// <returns>A task representing the async operation.</returns>
    Task UnregisterSystemAsync<T>() where T : class, ISystem;

    /// <summary>
    /// Executes all registered systems on relevant entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ExecuteSystemsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all currently registered systems.
    /// </summary>
    /// <returns>The collection of registered systems.</returns>
    IEnumerable<ISystem> GetRegisteredSystems();
}
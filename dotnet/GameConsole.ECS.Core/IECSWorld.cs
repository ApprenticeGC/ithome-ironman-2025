using GameConsole.Core.Abstractions;

namespace GameConsole.ECS.Core;

/// <summary>
/// Interface for the ECS world that manages entities, components, and systems.
/// Provides the main API for entity creation, component management, and world operations.
/// </summary>
public interface IECSWorld : IService
{
    /// <summary>
    /// Gets the unique identifier for this world.
    /// </summary>
    Guid WorldId { get; }

    /// <summary>
    /// Gets the name of this world.
    /// </summary>
    string Name { get; }

    // Entity Management
    /// <summary>
    /// Creates a new entity in this world.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the newly created entity.</returns>
    Task<IEntity> CreateEntityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Destroys an entity and removes all its components.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async destruction operation.</returns>
    Task DestroyEntityAsync(IEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity exists and is alive in this world.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the entity exists and is alive.</returns>
    Task<bool> IsEntityAliveAsync(IEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of alive entities in this world.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the entity count.</returns>
    Task<int> GetEntityCountAsync(CancellationToken cancellationToken = default);

    // Component Management
    /// <summary>
    /// Adds a component to an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component instance to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the component was added successfully.</returns>
    Task<bool> AddComponentAsync<T>(IEntity entity, T component, CancellationToken cancellationToken = default)
        where T : class, IComponent;

    /// <summary>
    /// Removes a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the component was removed successfully.</returns>
    Task<bool> RemoveComponentAsync<T>(IEntity entity, CancellationToken cancellationToken = default)
        where T : class, IComponent;

    /// <summary>
    /// Gets a component from an entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the component instance or null if not found.</returns>
    Task<T?> GetComponentAsync<T>(IEntity entity, CancellationToken cancellationToken = default)
        where T : class, IComponent;

    /// <summary>
    /// Checks if an entity has a specific component.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the entity has the component.</returns>
    Task<bool> HasComponentAsync<T>(IEntity entity, CancellationToken cancellationToken = default)
        where T : class, IComponent;

    /// <summary>
    /// Gets all component types that an entity has.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the set of component types.</returns>
    Task<IReadOnlySet<Type>> GetEntityComponentTypesAsync(IEntity entity, CancellationToken cancellationToken = default);

    // Query System
    /// <summary>
    /// Creates a query for entities with specific component requirements.
    /// </summary>
    /// <param name="requiredComponents">Component types that entities must have.</param>
    /// <param name="excludedComponents">Component types that entities must not have.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the entity query.</returns>
    Task<IEntityQuery> CreateQueryAsync(IEnumerable<Type> requiredComponents, IEnumerable<Type>? excludedComponents = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a query for entities with a specific component.
    /// </summary>
    /// <typeparam name="T">The required component type.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the entity query.</returns>
    Task<IEntityQuery> CreateQueryAsync<T>(CancellationToken cancellationToken = default)
        where T : class, IComponent;

    /// <summary>
    /// Creates a query for entities with two specific components.
    /// </summary>
    /// <typeparam name="T1">The first required component type.</typeparam>
    /// <typeparam name="T2">The second required component type.</typeparam>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the entity query.</returns>
    Task<IEntityQuery> CreateQueryAsync<T1, T2>(CancellationToken cancellationToken = default)
        where T1 : class, IComponent
        where T2 : class, IComponent;

    // System Management
    /// <summary>
    /// Adds a system to this world.
    /// </summary>
    /// <param name="system">The system to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async add operation.</returns>
    Task AddSystemAsync(ISystem system, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a system from this world.
    /// </summary>
    /// <param name="system">The system to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async remove operation.</returns>
    Task RemoveSystemAsync(ISystem system, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all systems in this world ordered by priority.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the systems in execution order.</returns>
    Task<IReadOnlyList<ISystem>> GetSystemsAsync(CancellationToken cancellationToken = default);

    // World Update
    /// <summary>
    /// Updates all systems in this world for one frame.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async update operation.</returns>
    Task UpdateAsync(float deltaTime, CancellationToken cancellationToken = default);
}
using GameConsole.Core.Abstractions;

namespace GameConsole.ECS.Core;

/// <summary>
/// Represents a world that manages entities, components, and systems in an ECS architecture.
/// Supports efficient component queries, entity management, and system execution.
/// </summary>
public interface IECSWorld : IService
{
    /// <summary>
    /// Unique identifier for this world instance.
    /// </summary>
    uint WorldId { get; }

    /// <summary>
    /// Total number of entities currently active in this world.
    /// </summary>
    int EntityCount { get; }

    /// <summary>
    /// Creates a new entity in this world.
    /// </summary>
    /// <returns>A new entity with a unique identifier.</returns>
    IEntity CreateEntity();

    /// <summary>
    /// Destroys an entity and removes all its components from this world.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    /// <returns>True if the entity was successfully destroyed, false if it was already invalid.</returns>
    bool DestroyEntity(IEntity entity);

    /// <summary>
    /// Adds a component to the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component data to add.</param>
    /// <returns>True if the component was added successfully, false otherwise.</returns>
    bool AddComponent<T>(IEntity entity, T component) where T : struct, IComponent;

    /// <summary>
    /// Removes a component from the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <returns>True if the component was removed successfully, false if the entity didn't have this component.</returns>
    bool RemoveComponent<T>(IEntity entity) where T : struct, IComponent;

    /// <summary>
    /// Checks if an entity has a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    bool HasComponent<T>(IEntity entity) where T : struct, IComponent;

    /// <summary>
    /// Gets a component from the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type to get.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <returns>The component if it exists, null otherwise.</returns>
    T? GetComponent<T>(IEntity entity) where T : struct, IComponent;

    /// <summary>
    /// Updates a component for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type to update.</typeparam>
    /// <param name="entity">The entity to update the component for.</param>
    /// <param name="component">The new component data.</param>
    /// <returns>True if the component was updated successfully, false if the entity doesn't have this component.</returns>
    bool UpdateComponent<T>(IEntity entity, T component) where T : struct, IComponent;

    /// <summary>
    /// Queries all entities that have the specified component types.
    /// </summary>
    /// <typeparam name="T">The component type to query for.</typeparam>
    /// <returns>An enumerable of entities that have the specified component.</returns>
    IEnumerable<IEntity> Query<T>() where T : struct, IComponent;

    /// <summary>
    /// Queries all entities that have the specified component types.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <typeparam name="T2">Second component type to query for.</typeparam>
    /// <returns>An enumerable of entities that have both specified components.</returns>
    IEnumerable<IEntity> Query<T1, T2>() 
        where T1 : struct, IComponent 
        where T2 : struct, IComponent;

    /// <summary>
    /// Adds a system to this world for execution.
    /// </summary>
    /// <param name="system">The system to add.</param>
    void AddSystem(ISystem system);

    /// <summary>
    /// Removes a system from this world.
    /// </summary>
    /// <param name="system">The system to remove.</param>
    /// <returns>True if the system was removed successfully, false if it wasn't found.</returns>
    bool RemoveSystem(ISystem system);

    /// <summary>
    /// Updates all systems in this world in priority order.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update in seconds.</param>
    void UpdateSystems(float deltaTime);
}
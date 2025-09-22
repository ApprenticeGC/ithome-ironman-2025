namespace GameConsole.ECS.Core;

/// <summary>
/// Base interface for component storage implementations.
/// </summary>
internal interface IComponentStorage
{
    /// <summary>
    /// Removes a component for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity ID to remove the component for.</param>
    /// <returns>True if the component was removed, false otherwise.</returns>
    bool RemoveComponent(uint entityId);

    /// <summary>
    /// Checks if a component exists for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <returns>True if the component exists, false otherwise.</returns>
    bool HasComponent(uint entityId);

    /// <summary>
    /// Gets all entity IDs that have this component type.
    /// </summary>
    /// <returns>An enumerable of entity IDs.</returns>
    IEnumerable<uint> GetEntities();
}

/// <summary>
/// Sparse set-based component storage for efficient component management.
/// Provides O(1) operations for add, remove, and has operations.
/// </summary>
/// <typeparam name="T">The component type to store.</typeparam>
internal class ComponentStorage<T> : IComponentStorage where T : struct, IComponent
{
    private readonly Dictionary<uint, int> _entityToIndex = new();
    private readonly List<uint> _entities = new();
    private readonly List<T> _components = new();

    /// <summary>
    /// Adds a component for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity ID to add the component for.</param>
    /// <param name="component">The component data to add.</param>
    /// <returns>True if the component was added, false if it already exists.</returns>
    public bool AddComponent(uint entityId, T component)
    {
        if (_entityToIndex.ContainsKey(entityId))
            return false;

        var index = _entities.Count;
        _entityToIndex[entityId] = index;
        _entities.Add(entityId);
        _components.Add(component);
        
        return true;
    }

    /// <summary>
    /// Removes a component for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity ID to remove the component for.</param>
    /// <returns>True if the component was removed, false if it didn't exist.</returns>
    public bool RemoveComponent(uint entityId)
    {
        if (!_entityToIndex.TryGetValue(entityId, out var index))
            return false;

        var lastIndex = _entities.Count - 1;
        
        if (index != lastIndex)
        {
            // Swap with the last element
            var lastEntity = _entities[lastIndex];
            _entities[index] = lastEntity;
            _components[index] = _components[lastIndex];
            _entityToIndex[lastEntity] = index;
        }

        // Remove the last element
        _entities.RemoveAt(lastIndex);
        _components.RemoveAt(lastIndex);
        _entityToIndex.Remove(entityId);

        return true;
    }

    /// <summary>
    /// Checks if a component exists for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity ID to check.</param>
    /// <returns>True if the component exists, false otherwise.</returns>
    public bool HasComponent(uint entityId)
    {
        return _entityToIndex.ContainsKey(entityId);
    }

    /// <summary>
    /// Gets a component for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity ID to get the component for.</param>
    /// <returns>The component if it exists, null otherwise.</returns>
    public T? GetComponent(uint entityId)
    {
        if (!_entityToIndex.TryGetValue(entityId, out var index))
            return null;

        return _components[index];
    }

    /// <summary>
    /// Updates a component for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity ID to update the component for.</param>
    /// <param name="component">The new component data.</param>
    /// <returns>True if the component was updated, false if it didn't exist.</returns>
    public bool UpdateComponent(uint entityId, T component)
    {
        if (!_entityToIndex.TryGetValue(entityId, out var index))
            return false;

        _components[index] = component;
        return true;
    }

    /// <summary>
    /// Gets all entity IDs that have this component type.
    /// </summary>
    /// <returns>An enumerable of entity IDs.</returns>
    public IEnumerable<uint> GetEntities()
    {
        return _entities.AsEnumerable();
    }

    /// <summary>
    /// Gets all components of this type.
    /// </summary>
    /// <returns>An enumerable of components.</returns>
    public IEnumerable<T> GetComponents()
    {
        return _components.AsEnumerable();
    }
}
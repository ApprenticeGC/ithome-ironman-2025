using GameConsole.Engine.Core;
using System.Collections.Concurrent;

namespace GameConsole.ECS.Core;

/// <summary>
/// Implementation of an ECS world using sparse sets for efficient component storage.
/// Supports multiple concurrent worlds and optimized component queries.
/// </summary>
public class ECSWorld : IECSWorld
{
    private static uint _nextWorldId = 1;
    private static readonly object _worldIdLock = new();

    private readonly Dictionary<uint, Entity> _entities = new();
    private readonly Dictionary<Type, IComponentStorage> _componentStorages = new();
    private readonly List<ISystem> _systems = new();
    private readonly object _entityLock = new();
    private readonly object _systemLock = new();
    private bool _disposed = false;
    private bool _isRunning = false;

    /// <summary>
    /// Gets a value indicating whether the service is currently running.
    /// </summary>
    public bool IsRunning => _isRunning && !_disposed;

    /// <summary>
    /// Unique identifier for this world instance.
    /// </summary>
    public uint WorldId { get; }

    /// <summary>
    /// Total number of entities currently active in this world.
    /// </summary>
    public int EntityCount 
    {
        get
        {
            lock (_entityLock)
            {
                return _entities.Count;
            }
        }
    }

    /// <summary>
    /// Creates a new ECS world with a unique identifier.
    /// </summary>
    public ECSWorld()
    {
        lock (_worldIdLock)
        {
            WorldId = _nextWorldId++;
        }
    }

    /// <summary>
    /// Initializes the service asynchronously.
    /// This method is called once during service setup before the service is started.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        // ECS World doesn't require async initialization
        return Task.CompletedTask;
    }

    /// <summary>
    /// Starts the service asynchronously.
    /// This method is called to begin service operations after initialization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the service asynchronously.
    /// This method is called to gracefully shut down service operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed) return Task.CompletedTask;
        
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates a new entity in this world.
    /// </summary>
    /// <returns>A new entity with a unique identifier.</returns>
    public IEntity CreateEntity()
    {
        ThrowIfDisposed();
        
        var entity = Entity.Create();
        lock (_entityLock)
        {
            _entities[entity.Id] = entity;
        }
        return entity;
    }

    /// <summary>
    /// Destroys an entity and removes all its components from this world.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    /// <returns>True if the entity was successfully destroyed, false if it was already invalid.</returns>
    public bool DestroyEntity(IEntity entity)
    {
        ThrowIfDisposed();
        
        if (entity == null || entity.Id == 0) return false;

        lock (_entityLock)
        {
            if (!_entities.Remove(entity.Id))
                return false;

            // Remove all components for this entity
            foreach (var storage in _componentStorages.Values)
            {
                storage.RemoveComponent(entity.Id);
            }
        }
        
        return true;
    }

    /// <summary>
    /// Adds a component to the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component data to add.</param>
    /// <returns>True if the component was added successfully, false otherwise.</returns>
    public bool AddComponent<T>(IEntity entity, T component) where T : struct, IComponent
    {
        ThrowIfDisposed();
        
        if (entity == null || entity.Id == 0) return false;

        lock (_entityLock)
        {
            if (!_entities.ContainsKey(entity.Id))
                return false;

            var storage = GetOrCreateComponentStorage<T>();
            return storage.AddComponent(entity.Id, component);
        }
    }

    /// <summary>
    /// Removes a component from the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <returns>True if the component was removed successfully, false if the entity didn't have this component.</returns>
    public bool RemoveComponent<T>(IEntity entity) where T : struct, IComponent
    {
        ThrowIfDisposed();
        
        if (entity == null || entity.Id == 0) return false;

        lock (_entityLock)
        {
            if (!_entities.ContainsKey(entity.Id))
                return false;

            if (!_componentStorages.TryGetValue(typeof(T), out var storage))
                return false;

            return storage.RemoveComponent(entity.Id);
        }
    }

    /// <summary>
    /// Checks if an entity has a specific component type.
    /// </summary>
    /// <typeparam name="T">The component type to check for.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    public bool HasComponent<T>(IEntity entity) where T : struct, IComponent
    {
        ThrowIfDisposed();
        
        if (entity == null || entity.Id == 0) return false;

        lock (_entityLock)
        {
            if (!_entities.ContainsKey(entity.Id))
                return false;

            if (!_componentStorages.TryGetValue(typeof(T), out var storage))
                return false;

            return storage.HasComponent(entity.Id);
        }
    }

    /// <summary>
    /// Gets a component from the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type to get.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <returns>The component if it exists, null otherwise.</returns>
    public T? GetComponent<T>(IEntity entity) where T : struct, IComponent
    {
        ThrowIfDisposed();
        
        if (entity == null || entity.Id == 0) return null;

        lock (_entityLock)
        {
            if (!_entities.ContainsKey(entity.Id))
                return null;

            if (!_componentStorages.TryGetValue(typeof(T), out var storage))
                return null;

            return ((ComponentStorage<T>)storage).GetComponent(entity.Id);
        }
    }

    /// <summary>
    /// Updates a component for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type to update.</typeparam>
    /// <param name="entity">The entity to update the component for.</param>
    /// <param name="component">The new component data.</param>
    /// <returns>True if the component was updated successfully, false if the entity doesn't have this component.</returns>
    public bool UpdateComponent<T>(IEntity entity, T component) where T : struct, IComponent
    {
        ThrowIfDisposed();
        
        if (entity == null || entity.Id == 0) return false;

        lock (_entityLock)
        {
            if (!_entities.ContainsKey(entity.Id))
                return false;

            if (!_componentStorages.TryGetValue(typeof(T), out var storage))
                return false;

            return ((ComponentStorage<T>)storage).UpdateComponent(entity.Id, component);
        }
    }

    /// <summary>
    /// Queries all entities that have the specified component type.
    /// </summary>
    /// <typeparam name="T">The component type to query for.</typeparam>
    /// <returns>An enumerable of entities that have the specified component.</returns>
    public IEnumerable<IEntity> Query<T>() where T : struct, IComponent
    {
        ThrowIfDisposed();

        lock (_entityLock)
        {
            if (!_componentStorages.TryGetValue(typeof(T), out var storage))
                return Enumerable.Empty<IEntity>();

            return storage.GetEntities()
                .Select(id => _entities.TryGetValue(id, out var entity) ? (IEntity)entity : Entity.None)
                .Where(e => e.IsValid)
                .ToList(); // Materialize to avoid holding lock
        }
    }

    /// <summary>
    /// Queries all entities that have the specified component types.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <typeparam name="T2">Second component type to query for.</typeparam>
    /// <returns>An enumerable of entities that have both specified components.</returns>
    public IEnumerable<IEntity> Query<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        ThrowIfDisposed();

        lock (_entityLock)
        {
            if (!_componentStorages.TryGetValue(typeof(T1), out var storage1) ||
                !_componentStorages.TryGetValue(typeof(T2), out var storage2))
                return Enumerable.Empty<IEntity>();

            var entities1 = storage1.GetEntities().ToHashSet();
            var entities2 = storage2.GetEntities();

            return entities2
                .Where(entities1.Contains)
                .Select(id => _entities.TryGetValue(id, out var entity) ? (IEntity)entity : Entity.None)
                .Where(e => e.IsValid)
                .ToList(); // Materialize to avoid holding lock
        }
    }

    /// <summary>
    /// Adds a system to this world for execution.
    /// </summary>
    /// <param name="system">The system to add.</param>
    public void AddSystem(ISystem system)
    {
        ThrowIfDisposed();
        
        if (system == null) return;

        lock (_systemLock)
        {
            if (!_systems.Contains(system))
            {
                _systems.Add(system);
                _systems.Sort((a, b) => b.Priority.CompareTo(a.Priority)); // Higher priority first
            }
        }
    }

    /// <summary>
    /// Removes a system from this world.
    /// </summary>
    /// <param name="system">The system to remove.</param>
    /// <returns>True if the system was removed successfully, false if it wasn't found.</returns>
    public bool RemoveSystem(ISystem system)
    {
        ThrowIfDisposed();
        
        if (system == null) return false;

        lock (_systemLock)
        {
            return _systems.Remove(system);
        }
    }

    /// <summary>
    /// Updates all systems in this world in priority order.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update in seconds.</param>
    public void UpdateSystems(float deltaTime)
    {
        ThrowIfDisposed();

        ISystem[] systemsToUpdate;
        lock (_systemLock)
        {
            systemsToUpdate = _systems.Where(s => s.IsEnabled).ToArray();
        }

        foreach (var system in systemsToUpdate)
        {
            try
            {
                system.Update(this, deltaTime);
            }
            catch (Exception ex)
            {
                // In a real implementation, you'd want proper logging
                Console.WriteLine($"Error updating system {system.GetType().Name}: {ex}");
            }
        }
    }

    private ComponentStorage<T> GetOrCreateComponentStorage<T>() where T : struct, IComponent
    {
        var type = typeof(T);
        if (!_componentStorages.TryGetValue(type, out var storage))
        {
            storage = new ComponentStorage<T>();
            _componentStorages[type] = storage;
        }
        return (ComponentStorage<T>)storage;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ECSWorld));
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        lock (_entityLock)
        {
            _entities.Clear();
            _componentStorages.Clear();
        }

        lock (_systemLock)
        {
            _systems.Clear();
        }

        _disposed = true;
        return ValueTask.CompletedTask;
    }
}
using GameConsole.Core.Abstractions;
using System.Collections.Concurrent;

namespace GameConsole.Engine.Core;

/// <summary>
/// Default implementation of IEntity with component management capabilities.
/// </summary>
public class Entity : IEntity
{
    private readonly ConcurrentDictionary<Type, IComponent> _components = new();
    private static readonly object _componentsLock = new();

    /// <summary>
    /// Initializes a new instance of the Entity class.
    /// </summary>
    /// <param name="id">The unique identifier for this entity.</param>
    public Entity(int id)
    {
        Id = id;
    }

    /// <inheritdoc />
    public int Id { get; }

    /// <inheritdoc />
    public IEnumerable<IComponent> Components 
    {
        get
        {
            lock (_componentsLock)
            {
                return _components.Values.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public T? GetComponent<T>() where T : class, IComponent
    {
        lock (_componentsLock)
        {
            return _components.TryGetValue(typeof(T), out var component) ? component as T : null;
        }
    }

    /// <inheritdoc />
    public void AddComponent(IComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        lock (_componentsLock)
        {
            component.EntityId = Id;
            _components.AddOrUpdate(component.GetType(), component, (_, _) => component);
        }
    }

    /// <inheritdoc />
    public void RemoveComponent<T>() where T : class, IComponent
    {
        lock (_componentsLock)
        {
            _components.TryRemove(typeof(T), out _);
        }
    }

    /// <inheritdoc />
    public bool HasComponent<T>() where T : class, IComponent
    {
        lock (_componentsLock)
        {
            return _components.ContainsKey(typeof(T));
        }
    }
}

/// <summary>
/// Tier 3 implementation of entity lifecycle management service.
/// Provides entity creation, retrieval, and querying capabilities with thread-safe operations.
/// </summary>
[Service(ServiceLifetime.Singleton)]
public class EntityManager : IEntityManager
{
    private readonly ConcurrentDictionary<int, IEntity> _entities = new();
    private int _nextEntityId = 1;
    private volatile bool _isRunning;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Entity manager initialization is minimal - just reset state
        _entities.Clear();
        _nextEntityId = 1;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEntity> CreateEntityAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("EntityManager is not running");

        var entityId = Interlocked.Increment(ref _nextEntityId);
        var entity = new Entity(entityId);
        
        _entities.TryAdd(entityId, entity);
        
        return Task.FromResult<IEntity>(entity);
    }

    /// <inheritdoc />
    public Task<IEntity?> GetEntityAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("EntityManager is not running");

        _entities.TryGetValue(entityId, out var entity);
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task RemoveEntityAsync(int entityId, CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("EntityManager is not running");

        _entities.TryRemove(entityId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IEnumerable<IEntity>> GetEntitiesWithComponentsAsync<T>(CancellationToken cancellationToken = default) 
        where T : class, IComponent
    {
        if (!_isRunning)
            throw new InvalidOperationException("EntityManager is not running");

        var matchingEntities = _entities.Values
            .Where(entity => entity.HasComponent<T>())
            .ToArray();

        return Task.FromResult<IEnumerable<IEntity>>(matchingEntities);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IEntity>> GetEntitiesWithComponentsAsync(IEnumerable<Type> componentTypes, 
        CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("EntityManager is not running");

        if (componentTypes == null)
            throw new ArgumentNullException(nameof(componentTypes));

        var typeArray = componentTypes.ToArray();
        if (typeArray.Length == 0)
            return Task.FromResult<IEnumerable<IEntity>>(Array.Empty<IEntity>());

        var matchingEntities = _entities.Values
            .Where(entity => typeArray.All(type => entity.Components.Any(c => type.IsAssignableFrom(c.GetType()))))
            .ToArray();

        return Task.FromResult<IEnumerable<IEntity>>(matchingEntities);
    }

    /// <inheritdoc />
    public Task<IEnumerable<IEntity>> GetAllEntitiesAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("EntityManager is not running");

        return Task.FromResult<IEnumerable<IEntity>>(_entities.Values.ToArray());
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            await StopAsync();
        }

        _entities.Clear();
        GC.SuppressFinalize(this);
    }
}
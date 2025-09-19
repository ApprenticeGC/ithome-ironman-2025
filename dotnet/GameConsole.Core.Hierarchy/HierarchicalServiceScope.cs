using System.Collections.Concurrent;
using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;

namespace GameConsole.Core.Hierarchy;

/// <summary>
/// Hierarchical service scope that supports parent-child relationships with service resolution fallback.
/// Implements thread-safe service management with weak references to prevent memory leaks.
/// </summary>
public sealed class HierarchicalServiceScope : IServiceHierarchy, IServiceRegistry
{
    private readonly ConcurrentDictionary<Type, object> _scopedServices = new();
    private readonly ConcurrentDictionary<Type, ServiceDescriptor> _localServices = new();
    private readonly ConcurrentSet<WeakReference<IServiceHierarchy>> _children = new();
    private readonly WeakReference<IServiceHierarchy>? _parentRef;
    private readonly IServiceProvider _rootProvider;
    private readonly ILogger<HierarchicalServiceScope>? _logger;
    private bool _disposed;
    private readonly object _disposeLock = new();

    /// <summary>
    /// Initializes a new root hierarchical service scope.
    /// </summary>
    /// <param name="rootProvider">The root service provider.</param>
    /// <param name="logger">Optional logger for service operations.</param>
    public HierarchicalServiceScope(IServiceProvider rootProvider, ILogger<HierarchicalServiceScope>? logger = null)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
        _logger = logger;
        _parentRef = null;
    }

    /// <summary>
    /// Initializes a new child hierarchical service scope.
    /// </summary>
    /// <param name="parent">The parent service hierarchy.</param>
    /// <param name="rootProvider">The root service provider.</param>
    /// <param name="logger">Optional logger for service operations.</param>
    internal HierarchicalServiceScope(IServiceHierarchy parent, IServiceProvider rootProvider, ILogger<HierarchicalServiceScope>? logger = null)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
        _logger = logger;
        _parentRef = new WeakReference<IServiceHierarchy>(parent ?? throw new ArgumentNullException(nameof(parent)));
    }

    /// <summary>
    /// Gets the parent service hierarchy, if any.
    /// </summary>
    public IServiceHierarchy? Parent
    {
        get
        {
            if (_parentRef?.TryGetTarget(out var parent) == true)
                return parent;
            return null;
        }
    }

    /// <summary>
    /// Gets the child service hierarchies.
    /// </summary>
    public IReadOnlyCollection<IServiceHierarchy> Children
    {
        get
        {
            var activeChildren = new List<IServiceHierarchy>();
            var deadRefs = new List<WeakReference<IServiceHierarchy>>();

            foreach (var childRef in _children)
            {
                if (childRef.TryGetTarget(out var child))
                {
                    activeChildren.Add(child);
                }
                else
                {
                    deadRefs.Add(childRef);
                }
            }

            // Clean up dead references
            foreach (var deadRef in deadRefs)
            {
                _children.TryRemove(deadRef);
            }

            return activeChildren.AsReadOnly();
        }
    }

    /// <summary>
    /// Creates a child service scope with this hierarchy as the parent.
    /// </summary>
    /// <returns>A new child service hierarchy.</returns>
    public IServiceHierarchy CreateChildScope()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        
        var child = new HierarchicalServiceScope(this, _rootProvider, _logger);
        _children.Add(new WeakReference<IServiceHierarchy>(child));
        
        _logger?.LogDebug("Created child scope with parent {ParentId}", GetHashCode());
        
        return child;
    }

    /// <summary>
    /// Creates a child service scope with this hierarchy as the parent and custom service registrations.
    /// </summary>
    /// <param name="configure">Action to configure services in the child scope.</param>
    /// <returns>A new child service hierarchy.</returns>
    public IServiceHierarchy CreateChildScope(Action<IServiceRegistry> configure)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        
        var child = new HierarchicalServiceScope(this, _rootProvider, _logger);
        configure?.Invoke(child);
        _children.Add(new WeakReference<IServiceHierarchy>(child));
        
        _logger?.LogDebug("Created configured child scope with parent {ParentId}", GetHashCode());
        
        return child;
    }

    /// <summary>
    /// Registers a service in this scope, potentially overriding parent registrations.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="lifetime">The service lifetime.</param>
    public void RegisterService<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TImplementation : class, TService
        where TService : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        
        var descriptor = new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime);
        _localServices.TryAdd(typeof(TService), descriptor);
        
        _logger?.LogDebug("Registered service {ServiceType} -> {ImplementationType} with lifetime {Lifetime}", 
            typeof(TService).Name, typeof(TImplementation).Name, lifetime);
    }

    /// <summary>
    /// Registers a service instance in this scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    public void RegisterInstance<TService>(TService instance) where TService : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        
        var descriptor = new ServiceDescriptor(typeof(TService), instance);
        _localServices.TryAdd(typeof(TService), descriptor);
        
        _logger?.LogDebug("Registered service instance {ServiceType}", typeof(TService).Name);
    }

    /// <summary>
    /// Checks if a service is registered in this hierarchy (including parent scopes).
    /// </summary>
    /// <param name="serviceType">The service type to check.</param>
    /// <returns>True if the service is registered in this scope or any parent scope.</returns>
    public bool IsRegistered(Type serviceType)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        
        // Check local registrations first
        if (_localServices.ContainsKey(serviceType))
            return true;

        // Check parent hierarchy
        return Parent?.IsRegistered(serviceType) ?? false;
    }

    /// <summary>
    /// Gets a service with hierarchy fallback resolution.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance or null if not found in hierarchy.</returns>
    public T? GetService<T>() where T : class
    {
        return (T?)GetService(typeof(T));
    }

    /// <summary>
    /// Gets a required service with hierarchy fallback resolution.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if service is not found in hierarchy.</exception>
    public T GetRequiredService<T>() where T : class
    {
        var service = GetService<T>();
        return service ?? throw new InvalidOperationException($"Required service of type {typeof(T).Name} was not found in the service hierarchy.");
    }

    /// <summary>
    /// Gets a service with hierarchy fallback resolution.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <returns>The service instance or null if not found in hierarchy.</returns>
    public object? GetService(Type serviceType)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        
        // Try local registration first (child overrides parent)
        if (_localServices.TryGetValue(serviceType, out var localDescriptor))
        {
            return GetOrCreateServiceInstance(serviceType, localDescriptor);
        }

        // Fallback to parent hierarchy
        if (Parent?.GetService(serviceType) is { } parentService)
        {
            return parentService;
        }

        // Finally, try root provider
        return _rootProvider.GetService(serviceType);
    }

    /// <summary>
    /// Gets or creates a service instance based on its descriptor and lifetime.
    /// </summary>
    private object GetOrCreateServiceInstance(Type serviceType, ServiceDescriptor descriptor)
    {
        return descriptor.Lifetime switch
        {
            ServiceLifetime.Singleton or ServiceLifetime.Scoped => GetOrCreateScopedInstance(serviceType, descriptor),
            ServiceLifetime.Transient => CreateTransientInstance(descriptor),
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor.Lifetime))
        };
    }

    /// <summary>
    /// Gets or creates a scoped service instance.
    /// </summary>
    private object GetOrCreateScopedInstance(Type serviceType, ServiceDescriptor descriptor)
    {
        return _scopedServices.GetOrAdd(serviceType, _ => CreateServiceInstance(descriptor));
    }

    /// <summary>
    /// Creates a transient service instance.
    /// </summary>
    private object CreateTransientInstance(ServiceDescriptor descriptor)
    {
        return CreateServiceInstance(descriptor);
    }

    /// <summary>
    /// Creates a service instance from its descriptor.
    /// </summary>
    private object CreateServiceInstance(ServiceDescriptor descriptor)
    {
        // Use the factory directly since ServiceDescriptor always has one
        return descriptor.Factory(this);
    }

    /// <summary>
    /// Disposes the service scope and all child scopes.
    /// </summary>
    public void Dispose()
    {
        lock (_disposeLock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        _logger?.LogDebug("Disposing hierarchical service scope {ScopeId}", GetHashCode());

        // Dispose children first (reverse hierarchy order)
        foreach (var child in Children.ToArray())
        {
            child.Dispose();
        }

        // Dispose local scoped services
        foreach (var service in _scopedServices.Values)
        {
            if (service is IDisposable disposable)
                disposable.Dispose();
        }

        _scopedServices.Clear();
        _localServices.Clear();
        _children.Clear();

        _logger?.LogDebug("Disposed hierarchical service scope {ScopeId}", GetHashCode());
    }

    /// <summary>
    /// Asynchronously disposes the service scope and all child scopes.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        lock (_disposeLock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        _logger?.LogDebug("Disposing hierarchical service scope {ScopeId} async", GetHashCode());

        // Dispose children first (reverse hierarchy order)
        foreach (var child in Children.ToArray())
        {
            await child.DisposeAsync();
        }

        // Dispose local scoped services
        foreach (var service in _scopedServices.Values)
        {
            if (service is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (service is IDisposable disposable)
                disposable.Dispose();
        }

        _scopedServices.Clear();
        _localServices.Clear();
        _children.Clear();

        _logger?.LogDebug("Disposed hierarchical service scope {ScopeId} async", GetHashCode());
    }

    #region IServiceRegistry Implementation

    public void Register(ServiceDescriptor descriptor)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        _localServices.TryAdd(descriptor.ServiceType, descriptor);
    }

    public void RegisterTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        RegisterService<TService, TImplementation>(ServiceLifetime.Transient);
    }

    public void RegisterTransient<TService>(Func<IServiceProvider, TService> factory) where TService : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        var descriptor = new ServiceDescriptor(typeof(TService), provider => factory(provider), ServiceLifetime.Transient);
        _localServices.TryAdd(typeof(TService), descriptor);
    }

    public void RegisterScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        RegisterService<TService, TImplementation>(ServiceLifetime.Scoped);
    }

    public void RegisterScoped<TService>(Func<IServiceProvider, TService> factory) where TService : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        var descriptor = new ServiceDescriptor(typeof(TService), provider => factory(provider), ServiceLifetime.Scoped);
        _localServices.TryAdd(typeof(TService), descriptor);
    }

    public void RegisterSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        RegisterService<TService, TImplementation>(ServiceLifetime.Singleton);
    }

    public void RegisterSingleton<TService>(TService instance) where TService : class
    {
        RegisterInstance(instance);
    }

    public void RegisterSingleton<TService>(Func<IServiceProvider, TService> factory) where TService : class
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        var descriptor = new ServiceDescriptor(typeof(TService), provider => factory(provider), ServiceLifetime.Singleton);
        _localServices.TryAdd(typeof(TService), descriptor);
    }

    public bool TryRegister(ServiceDescriptor descriptor)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        return _localServices.TryAdd(descriptor.ServiceType, descriptor);
    }

    public bool TryRegisterTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        var descriptor = new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient);
        return _localServices.TryAdd(typeof(TService), descriptor);
    }

    public bool TryRegisterScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        var descriptor = new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped);
        return _localServices.TryAdd(typeof(TService), descriptor);
    }

    public bool TryRegisterSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        var descriptor = new ServiceDescriptor(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton);
        return _localServices.TryAdd(typeof(TService), descriptor);
    }

    public void RegisterFromAttributes(System.Reflection.Assembly assembly, params string[] categories)
    {
        // Implementation would scan assembly for ServiceAttribute decorated types
        // This is a simplified version for now
        throw new NotImplementedException("Attribute-based registration will be implemented in a future version.");
    }

    public bool IsRegistered<TService>()
    {
        return IsRegistered(typeof(TService));
    }

    public IEnumerable<ServiceDescriptor> GetRegisteredServices()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HierarchicalServiceScope));
        return _localServices.Values.ToArray();
    }

    #endregion
}

/// <summary>
/// Thread-safe concurrent set implementation using ConcurrentDictionary.
/// </summary>
internal class ConcurrentSet<T> where T : notnull
{
    private readonly ConcurrentDictionary<T, byte> _dictionary = new();

    public void Add(T item) => _dictionary.TryAdd(item, 0);
    
    public bool TryRemove(T item) => _dictionary.TryRemove(item, out _);
    
    public void Clear() => _dictionary.Clear();
    
    public IEnumerable<T> ToArray() => _dictionary.Keys.ToArray();
    
    public IEnumerator<T> GetEnumerator() => _dictionary.Keys.GetEnumerator();
}
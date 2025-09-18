using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Core.Registry;

/// <summary>
/// Main service provider implementation with dependency injection, circular dependency detection, and performance optimization.
/// </summary>
public sealed class ServiceProvider : IServiceProvider, IServiceRegistry, IAsyncDisposable, IDisposable
{
    private readonly ConcurrentDictionary<Type, ServiceDescriptor> _services = new();
    private readonly ConcurrentDictionary<Type, object> _singletonInstances = new();
    private readonly ThreadLocal<HashSet<Type>> _resolutionPath = new(() => new HashSet<Type>());
    private readonly ILogger<ServiceProvider>? _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceProvider"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for service operations.</param>
    public ServiceProvider(ILogger<ServiceProvider>? logger = null)
    {
        _logger = logger;
        
        // Register self as IServiceProvider and IServiceRegistry
        RegisterSingleton<IServiceProvider>(this);
        RegisterSingleton<IServiceRegistry>(this);
    }

    #region IServiceProvider Implementation

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <param name="serviceType">The type of service to get.</param>
    /// <returns>The service instance, or null if not found.</returns>
    public object? GetService(Type serviceType)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceProvider));

        if (!_services.TryGetValue(serviceType, out var descriptor))
        {
            _logger?.LogTrace("Service {ServiceType} not registered", serviceType.Name);
            return null;
        }

        return CreateServiceInstance(descriptor);
    }

    /// <summary>
    /// Gets a required service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to get.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not registered.</exception>
    public T GetRequiredService<T>() where T : notnull
    {
        return (T)(GetService(typeof(T)) ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered"));
    }

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to get.</typeparam>
    /// <returns>The service instance, or null if not found.</returns>
    public T? GetService<T>() where T : class
    {
        return GetService(typeof(T)) as T;
    }

    /// <summary>
    /// Gets a service asynchronously and initializes it if it implements <see cref="IService"/>.
    /// </summary>
    /// <typeparam name="T">The type of service to get.</typeparam>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The initialized service instance.</returns>
    public async Task<T?> GetServiceAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        var service = GetService<T>();
        if (service is IService serviceLifecycle && !serviceLifecycle.IsRunning)
        {
            await serviceLifecycle.InitializeAsync(cancellationToken);
            await serviceLifecycle.StartAsync(cancellationToken);
        }
        return service;
    }

    /// <summary>
    /// Creates a service scope for scoped service management.
    /// </summary>
    /// <returns>A new service scope.</returns>
    public IServiceScope CreateScope()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceProvider));
        return new ServiceScope(this);
    }

    #endregion

    #region IServiceRegistry Implementation

    /// <summary>
    /// Registers a service descriptor.
    /// </summary>
    /// <param name="descriptor">The service descriptor.</param>
    public void Register(ServiceDescriptor descriptor)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceProvider));
        ArgumentNullException.ThrowIfNull(descriptor);

        _services.AddOrUpdate(descriptor.ServiceType, descriptor, (_, _) => descriptor);
        _logger?.LogDebug("Registered {ServiceType} with {Lifetime} lifetime", 
            descriptor.ServiceType.Name, descriptor.Lifetime);
    }

    /// <summary>
    /// Registers a transient service.
    /// </summary>
    public void RegisterTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        Register(ServiceDescriptor.Transient<TService, TImplementation>());
    }

    /// <summary>
    /// Registers a transient service with a factory.
    /// </summary>
    public void RegisterTransient<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Register(new ServiceDescriptor(typeof(TService), provider => factory(provider), ServiceLifetime.Transient));
    }

    /// <summary>
    /// Registers a scoped service.
    /// </summary>
    public void RegisterScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        Register(ServiceDescriptor.Scoped<TService, TImplementation>());
    }

    /// <summary>
    /// Registers a scoped service with a factory.
    /// </summary>
    public void RegisterScoped<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Register(new ServiceDescriptor(typeof(TService), provider => factory(provider), ServiceLifetime.Scoped));
    }

    /// <summary>
    /// Registers a singleton service.
    /// </summary>
    public void RegisterSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        Register(ServiceDescriptor.Singleton<TService, TImplementation>());
    }

    /// <summary>
    /// Registers a singleton service with an instance.
    /// </summary>
    public void RegisterSingleton<TService>(TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        Register(ServiceDescriptor.Singleton(instance));
    }

    /// <summary>
    /// Registers a singleton service with a factory.
    /// </summary>
    public void RegisterSingleton<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Register(ServiceDescriptor.Singleton(factory));
    }

    /// <summary>
    /// Tries to register a service descriptor if not already registered.
    /// </summary>
    public bool TryRegister(ServiceDescriptor descriptor)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceProvider));
        ArgumentNullException.ThrowIfNull(descriptor);

        if (_services.ContainsKey(descriptor.ServiceType))
            return false;

        Register(descriptor);
        return true;
    }

    /// <summary>
    /// Tries to register a transient service if not already registered.
    /// </summary>
    public bool TryRegisterTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return TryRegister(ServiceDescriptor.Transient<TService, TImplementation>());
    }

    /// <summary>
    /// Tries to register a scoped service if not already registered.
    /// </summary>
    public bool TryRegisterScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return TryRegister(ServiceDescriptor.Scoped<TService, TImplementation>());
    }

    /// <summary>
    /// Tries to register a singleton service if not already registered.
    /// </summary>
    public bool TryRegisterSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return TryRegister(ServiceDescriptor.Singleton<TService, TImplementation>());
    }

    /// <summary>
    /// Scans an assembly for types decorated with <see cref="ServiceAttribute"/> and registers them.
    /// </summary>
    public void RegisterFromAttributes(Assembly assembly, params string[] categories)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceProvider));
        ArgumentNullException.ThrowIfNull(assembly);

        var categorySet = categories.Length > 0 ? new HashSet<string>(categories) : null;

        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            var attribute = type.GetCustomAttribute<ServiceAttribute>();
            if (attribute == null) continue;

            // Filter by categories if specified
            if (categorySet != null && !attribute.Categories.Any(categorySet.Contains))
                continue;

            // Find service interface
            var serviceType = type.GetInterfaces().FirstOrDefault() ?? type;

            var descriptor = new ServiceDescriptor(serviceType, type, attribute.Lifetime);
            TryRegister(descriptor);

            _logger?.LogDebug("Auto-registered {ServiceType} -> {ImplementationType} from attribute", 
                serviceType.Name, type.Name);
        }
    }

    /// <summary>
    /// Checks if a service type is registered.
    /// </summary>
    public bool IsRegistered<TService>()
    {
        return IsRegistered(typeof(TService));
    }

    /// <summary>
    /// Checks if a service type is registered.
    /// </summary>
    public bool IsRegistered(Type serviceType)
    {
        return _services.ContainsKey(serviceType);
    }

    /// <summary>
    /// Gets all registered service descriptors.
    /// </summary>
    public IEnumerable<ServiceDescriptor> GetRegisteredServices()
    {
        return _services.Values.ToList();
    }

    #endregion

    #region Private Implementation

    /// <summary>
    /// Creates a service instance based on its descriptor and lifetime.
    /// </summary>
    private object CreateServiceInstance(ServiceDescriptor descriptor)
    {
        // Check for circular dependencies
        var resolutionPath = _resolutionPath.Value!;
        if (!resolutionPath.Add(descriptor.ServiceType))
        {
            var cycle = string.Join(" -> ", resolutionPath.Append(descriptor.ServiceType).Select(t => t.Name));
            _logger?.LogError("Circular dependency detected: {Cycle}", cycle);
            throw new InvalidOperationException($"Circular dependency detected: {cycle}");
        }

        try
        {
            return descriptor.Lifetime switch
            {
                ServiceLifetime.Singleton => GetOrCreateSingleton(descriptor),
                ServiceLifetime.Scoped => CreateScopedInstance(descriptor),
                ServiceLifetime.Transient => CreateTransientInstance(descriptor),
                _ => throw new ArgumentOutOfRangeException(nameof(descriptor.Lifetime))
            };
        }
        finally
        {
            resolutionPath.Remove(descriptor.ServiceType);
        }
    }

    /// <summary>
    /// Gets or creates a singleton instance.
    /// </summary>
    private object GetOrCreateSingleton(ServiceDescriptor descriptor)
    {
        return _singletonInstances.GetOrAdd(descriptor.ServiceType, _ =>
        {
            var instance = descriptor.Factory(this);
            _logger?.LogTrace("Created singleton instance of {ServiceType}", descriptor.ServiceType.Name);
            return instance;
        });
    }

    /// <summary>
    /// Creates a scoped instance (requires a service scope).
    /// </summary>
    private object CreateScopedInstance(ServiceDescriptor descriptor)
    {
        // For now, treat as singleton for the root container
        // In a proper scope, this would be handled by ServiceScope
        return GetOrCreateSingleton(descriptor);
    }

    /// <summary>
    /// Creates a transient instance.
    /// </summary>
    private object CreateTransientInstance(ServiceDescriptor descriptor)
    {
        var instance = descriptor.Factory(this);
        _logger?.LogTrace("Created transient instance of {ServiceType}", descriptor.ServiceType.Name);
        return instance;
    }

    #endregion

    #region IDisposable Implementation

    /// <summary>
    /// Disposes all singleton services synchronously.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var instance in _singletonInstances.Values)
        {
            if (instance is IDisposable disposable)
                disposable.Dispose();
        }

        _singletonInstances.Clear();
        _resolutionPath.Dispose();
    }

    /// <summary>
    /// Disposes all singleton services asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var instance in _singletonInstances.Values)
        {
            if (instance is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (instance is IDisposable disposable)
                disposable.Dispose();
        }

        _singletonInstances.Clear();
        _resolutionPath.Dispose();
    }

    #endregion
}
using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Registry;

/// <summary>
/// Interface for registering services in the dependency injection container.
/// Supports programmatic and attribute-based service registration.
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// Registers a service descriptor.
    /// </summary>
    /// <param name="descriptor">The service descriptor.</param>
    void Register(ServiceDescriptor descriptor);

    /// <summary>
    /// Registers a transient service.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    void RegisterTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Registers a transient service with a factory.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    void RegisterTransient<TService>(Func<IServiceProvider, TService> factory)
        where TService : class;

    /// <summary>
    /// Registers a scoped service.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    void RegisterScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Registers a scoped service with a factory.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    void RegisterScoped<TService>(Func<IServiceProvider, TService> factory)
        where TService : class;

    /// <summary>
    /// Registers a singleton service.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    void RegisterSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Registers a singleton service with an instance.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    void RegisterSingleton<TService>(TService instance)
        where TService : class;

    /// <summary>
    /// Registers a singleton service with a factory.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    void RegisterSingleton<TService>(Func<IServiceProvider, TService> factory)
        where TService : class;

    /// <summary>
    /// Tries to register a service descriptor if not already registered.
    /// </summary>
    /// <param name="descriptor">The service descriptor.</param>
    /// <returns>True if the service was registered, false if already exists.</returns>
    bool TryRegister(ServiceDescriptor descriptor);

    /// <summary>
    /// Tries to register a transient service if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>True if the service was registered, false if already exists.</returns>
    bool TryRegisterTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Tries to register a scoped service if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>True if the service was registered, false if already exists.</returns>
    bool TryRegisterScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Tries to register a singleton service if not already registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>True if the service was registered, false if already exists.</returns>
    bool TryRegisterSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService;

    /// <summary>
    /// Scans an assembly for types decorated with <see cref="ServiceAttribute"/> and registers them.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="categories">Optional categories to filter by.</param>
    void RegisterFromAttributes(System.Reflection.Assembly assembly, params string[] categories);

    /// <summary>
    /// Checks if a service type is registered.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <returns>True if the service is registered.</returns>
    bool IsRegistered<TService>();

    /// <summary>
    /// Checks if a service type is registered.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <returns>True if the service is registered.</returns>
    bool IsRegistered(Type serviceType);

    /// <summary>
    /// Gets all registered service descriptors.
    /// </summary>
    /// <returns>An enumerable of service descriptors.</returns>
    IEnumerable<ServiceDescriptor> GetRegisteredServices();
}
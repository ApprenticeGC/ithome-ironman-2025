using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Registry;

/// <summary>
/// Describes a service with its type, implementation, and lifetime information.
/// </summary>
public sealed class ServiceDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceDescriptor"/> class with a factory delegate.
    /// </summary>
    /// <param name="serviceType">The type of the service.</param>
    /// <param name="factory">The factory delegate to create service instances.</param>
    /// <param name="lifetime">The service lifetime.</param>
    public ServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Lifetime = lifetime;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceDescriptor"/> class with an implementation type.
    /// </summary>
    /// <param name="serviceType">The type of the service.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <param name="lifetime">The service lifetime.</param>
    public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
        Lifetime = lifetime;
        
        // Create factory delegate from implementation type
        Factory = serviceProvider => CreateInstance(implementationType, serviceProvider);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceDescriptor"/> class with a singleton instance.
    /// </summary>
    /// <param name="serviceType">The type of the service.</param>
    /// <param name="instance">The singleton instance.</param>
    public ServiceDescriptor(Type serviceType, object instance)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ArgumentNullException.ThrowIfNull(instance);
        ImplementationType = instance.GetType();
        Lifetime = ServiceLifetime.Singleton;
        Factory = _ => instance;
    }

    /// <summary>
    /// Gets the type of the service.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the implementation type, if available.
    /// </summary>
    public Type? ImplementationType { get; }

    /// <summary>
    /// Gets the service lifetime.
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Gets the factory delegate to create service instances.
    /// </summary>
    public Func<IServiceProvider, object> Factory { get; }

    /// <summary>
    /// Creates a service instance using reflection.
    /// </summary>
    /// <param name="implementationType">The implementation type.</param>
    /// <param name="serviceProvider">The service provider for dependency resolution.</param>
    /// <returns>The created service instance.</returns>
    private static object CreateInstance(Type implementationType, IServiceProvider serviceProvider)
    {
        var constructors = implementationType.GetConstructors();
        var constructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0) 
                         ?? constructors.OrderBy(c => c.GetParameters().Length).First();

        var parameters = constructor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = serviceProvider.GetService(parameters[i].ParameterType) 
                     ?? throw new InvalidOperationException($"Unable to resolve service for type {parameters[i].ParameterType}");
        }

        return Activator.CreateInstance(implementationType, args) 
               ?? throw new InvalidOperationException($"Unable to create instance of {implementationType}");
    }

    // Factory methods for convenience
    
    /// <summary>
    /// Creates a transient service descriptor.
    /// </summary>
    public static ServiceDescriptor Transient<TService, TImplementation>()
        where TImplementation : class, TService
        where TService : class
        => new(typeof(TService), typeof(TImplementation), ServiceLifetime.Transient);

    /// <summary>
    /// Creates a scoped service descriptor.
    /// </summary>
    public static ServiceDescriptor Scoped<TService, TImplementation>()
        where TImplementation : class, TService
        where TService : class
        => new(typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped);

    /// <summary>
    /// Creates a singleton service descriptor.
    /// </summary>
    public static ServiceDescriptor Singleton<TService, TImplementation>()
        where TImplementation : class, TService
        where TService : class
        => new(typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton);

    /// <summary>
    /// Creates a singleton service descriptor with an instance.
    /// </summary>
    public static ServiceDescriptor Singleton<TService>(TService instance)
        where TService : class
        => new(typeof(TService), instance);

    /// <summary>
    /// Creates a singleton service descriptor with a factory.
    /// </summary>
    public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
        => new(typeof(TService), provider => factory(provider), ServiceLifetime.Singleton);
}
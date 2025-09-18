using Pure.DI;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Core.Registry;

/// <summary>
/// Pure.DI composition root for hierarchical dependency injection with compile-time safety.
/// This provides a simplified integration with Pure.DI that complements the existing ServiceProvider.
/// </summary>
public partial class ServiceComposition
{
    private ServiceComposition? _parent;

    /// <summary>
    /// Sets the parent service provider for hierarchical container support.
    /// </summary>
    /// <param name="parent">The parent service provider.</param>
    public void SetParent(ServiceComposition parent) => _parent = parent;

    /// <summary>
    /// Gets the parent service provider.
    /// </summary>
    public ServiceComposition? Parent => _parent;

    /// <summary>
    /// Configure Pure.DI composition with minimal setup to avoid threading issues.
    /// This provides compile-time dependency validation and basic service registration.
    /// </summary>
    private static void Setup() => DI.Setup(nameof(ServiceComposition))
        // Keep it simple to avoid threading complexity
        .Bind<IServiceRegistry>().To<ServiceProvider>()
        .Root<IServiceRegistry>("Registry");

    /// <summary>
    /// Creates a child service composition for plugin or mode isolation.
    /// </summary>
    /// <returns>A new child service composition.</returns>
    public ServiceComposition CreateChild()
    {
        var child = new ServiceComposition();
        child.SetParent(this);
        return child;
    }

    /// <summary>
    /// Gets the service registry from Pure.DI composition.
    /// This provides compile-time validated dependency injection.
    /// </summary>
    /// <returns>The service registry instance.</returns>
    public IServiceRegistry GetServiceRegistry()
    {
        return this.Registry;
    }
}

/// <summary>
/// Extension methods to integrate Pure.DI with the existing ServiceProvider.
/// This provides hierarchical container support and compile-time validation.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Creates a Pure.DI enhanced service provider with hierarchical support.
    /// </summary>
    /// <param name="serviceProvider">The base service provider.</param>
    /// <param name="parent">Optional parent composition for hierarchical scoping.</param>
    /// <returns>A wrapper that provides Pure.DI integration.</returns>
    public static HierarchicalServiceProvider CreateHierarchical(
        this ServiceProvider serviceProvider, 
        ServiceComposition? parent = null)
    {
        return new HierarchicalServiceProvider(serviceProvider, parent);
    }
}

/// <summary>
/// Hierarchical service provider that combines the existing ServiceProvider with Pure.DI composition.
/// This provides the best of both worlds: existing functionality plus Pure.DI compile-time validation.
/// </summary>
public sealed class HierarchicalServiceProvider : IServiceProvider, IServiceRegistry, IAsyncDisposable
{
    private readonly ServiceProvider _baseProvider;
    private readonly ServiceComposition _composition;

    public HierarchicalServiceProvider(ServiceProvider baseProvider, ServiceComposition? parent = null)
    {
        _baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
        _composition = new ServiceComposition();
        
        if (parent != null)
            _composition.SetParent(parent);
    }

    #region IServiceProvider Implementation

    public object? GetService(Type serviceType)
    {
        // Try Pure.DI composition first for compile-time validated services
        try
        {
            var registry = _composition.GetServiceRegistry();
            if (registry.IsRegistered(serviceType) && registry is IServiceProvider provider)
            {
                return provider.GetService(serviceType);
            }
        }
        catch (InvalidOperationException)
        {
            // Service not in Pure.DI composition, fall through to base provider
        }

        // Fallback to existing service provider
        var service = _baseProvider.GetService(serviceType);
        if (service != null)
            return service;

        // Try parent composition if available
        var parentRegistry = _composition.Parent?.GetServiceRegistry();
        if (parentRegistry is IServiceProvider parentProvider)
        {
            var parentService = parentProvider.GetService(serviceType);
            if (parentService != null)
                return parentService;
        }
            
        return null;
    }

    #endregion

    #region IServiceRegistry Implementation

    public void Register(ServiceDescriptor descriptor) => _baseProvider.Register(descriptor);
    public void RegisterTransient<TService, TImplementation>() 
        where TService : class 
        where TImplementation : class, TService 
        => _baseProvider.RegisterTransient<TService, TImplementation>();
    
    public void RegisterTransient<TService>(Func<IServiceProvider, TService> factory) 
        where TService : class 
        => _baseProvider.RegisterTransient(factory);
    
    public void RegisterScoped<TService, TImplementation>() 
        where TService : class 
        where TImplementation : class, TService 
        => _baseProvider.RegisterScoped<TService, TImplementation>();
    
    public void RegisterScoped<TService>(Func<IServiceProvider, TService> factory) 
        where TService : class 
        => _baseProvider.RegisterScoped(factory);
    
    public void RegisterSingleton<TService, TImplementation>() 
        where TService : class 
        where TImplementation : class, TService 
        => _baseProvider.RegisterSingleton<TService, TImplementation>();
    
    public void RegisterSingleton<TService>(TService instance) 
        where TService : class 
        => _baseProvider.RegisterSingleton(instance);
    
    public void RegisterSingleton<TService>(Func<IServiceProvider, TService> factory) 
        where TService : class 
        => _baseProvider.RegisterSingleton(factory);
    
    public bool TryRegister(ServiceDescriptor descriptor) => _baseProvider.TryRegister(descriptor);
    public bool TryRegisterTransient<TService, TImplementation>() 
        where TService : class 
        where TImplementation : class, TService 
        => _baseProvider.TryRegisterTransient<TService, TImplementation>();
    
    public bool TryRegisterScoped<TService, TImplementation>() 
        where TService : class 
        where TImplementation : class, TService 
        => _baseProvider.TryRegisterScoped<TService, TImplementation>();
    
    public bool TryRegisterSingleton<TService, TImplementation>() 
        where TService : class 
        where TImplementation : class, TService 
        => _baseProvider.TryRegisterSingleton<TService, TImplementation>();
    
    public void RegisterFromAttributes(System.Reflection.Assembly assembly, params string[] categories) 
        => _baseProvider.RegisterFromAttributes(assembly, categories);
    
    public bool IsRegistered<TService>() => _baseProvider.IsRegistered<TService>();
    public bool IsRegistered(Type serviceType) => _baseProvider.IsRegistered(serviceType);
    public IEnumerable<ServiceDescriptor> GetRegisteredServices() => _baseProvider.GetRegisteredServices();

    #endregion

    #region Hierarchical Container Support

    /// <summary>
    /// Creates a child hierarchical container for plugin or mode isolation.
    /// </summary>
    /// <returns>A new child container.</returns>
    public HierarchicalServiceProvider CreateChild()
    {
        var childComposition = _composition.CreateChild();
        var childProvider = new ServiceProvider();
        return new HierarchicalServiceProvider(childProvider, childComposition);
    }

    /// <summary>
    /// Gets the Pure.DI composition for advanced scenarios.
    /// </summary>
    public ServiceComposition Composition => _composition;

    #endregion

    #region IAsyncDisposable Implementation

    public async ValueTask DisposeAsync()
    {
        await _baseProvider.DisposeAsync();
    }

    #endregion
}
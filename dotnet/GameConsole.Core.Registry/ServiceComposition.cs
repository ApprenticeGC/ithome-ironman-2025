using Pure.DI;
using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Registry;

/// <summary>
/// Pure.DI service composition root for hierarchical dependency injection.
/// Provides compile-time dependency injection with hierarchical scoping support.
/// </summary>
public partial class ServiceComposition : IServiceProvider
{
    private IServiceProvider? _parent;

    /// <summary>
    /// Sets the parent container for hierarchical service resolution.
    /// </summary>
    /// <param name="parent">The parent service provider.</param>
    public void SetParent(IServiceProvider parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Gets the parent service provider.
    /// </summary>
    public IServiceProvider? Parent => _parent;

    /// <summary>
    /// Pure.DI setup configuration for service composition.
    /// </summary>
    private static void Setup() => DI.Setup(nameof(ServiceComposition))
        // Performance hints
        .Hint(Hint.Resolve, "Off")           // Disable automatic resolution
        .Hint(Hint.ThreadSafe, "On")         // Enable thread-safe code generation
        
        // Default service lifetime policies
        .DefaultLifetime(Pure.DI.Lifetime.Transient)
        
        // Root composition for ServiceProvider itself
        .Root<ServiceComposition>("Root");

    /// <summary>
    /// Resolves a service with fallback to parent container.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve.</param>
    /// <returns>The service instance or null if not found.</returns>
    public object? GetService(Type serviceType)
    {
        // Handle IServiceProvider requests directly
        if (serviceType == typeof(IServiceProvider))
            return this;

        // Fallback to parent container for any other service
        return _parent?.GetService(serviceType);
    }

    /// <summary>
    /// Gets a required service with fallback to parent container.
    /// </summary>
    /// <param name="serviceType">The type of service to resolve.</param>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when service cannot be resolved.</exception>
    public object GetRequiredService(Type serviceType)
    {
        return GetService(serviceType) 
            ?? throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
    }

    /// <summary>
    /// Creates a new child scope for scoped service management.
    /// </summary>
    /// <returns>A new service scope.</returns>
    public IServiceScope CreateScope()
    {
        return new ServiceScope(this);
    }
}

/// <summary>
/// Service lifetime management policies for Pure.DI configuration.
/// </summary>
internal static class ServiceLifetimePolicies
{
    /// <summary>
    /// Gets the Pure.DI lifetime for singleton services.
    /// Singleton services are created once per container.
    /// </summary>
    public static Pure.DI.Lifetime Singleton => Pure.DI.Lifetime.Singleton;

    /// <summary>
    /// Gets the Pure.DI lifetime for scoped services.
    /// Scoped services are created once per scope.
    /// </summary>
    public static Pure.DI.Lifetime Scoped => Pure.DI.Lifetime.Scoped;

    /// <summary>
    /// Gets the Pure.DI lifetime for transient services.
    /// Transient services are created every time they are requested.
    /// </summary>
    public static Pure.DI.Lifetime Transient => Pure.DI.Lifetime.Transient;

    /// <summary>
    /// Determines the appropriate lifetime based on service characteristics.
    /// </summary>
    /// <param name="serviceType">The service type to analyze.</param>
    /// <returns>The recommended lifetime for the service.</returns>
    public static Pure.DI.Lifetime DetermineLifetime(Type serviceType)
    {
        // Stateless services default to transient
        if (serviceType.GetInterfaces().Any(i => i.Name.Contains("Stateless")))
            return Transient;

        // Services implementing IDisposable typically should be scoped
        if (typeof(IDisposable).IsAssignableFrom(serviceType))
            return Scoped;

        // Configuration and infrastructure services are typically singleton
        if (serviceType.Name.Contains("Configuration") || 
            serviceType.Name.Contains("Logger") ||
            serviceType.Name.Contains("Registry"))
            return Singleton;

        // Default to transient for safety
        return Transient;
    }
}
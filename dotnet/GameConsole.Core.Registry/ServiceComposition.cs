using Pure.DI;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

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
        
        // Core service bindings with different lifetimes to demonstrate hierarchical scoping
        .Bind<IExampleSingletonService>().As(Pure.DI.Lifetime.Singleton).To<ExampleSingletonService>()
        .Bind<IExampleScopedService>().As(Pure.DI.Lifetime.Scoped).To<ExampleScopedService>()
        .Bind<IExampleTransientService>().As(Pure.DI.Lifetime.Transient).To<ExampleTransientService>()
        
        // Example services with dependencies to demonstrate compile-time validation
        .Bind<IExampleServiceWithDependency>().To<ExampleServiceWithDependency>()
        .Bind<IExampleDependency>().To<ExampleDependency>()
        
        // Root composition - expose example services for resolution and Pure.DI container itself
        .Root<IExampleSingletonService>("ExampleSingletonService")
        .Root<IExampleScopedService>("ExampleScopedService") 
        .Root<IExampleTransientService>("ExampleTransientService")
        .Root<IExampleServiceWithDependency>("ExampleServiceWithDependency")
        .Root<IExampleDependency>("ExampleDependency")
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

        // Try to resolve from Pure.DI first for bound services
        try
        {
            // Pure.DI will generate specific resolution code for bound services
            if (serviceType == typeof(IExampleSingletonService))
                return ExampleSingletonService;
            if (serviceType == typeof(IExampleScopedService))
                return ExampleScopedService;
            if (serviceType == typeof(IExampleTransientService))
                return ExampleTransientService;
            if (serviceType == typeof(IExampleServiceWithDependency))
                return ExampleServiceWithDependency;
            if (serviceType == typeof(IExampleDependency))
                return ExampleDependency;
        }
        catch
        {
            // If Pure.DI resolution fails, fall back to parent
        }

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

// Example services for Pure.DI demonstration and compile-time validation

/// <summary>
/// Example singleton service interface for Pure.DI demonstration.
/// </summary>
public interface IExampleSingletonService
{
    string GetMessage();
}

/// <summary>
/// Example scoped service interface for Pure.DI demonstration.
/// </summary>
public interface IExampleScopedService
{
    string GetScopedMessage();
}

/// <summary>
/// Example transient service interface for Pure.DI demonstration.
/// </summary>
public interface IExampleTransientService
{
    string GetTransientMessage();
}

/// <summary>
/// Example service with dependency to demonstrate compile-time validation.
/// </summary>
public interface IExampleServiceWithDependency
{
    string GetMessageWithDependency();
}

/// <summary>
/// Example dependency interface for compile-time validation.
/// </summary>
public interface IExampleDependency
{
    string GetDependencyMessage();
}

/// <summary>
/// Example singleton service implementation.
/// </summary>
public class ExampleSingletonService : IExampleSingletonService
{
    public string GetMessage() => "Singleton service instance";
}

/// <summary>
/// Example scoped service implementation.
/// </summary>
public class ExampleScopedService : IExampleScopedService
{
    public string GetScopedMessage() => "Scoped service instance";
}

/// <summary>
/// Example transient service implementation.
/// </summary>
public class ExampleTransientService : IExampleTransientService
{
    public string GetTransientMessage() => "Transient service instance";
}

/// <summary>
/// Example service with dependency implementation.
/// </summary>
public class ExampleServiceWithDependency : IExampleServiceWithDependency
{
    private readonly IExampleDependency _dependency;

    public ExampleServiceWithDependency(IExampleDependency dependency)
    {
        _dependency = dependency;
    }

    public string GetMessageWithDependency() => $"Service with dependency: {_dependency.GetDependencyMessage()}";
}

/// <summary>
/// Example dependency implementation.
/// </summary>
public class ExampleDependency : IExampleDependency
{
    public string GetDependencyMessage() => "Dependency message";
}
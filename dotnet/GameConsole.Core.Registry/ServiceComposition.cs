using Pure.DI;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Core.Registry;

/// <summary>
/// Pure.DI composition root for hierarchical dependency injection with compile-time safety.
/// Provides hierarchical service scoping, circular dependency detection, and performance optimization.
/// </summary>
public partial class ServiceComposition : IServiceProvider
{
    private ServiceComposition? _parent;
    private readonly object _lockM09D18di = new();  // Pure.DI generated lock field

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
    /// Configure Pure.DI composition with hierarchical scoping and compile-time validation.
    /// </summary>
    private void Setup() => DI.Setup(nameof(ServiceComposition))
        .Hint(Hint.ThreadSafe, "Off")  // Disable thread safety to avoid lock issues
        
        // Core infrastructure registrations
        .Bind<IServiceRegistry>().As(Lifetime.Singleton).To<ServiceProvider>()
        
        // Logging infrastructure
        .Bind<ILogger<ServiceProvider>>().As(Lifetime.Singleton).To<LoggerStub<ServiceProvider>>()
        
        // Root composition for main container
        .Root<IServiceRegistry>("Registry");

    /// <summary>
    /// Gets a service of the specified type, with fallback to parent container.
    /// This provides the IServiceProvider interface for hierarchical resolution.
    /// </summary>
    /// <param name="serviceType">The type of service to get.</param>
    /// <returns>The service instance, or null if not found.</returns>
    public object? GetService(Type serviceType)
    {
        try
        {
            // Try to resolve from Pure.DI first
            if (serviceType == typeof(IServiceRegistry))
                return this.Registry;
                
            // For other types, delegate to the registry
            var registry = this.Registry;
            if (registry is IServiceProvider provider)
                return provider.GetService(serviceType);
        }
        catch (InvalidOperationException)
        {
            // Service not registered in this composition
        }

        // Fallback to parent container if available
        return _parent?.GetService(serviceType);
    }

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
}

/// <summary>
/// Stub logger implementation for when no logging provider is available.
/// </summary>
/// <typeparam name="T">The logger category type.</typeparam>
internal sealed class LoggerStub<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
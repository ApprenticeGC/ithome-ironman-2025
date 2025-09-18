using System.Collections.Concurrent;

namespace GameConsole.Core.Registry;

/// <summary>
/// Manages scoped service instances and their disposal.
/// </summary>
internal sealed class ServiceScope : IServiceScope, IAsyncDisposable
{
    private readonly ConcurrentDictionary<Type, object> _scopedServices = new();
    private readonly IServiceProvider _rootProvider;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceScope"/> class.
    /// </summary>
    /// <param name="rootProvider">The root service provider.</param>
    public ServiceScope(IServiceProvider rootProvider)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
        ServiceProvider = new ScopedServiceProvider(this, rootProvider);
    }

    /// <summary>
    /// Gets the service provider for this scope.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets or creates a scoped service instance.
    /// </summary>
    /// <param name="serviceType">The service type.</param>
    /// <param name="factory">The factory to create the service.</param>
    /// <returns>The service instance.</returns>
    public object GetOrCreateScopedService(Type serviceType, Func<IServiceProvider, object> factory)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(ServiceScope));
        
        return _scopedServices.GetOrAdd(serviceType, _ => factory(ServiceProvider));
    }

    /// <summary>
    /// Disposes all scoped services synchronously.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var service in _scopedServices.Values)
        {
            if (service is IDisposable disposable)
                disposable.Dispose();
        }

        _scopedServices.Clear();
    }

    /// <summary>
    /// Disposes all scoped services asynchronously.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var service in _scopedServices.Values)
        {
            if (service is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            else if (service is IDisposable disposable)
                disposable.Dispose();
        }

        _scopedServices.Clear();
    }
}

/// <summary>
/// Service provider that provides scoped services from a specific scope.
/// </summary>
internal sealed class ScopedServiceProvider : IServiceProvider
{
    private readonly ServiceScope _scope;
    private readonly IServiceProvider _rootProvider;

    public ScopedServiceProvider(ServiceScope scope, IServiceProvider rootProvider)
    {
        _scope = scope;
        _rootProvider = rootProvider;
    }

    public object? GetService(Type serviceType)
    {
        return _rootProvider.GetService(serviceType);
    }
}

/// <summary>
/// Interface for service scopes (compatible with Microsoft.Extensions.DependencyInjection).
/// </summary>
public interface IServiceScope : IDisposable
{
    /// <summary>
    /// Gets the service provider for this scope.
    /// </summary>
    IServiceProvider ServiceProvider { get; }
}
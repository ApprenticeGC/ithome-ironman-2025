using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;

namespace GameConsole.Core.Hierarchy;

/// <summary>
/// Interface for managing hierarchical service relationships with parent-child scope support.
/// </summary>
public interface IServiceHierarchy : IServiceProvider, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the parent service hierarchy, if any.
    /// </summary>
    IServiceHierarchy? Parent { get; }

    /// <summary>
    /// Gets the child service hierarchies.
    /// </summary>
    IReadOnlyCollection<IServiceHierarchy> Children { get; }

    /// <summary>
    /// Creates a child service scope with this hierarchy as the parent.
    /// </summary>
    /// <returns>A new child service hierarchy.</returns>
    IServiceHierarchy CreateChildScope();

    /// <summary>
    /// Creates a child service scope with this hierarchy as the parent and custom service registrations.
    /// </summary>
    /// <param name="configure">Action to configure services in the child scope.</param>
    /// <returns>A new child service hierarchy.</returns>
    IServiceHierarchy CreateChildScope(Action<IServiceRegistry> configure);

    /// <summary>
    /// Registers a service in this scope, potentially overriding parent registrations.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <param name="lifetime">The service lifetime.</param>
    void RegisterService<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TImplementation : class, TService
        where TService : class;

    /// <summary>
    /// Registers a service instance in this scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    void RegisterInstance<TService>(TService instance) where TService : class;

    /// <summary>
    /// Checks if a service is registered in this hierarchy (including parent scopes).
    /// </summary>
    /// <param name="serviceType">The service type to check.</param>
    /// <returns>True if the service is registered in this scope or any parent scope.</returns>
    bool IsRegistered(Type serviceType);

    /// <summary>
    /// Gets a service with hierarchy fallback resolution.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance or null if not found in hierarchy.</returns>
    T? GetService<T>() where T : class;

    /// <summary>
    /// Gets a required service with hierarchy fallback resolution.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if service is not found in hierarchy.</exception>
    T GetRequiredService<T>() where T : class;
}
using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using Microsoft.Extensions.Logging;

namespace GameConsole.Core.Hierarchy;

/// <summary>
/// Fluent builder for constructing hierarchical service trees with parent-child relationships.
/// </summary>
public sealed class HierarchyBuilder
{
    private readonly IServiceProvider _rootProvider;
    private readonly ILogger<HierarchicalServiceScope>? _logger;
    private readonly List<Action<IServiceRegistry>> _rootConfigurations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HierarchyBuilder"/> class.
    /// </summary>
    /// <param name="rootProvider">The root service provider.</param>
    /// <param name="logger">Optional logger for service operations.</param>
    public HierarchyBuilder(IServiceProvider rootProvider, ILogger<HierarchicalServiceScope>? logger = null)
    {
        _rootProvider = rootProvider ?? throw new ArgumentNullException(nameof(rootProvider));
        _logger = logger;
    }

    /// <summary>
    /// Configures services in the root scope.
    /// </summary>
    /// <param name="configure">Action to configure root services.</param>
    /// <returns>The hierarchy builder for method chaining.</returns>
    public HierarchyBuilder ConfigureRoot(Action<IServiceRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _rootConfigurations.Add(configure);
        return this;
    }

    /// <summary>
    /// Adds a transient service to the root scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The hierarchy builder for method chaining.</returns>
    public HierarchyBuilder AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return ConfigureRoot(registry => registry.RegisterTransient<TService, TImplementation>());
    }

    /// <summary>
    /// Adds a transient service with factory to the root scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    /// <returns>The hierarchy builder for method chaining.</returns>
    public HierarchyBuilder AddTransient<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return ConfigureRoot(registry => registry.RegisterTransient(factory));
    }

    /// <summary>
    /// Adds a scoped service to the root scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The hierarchy builder for method chaining.</returns>
    public HierarchyBuilder AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return ConfigureRoot(registry => registry.RegisterScoped<TService, TImplementation>());
    }

    /// <summary>
    /// Adds a scoped service with factory to the root scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    /// <returns>The hierarchy builder for method chaining.</returns>
    public HierarchyBuilder AddScoped<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return ConfigureRoot(registry => registry.RegisterScoped(factory));
    }

    /// <summary>
    /// Adds a singleton service to the root scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The hierarchy builder for method chaining.</returns>
    public HierarchyBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return ConfigureRoot(registry => registry.RegisterSingleton<TService, TImplementation>());
    }

    /// <summary>
    /// Adds a singleton service instance to the root scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    /// <returns>The hierarchy builder for method chaining.</returns>
    public HierarchyBuilder AddSingleton<TService>(TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        return ConfigureRoot(registry => registry.RegisterSingleton(instance));
    }

    /// <summary>
    /// Adds a singleton service with factory to the root scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    /// <returns>The hierarchy builder for method chaining.</returns>
    public HierarchyBuilder AddSingleton<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return ConfigureRoot(registry => registry.RegisterSingleton(factory));
    }

    /// <summary>
    /// Builds the hierarchical service scope with configured services.
    /// </summary>
    /// <returns>The root hierarchical service scope.</returns>
    public IServiceHierarchy Build()
    {
        var rootScope = new HierarchicalServiceScope(_rootProvider, _logger);

        // Apply all root configurations
        foreach (var configuration in _rootConfigurations)
        {
            configuration(rootScope);
        }

        return rootScope;
    }

    /// <summary>
    /// Creates a child scope builder for the given parent scope.
    /// </summary>
    /// <param name="parent">The parent service hierarchy.</param>
    /// <returns>A child scope builder.</returns>
    public static ChildScopeBuilder CreateChild(IServiceHierarchy parent)
    {
        ArgumentNullException.ThrowIfNull(parent);
        return new ChildScopeBuilder(parent);
    }
}

/// <summary>
/// Fluent builder for creating child scopes with specific configurations.
/// </summary>
public sealed class ChildScopeBuilder
{
    private readonly IServiceHierarchy _parent;
    private readonly List<Action<IServiceRegistry>> _configurations = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ChildScopeBuilder"/> class.
    /// </summary>
    /// <param name="parent">The parent service hierarchy.</param>
    internal ChildScopeBuilder(IServiceHierarchy parent)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /// <summary>
    /// Configures services in the child scope.
    /// </summary>
    /// <param name="configure">Action to configure child services.</param>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder Configure(Action<IServiceRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configurations.Add(configure);
        return this;
    }

    /// <summary>
    /// Adds a transient service to the child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder AddTransient<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return Configure(registry => registry.RegisterTransient<TService, TImplementation>());
    }

    /// <summary>
    /// Adds a transient service with factory to the child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder AddTransient<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Configure(registry => registry.RegisterTransient(factory));
    }

    /// <summary>
    /// Adds a scoped service to the child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder AddScoped<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return Configure(registry => registry.RegisterScoped<TService, TImplementation>());
    }

    /// <summary>
    /// Adds a scoped service with factory to the child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder AddScoped<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Configure(registry => registry.RegisterScoped(factory));
    }

    /// <summary>
    /// Adds a singleton service to the child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The implementation type.</typeparam>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder AddSingleton<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        return Configure(registry => registry.RegisterSingleton<TService, TImplementation>());
    }

    /// <summary>
    /// Adds a singleton service instance to the child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder AddSingleton<TService>(TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        return Configure(registry => registry.RegisterSingleton(instance));
    }

    /// <summary>
    /// Adds a singleton service with factory to the child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder AddSingleton<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Configure(registry => registry.RegisterSingleton(factory));
    }

    /// <summary>
    /// Overrides a parent service registration in this child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <typeparam name="TImplementation">The new implementation type.</typeparam>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder Override<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TService : class
        where TImplementation : class, TService
    {
        return Configure(registry => 
        {
            if (registry is HierarchicalServiceScope hierarchical)
            {
                hierarchical.RegisterService<TService, TImplementation>(lifetime);
            }
            else
            {
                // Fallback to standard registration methods
                switch (lifetime)
                {
                    case ServiceLifetime.Transient:
                        registry.RegisterTransient<TService, TImplementation>();
                        break;
                    case ServiceLifetime.Scoped:
                        registry.RegisterScoped<TService, TImplementation>();
                        break;
                    case ServiceLifetime.Singleton:
                        registry.RegisterSingleton<TService, TImplementation>();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(lifetime));
                }
            }
        });
    }

    /// <summary>
    /// Overrides a parent service registration with an instance in this child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder Override<TService>(TService instance) where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        return Configure(registry => registry.RegisterSingleton(instance));
    }

    /// <summary>
    /// Overrides a parent service registration with a factory in this child scope.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <param name="factory">The factory delegate.</param>
    /// <param name="lifetime">The service lifetime.</param>
    /// <returns>The child scope builder for method chaining.</returns>
    public ChildScopeBuilder Override<TService>(Func<IServiceProvider, TService> factory, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Configure(registry => 
        {
            switch (lifetime)
            {
                case ServiceLifetime.Transient:
                    registry.RegisterTransient(factory);
                    break;
                case ServiceLifetime.Scoped:
                    registry.RegisterScoped(factory);
                    break;
                case ServiceLifetime.Singleton:
                    registry.RegisterSingleton(factory);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime));
            }
        });
    }

    /// <summary>
    /// Builds the child service scope with configured services.
    /// </summary>
    /// <returns>The child service hierarchy.</returns>
    public IServiceHierarchy Build()
    {
        return _parent.CreateChildScope(registry =>
        {
            foreach (var configuration in _configurations)
            {
                configuration(registry);
            }
        });
    }

    /// <summary>
    /// Creates a nested child scope builder for the current child scope being built.
    /// </summary>
    /// <returns>A nested child scope builder.</returns>
    public ChildScopeBuilder CreateChild()
    {
        var childScope = Build();
        return new ChildScopeBuilder(childScope);
    }
}
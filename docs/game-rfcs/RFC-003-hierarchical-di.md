# RFC-003: Hierarchical Dependency Injection with Pure.DI

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-001, RFC-002

## Summary

This RFC defines the hierarchical dependency injection strategy for GameConsole using Pure.DI with Microsoft.Extensions.DI compatibility. The system supports true child containers for plugin isolation, mode-specific service scoping, and Unity integration via VContainer bridge.

## Motivation

GameConsole requires sophisticated dependency injection that supports:

1. **Hierarchical Containers**: Child containers for plugins and modes
2. **Plugin Isolation**: Each plugin gets its own service scope
3. **Mode-Based Scoping**: Different service sets for Game vs Editor modes
4. **Unity Integration**: Bridge to VContainer for Unity projects
5. **Performance**: Zero-allocation service resolution
6. **Compatibility**: Microsoft.Extensions.DI API surface

Microsoft.Extensions.DI lacks hierarchical container support, making plugin isolation and mode switching difficult. Pure.DI provides compile-time hierarchical containers with excellent performance.

## Detailed Design

### Container Hierarchy

```
Root Container (Host)
├── Core Services (Infrastructure, Configuration)
├── Game Mode Container
│   ├── Game-specific services
│   ├── Game plugins
│   └── Runtime AI profile services
├── Editor Mode Container
│   ├── Editor-specific services
│   ├── Editor plugins
│   └── Editor AI profile services
└── Plugin Containers (per plugin)
    ├── Plugin-specific services
    ├── Plugin dependencies
    └── Isolated dependency graphs
```

### Pure.DI Integration

#### Core Composition Root
```csharp
// GameConsole.Infrastructure.DI/src/DIComposition.cs
using Pure.DI;

[CompositionRoot]
public partial class DIComposition : IServiceProvider
{
    private IServiceProvider? _parent;

    public void SetParent(IServiceProvider parent) => _parent = parent;

    private void Setup() => DI.Setup(nameof(DIComposition))
        .Hint(Hint.Resolve, "Off")
        .Hint(Hint.ThreadSafe, "On")
        .Hint(Hint.OnCannotResolve, "On") // Fallback to parent

        // Core infrastructure services (always available)
        .Bind<IConfiguration>().To<Configuration>()
        .Bind<ILogger<TT>>().To<Logger<TT>>()
        .Bind<IHostEnvironment>().To<HostEnvironment>()

        // Service registries for provider selection
        .Bind<IServiceRegistry<IGameEngine>>().To<ServiceRegistry<IGameEngine>>()
        .Bind<IServiceRegistry<IAudioService>>().To<ServiceRegistry<IAudioService>>()
        .Bind<IServiceRegistry<IInputService>>().To<ServiceRegistry<IInputService>>()

        // Generated service proxies
        .Bind<IGameEngine>().To<GameEngineProxy>()
        .Bind<IAudioService>().To<AudioServiceProxy>()
        .Bind<IInputService>().To<InputServiceProxy>()

        // Plugin management services
        .Bind<IPluginManager>().To<PluginManager>()
        .Bind<IPluginLoader>().To<AssemblyContextPluginLoader>()

        // Parent container fallback
        .Bind<IServiceProvider>().To(() => _parent ?? this.Root)

        .Root<IServiceProvider>("Root");

    // Resolve method with parent fallback
    object? IServiceProvider.GetService(Type serviceType)
    {
        try
        {
            return Root.GetService(serviceType);
        }
        catch when (_parent != null)
        {
            return _parent.GetService(serviceType);
        }
    }
}
```

#### MS Extensions DI Bridge
```csharp
// GameConsole.Infrastructure.DI/src/PureDiExtensions.cs
using Microsoft.Extensions.DependencyInjection;

public static class PureDiExtensions
{
    /// <summary>
    /// Register Pure.DI composition as the service provider
    /// </summary>
    public static IServiceCollection AddPureDI(this IServiceCollection services)
    {
        return services.AddSingleton<IServiceProvider>(provider =>
        {
            var composition = new DIComposition();
            // Bridge existing registrations from IServiceCollection
            composition.ImportServices(services);
            return composition.Root;
        });
    }

    /// <summary>
    /// Create hierarchical child container (VContainer-style)
    /// </summary>
    public static IServiceProvider CreateChildContainer(
        this IServiceProvider parent,
        Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);

        var childComposition = new DIComposition();
        childComposition.SetParent(parent);
        childComposition.ImportServices(services);

        return childComposition.Root;
    }

    /// <summary>
    /// Register service with Pure.DI composition
    /// </summary>
    public static IServiceCollection AddPureDIScoped<TInterface, TImplementation>(
        this IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        return services.AddSingleton<TInterface>(provider =>
        {
            var composition = provider.GetRequiredService<DIComposition>();
            return composition.ResolveScoped<TImplementation>();
        });
    }
}
```

### Mode-Specific Container Factory

```csharp
// GameConsole.Infrastructure.DI/src/ContainerFactory.cs
public static class ContainerFactory
{
    /// <summary>
    /// Create container for Game mode with game-specific services
    /// </summary>
    public static IServiceProvider CreateGameModeContainer(
        IServiceProvider rootContainer,
        GameModeProfile profile)
    {
        return rootContainer.CreateChildContainer(services =>
        {
            // Game-specific services
            services.AddSingleton<IGameDirector, GameDirectorService>();
            services.AddSingleton<IPlayerService, PlayerService>();
            services.AddSingleton<IGameState, GameStateService>();

            // AI services with runtime profile (tight token budget)
            services.Configure<AIProfile>(opt => profile.AIProfile.CopyTo(opt));
            services.AddSingleton<IAgentDirector, RuntimeAgentDirector>();

            // Load game mode plugins
            foreach (var plugin in profile.EnabledPlugins)
            {
                services.AddPlugin(plugin);
            }

            // Override core services with game-optimized versions
            services.Replace(ServiceDescriptor.Singleton<ILogger, GameOptimizedLogger>());
        });
    }

    /// <summary>
    /// Create container for Editor mode with editor-specific services
    /// </summary>
    public static IServiceProvider CreateEditorModeContainer(
        IServiceProvider rootContainer,
        EditorModeProfile profile)
    {
        return rootContainer.CreateChildContainer(services =>
        {
            // Editor-specific services
            services.AddSingleton<IAssetImporter, AssetImporterService>();
            services.AddSingleton<ISceneEditor, SceneEditorService>();
            services.AddSingleton<IProjectManager, ProjectManagerService>();

            // AI services with editor profile (rich context, more tools)
            services.Configure<AIProfile>(opt => profile.AIProfile.CopyTo(opt));
            services.AddSingleton<IDialogueAgent, DialogueAuthoringAgent>();
            services.AddSingleton<IAssetAnalysisAgent, AssetAnalysisAgent>();

            // Load editor mode plugins
            foreach (var plugin in profile.EnabledPlugins)
            {
                services.AddPlugin(plugin);
            }

            // Override core services with editor-optimized versions
            services.Replace(ServiceDescriptor.Singleton<ILogger, EditorDetailedLogger>());
        });
    }

    /// <summary>
    /// Create isolated container for individual plugin
    /// </summary>
    public static IServiceProvider CreatePluginContainer(
        IServiceProvider parentContainer,
        PluginMetadata plugin)
    {
        return parentContainer.CreateChildContainer(services =>
        {
            // Plugin-specific services in isolated context
            services.AddPluginServices(plugin);
            services.AddPluginDependencies(plugin);

            // Plugin configuration
            services.Configure<PluginConfig>(plugin.Name, opt => plugin.Configuration.CopyTo(opt));

            // Plugin logging with category
            services.AddSingleton<ILogger>(provider =>
                provider.GetRequiredService<ILoggerFactory>().CreateLogger(plugin.Name));
        });
    }
}
```

### Unity Integration (VContainer Bridge)

```csharp
// GameConsole.Infrastructure.DI/src/VContainerBridge.cs
#if UNITY_EDITOR || UNITY_STANDALONE
using VContainer;

/// <summary>
/// Bridge Pure.DI services into VContainer for Unity projects
/// </summary>
public static class VContainerBridge
{
    /// <summary>
    /// Register Pure.DI services in VContainer
    /// </summary>
    public static ContainerBuilder AddPureDI(
        this ContainerBuilder builder,
        IServiceProvider pureDiContainer)
    {
        // Register the Pure.DI container itself
        builder.RegisterInstance(pureDiContainer).AsImplementedInterfaces();

        // Bridge common GameConsole services
        builder.Register<IGameEngine>(container =>
            pureDiContainer.GetRequiredService<IGameEngine>(), Lifetime.Singleton);

        builder.Register<IAudioService>(container =>
            pureDiContainer.GetRequiredService<IAudioService>(), Lifetime.Singleton);

        builder.Register<IInputService>(container =>
            pureDiContainer.GetRequiredService<IInputService>(), Lifetime.Singleton);

        // Bridge AI services
        builder.Register<IAgentDirector>(container =>
            pureDiContainer.GetRequiredService<IAgentDirector>(), Lifetime.Singleton);

        return builder;
    }

    /// <summary>
    /// Create Unity-specific child container
    /// </summary>
    public static IServiceProvider CreateUnityContainer(
        IServiceProvider parentContainer,
        UnityModeProfile profile)
    {
        return parentContainer.CreateChildContainer(services =>
        {
            // Unity-specific services
            services.AddSingleton<IUnityBridge, UnityBridge>();
            services.AddSingleton<IUnityAssetDatabase, UnityAssetDatabase>();

            // Configure for Unity constraints
            services.Configure<PerformanceProfile>(opt =>
            {
                opt.MaxFrameTime = TimeSpan.FromMilliseconds(16); // 60 FPS target
                opt.EnableAsyncLoading = profile.EnableAsyncLoading;
            });
        });
    }
}
#endif
```

### Plugin Service Registration

```csharp
// GameConsole.Infrastructure.Plugin/src/PluginServiceRegistration.cs
public static class PluginServiceRegistration
{
    /// <summary>
    /// Register plugin services discovered from assembly
    /// </summary>
    public static IServiceCollection AddPlugin(
        this IServiceCollection services,
        PluginMetadata plugin)
    {
        var assembly = plugin.Assembly;

        foreach (var type in assembly.GetTypes())
        {
            var pluginServiceAttr = type.GetCustomAttribute<PluginServiceAttribute>();
            if (pluginServiceAttr == null) continue;

            // Provider service (implements existing contract)
            var providerFor = type.GetCustomAttribute<ProviderForAttribute>();
            if (providerFor != null)
            {
                RegisterProviderService(services, type, providerFor);
            }

            // Extension service (new functionality)
            var serviceContract = type.GetCustomAttribute<ServiceContractAttribute>();
            if (serviceContract != null)
            {
                RegisterExtensionService(services, type);
            }
        }

        return services;
    }

    private static void RegisterProviderService(
        IServiceCollection services,
        Type implementationType,
        ProviderForAttribute attr)
    {
        // Register in service registry for provider selection
        services.AddSingleton(provider =>
        {
            var registryType = typeof(IServiceRegistry<>).MakeGenericType(attr.ContractType);
            var registry = provider.GetRequiredService(registryType);
            var instance = ActivatorUtilities.CreateInstance(provider, implementationType);

            // Add to registry with priority and capability detection
            var priority = GetProviderPriority(implementationType);
            var addMethod = registryType.GetMethod("RegisterProvider");
            addMethod?.Invoke(registry, new[] { instance, priority });

            return instance;
        });
    }

    private static void RegisterExtensionService(
        IServiceCollection services,
        Type implementationType)
    {
        // Register new service contract directly
        var interfaces = implementationType.GetInterfaces()
            .Where(i => i != typeof(IService) && typeof(IService).IsAssignableFrom(i));

        foreach (var serviceInterface in interfaces)
        {
            services.AddSingleton(serviceInterface, implementationType);
        }
    }
}
```

### Container Lifecycle Management

```csharp
// GameConsole.Infrastructure.DI/src/ContainerManager.cs
public class ContainerManager : IHostedService, IDisposable
{
    private readonly DIComposition _rootComposition;
    private readonly Dictionary<string, IServiceProvider> _modeContainers = new();
    private readonly Dictionary<string, IServiceProvider> _pluginContainers = new();

    public IServiceProvider RootContainer => _rootComposition.Root;
    public IServiceProvider? CurrentModeContainer { get; private set; }

    public async Task SwitchModeAsync(ConsoleMode mode, ModeProfile profile)
    {
        var containerId = $"{mode}:{profile.Id}";

        if (!_modeContainers.TryGetValue(containerId, out var container))
        {
            container = mode switch
            {
                ConsoleMode.Game => ContainerFactory.CreateGameModeContainer(RootContainer, (GameModeProfile)profile),
                ConsoleMode.Editor => ContainerFactory.CreateEditorModeContainer(RootContainer, (EditorModeProfile)profile),
                _ => throw new ArgumentException($"Unknown mode: {mode}")
            };

            _modeContainers[containerId] = container;
        }

        CurrentModeContainer = container;

        // Start mode-specific services
        await StartModeServicesAsync(container);
    }

    public async Task LoadPluginAsync(PluginMetadata plugin)
    {
        if (_pluginContainers.ContainsKey(plugin.Id))
        {
            await UnloadPluginAsync(plugin.Id);
        }

        var pluginContainer = ContainerFactory.CreatePluginContainer(
            CurrentModeContainer ?? RootContainer,
            plugin);

        _pluginContainers[plugin.Id] = pluginContainer;

        // Start plugin services
        await StartPluginServicesAsync(pluginContainer, plugin);
    }

    public void Dispose()
    {
        foreach (var container in _modeContainers.Values.Concat(_pluginContainers.Values))
        {
            if (container is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _rootComposition.Dispose();
    }
}
```

## Benefits

### True Hierarchical Containers
- Parent-child relationships with proper fallback
- Plugin isolation without service leakage
- Mode-specific service overrides

### Performance
- Compile-time dependency resolution with Pure.DI
- Zero-allocation service resolution
- Minimal runtime overhead

### Unity Integration
- Seamless bridge to VContainer
- Unity-specific optimizations
- Shared service instances

### Plugin Safety
- Isolated dependency graphs per plugin
- No cross-plugin service pollution
- Safe plugin unloading

## Drawbacks

### Complexity
- Multiple DI systems to understand
- Complex hierarchical service resolution
- Debugging across container boundaries

### Build-Time Dependencies
- Pure.DI source generation at compile time
- May increase build times
- Requires careful setup of source generators

### Memory Usage
- Multiple container instances
- Service duplication across containers
- Plugin isolation overhead

## Alternatives Considered

### Microsoft.Extensions.DI Only
- Simpler but lacks hierarchical support
- **Rejected**: Can't achieve plugin isolation

### Autofac
- Mature hierarchical DI container
- **Rejected**: Runtime resolution overhead, less .NET native

### Unity Container
- Microsoft's hierarchical DI solution
- **Rejected**: Less performant, not as actively maintained

## Migration Strategy

### Phase 1: Core Infrastructure
- Implement DIComposition and MS Extensions bridge
- Convert core services to Pure.DI registration
- Test basic service resolution

### Phase 2: Mode Containers
- Implement ContainerFactory for Game/Editor modes
- Add mode switching capability
- Test service isolation between modes

### Phase 3: Plugin Containers
- Implement plugin container creation
- Add plugin service registration
- Test plugin isolation and unloading

### Phase 4: Unity Integration
- Implement VContainer bridge
- Test Unity-specific scenarios
- Performance optimization

## Success Metrics

- **Container Isolation**: Plugins can't access each other's services
- **Mode Switching**: Clean transitions between Game/Editor modes
- **Performance**: Service resolution under 1μs for common services
- **Unity Compatibility**: Seamless VContainer integration

## Future Possibilities

- **Container Pooling**: Reuse containers for better performance
- **Lazy Container Creation**: Create containers only when needed
- **Container Analytics**: Monitor service usage and performance
- **Dynamic Service Swapping**: Hot-swap services without container recreation
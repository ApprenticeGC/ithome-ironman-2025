# RFC-005: Service Provider Pattern

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-002, RFC-004

## Summary

This RFC defines the service provider pattern for GameConsole's plugin system, enabling dynamic provider selection, capability discovery, and runtime service swapping. The pattern supports multiple implementations of the same contract with intelligent selection based on capabilities, priority, and runtime context.

## Motivation

GameConsole needs dynamic provider selection for core services to support:

1. **Multiple Implementations**: Different audio backends (OpenAL, XAudio2, etc.)
2. **Capability-Based Selection**: Choose providers based on available features
3. **Fallback Strategies**: Graceful degradation when preferred providers fail
4. **Runtime Switching**: Hot-swap providers without application restart
5. **Plugin-Provided Services**: Plugins can extend or replace core services
6. **Platform Optimization**: Platform-specific provider selection

## Detailed Design

### Provider Registry Architecture

```csharp
// GameConsole.Infrastructure.Provider/src/IServiceRegistry.cs
public interface IServiceRegistry<TContract> where TContract : class
{
    /// <summary>
    /// Register a provider with priority and capabilities
    /// </summary>
    void RegisterProvider(TContract provider, ProviderMetadata metadata);

    /// <summary>
    /// Get the best provider based on selection criteria
    /// </summary>
    TContract GetProvider(ProviderSelectionCriteria? criteria = null);

    /// <summary>
    /// Get all providers matching criteria
    /// </summary>
    IReadOnlyList<TContract> GetProviders(ProviderSelectionCriteria? criteria = null);

    /// <summary>
    /// Check if any provider supports given capabilities
    /// </summary>
    bool SupportsCapabilities(IReadOnlySet<string> requiredCapabilities);

    /// <summary>
    /// Provider registration/unregistration events
    /// </summary>
    event EventHandler<ProviderChangedEventArgs<TContract>> ProviderChanged;
}

public record ProviderMetadata(
    string Name,
    int Priority,
    IReadOnlySet<string> Capabilities,
    Platform SupportedPlatforms,
    Version Version);

public record ProviderSelectionCriteria(
    IReadOnlySet<string>? RequiredCapabilities = null,
    IReadOnlySet<string>? PreferredCapabilities = null,
    Platform? TargetPlatform = null,
    int? MinimumPriority = null);
```

### Service Registry Implementation

```csharp
// GameConsole.Infrastructure.Provider/src/ServiceRegistry.cs
public class ServiceRegistry<TContract> : IServiceRegistry<TContract>
    where TContract : class
{
    private readonly List<RegisteredProvider> _providers = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public void RegisterProvider(TContract provider, ProviderMetadata metadata)
    {
        _lock.EnterWriteLock();
        try
        {
            var registered = new RegisteredProvider(provider, metadata);

            // Remove existing provider with same name
            _providers.RemoveAll(p => p.Metadata.Name == metadata.Name);

            // Insert in priority order (higher priority first)
            var insertIndex = _providers.FindIndex(p => p.Metadata.Priority < metadata.Priority);
            if (insertIndex == -1)
            {
                _providers.Add(registered);
            }
            else
            {
                _providers.Insert(insertIndex, registered);
            }

            ProviderChanged?.Invoke(this, new ProviderChangedEventArgs<TContract>(
                ProviderChangeType.Added, provider, metadata));
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public TContract GetProvider(ProviderSelectionCriteria? criteria = null)
    {
        _lock.EnterReadLock();
        try
        {
            var matchingProviders = GetMatchingProviders(criteria);

            if (matchingProviders.Count == 0)
            {
                throw new NoSuitableProviderException(typeof(TContract), criteria);
            }

            // Return highest priority matching provider
            return matchingProviders[0].Provider;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private List<RegisteredProvider> GetMatchingProviders(ProviderSelectionCriteria? criteria)
    {
        return _providers.Where(p => MatchesCriteria(p, criteria)).ToList();
    }

    private bool MatchesCriteria(RegisteredProvider provider, ProviderSelectionCriteria? criteria)
    {
        if (criteria == null) return true;

        // Check minimum priority
        if (criteria.MinimumPriority.HasValue && provider.Metadata.Priority < criteria.MinimumPriority.Value)
            return false;

        // Check platform support
        if (criteria.TargetPlatform.HasValue &&
            !provider.Metadata.SupportedPlatforms.HasFlag(criteria.TargetPlatform.Value))
            return false;

        // Check required capabilities
        if (criteria.RequiredCapabilities != null &&
            !criteria.RequiredCapabilities.All(cap => provider.Metadata.Capabilities.Contains(cap)))
            return false;

        return true;
    }

    private record RegisteredProvider(TContract Provider, ProviderMetadata Metadata);
}
```

### Provider Proxy Generation

```csharp
// GameConsole.Infrastructure.Provider/src/ProviderProxy.cs
[ServiceContract]
public interface IGameEngine : IService
{
    Task InitializeAsync(GameEngineConfig config);
    Task<Scene> LoadSceneAsync(string scenePath);
    Task RenderFrameAsync();
    void Dispose();
}

// Source-generated proxy (Tier 2)
[GeneratedCode("ProviderProxyGenerator", "1.0.0")]
public partial class GameEngineProxy : IGameEngine
{
    private readonly IServiceRegistry<IGameEngine> _registry;
    private readonly ILogger<GameEngineProxy> _logger;

    public GameEngineProxy(
        IServiceRegistry<IGameEngine> registry,
        ILogger<GameEngineProxy> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task InitializeAsync(GameEngineConfig config)
    {
        var criteria = new ProviderSelectionCriteria(
            RequiredCapabilities: config.RequiredFeatures,
            TargetPlatform: config.TargetPlatform);

        try
        {
            var provider = _registry.GetProvider(criteria);
            await provider.InitializeAsync(config);
        }
        catch (NoSuitableProviderException ex)
        {
            _logger.LogError(ex, "No suitable game engine provider found");
            throw new ServiceInitializationException("Game engine initialization failed", ex);
        }
    }

    public async Task<Scene> LoadSceneAsync(string scenePath)
    {
        var provider = _registry.GetProvider();
        return await provider.LoadSceneAsync(scenePath);
    }

    // Additional methods follow same pattern...
}
```

### Capability Discovery

```csharp
// GameConsole.Infrastructure.Provider/src/CapabilityDiscovery.cs
public static class CapabilityDiscovery
{
    /// <summary>
    /// Discover capabilities from provider attributes and runtime checks
    /// </summary>
    public static IReadOnlySet<string> DiscoverCapabilities(Type providerType)
    {
        var capabilities = new HashSet<string>();

        // Static capabilities from attributes
        var capabilityAttrs = providerType.GetCustomAttributes<CapabilityAttribute>();
        foreach (var attr in capabilityAttrs)
        {
            capabilities.Add(attr.Name);
        }

        // Dynamic capabilities from runtime checks
        var dynamicAttrs = providerType.GetCustomAttributes<DynamicCapabilityAttribute>();
        foreach (var attr in dynamicAttrs)
        {
            if (attr.CheckMethod != null)
            {
                var method = providerType.GetMethod(attr.CheckMethod, BindingFlags.Public | BindingFlags.Static);
                if (method?.Invoke(null, null) is true)
                {
                    capabilities.Add(attr.CapabilityName);
                }
            }
        }

        return capabilities.ToHashSet();
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CapabilityAttribute : Attribute
{
    public string Name { get; }
    public CapabilityAttribute(string name) => Name = name;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DynamicCapabilityAttribute : Attribute
{
    public string CapabilityName { get; }
    public string? CheckMethod { get; }

    public DynamicCapabilityAttribute(string capabilityName, string? checkMethod = null)
    {
        CapabilityName = capabilityName;
        CheckMethod = checkMethod;
    }
}
```

### Plugin Provider Registration

```csharp
// Example Unity Plugin Provider
// Plugins.Unity.GameEngine/src/UnityGameEngineProvider.cs
[PluginService]
[ProviderFor(typeof(IGameEngine))]
[Capability("3d-rendering")]
[Capability("physics")]
[Capability("animation")]
[DynamicCapability("unity-native", "IsUnityAvailable")]
[Priority(90)] // High priority if Unity is available
public class UnityGameEngineProvider : IGameEngine
{
    public static bool IsUnityAvailable()
    {
        // Runtime check for Unity availability
        try
        {
            var unityEngine = Assembly.LoadFrom("UnityEngine.dll");
            return unityEngine != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task InitializeAsync(GameEngineConfig config)
    {
        // Unity-specific initialization
        await InitializeUnityEngineAsync(config);
    }

    // Implementation details...
}

// Fallback Provider
// GameConsole.Engine.Fallback/src/FallbackGameEngineProvider.cs
[PluginService]
[ProviderFor(typeof(IGameEngine))]
[Capability("basic-rendering")]
[Priority(10)] // Low priority fallback
public class FallbackGameEngineProvider : IGameEngine
{
    public async Task InitializeAsync(GameEngineConfig config)
    {
        // Basic software renderer initialization
        await InitializeSoftwareRendererAsync(config);
    }

    // Minimal implementation...
}
```

### Provider Hot-Swapping

```csharp
// GameConsole.Infrastructure.Provider/src/HotSwapManager.cs
public class HotSwapManager<TContract> where TContract : class
{
    private readonly IServiceRegistry<TContract> _registry;
    private readonly ILogger<HotSwapManager<TContract>> _logger;

    public async Task SwapProviderAsync(
        string currentProviderName,
        string newProviderName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Swapping provider from {Current} to {New}",
            currentProviderName, newProviderName);

        var newProvider = _registry.GetProviders()
            .FirstOrDefault(p => GetProviderName(p) == newProviderName);

        if (newProvider == null)
        {
            throw new ProviderNotFoundException(newProviderName);
        }

        // Graceful transition for stateful services
        if (newProvider is IStatefulService stateful)
        {
            await stateful.PrepareForActivationAsync(cancellationToken);
        }

        // Update registry to prefer new provider
        var metadata = GetProviderMetadata(newProvider);
        _registry.RegisterProvider(newProvider, metadata with { Priority = 1000 });

        _logger.LogInformation("Provider swap completed successfully");
    }
}
```

### Configuration Integration

```csharp
// GameConsole.Infrastructure.Provider/src/ProviderConfiguration.cs
public class ProviderConfiguration
{
    public Dictionary<string, ProviderPreferences> ServicePreferences { get; set; } = new();
    public Dictionary<string, Dictionary<string, object>> ProviderSettings { get; set; } = new();
}

public record ProviderPreferences(
    string? PreferredProvider = null,
    IReadOnlySet<string>? RequiredCapabilities = null,
    IReadOnlySet<string>? PreferredCapabilities = null,
    int MinimumPriority = 0);

// Configuration usage
services.Configure<ProviderConfiguration>(configuration.GetSection("Providers"));
services.PostConfigure<ProviderConfiguration>(config =>
{
    // Audio service preferences
    config.ServicePreferences["Audio"] = new ProviderPreferences(
        PreferredProvider: "OpenAL",
        RequiredCapabilities: new[] { "3d-audio", "streaming" }.ToHashSet(),
        PreferredCapabilities: new[] { "hardware-acceleration" }.ToHashSet());

    // Graphics service preferences
    config.ServicePreferences["Graphics"] = new ProviderPreferences(
        RequiredCapabilities: new[] { "3d-rendering" }.ToHashSet(),
        PreferredCapabilities: new[] { "hardware-acceleration", "shader-support" }.ToHashSet());
});
```

## Benefits

### Dynamic Service Selection
- Runtime provider selection based on capabilities
- Graceful fallback when preferred providers unavailable
- Platform-specific optimization

### Plugin Extensibility
- Plugins can provide new implementations
- Multiple plugins can compete for same contract
- Clean service replacement without code changes

### Performance Optimization
- Compile-time proxy generation
- Efficient provider lookup
- Minimal runtime overhead

### Maintainability
- Clear separation between contract and implementation
- Consistent provider registration pattern
- Centralized provider management

## Drawbacks

### Complexity
- Additional abstraction layer
- Complex provider selection logic
- Debugging across provider boundaries

### Runtime Overhead
- Provider selection on each call
- Registry lookup costs
- Proxy method dispatch

### Configuration Complexity
- Provider preference configuration
- Capability specification maintenance
- Priority management

## Alternatives Considered

### Direct Service Registration
- Simpler but lacks dynamic selection
- **Rejected**: Can't support multiple implementations

### Factory Pattern
- More explicit but requires manual factory management
- **Rejected**: Doesn't integrate well with DI container

### Strategy Pattern
- Similar but requires explicit strategy selection
- **Rejected**: Less discoverable, more coupling

## Migration Strategy

### Phase 1: Core Provider Registry
- Implement IServiceRegistry and ServiceRegistry
- Add provider metadata and selection criteria
- Test basic provider registration and selection

### Phase 2: Proxy Generation
- Implement source generation for provider proxies
- Add capability discovery infrastructure
- Test provider selection in proxies

### Phase 3: Plugin Integration
- Add provider registration in plugin loading
- Implement hot-swapping capability
- Test multi-provider scenarios

### Phase 4: Configuration Integration
- Add provider preference configuration
- Implement configuration-driven selection
- Add provider health checking and failover

## Success Metrics

- **Provider Selection**: Sub-millisecond provider lookup
- **Plugin Integration**: Plugins successfully register providers
- **Hot Swapping**: Zero-downtime provider switching
- **Capability Discovery**: Automatic capability detection

## Future Possibilities

- **Provider Health Monitoring**: Automatic failover on provider failure
- **Load Balancing**: Distribute load across multiple providers
- **Provider Metrics**: Performance tracking and optimization
- **Dynamic Capability Updates**: Runtime capability changes
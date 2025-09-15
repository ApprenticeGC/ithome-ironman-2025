# RFC-006: Plugin Management Service

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-001, RFC-003, RFC-004

## Summary

This RFC defines the Plugin Management Service, a Tier 3 service that orchestrates plugin loading, unloading, and lifecycle management using different AssemblyContextLoader providers at Tier 4. The service follows the 4-tier architecture pattern and supports provider selection strategies for different plugin loading mechanisms.

## Motivation

GameConsole requires sophisticated plugin management that supports:

1. **Multiple Loading Strategies**: AssemblyContextLoader, McMaster.NETCore.Plugins, etc.
2. **Provider Selection**: Choose optimal loader based on requirements and availability
3. **Plugin Lifecycle**: Load, unload, reload, dependency management
4. **Hot Reload**: Development-time plugin updates without restart
5. **Container Integration**: Create hierarchical DI containers for plugin isolation
6. **Mode Awareness**: Load different plugins for Game vs Editor modes

The Plugin Management Service abstracts these concerns behind a clean interface while delegating implementation to pluggable providers.

## Detailed Design

### Tier 1+2: Plugin Management Contracts

#### Core Plugin Service Interface
```csharp
// GameConsole.Plugin.Core/src/Services/IService.cs
namespace GameConsole.Plugin.Services;

/// <summary>
/// Tier 1: Plugin management service interface (pure .NET, async-first).
/// Implemented by Tier 3 service that selects providers via strategy.
/// </summary>
public interface IService : GameConsole.Services.IService
{
    Task InitializeAsync(CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);

    // Plugin lifecycle management
    Task<PluginLoadResult> LoadPluginAsync(string pluginPath, CancellationToken ct = default);
    Task<bool> UnloadPluginAsync(string pluginId, CancellationToken ct = default);
    Task ReloadPluginAsync(string pluginId, CancellationToken ct = default);

    // Plugin discovery and management
    Task<IEnumerable<PluginMetadata>> DiscoverPluginsAsync(string searchPath, CancellationToken ct = default);
    IEnumerable<LoadedPlugin> GetLoadedPlugins();
    Task<PluginMetadata?> GetPluginInfoAsync(string pluginId, CancellationToken ct = default);

    // Plugin container management
    Task<IServiceProvider> CreatePluginContainerAsync(string pluginId, CancellationToken ct = default);
    Task<bool> DisposePluginContainerAsync(string pluginId, CancellationToken ct = default);

    // Plugin validation and health
    Task<ValidationResult> ValidatePluginAsync(PluginMetadata plugin, CancellationToken ct = default);
    Task<HealthCheckResult> CheckPluginHealthAsync(string pluginId, CancellationToken ct = default);
}

/// <summary>
/// Result of plugin loading operation
/// </summary>
public class PluginLoadResult
{
    public bool Success { get; set; }
    public string? PluginId { get; set; }
    public string? Error { get; set; }
    public string[] ServiceIds { get; set; } = Array.Empty<string>();
    public TimeSpan LoadTime { get; set; }
}

/// <summary>
/// Metadata about a discovered or loaded plugin
/// </summary>
public class PluginMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "0.0.0";
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AssemblyPath { get; set; } = string.Empty;
    public string[] PluginTypes { get; set; } = Array.Empty<string>();
    public PluginDependency[] Dependencies { get; set; } = Array.Empty<PluginDependency>();
    public string[] RequiredGates { get; set; } = Array.Empty<string>();
    public ConsoleMode[] SupportedModes { get; set; } = Array.Empty<ConsoleMode>();
    public Assembly? Assembly { get; set; }
    public DateTime? LoadedAt { get; set; }
}

/// <summary>
/// Information about a loaded plugin
/// </summary>
public class LoadedPlugin
{
    public PluginMetadata Metadata { get; set; } = new();
    public IServiceProvider? Container { get; set; }
    public IService[] Services { get; set; } = Array.Empty<IService>();
    public PluginStatus Status { get; set; }
    public string? LastError { get; set; }
}

public enum PluginStatus
{
    Loading,
    Loaded,
    Failed,
    Unloading,
    Unloaded
}
```

#### Generated Service Proxy (Tier 2)
```csharp
// GameConsole.Plugin.Core/src/Services/Proxy/PluginServiceProxy.generated.cs
public partial class PluginServiceProxy : IService
{
    private readonly IServiceRegistry<IPluginLoader> _registry;
    private readonly ILogger<PluginServiceProxy> _logger;

    public PluginServiceProxy(
        IServiceRegistry<IPluginLoader> registry,
        ILogger<PluginServiceProxy> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task<PluginLoadResult> LoadPluginAsync(string pluginPath, CancellationToken ct = default)
    {
        var loader = await _registry.GetProviderAsync(ct);
        using var activity = Diagnostics.StartActivity(nameof(LoadPluginAsync));
        activity?.SetTag("plugin.path", pluginPath);

        try
        {
            return await loader.LoadPluginAsync(pluginPath, ct)
                .WithTimeout(_registry.DefaultTimeout, ct)
                .WithRetry(2, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin loading failed for {PluginPath}", pluginPath);
            return new PluginLoadResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<bool> UnloadPluginAsync(string pluginId, CancellationToken ct = default)
    {
        var loader = await _registry.GetProviderAsync(ct);
        using var activity = Diagnostics.StartActivity(nameof(UnloadPluginAsync));
        activity?.SetTag("plugin.id", pluginId);

        try
        {
            return await loader.UnloadPluginAsync(pluginId, ct)
                .WithTimeout(_registry.DefaultTimeout, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin unloading failed for {PluginId}", pluginId);
            return false;
        }
    }

    // Other methods follow similar pattern...
}
```

### Tier 3: Plugin Service Configuration

#### Plugin Service Configuration Container
```csharp
// GameConsole.Profiles.Plugin/PluginServiceConfig.cs
namespace GameConsole.Profiles.Plugin;

/// <summary>
/// Tier 3: Plugin service configuration (like ScriptableObject)
/// Determines which AssemblyContextLoader provider to use
/// </summary>
public class PluginServiceConfig
{
    public PluginLoaderType LoaderType { get; set; } = PluginLoaderType.Auto;
    public ProviderSelectionStrategy Strategy { get; set; } = ProviderSelectionStrategy.Priority;
    public string PluginsDirectory { get; set; } = "./plugins";
    public bool EnableHotReload { get; set; } = true;
    public bool IsolatePluginDependencies { get; set; } = true;
    public TimeSpan LoadTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public TimeSpan UnloadTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public string[] AllowedPluginPaths { get; set; } = Array.Empty<string>();
    public int MaxPlugins { get; set; } = 100;
    public bool ValidatePluginSecurity { get; set; } = true;

    // Provider-specific settings
    public AssemblyContextLoaderSettings AssemblyContextSettings { get; set; } = new();
    public McMasterPluginSettings McMasterSettings { get; set; } = new();
    public MockPluginSettings MockSettings { get; set; } = new();
}

public enum PluginLoaderType
{
    Auto,                    // Choose best available provider
    AssemblyContextLoader,   // System.Runtime.Loader (preferred)
    McMaster,               // McMaster.NETCore.Plugins (feature-rich)
    AppDomain,              // Legacy AppDomain isolation (Windows only)
    Mock                    // Testing/development
}

public class AssemblyContextLoaderSettings
{
    public bool IsCollectible { get; set; } = true;
    public bool PreferSharedTypes { get; set; } = true;
    public string[] SharedAssemblies { get; set; } = Array.Empty<string>();
    public bool EnableShadowCopy { get; set; } = false;
}

public class McMasterPluginSettings
{
    public string[] SharedTypes { get; set; } = Array.Empty<string>();
    public bool IsUnloadable { get; set; } = true;
    public bool PreferSharedTypes { get; set; } = true;
}

public class MockPluginSettings
{
    public int SimulatedLoadTimeMs { get; set; } = 100;
    public double FailureRate { get; set; } = 0.0; // 0.0 to 1.0
    public bool SimulateMemoryLeaks { get; set; } = false;
}
```

#### Plugin Service Implementation (Tier 3)
```csharp
// GameConsole.Infrastructure.Plugin/PluginService.cs
namespace GameConsole.Infrastructure.Plugin;

/// <summary>
/// Tier 3: Plugin service that uses provider selection strategy
/// This is NOT the provider - it USES providers
/// </summary>
public class PluginService : IService
{
    private readonly IServiceRegistry<IPluginLoader> _loaderRegistry;
    private readonly PluginServiceConfig _config;
    private readonly ILogger<PluginService> _logger;
    private readonly Dictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public string ServiceId => "gameconsole.plugin.service";
    public int Priority => 100; // High priority core service
    public double StartupTimeMs => 200.0;

    public PluginService(
        IServiceRegistry<IPluginLoader> loaderRegistry,
        IOptions<PluginServiceConfig> config,
        ILogger<PluginService> logger)
    {
        _loaderRegistry = loaderRegistry;
        _config = config.Value;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting plugin service with loader type: {LoaderType}", _config.LoaderType);

        // Initialize the selected provider
        var loader = await _loaderRegistry.GetProviderAsync();
        await loader.InitializeAsync(_config);

        // Auto-discover and load plugins if configured
        if (!string.IsNullOrEmpty(_config.PluginsDirectory) && Directory.Exists(_config.PluginsDirectory))
        {
            await AutoDiscoverAndLoadPluginsAsync();
        }

        _logger.LogInformation("Plugin service started successfully");
    }

    public async Task<PluginLoadResult> LoadPluginAsync(string pluginPath, CancellationToken ct = default)
    {
        using (await _operationLock.WaitAsync(ct))
        {
            var loader = await _loaderRegistry.GetProviderAsync(ct);
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Loading plugin: {PluginPath}", pluginPath);

                // Discover plugin metadata first
                var metadata = await DiscoverPluginMetadataAsync(pluginPath, ct);
                if (metadata == null)
                {
                    return new PluginLoadResult { Success = false, Error = "No plugin metadata found" };
                }

                // Check if already loaded
                if (_loadedPlugins.ContainsKey(metadata.Id))
                {
                    _logger.LogWarning("Plugin {PluginId} is already loaded", metadata.Id);
                    return new PluginLoadResult { Success = false, Error = "Plugin already loaded" };
                }

                // Load via provider
                var result = await loader.LoadPluginAsync(pluginPath, ct);
                stopwatch.Stop();
                result.LoadTime = stopwatch.Elapsed;

                if (result.Success && result.PluginId != null)
                {
                    // Track loaded plugin
                    _loadedPlugins[result.PluginId] = new LoadedPlugin
                    {
                        Metadata = metadata,
                        Status = PluginStatus.Loaded
                    };

                    _logger.LogInformation("Plugin {PluginId} loaded successfully in {LoadTime}ms",
                        result.PluginId, result.LoadTime.TotalMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Failed to load plugin {PluginPath}", pluginPath);
                return new PluginLoadResult
                {
                    Success = false,
                    Error = ex.Message,
                    LoadTime = stopwatch.Elapsed
                };
            }
        }
    }

    public async Task<bool> UnloadPluginAsync(string pluginId, CancellationToken ct = default)
    {
        using (await _operationLock.WaitAsync(ct))
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                _logger.LogWarning("Plugin {PluginId} is not loaded", pluginId);
                return false;
            }

            try
            {
                _logger.LogInformation("Unloading plugin: {PluginId}", pluginId);

                plugin.Status = PluginStatus.Unloading;

                var loader = await _loaderRegistry.GetProviderAsync(ct);
                var success = await loader.UnloadPluginAsync(pluginId, ct);

                if (success)
                {
                    _loadedPlugins.Remove(pluginId);
                    _logger.LogInformation("Plugin {PluginId} unloaded successfully", pluginId);
                }
                else
                {
                    plugin.Status = PluginStatus.Failed;
                    plugin.LastError = "Unload operation failed";
                }

                return success;
            }
            catch (Exception ex)
            {
                plugin.Status = PluginStatus.Failed;
                plugin.LastError = ex.Message;
                _logger.LogError(ex, "Failed to unload plugin {PluginId}", pluginId);
                return false;
            }
        }
    }

    public async Task ReloadPluginAsync(string pluginId, CancellationToken ct = default)
    {
        if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
        {
            throw new InvalidOperationException($"Plugin {pluginId} is not loaded");
        }

        var pluginPath = plugin.Metadata.AssemblyPath;

        // Unload first
        await UnloadPluginAsync(pluginId, ct);

        // Wait a bit for cleanup
        await Task.Delay(100, ct);

        // Reload
        var result = await LoadPluginAsync(pluginPath, ct);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Failed to reload plugin {pluginId}: {result.Error}");
        }
    }

    public IEnumerable<LoadedPlugin> GetLoadedPlugins()
    {
        return _loadedPlugins.Values.ToArray(); // Return copy to avoid concurrent modification
    }

    private async Task AutoDiscoverAndLoadPluginsAsync()
    {
        try
        {
            var plugins = await DiscoverPluginsAsync(_config.PluginsDirectory);

            foreach (var plugin in plugins)
            {
                try
                {
                    await LoadPluginAsync(plugin.AssemblyPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-load plugin {PluginId}", plugin.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-discover plugins");
        }
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Shutting down plugin service");

        var pluginIds = _loadedPlugins.Keys.ToArray();
        foreach (var pluginId in pluginIds)
        {
            try
            {
                await UnloadPluginAsync(pluginId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading plugin {PluginId} during shutdown", pluginId);
            }
        }

        _operationLock.Dispose();
    }
}
```

### Tier 4: Plugin Loader Providers

#### IPluginLoader Contract (Shared by all providers)
```csharp
// GameConsole.Infrastructure.Plugin/src/IPluginLoader.cs
namespace GameConsole.Infrastructure.Plugin;

/// <summary>
/// Tier 4 provider contract for plugin loading implementations
/// </summary>
public interface IPluginLoader : IService
{
    Task InitializeAsync(PluginServiceConfig config, CancellationToken ct = default);
    Task<PluginLoadResult> LoadPluginAsync(string pluginPath, CancellationToken ct = default);
    Task<bool> UnloadPluginAsync(string pluginId, CancellationToken ct = default);
    Task<IEnumerable<PluginMetadata>> DiscoverPluginsAsync(string searchPath, CancellationToken ct = default);
    Task<ValidationResult> ValidatePluginAsync(PluginMetadata plugin, CancellationToken ct = default);
}
```

#### Provider Selection and Registration
```csharp
// GameConsole.Infrastructure.Plugin/src/PluginServiceRegistration.cs
public static class PluginServiceRegistration
{
    public static IServiceCollection AddPluginServices(this IServiceCollection services, PluginServiceConfig config)
    {
        // Register the main plugin service (Tier 3)
        services.AddSingleton<GameConsole.Plugin.Services.IService, PluginService>();

        // Register service registry for provider selection
        services.AddSingleton<IServiceRegistry<IPluginLoader>>();

        // Register available providers (Tier 4) based on configuration
        RegisterPluginLoaderProviders(services, config);

        // Configure plugin service
        services.Configure<PluginServiceConfig>(opt => config.CopyTo(opt));

        return services;
    }

    private static void RegisterPluginLoaderProviders(IServiceCollection services, PluginServiceConfig config)
    {
        // AssemblyContextLoader provider (preferred)
        if (config.LoaderType == PluginLoaderType.Auto ||
            config.LoaderType == PluginLoaderType.AssemblyContextLoader)
        {
            services.AddSingleton<IPluginLoader, AssemblyContextPluginLoader>();
        }

        // McMaster provider (alternative)
        if (config.LoaderType == PluginLoaderType.Auto ||
            config.LoaderType == PluginLoaderType.McMaster)
        {
            services.AddSingleton<IPluginLoader, McMasterPluginLoader>();
        }

        // Mock provider (testing)
        if (config.LoaderType == PluginLoaderType.Mock)
        {
            services.AddSingleton<IPluginLoader, MockPluginLoader>();
        }

        // AppDomain provider (Windows only, legacy)
        #if NET48 && WINDOWS
        if (config.LoaderType == PluginLoaderType.AppDomain)
        {
            services.AddSingleton<IPluginLoader, AppDomainPluginLoader>();
        }
        #endif
    }
}
```

## Benefits

### Clean Tier Separation
- **Tier 1+2**: Pure contracts and generated proxies
- **Tier 3**: Configuration-driven orchestration service
- **Tier 4**: Multiple plugin loading implementations

### Provider Flexibility
- Multiple plugin loading strategies available
- Runtime provider selection based on configuration
- Easy to add new loading mechanisms

### Plugin Safety
- Isolated assembly contexts prevent cross-plugin interference
- Comprehensive validation before loading
- Safe unloading with proper cleanup

### Development Experience
- Hot reload for rapid plugin development
- Rich diagnostics and error reporting
- Easy configuration switching

## Drawbacks

### Complexity
- Multiple layers of abstraction
- Complex error handling across tiers
- Debugging across isolation boundaries

### Performance Overhead
- Assembly context isolation has memory cost
- Provider selection adds indirection
- Plugin metadata discovery can be slow

### Resource Management
- Plugin unloading may not free all memory
- Assembly contexts can have finalizer pressure
- Need careful monitoring of plugin resource usage

## Testing Strategy

### Unit Testing
- Mock provider for testing plugin service logic
- Provider-specific tests for loading mechanisms
- Validation logic testing with malformed plugins

### Integration Testing
- Real plugin loading with sample plugins
- Hot reload scenarios
- Error condition handling

### Performance Testing
- Plugin loading time benchmarks
- Memory usage during plugin lifecycle
- Assembly context cleanup verification

## Migration Path

### Phase 1: Basic Implementation
- Implement core plugin service (Tier 3)
- AssemblyContextLoader provider (Tier 4)
- Basic plugin discovery and loading

### Phase 2: Provider Options
- McMaster plugin loader provider
- Mock provider for testing
- Provider selection strategy

### Phase 3: Advanced Features
- Plugin validation pipeline
- Hot reload support
- Container integration

### Phase 4: Production Hardening
- Security validation
- Performance monitoring
- Error recovery mechanisms

## Success Metrics

- **Plugin Load Time**: < 100ms for typical plugins
- **Memory Efficiency**: Plugin unload frees > 90% of memory
- **Hot Reload Time**: < 2 seconds for plugin reload
- **Provider Selection**: Automatic selection works 95% of time

## Future Enhancements

- **Plugin Sandboxing**: Run plugins in separate processes
- **Plugin Marketplace**: Discover and install plugins
- **Plugin Analytics**: Usage and performance metrics
- **Cross-Platform Loading**: Consistent behavior across platforms
- **Plugin Signing**: Cryptographic plugin validation
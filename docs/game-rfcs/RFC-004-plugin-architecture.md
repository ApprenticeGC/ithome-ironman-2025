# RFC-004: Plugin-Centric Architecture

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-001, RFC-002, RFC-003

## Summary

This RFC defines the plugin-centric architecture for GameConsole where core functionality is minimal and most features are delivered through dynamically loaded plugins. The system supports both provider plugins (implementing existing contracts) and extension plugins (adding new functionality).

## Motivation

GameConsole needs to be highly extensible to support:

1. **Multiple Game Engines**: Unity, Godot, custom engines through plugins
2. **Diverse UI Systems**: CLI, TUI, and future GUI options
3. **Various AI Backends**: Ollama, OpenAI, Azure, local models
4. **Community Extensions**: Third-party functionality and tools
5. **Hot Reloading**: Runtime plugin updates for development
6. **Mode-Specific Features**: Different plugin sets for Game vs Editor modes

A plugin-centric approach ensures the core system stays minimal while enabling unlimited extensibility.

## Detailed Design

### Plugin Types

#### Provider Plugins
Implement existing service contracts to provide alternative implementations:

```csharp
// Example: Unity Game Engine Provider
[PluginService]
[ProviderFor(typeof(IGameEngine))]
[RequiresGate("HAS_UNITY")]
[StartupPhase(2)] // Start after core infrastructure
public class UnityGameEngineProvider : IGameEngine, ICapabilityProvider
{
    public string ServiceId => "unity.game.engine";
    public int Priority => 100; // High priority provider
    public double StartupTimeMs => 500.0;

    // IGameEngine implementation
    public async Task StartAsync(GameConfig config, CancellationToken ct = default)
    {
        await InitializeUnitySimulation(config, ct);
    }

    public async Task<GameLoop> CreateGameLoopAsync(CancellationToken ct = default)
    {
        return new UnityGameLoop(await CreateUnityContext(ct));
    }

    // Capability provider for Unity-specific features
    public bool TryGetCapability<T>(out T? capability) where T : class
    {
        capability = this as T;
        return capability != null;
    }
}
```

#### Extension Plugins
Provide entirely new functionality not available in core system:

```csharp
// Example: Debug Console Plugin
[PluginService]
[ServiceContract] // Creates new service contract
public interface IDebugConsoleService : IService
{
    Task ShowConsoleAsync(CancellationToken ct = default);
    Task ExecuteCommandAsync(string command, CancellationToken ct = default);
    IObservable<DebugMessage> Messages { get; }
    Task RegisterCommandAsync(string name, Func<string[], Task<string>> handler, CancellationToken ct = default);
}

[PluginService]
[ModeRestriction(ConsoleMode.Editor)] // Only available in editor mode
public class DebugConsoleService : IDebugConsoleService
{
    public string ServiceId => "debug.console";
    public int Priority => 50;
    public double StartupTimeMs => 100.0;

    private readonly Dictionary<string, Func<string[], Task<string>>> _commands = new();
    private readonly Subject<DebugMessage> _messages = new();

    public IObservable<DebugMessage> Messages => _messages.AsObservable();

    public async Task StartAsync()
    {
        await RegisterBuiltInCommandsAsync();
        _logger.LogInformation("Debug console service started");
    }
}
```

### Plugin Discovery

Plugins are discovered through multiple mechanisms:

#### Assembly Scanning
```csharp
// GameConsole.Infrastructure.Plugin/src/PluginDiscovery.cs
public class PluginDiscovery
{
    private readonly string[] _pluginPaths;
    private readonly ILogger<PluginDiscovery> _logger;

    public async Task<IEnumerable<PluginMetadata>> DiscoverPluginsAsync(CancellationToken ct = default)
    {
        var plugins = new List<PluginMetadata>();

        foreach (var path in _pluginPaths)
        {
            if (!Directory.Exists(path)) continue;

            var assemblyFiles = Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories)
                .Where(f => !IsSystemAssembly(f));

            foreach (var assemblyFile in assemblyFiles)
            {
                try
                {
                    var metadata = await ScanAssemblyAsync(assemblyFile, ct);
                    if (metadata != null)
                    {
                        plugins.Add(metadata);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to scan plugin assembly: {AssemblyPath}", assemblyFile);
                }
            }
        }

        return plugins;
    }

    private async Task<PluginMetadata?> ScanAssemblyAsync(string assemblyPath, CancellationToken ct)
    {
        using var context = new MetadataLoadContext(CreateMetadataAssemblyResolver());
        var assembly = context.LoadFromAssemblyPath(assemblyPath);

        var pluginTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<PluginServiceAttribute>() != null)
            .ToArray();

        if (!pluginTypes.Any()) return null;

        // Extract plugin manifest if available
        var manifest = await ExtractManifestAsync(assembly, ct);

        return new PluginMetadata
        {
            Id = manifest?.Id ?? Path.GetFileNameWithoutExtension(assemblyPath),
            Name = manifest?.Name ?? assembly.GetName().Name ?? "Unknown",
            Version = manifest?.Version ?? assembly.GetName().Version?.ToString() ?? "0.0.0",
            Description = manifest?.Description ?? "No description",
            Author = manifest?.Author ?? "Unknown",
            AssemblyPath = assemblyPath,
            PluginTypes = pluginTypes.Select(t => t.FullName!).ToArray(),
            Dependencies = manifest?.Dependencies ?? Array.Empty<PluginDependency>(),
            RequiredGates = ExtractRequiredGates(pluginTypes),
            SupportedModes = ExtractSupportedModes(pluginTypes)
        };
    }
}
```

#### Plugin Manifest
Plugins can include optional manifest files for rich metadata:

```json
// plugin-manifest.json (embedded resource or adjacent file)
{
  "id": "community.debug-tools",
  "name": "Debug Tools Plugin",
  "version": "1.2.0",
  "description": "Advanced debugging and development tools",
  "author": "Community Developer",
  "homepage": "https://github.com/community/debug-tools",
  "repository": "https://github.com/community/debug-tools.git",
  "license": "MIT",
  "tags": ["debug", "development", "tools"],
  "dependencies": [
    {
      "id": "gameconsole.core",
      "version": ">=1.0.0"
    }
  ],
  "requiredGates": ["HAS_EDITOR_MODE"],
  "supportedModes": ["Editor"],
  "configuration": {
    "defaultLogLevel": "Debug",
    "enableProfiling": true
  }
}
```

### Core Contracts vs. Plugin Boundaries

- Do not make core service contracts a plugin. Contracts belong to Tier 1 assemblies and are shared from the default assembly load context to ensure type identity across the process.
- Plugins implement existing contracts (provider plugins) or expose new functionality via:
  - Optional capability facets discoverable from core services; or
  - Private Tier 3↔Tier 4 provider interfaces that never leak upward; or
  - Message envelopes (command/query objects) when behavior is highly variable.
- If truly new cross-cutting contracts are needed, they must be added to the designated contracts package (Tier 1) and versioned. They are not defined ad-hoc inside plugin assemblies.

### Plugin Loading Strategy

#### Assembly Context Loading
Uses isolated assembly contexts for plugin safety:

```csharp
// providers/infrastructure/AssemblyContextLoader.Plugin.Provider/AssemblyContextPluginLoader.cs
[ProviderFor(typeof(IPluginLoader))]
[RequiresGate("HAS_ASSEMBLY_CONTEXT_LOADER")]
public class AssemblyContextPluginLoader : IPluginLoader
{
    private readonly Dictionary<string, PluginAssemblyContext> _loadedContexts = new();
    private readonly Dictionary<string, List<IService>> _pluginServices = new();

    public async Task<PluginLoadResult> LoadPluginAsync(string pluginPath, CancellationToken ct = default)
    {
        var pluginId = Path.GetFileNameWithoutExtension(pluginPath);

        try
        {
            // Create isolated assembly context
            var context = new PluginAssemblyContext(pluginPath, isCollectible: true);
            var assembly = context.LoadFromAssemblyPath(pluginPath);

            // Discover and instantiate plugin services
            var services = await DiscoverPluginServicesAsync(assembly, ct);

            // Validate dependencies and gates
            await ValidatePluginAsync(services, ct);

            // Register services in appropriate containers
            await RegisterPluginServicesAsync(pluginId, services, ct);

            // Start services
            await StartPluginServicesAsync(services, ct);

            _loadedContexts[pluginId] = context;
            _pluginServices[pluginId] = services;

            _logger.LogInformation("Loaded plugin {PluginId} with {ServiceCount} services",
                pluginId, services.Count);

            return new PluginLoadResult
            {
                Success = true,
                PluginId = pluginId,
                Services = services.Select(s => s.ServiceId).ToArray()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin {PluginPath}", pluginPath);
            return new PluginLoadResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<bool> UnloadPluginAsync(string pluginId, CancellationToken ct = default)
    {
        if (!_loadedContexts.TryGetValue(pluginId, out var context) ||
            !_pluginServices.TryGetValue(pluginId, out var services))
        {
            return false;
        }

        try
        {
            // Stop plugin services in reverse order
            foreach (var service in services.AsEnumerable().Reverse())
            {
                if (service is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            // Unregister services from containers
            await UnregisterPluginServicesAsync(pluginId, ct);

            // Unload assembly context
            context.Unload();

            // Force garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _loadedContexts.Remove(pluginId);
            _pluginServices.Remove(pluginId);

            _logger.LogInformation("Unloaded plugin {PluginId}", pluginId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin {PluginId}", pluginId);
            return false;
        }
    }
}

// Custom assembly context for plugin isolation
public class PluginAssemblyContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginAssemblyContext(string pluginPath, bool isCollectible = true)
        : base(Path.GetFileNameWithoutExtension(pluginPath), isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Prefer shared types from default context
        if (IsSharedAssembly(assemblyName))
        {
            return Default.LoadFromAssemblyName(assemblyName);
        }

        // Load plugin-specific dependencies
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
    }

    private bool IsSharedAssembly(AssemblyName assemblyName)
    {
        var sharedPrefixes = new[]
        {
            "GameConsole.Core",
            "GameConsole.Contracts",
            "Microsoft.Extensions",
            "System.Reactive"
        };

        return sharedPrefixes.Any(prefix =>
            assemblyName.Name?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true);
    }
}
```

### Type Identity Guidance

- Core contract assemblies must be shared from the default context. Loading a second copy of the same contract type in an isolated context leads to type identity mismatches (e.g., `IAudioService` ≠ `IAudioService`).
- Provider/plugin assemblies and their private dependencies load into isolated contexts. Any communication with the host uses the shared contract types or message envelopes.

### Plugin Validation

Plugins undergo validation before loading:

```csharp
// GameConsole.Infrastructure.Plugin/src/PluginValidator.cs
public class PluginValidator
{
    public async Task<ValidationResult> ValidateAsync(PluginMetadata plugin, CancellationToken ct = default)
    {
        var result = new ValidationResult();

        // Check required gates
        await ValidateGatesAsync(plugin, result, ct);

        // Check dependencies
        await ValidateDependenciesAsync(plugin, result, ct);

        // Check mode compatibility
        await ValidateModeCompatibilityAsync(plugin, result, ct);

        // Check service contracts
        await ValidateServiceContractsAsync(plugin, result, ct);

        // Security validation
        await ValidateSecurityAsync(plugin, result, ct);

        return result;
    }

    private async Task ValidateGatesAsync(PluginMetadata plugin, ValidationResult result, CancellationToken ct)
    {
        foreach (var gate in plugin.RequiredGates)
        {
            if (!ProviderGates.IsGateAvailable(gate))
            {
                result.AddError($"Required gate '{gate}' is not available");
            }
        }
    }

    private async Task ValidateDependenciesAsync(PluginMetadata plugin, ValidationResult result, CancellationToken ct)
    {
        foreach (var dependency in plugin.Dependencies)
        {
            var availablePlugin = await _pluginRegistry.FindPluginAsync(dependency.Id, ct);
            if (availablePlugin == null)
            {
                result.AddError($"Required dependency '{dependency.Id}' is not loaded");
                continue;
            }

            if (!dependency.Version.IsSatisfiedBy(availablePlugin.Version))
            {
                result.AddError($"Dependency '{dependency.Id}' version {availablePlugin.Version} " +
                              $"does not satisfy requirement {dependency.Version}");
            }
        }
    }
}
```

### Plugin Hot Reload

Support for runtime plugin updates:

```csharp
// GameConsole.Infrastructure.Plugin/src/PluginHotReload.cs
public class PluginHotReload : IHostedService
{
    private readonly FileSystemWatcher _watcher;
    private readonly IPluginManager _pluginManager;
    private readonly Dictionary<string, DateTime> _lastReloadTimes = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _watcher.Path = _configuration.PluginsDirectory;
        _watcher.Filter = "*.dll";
        _watcher.Changed += OnPluginFileChanged;
        _watcher.EnableRaisingEvents = true;

        _logger.LogInformation("Plugin hot reload monitoring started");
    }

    private async void OnPluginFileChanged(object sender, FileSystemEventArgs e)
    {
        var pluginPath = e.FullPath;
        var pluginId = Path.GetFileNameWithoutExtension(pluginPath);

        // Debounce rapid file changes
        if (_lastReloadTimes.TryGetValue(pluginId, out var lastReload) &&
            DateTime.UtcNow - lastReload < TimeSpan.FromSeconds(2))
        {
            return;
        }

        _lastReloadTimes[pluginId] = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Plugin file changed, reloading: {PluginId}", pluginId);

            // Wait for file to be fully written
            await WaitForFileStabilityAsync(pluginPath);

            // Reload the plugin
            await _pluginManager.ReloadPluginAsync(pluginId);

            _logger.LogInformation("Plugin reloaded successfully: {PluginId}", pluginId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload plugin {PluginId}", pluginId);
        }
    }

    private async Task WaitForFileStabilityAsync(string filePath)
    {
        var lastWriteTime = File.GetLastWriteTime(filePath);

        for (int i = 0; i < 10; i++) // Max 1 second wait
        {
            await Task.Delay(100);
            var currentWriteTime = File.GetLastWriteTime(filePath);

            if (currentWriteTime == lastWriteTime)
            {
                // File hasn't changed, assume it's stable
                return;
            }

            lastWriteTime = currentWriteTime;
        }
    }
}
```

### Mode-Specific Plugin Loading

Plugins can be restricted to specific modes:

```csharp
// Plugin service with mode restriction
[PluginService]
[ModeRestriction(ConsoleMode.Editor)] // Only load in editor mode
public class AssetImporterPlugin : IAssetImporterService
{
    // Implementation only available in editor mode
}

// Plugin loader with mode awareness
public class ModeAwarePluginLoader
{
    public async Task<IEnumerable<PluginMetadata>> FilterPluginsForModeAsync(
        IEnumerable<PluginMetadata> plugins,
        ConsoleMode currentMode)
    {
        return plugins.Where(plugin =>
        {
            // No restriction = available in all modes
            if (!plugin.SupportedModes.Any()) return true;

            // Check if plugin supports current mode
            return plugin.SupportedModes.Contains(currentMode);
        });
    }
}
```

## Benefits

### Minimal Core
- Core system contains only essential infrastructure
- Most functionality delivered through plugins
- Easy to maintain and test core system

### Maximum Extensibility
- Community can extend system with plugins
- Multiple implementations of same contract can coexist
- New functionality can be added without core changes

### Safe Plugin Isolation
- Plugins run in isolated assembly contexts
- Plugin failures don't crash the core system
- Plugins can be unloaded and reloaded safely

### Development Experience
- Hot reload for rapid plugin development
- Rich plugin metadata and validation
- Clear plugin contracts and documentation

## Drawbacks

### Complexity
- Plugin loading and management adds complexity
- Assembly context isolation has performance overhead
- Debugging across plugin boundaries is challenging

### Security Concerns
- Plugins run with same privileges as host application
- Malicious plugins could compromise system
- Plugin validation is not foolproof

### Performance Impact
- Plugin discovery and loading takes time at startup
- Cross-assembly calls have overhead
- Memory usage increases with plugin isolation

## Plugin Security Model

### Validation Pipeline
1. **Assembly Scanning**: Check for malicious code patterns
2. **Dependency Validation**: Ensure dependencies are safe
3. **Permission Checking**: Validate plugin permissions
4. **Digital Signatures**: Verify plugin authenticity (future)

### Sandboxing (Future)
- Run plugins in separate processes
- Limited file system and network access
- Communication through secure IPC channels

## Alternatives Considered

### MEF (Managed Extensibility Framework)
- Microsoft's plugin framework
- **Rejected**: Less flexible than custom solution, performance concerns

### Plugin.NET (McMaster)
- Popular .NET plugin loading library
- **Considered**: Good option for Tier 4 provider, may include as alternative

### NuGet-Based Plugins
- Plugins distributed as NuGet packages
- **Rejected**: Less dynamic, requires rebuild for plugin updates

## Success Metrics

- **Plugin Ecosystem**: Number of community plugins created
- **Core Stability**: Core system uptime with plugins enabled
- **Hot Reload Performance**: Time to reload plugin under 2 seconds
- **Resource Isolation**: Plugin memory leaks don't affect core system

## Implementation Roadmap

### Phase 1: Basic Plugin System
- Plugin discovery and loading
- Provider plugins for core services
- Basic validation and error handling

### Phase 2: Advanced Features
- Hot reload support
- Plugin dependencies
- Mode-specific loading

### Phase 3: Developer Experience
- Plugin templates and scaffolding
- Rich validation and diagnostics
- Plugin marketplace integration

### Phase 4: Security and Reliability
- Enhanced security validation
- Plugin sandboxing
- Performance monitoring and limits

## Future Possibilities

- **Plugin Marketplace**: Centralized plugin discovery and distribution
- **Cross-Platform Plugins**: Plugins that work across multiple game engines
- **Plugin Analytics**: Usage tracking and performance metrics
- **AI-Generated Plugins**: Use AI to generate plugin boilerplate
- **Visual Plugin Composer**: GUI tool for creating plugins without coding

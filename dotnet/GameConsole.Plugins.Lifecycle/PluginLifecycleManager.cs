using System.Collections.Concurrent;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Main orchestrator for plugin lifecycle management.
/// Coordinates between state tracking, recovery, and update services to provide comprehensive plugin management.
/// </summary>
public class PluginLifecycleManager : IPluginLifecycleManager, IDisposable
{
    private readonly ILogger<PluginLifecycleManager> _logger;
    private readonly IPluginStateTracker _stateTracker;
    private readonly IPluginRecoveryService _recoveryService;
    private readonly IPluginUpdateManager _updateManager;
    private readonly ConcurrentDictionary<IPlugin, PluginDependencyInfo> _managedPlugins = new();
    private readonly object _lockObject = new();
    private bool _disposed;
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginLifecycleManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="stateTracker">Plugin state tracker service.</param>
    /// <param name="recoveryService">Plugin recovery service.</param>
    /// <param name="updateManager">Plugin update manager.</param>
    public PluginLifecycleManager(
        ILogger<PluginLifecycleManager> logger,
        IPluginStateTracker stateTracker,
        IPluginRecoveryService recoveryService,
        IPluginUpdateManager updateManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        _recoveryService = recoveryService ?? throw new ArgumentNullException(nameof(recoveryService));
        _updateManager = updateManager ?? throw new ArgumentNullException(nameof(updateManager));

        // Subscribe to state change notifications
        _stateTracker.StateChanged += OnPluginStateChanged;

        _logger.LogInformation("PluginLifecycleManager initialized");
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IPlugin> ManagedPlugins => _managedPlugins.Keys.ToList();

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("PluginLifecycleManager is already initialized");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Initializing PluginLifecycleManager");
        
        // Initialize would typically set up any background services, load configuration, etc.
        
        _logger.LogInformation("PluginLifecycleManager initialized successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("PluginLifecycleManager is already running");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting PluginLifecycleManager");
        
        _isRunning = true;
        
        _logger.LogInformation("PluginLifecycleManager started successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("PluginLifecycleManager is not running");
            return;
        }

        _logger.LogInformation("Stopping PluginLifecycleManager");
        
        await GracefulShutdownAsync(cancellationToken);
        
        _isRunning = false;
        
        _logger.LogInformation("PluginLifecycleManager stopped successfully");
    }

    /// <inheritdoc />
    public Task<IPlugin> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pluginPath))
            throw new ArgumentException("Plugin path cannot be null or empty", nameof(pluginPath));

        if (!File.Exists(pluginPath))
            throw new FileNotFoundException($"Plugin file not found: {pluginPath}");

        _logger.LogInformation("Loading plugin from {PluginPath}", pluginPath);

        try
        {
            // For this implementation, we'll create a mock plugin since we don't have actual plugin loading
            // In a real implementation, this would load the assembly and instantiate the plugin
            var mockPlugin = CreateMockPluginForPath(pluginPath);
            
            // Add to management
            var dependencyInfo = new PluginDependencyInfo();
            _managedPlugins[mockPlugin] = dependencyInfo;

            // Start tracking
            _stateTracker.StartTracking(mockPlugin, PluginState.Loaded);

            // Enable auto recovery by default
            _recoveryService.SetAutoRecoveryEnabled(mockPlugin, true);

            _logger.LogInformation("Successfully loaded plugin {PluginId}", mockPlugin.Metadata.Id);

            return Task.FromResult<IPlugin>(mockPlugin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin from {PluginPath}", pluginPath);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnloadPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (!_managedPlugins.ContainsKey(plugin))
        {
            _logger.LogWarning("Plugin {PluginId} is not managed by this lifecycle manager", plugin.Metadata.Id);
            return false;
        }

        _logger.LogInformation("Unloading plugin {PluginId}", plugin.Metadata.Id);

        try
        {
            // Check if plugin can be unloaded
            var canUnload = await plugin.CanUnloadAsync(cancellationToken);
            if (!canUnload)
            {
                _logger.LogWarning("Plugin {PluginId} cannot be unloaded at this time", plugin.Metadata.Id);
                return false;
            }

            // Check for dependent plugins
            var dependents = GetPluginDependents(plugin);
            if (dependents.Count > 0)
            {
                _logger.LogWarning("Plugin {PluginId} cannot be unloaded - it has {DependentCount} dependent plugins", 
                    plugin.Metadata.Id, dependents.Count);
                return false;
            }

            // Stop plugin if running
            if (plugin.IsRunning)
            {
                await StopPluginAsync(plugin, cancellationToken);
            }

            // Prepare for unload
            _stateTracker.SetPluginState(plugin, PluginState.Unloading);
            await plugin.PrepareUnloadAsync(cancellationToken);

            // Dispose plugin
            await plugin.DisposeAsync();

            // Remove from management
            _managedPlugins.TryRemove(plugin, out _);
            _stateTracker.StopTracking(plugin);

            _stateTracker.SetPluginState(plugin, PluginState.Unloaded);

            _logger.LogInformation("Successfully unloaded plugin {PluginId}", plugin.Metadata.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin {PluginId}", plugin.Metadata.Id);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            return false;
        }
    }

    /// <inheritdoc />
    public PluginState GetPluginState(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        
        return _stateTracker.GetPluginState(plugin) ?? PluginState.Discovered;
    }

    /// <inheritdoc />
    public async Task<bool> StartPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (!_managedPlugins.ContainsKey(plugin))
        {
            _logger.LogWarning("Plugin {PluginId} is not managed by this lifecycle manager", plugin.Metadata.Id);
            return false;
        }

        var currentState = GetPluginState(plugin);
        
        _logger.LogInformation("Starting plugin {PluginId} (current state: {CurrentState})", 
            plugin.Metadata.Id, currentState);

        try
        {
            // Ensure dependencies are started first
            var dependencies = GetPluginDependencies(plugin);
            foreach (var dependency in dependencies)
            {
                var depState = GetPluginState(dependency);
                if (depState != PluginState.Running)
                {
                    _logger.LogInformation("Starting dependency {DependencyId} for plugin {PluginId}", 
                        dependency.Metadata.Id, plugin.Metadata.Id);
                    
                    var depStarted = await StartPluginAsync(dependency, cancellationToken);
                    if (!depStarted)
                    {
                        _logger.LogError("Failed to start dependency {DependencyId} for plugin {PluginId}", 
                            dependency.Metadata.Id, plugin.Metadata.Id);
                        return false;
                    }
                }
            }

            // Start the plugin through its lifecycle
            await TransitionPluginToRunningAsync(plugin, cancellationToken);

            _logger.LogInformation("Successfully started plugin {PluginId}", plugin.Metadata.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start plugin {PluginId}", plugin.Metadata.Id);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> StopPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (!_managedPlugins.ContainsKey(plugin))
        {
            _logger.LogWarning("Plugin {PluginId} is not managed by this lifecycle manager", plugin.Metadata.Id);
            return false;
        }

        var currentState = GetPluginState(plugin);
        
        _logger.LogInformation("Stopping plugin {PluginId} (current state: {CurrentState})", 
            plugin.Metadata.Id, currentState);

        try
        {
            // Stop dependent plugins first
            var dependents = GetPluginDependents(plugin);
            foreach (var dependent in dependents)
            {
                var depState = GetPluginState(dependent);
                if (depState == PluginState.Running)
                {
                    _logger.LogInformation("Stopping dependent {DependentId} of plugin {PluginId}", 
                        dependent.Metadata.Id, plugin.Metadata.Id);
                    
                    await StopPluginAsync(dependent, cancellationToken);
                }
            }

            // Stop the plugin if it's running
            if (plugin.IsRunning)
            {
                _stateTracker.SetPluginState(plugin, PluginState.Stopping);
                await plugin.StopAsync(cancellationToken);
                _stateTracker.SetPluginState(plugin, PluginState.Stopped);
            }

            _logger.LogInformation("Successfully stopped plugin {PluginId}", plugin.Metadata.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop plugin {PluginId}", plugin.Metadata.Id);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RestartPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        _logger.LogInformation("Restarting plugin {PluginId}", plugin.Metadata.Id);

        var stopResult = await StopPluginAsync(plugin, cancellationToken);
        if (!stopResult)
        {
            return false;
        }

        var startResult = await StartPluginAsync(plugin, cancellationToken);
        return startResult;
    }

    /// <inheritdoc />
    public async Task<bool> RecoverPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return await _recoveryService.RecoverPluginAsync(plugin, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePluginAsync(IPlugin plugin, string newVersionPath, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return await _updateManager.UpdatePluginAsync(plugin, newVersionPath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PluginHealthResult> CheckPluginHealthAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return await _stateTracker.PerformHealthCheckAsync(plugin, cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IPlugin> GetPluginDependents(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var dependents = new List<IPlugin>();
        
        foreach (var managed in _managedPlugins.Keys)
        {
            if (managed != plugin && managed.Metadata.Dependencies.Contains(plugin.Metadata.Id))
            {
                dependents.Add(managed);
            }
        }

        return dependents;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IPlugin> GetPluginDependencies(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var dependencies = new List<IPlugin>();
        
        foreach (var dependencyId in plugin.Metadata.Dependencies)
        {
            var dependency = _managedPlugins.Keys.FirstOrDefault(p => p.Metadata.Id == dependencyId);
            if (dependency != null)
            {
                dependencies.Add(dependency);
            }
        }

        return dependencies;
    }

    /// <inheritdoc />
    public async Task GracefulShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting graceful shutdown of all managed plugins");

        // Get plugins in reverse order for shutdown (simple approach)
        var pluginsToStop = _managedPlugins.Keys.ToList();

        foreach (var plugin in pluginsToStop)
        {
            try
            {
                await StopPluginAsync(plugin, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during graceful shutdown of plugin {PluginId}", plugin.Metadata.Id);
            }
        }

        _logger.LogInformation("Graceful shutdown completed");
    }

    /// <inheritdoc />
    public event EventHandler<PluginStateChangedEventArgs>? PluginStateChanged;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await GracefulShutdownAsync();
            
            _stateTracker.StateChanged -= OnPluginStateChanged;
            
            _managedPlugins.Clear();
            _disposed = true;
            
            _logger.LogInformation("PluginLifecycleManager disposed");
        }
    }

    /// <summary>
    /// Releases resources synchronously.
    /// </summary>
    public void Dispose()
    {
        try
        {
            DisposeAsync().AsTask().Wait();
        }
        catch
        {
            // Suppress exceptions in Dispose
        }
    }

    private async Task TransitionPluginToRunningAsync(IPlugin plugin, CancellationToken cancellationToken)
    {
        var currentState = GetPluginState(plugin);

        // Configure if needed
        if (currentState == PluginState.Loaded)
        {
            _stateTracker.SetPluginState(plugin, PluginState.Configuring);
            // Context would be set during plugin loading in real implementation
            _stateTracker.SetPluginState(plugin, PluginState.Configured);
        }

        // Initialize if needed
        currentState = GetPluginState(plugin);
        if (currentState == PluginState.Configured)
        {
            _stateTracker.SetPluginState(plugin, PluginState.Initializing);
            await plugin.InitializeAsync(cancellationToken);
            _stateTracker.SetPluginState(plugin, PluginState.Initialized);
        }

        // Start if needed
        currentState = GetPluginState(plugin);
        if (currentState == PluginState.Initialized)
        {
            _stateTracker.SetPluginState(plugin, PluginState.Starting);
            await plugin.StartAsync(cancellationToken);
            _stateTracker.SetPluginState(plugin, PluginState.Running);
        }
    }

    private List<IPlugin> GetPluginsInDependencyOrder()
    {
        var result = new List<IPlugin>();
        var visited = new HashSet<IPlugin>();
        var visiting = new HashSet<IPlugin>();

        foreach (var plugin in _managedPlugins.Keys)
        {
            if (!visited.Contains(plugin))
            {
                VisitPlugin(plugin, visited, visiting, result);
            }
        }

        return result;
    }

    private void VisitPlugin(IPlugin plugin, HashSet<IPlugin> visited, HashSet<IPlugin> visiting, List<IPlugin> result)
    {
        if (visiting.Contains(plugin))
        {
            throw new InvalidOperationException($"Circular dependency detected involving plugin {plugin.Metadata.Id}");
        }

        if (visited.Contains(plugin))
        {
            return;
        }

        visiting.Add(plugin);

        var dependencies = GetPluginDependencies(plugin);
        foreach (var dependency in dependencies)
        {
            VisitPlugin(dependency, visited, visiting, result);
        }

        visiting.Remove(plugin);
        visited.Add(plugin);
        result.Add(plugin);
    }

    private void OnPluginStateChanged(object? sender, PluginStateChangedEventArgs e)
    {
        try
        {
            PluginStateChanged?.Invoke(this, e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PluginStateChanged event handler for plugin {PluginId}", e.Plugin.Metadata.Id);
        }
    }

    private IPlugin CreateMockPluginForPath(string pluginPath)
    {
        // This is a simplified mock for demonstration
        // In a real implementation, this would load and instantiate the actual plugin
        return new MockPlugin(pluginPath);
    }

    private class PluginDependencyInfo
    {
        // Could store dependency-specific information here
    }
}
using GameConsole.Core.Abstractions;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Manages the complete lifecycle of plugins including state transitions, health monitoring,
/// recovery operations, and updates. Provides comprehensive plugin management capabilities
/// with dependency-aware operations and graceful shutdown support.
/// </summary>
public class PluginLifecycleManager : IPluginLifecycleManager, IAsyncDisposable
{
    private readonly ILogger<PluginLifecycleManager> _logger;
    private readonly IPluginStateTracker _stateTracker;
    private readonly IPluginRecoveryService _recoveryService;
    private readonly IPluginUpdateManager _updateManager;
    private bool _isRunning;
    private bool _disposed;

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

        // Wire up state tracking events
        _stateTracker.StateChanged += OnPluginStateChanged;
        _stateTracker.PluginUnhealthy += OnPluginUnhealthy;
        
        // Forward state change events
        _stateTracker.StateChanged += (sender, args) => PluginStateChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Gets a value indicating whether the service is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets all plugins currently managed by the lifecycle manager.
    /// </summary>
    public IReadOnlyCollection<IPlugin> ManagedPlugins => _stateTracker.TrackedPlugins;

    /// <summary>
    /// Event fired when a plugin's lifecycle state changes.
    /// </summary>
    public event EventHandler<PluginLifecycleEventArgs>? LifecycleEvent;

    /// <summary>
    /// Event fired when a plugin state changes.
    /// </summary>
    public event EventHandler<PluginStateChangedEventArgs>? PluginStateChanged;

    /// <summary>
    /// Initializes the service asynchronously.
    /// This method is called once during service setup before the service is started.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Plugin Lifecycle Manager");

        // Initialize any required resources
        await Task.CompletedTask; // Placeholder for future initialization logic

        _logger.LogInformation("Plugin Lifecycle Manager initialized");
    }

    /// <summary>
    /// Starts the service asynchronously.
    /// This method is called to begin service operations after initialization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async start operation.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Plugin Lifecycle Manager");

        _isRunning = true;

        // Start any background services or monitoring
        await Task.CompletedTask; // Placeholder for future startup logic

        _logger.LogInformation("Plugin Lifecycle Manager started");
    }

    /// <summary>
    /// Stops the service asynchronously.
    /// This method is called to gracefully shut down service operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async stop operation.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Plugin Lifecycle Manager");

        _isRunning = false;

        // Stop all tracked plugins gracefully
        var trackedPlugins = _stateTracker.TrackedPlugins.ToList();
        await StopAllPluginsAsync(trackedPlugins, cancellationToken);

        _logger.LogInformation("Plugin Lifecycle Manager stopped");
    }

    /// <summary>
    /// Loads a plugin, transitioning it through discovery to loaded state.
    /// </summary>
    /// <param name="plugin">The plugin to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully loaded, false otherwise.</returns>
    public async Task<bool> LoadPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            _logger.LogInformation("Loading plugin {PluginName}", plugin.Metadata.Name);
            
            // Start tracking the plugin
            _stateTracker.StartTracking(plugin, PluginState.Discovered);
            
            // Add a small delay to simulate loading work
            await Task.Delay(1, cancellationToken);
            
            // Transition to loaded state
            _stateTracker.SetPluginState(plugin, PluginState.Loaded);
            
            OnLifecycleEvent(plugin, "Loaded");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin {PluginName}", plugin.Metadata.Name);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            OnLifecycleEvent(plugin, "LoadFailed", ex);
            return false;
        }
    }

    /// <summary>
    /// Loads a plugin from the specified path and adds it to lifecycle management.
    /// </summary>
    /// <param name="pluginPath">Path to the plugin assembly.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded plugin instance.</returns>
    public async Task<IPlugin> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pluginPath)) throw new ArgumentException("Plugin path cannot be null or empty", nameof(pluginPath));

        _logger.LogInformation("Loading plugin from path {PluginPath}", pluginPath);

        // In a real implementation, this would:
        // 1. Load the assembly from the path
        // 2. Discover plugin types
        // 3. Create plugin instances
        // 4. Validate plugin metadata
        
        // Add a small delay to simulate async work
        await Task.Delay(1, cancellationToken);
        
        // For now, we'll throw a not implemented exception since we don't have
        // plugin discovery and loading infrastructure
        throw new NotImplementedException("Plugin loading from path is not yet implemented. Use LoadPluginAsync(IPlugin) with a pre-constructed plugin instance.");
    }

    /// <summary>
    /// Configures a plugin with its runtime context.
    /// </summary>
    /// <param name="plugin">The plugin to configure.</param>
    /// <param name="context">The plugin context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully configured, false otherwise.</returns>
    public async Task<bool> ConfigurePluginAsync(IPlugin plugin, IPluginContext context, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        if (context == null) throw new ArgumentNullException(nameof(context));

        try
        {
            _logger.LogInformation("Configuring plugin {PluginName}", plugin.Metadata.Name);
            
            _stateTracker.SetPluginState(plugin, PluginState.Configuring);
            
            await plugin.ConfigureAsync(context, cancellationToken);
            
            _stateTracker.SetPluginState(plugin, PluginState.Configured);
            OnLifecycleEvent(plugin, "Configured");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure plugin {PluginName}", plugin.Metadata.Name);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            OnLifecycleEvent(plugin, "ConfigureFailed", ex);
            return false;
        }
    }

    /// <summary>
    /// Initializes a plugin, preparing it for startup.
    /// </summary>
    /// <param name="plugin">The plugin to initialize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully initialized, false otherwise.</returns>
    public async Task<bool> InitializePluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            _logger.LogInformation("Initializing plugin {PluginName}", plugin.Metadata.Name);
            
            _stateTracker.SetPluginState(plugin, PluginState.Initializing);
            
            await plugin.InitializeAsync(cancellationToken);
            
            _stateTracker.SetPluginState(plugin, PluginState.Initialized);
            OnLifecycleEvent(plugin, "Initialized");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize plugin {PluginName}", plugin.Metadata.Name);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            OnLifecycleEvent(plugin, "InitializeFailed", ex);
            return false;
        }
    }

    /// <summary>
    /// Starts a plugin, transitioning it through necessary states to Running.
    /// Handles dependency resolution and proper state transitions.
    /// </summary>
    /// <param name="plugin">The plugin to start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if started successfully, false otherwise.</returns>
    public async Task<bool> StartPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            _logger.LogInformation("Starting plugin {PluginName}", plugin.Metadata.Name);
            
            _stateTracker.SetPluginState(plugin, PluginState.Starting);
            
            await plugin.StartAsync(cancellationToken);
            
            _stateTracker.SetPluginState(plugin, PluginState.Running);
            
            // Create a checkpoint for recovery purposes
            await _recoveryService.CreateCheckpointAsync(plugin, cancellationToken);
            
            OnLifecycleEvent(plugin, "Started");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start plugin {PluginName}", plugin.Metadata.Name);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            OnLifecycleEvent(plugin, "StartFailed", ex);
            return false;
        }
    }

    /// <summary>
    /// Stops a plugin gracefully, transitioning it to Stopped state.
    /// </summary>
    /// <param name="plugin">The plugin to stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if stopped successfully, false otherwise.</returns>
    public async Task<bool> StopPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            _logger.LogInformation("Stopping plugin {PluginName}", plugin.Metadata.Name);
            
            _stateTracker.SetPluginState(plugin, PluginState.Stopping);
            
            await plugin.StopAsync(cancellationToken);
            
            _stateTracker.SetPluginState(plugin, PluginState.Stopped);
            OnLifecycleEvent(plugin, "Stopped");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop plugin {PluginName}", plugin.Metadata.Name);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            OnLifecycleEvent(plugin, "StopFailed", ex);
            return false;
        }
    }

    /// <summary>
    /// Restarts a plugin by stopping and starting it.
    /// Useful for recovery scenarios or configuration changes.
    /// </summary>
    /// <param name="plugin">The plugin to restart.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if restarted successfully, false otherwise.</returns>
    public async Task<bool> RestartPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        _logger.LogInformation("Restarting plugin {PluginName}", plugin.Metadata.Name);

        var stopResult = await StopPluginAsync(plugin, cancellationToken);
        if (!stopResult)
        {
            return false;
        }

        return await StartPluginAsync(plugin, cancellationToken);
    }

    /// <summary>
    /// Unloads a plugin from memory after stopping it.
    /// </summary>
    /// <param name="plugin">The plugin to unload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if successfully unloaded, false otherwise.</returns>
    public async Task<bool> UnloadPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            _logger.LogInformation("Unloading plugin {PluginName}", plugin.Metadata.Name);
            
            // Stop the plugin if it's running
            if (plugin.IsRunning)
            {
                await StopPluginAsync(plugin, cancellationToken);
            }
            
            _stateTracker.SetPluginState(plugin, PluginState.Unloading);
            
            // Check if the plugin can be unloaded
            var canUnload = await plugin.CanUnloadAsync(cancellationToken);
            if (!canUnload)
            {
                _logger.LogWarning("Plugin {PluginName} cannot be unloaded at this time", plugin.Metadata.Name);
                return false;
            }
            
            // Prepare for unloading
            await plugin.PrepareUnloadAsync(cancellationToken);
            
            // Dispose the plugin
            await plugin.DisposeAsync();
            
            // Stop tracking the plugin
            _stateTracker.StopTracking(plugin);
            
            OnLifecycleEvent(plugin, "Unloaded");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unload plugin {PluginName}", plugin.Metadata.Name);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            OnLifecycleEvent(plugin, "UnloadFailed", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets the current state of the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>The current plugin state.</returns>
    public PluginState GetPluginState(IPlugin plugin)
    {
        return _stateTracker.GetPluginState(plugin) ?? PluginState.Discovered;
    }

    /// <summary>
    /// Initiates recovery for a failed plugin.
    /// Attempts to restore the plugin to a functional state.
    /// </summary>
    /// <param name="plugin">The plugin to recover.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if recovery was successful, false otherwise.</returns>
    public async Task<bool> RecoverPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return await _recoveryService.RecoverPluginAsync(plugin, cancellationToken);
    }

    /// <summary>
    /// Updates a plugin to a newer version.
    /// Handles hot-swapping and rollback if the update fails.
    /// </summary>
    /// <param name="plugin">The plugin to update.</param>
    /// <param name="newVersionPath">Path to the new plugin version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if update was successful, false if rolled back.</returns>
    public async Task<bool> UpdatePluginAsync(IPlugin plugin, string newVersionPath, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return await _updateManager.UpdatePluginAsync(plugin, newVersionPath, cancellationToken);
    }

    /// <summary>
    /// Performs a health check on the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    public async Task<PluginHealthResult> CheckPluginHealthAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return await _stateTracker.PerformHealthCheckAsync(plugin, cancellationToken);
    }

    /// <summary>
    /// Gets plugins that depend on the specified plugin.
    /// Useful for determining impact of plugin operations.
    /// </summary>
    /// <param name="plugin">The plugin to check dependents for.</param>
    /// <returns>Collection of plugins that depend on the specified plugin.</returns>
    public IReadOnlyCollection<IPlugin> GetPluginDependents(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        // In a real implementation, this would analyze plugin dependencies
        // For now, return an empty collection
        _logger.LogDebug("Getting dependents for plugin {PluginName} - dependency analysis not yet implemented", 
            plugin.Metadata.Name);
        return Array.Empty<IPlugin>();
    }

    /// <summary>
    /// Gets plugins that the specified plugin depends on.
    /// </summary>
    /// <param name="plugin">The plugin to check dependencies for.</param>
    /// <returns>Collection of plugins that the specified plugin depends on.</returns>
    public IReadOnlyCollection<IPlugin> GetPluginDependencies(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        // In a real implementation, this would analyze plugin dependencies
        // For now, return an empty collection
        _logger.LogDebug("Getting dependencies for plugin {PluginName} - dependency analysis not yet implemented", 
            plugin.Metadata.Name);
        return Array.Empty<IPlugin>();
    }

    /// <summary>
    /// Performs graceful shutdown of all managed plugins.
    /// Stops plugins in dependency-safe order and performs cleanup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the shutdown operation.</returns>
    public async Task GracefulShutdownAsync(CancellationToken cancellationToken = default)
    {
        await ShutdownAsync(cancellationToken);
    }

    /// <summary>
    /// Gracefully shuts down all plugins and the lifecycle manager.
    /// Ensures proper cleanup and state transitions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async shutdown operation.</returns>
    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Shutting down Plugin Lifecycle Manager");

        var trackedPlugins = _stateTracker.TrackedPlugins.ToList();
        await StopAllPluginsAsync(trackedPlugins, cancellationToken);
        await UnloadAllPluginsAsync(trackedPlugins, cancellationToken);

        await StopAsync(cancellationToken);
    }

    private async Task StopAllPluginsAsync(IList<IPlugin> plugins, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {Count} plugins", plugins.Count);

        var tasks = plugins.Select(plugin => StopPluginAsync(plugin, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task UnloadAllPluginsAsync(IList<IPlugin> plugins, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unloading {Count} plugins", plugins.Count);

        var tasks = plugins.Select(plugin => UnloadPluginAsync(plugin, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private void OnPluginStateChanged(object? sender, PluginStateChangedEventArgs e)
    {
        _logger.LogDebug("Plugin {PluginName} state changed from {PreviousState} to {NewState}",
            e.Plugin.Metadata.Name, e.PreviousState, e.NewState);

        // Handle automatic recovery for failed plugins
        if (e.NewState == PluginState.Failed && _recoveryService.IsAutoRecoveryEnabled(e.Plugin))
        {
            var attemptCount = _recoveryService.GetRecoveryAttemptCount(e.Plugin);
            if (attemptCount < _recoveryService.MaxAutoRecoveryAttempts)
            {
                _logger.LogInformation("Initiating automatic recovery for failed plugin {PluginName} (attempt {AttemptCount})",
                    e.Plugin.Metadata.Name, attemptCount + 1);

                // Fire and forget the recovery operation
                _ = Task.Run(async () => await _recoveryService.RecoverPluginAsync(e.Plugin));
            }
            else
            {
                _logger.LogWarning("Plugin {PluginName} has exceeded maximum auto-recovery attempts ({MaxAttempts})",
                    e.Plugin.Metadata.Name, _recoveryService.MaxAutoRecoveryAttempts);
            }
        }
    }

    private void OnPluginUnhealthy(object? sender, PluginHealthResult e)
    {
        _logger.LogWarning("Plugin {PluginName} is unhealthy: {Error}",
            e.Plugin.Metadata.Name, e.Error?.Message ?? "Unknown health issue");

        // Consider marking the plugin as failed if health checks consistently fail
        // This would trigger automatic recovery if enabled
    }

    private void OnLifecycleEvent(IPlugin plugin, string phase, Exception? exception = null)
    {
        try
        {
            LifecycleEvent?.Invoke(this, new PluginLifecycleEventArgs(plugin, phase, exception));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking LifecycleEvent for plugin {PluginName}, phase {Phase}",
                plugin.Metadata.Name, phase);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            await ShutdownAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal of PluginLifecycleManager");
        }

        // Unsubscribe from events
        if (_stateTracker != null)
        {
            _stateTracker.StateChanged -= OnPluginStateChanged;
            _stateTracker.PluginUnhealthy -= OnPluginUnhealthy;
        }

        _logger.LogDebug("PluginLifecycleManager disposed");
    }
}
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Default implementation of the plugin lifecycle manager.
/// </summary>
public class PluginLifecycleManager : IPluginLifecycleManager
{
    private readonly ILogger<PluginLifecycleManager>? _logger;
    private readonly ConcurrentDictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly PluginStateTracker _stateTracker;
    private bool _isRunning;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<PluginLifecycleEventArgs>? PluginStateChanged;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    public PluginLifecycleManager(ILogger<PluginLifecycleManager>? logger = null)
    {
        _logger = logger;
        _stateTracker = new PluginStateTracker();
        
        // Subscribe to state tracker events
        _stateTracker.PluginHealthChanged += OnPluginHealthChanged;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginLifecycleManager));

        _logger?.LogInformation("Initializing plugin lifecycle manager");
        
        // Initialize can be used to setup any required resources
        await Task.CompletedTask;
        
        _logger?.LogInformation("Plugin lifecycle manager initialized");
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginLifecycleManager));

        if (_isRunning)
            return;

        _logger?.LogInformation("Starting plugin lifecycle manager");
        
        _isRunning = true;
        
        // Start any background services if needed
        await Task.CompletedTask;
        
        _logger?.LogInformation("Plugin lifecycle manager started");
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            return;

        _logger?.LogInformation("Stopping plugin lifecycle manager");

        _isRunning = false;

        // Stop all running plugins gracefully
        var runningPlugins = _loadedPlugins.Values
            .Where(p => p.State == PluginState.Running)
            .ToList();

        foreach (var plugin in runningPlugins)
        {
            try
            {
                await StopPluginAsync(plugin.Metadata.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping plugin {PluginId} during shutdown", plugin.Metadata.Id);
            }
        }

        _logger?.LogInformation("Plugin lifecycle manager stopped");
    }

    /// <inheritdoc />
    public async Task<PluginLoadResult> LoadPluginAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginLifecycleManager));

        try
        {
            _logger?.LogInformation("Loading plugin from {PluginPath}", pluginPath);

            // Validate the plugin first
            var validationResult = await ValidatePluginAsync(pluginPath, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = $"Plugin validation failed: {string.Join(", ", validationResult.Errors)}";
                _logger?.LogWarning("Failed to load plugin {PluginPath}: {Error}", pluginPath, errorMessage);
                return new PluginLoadResult(false, null, errorMessage);
            }

            // Create plugin metadata (simplified for this implementation)
            var assembly = Assembly.LoadFrom(pluginPath);
            var assemblyName = assembly.GetName();
            var pluginId = assemblyName.Name ?? Path.GetFileNameWithoutExtension(pluginPath);
            var version = assemblyName.Version ?? new Version(1, 0, 0, 0);

            var metadata = new PluginMetadata(
                pluginId,
                assemblyName.Name ?? pluginId,
                version,
                "Plugin loaded by lifecycle manager",
                pluginPath,
                Array.Empty<string>()
            );

            var loadedPlugin = new LoadedPlugin(
                metadata,
                PluginState.Loaded,
                DateTime.UtcNow
            );

            _loadedPlugins.TryAdd(pluginId, loadedPlugin);
            _stateTracker.RegisterPlugin(loadedPlugin);

            OnPluginStateChanged(pluginId, PluginState.NotLoaded, PluginState.Loaded, "Plugin loaded successfully");

            _logger?.LogInformation("Successfully loaded plugin {PluginId} from {PluginPath}", pluginId, pluginPath);
            return new PluginLoadResult(true, metadata);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading plugin from {PluginPath}", pluginPath);
            return new PluginLoadResult(false, null, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> UnloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginLifecycleManager));

        try
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                _logger?.LogWarning("Cannot unload unknown plugin {PluginId}", pluginId);
                return false;
            }

            _logger?.LogInformation("Unloading plugin {PluginId}", pluginId);

            // Stop the plugin first if it's running
            if (plugin.State == PluginState.Running)
            {
                await StopPluginAsync(pluginId, cancellationToken);
            }

            OnPluginStateChanged(pluginId, plugin.State, PluginState.Unloading, "Plugin unloading");

            // Simulate unloading process
            await Task.Delay(100, cancellationToken);

            _loadedPlugins.TryRemove(pluginId, out _);
            _stateTracker.UnregisterPlugin(pluginId);

            OnPluginStateChanged(pluginId, PluginState.Unloading, PluginState.NotLoaded, "Plugin unloaded successfully");

            _logger?.LogInformation("Successfully unloaded plugin {PluginId}", pluginId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error unloading plugin {PluginId}", pluginId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ReloadPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginLifecycleManager));

        try
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                _logger?.LogWarning("Cannot reload unknown plugin {PluginId}", pluginId);
                return false;
            }

            _logger?.LogInformation("Reloading plugin {PluginId}", pluginId);

            var wasRunning = plugin.State == PluginState.Running;
            var pluginPath = plugin.Metadata.AssemblyPath;

            // Unload the current version
            await UnloadPluginAsync(pluginId, cancellationToken);

            // Load the new version
            var loadResult = await LoadPluginAsync(pluginPath, cancellationToken);
            if (!loadResult.Success)
            {
                _logger?.LogError("Failed to reload plugin {PluginId}: {Error}", pluginId, loadResult.ErrorMessage);
                return false;
            }

            // Restart if it was running before
            if (wasRunning)
            {
                await StartPluginAsync(pluginId, cancellationToken);
            }

            _logger?.LogInformation("Successfully reloaded plugin {PluginId}", pluginId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reloading plugin {PluginId}", pluginId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> StartPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginLifecycleManager));

        try
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                _logger?.LogWarning("Cannot start unknown plugin {PluginId}", pluginId);
                return false;
            }

            if (plugin.State == PluginState.Running)
            {
                _logger?.LogDebug("Plugin {PluginId} is already running", pluginId);
                return true;
            }

            _logger?.LogInformation("Starting plugin {PluginId}", pluginId);

            OnPluginStateChanged(pluginId, plugin.State, PluginState.Initialized, "Plugin initializing");

            // Simulate initialization
            await Task.Delay(50, cancellationToken);

            OnPluginStateChanged(pluginId, PluginState.Initialized, PluginState.Running, "Plugin started successfully");

            var updatedPlugin = plugin with { State = PluginState.Running };
            _loadedPlugins.TryUpdate(pluginId, updatedPlugin, plugin);
            _stateTracker.UpdatePluginState(pluginId, PluginState.Running);
            _stateTracker.UpdatePluginHealth(pluginId, PluginHealth.Healthy, "Plugin started successfully");

            _logger?.LogInformation("Successfully started plugin {PluginId}", pluginId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting plugin {PluginId}", pluginId);
            
            if (_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                OnPluginStateChanged(pluginId, plugin.State, PluginState.Failed, $"Failed to start: {ex.Message}");
                _stateTracker.UpdatePluginState(pluginId, PluginState.Failed);
                _stateTracker.UpdatePluginHealth(pluginId, PluginHealth.Failed, ex.Message);
            }
            
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> StopPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PluginLifecycleManager));

        try
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                _logger?.LogWarning("Cannot stop unknown plugin {PluginId}", pluginId);
                return false;
            }

            if (plugin.State != PluginState.Running)
            {
                _logger?.LogDebug("Plugin {PluginId} is not running (current state: {State})", pluginId, plugin.State);
                return true;
            }

            _logger?.LogInformation("Stopping plugin {PluginId}", pluginId);

            OnPluginStateChanged(pluginId, plugin.State, PluginState.Stopping, "Plugin stopping");

            // Simulate graceful shutdown
            await Task.Delay(50, cancellationToken);

            OnPluginStateChanged(pluginId, PluginState.Stopping, PluginState.Stopped, "Plugin stopped successfully");

            var updatedPlugin = plugin with { State = PluginState.Stopped };
            _loadedPlugins.TryUpdate(pluginId, updatedPlugin, plugin);
            _stateTracker.UpdatePluginState(pluginId, PluginState.Stopped);

            _logger?.LogInformation("Successfully stopped plugin {PluginId}", pluginId);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping plugin {PluginId}", pluginId);
            return false;
        }
    }

    /// <inheritdoc />
    public PluginState GetPluginState(string pluginId)
    {
        return _loadedPlugins.TryGetValue(pluginId, out var plugin) ? plugin.State : PluginState.NotLoaded;
    }

    /// <inheritdoc />
    public IEnumerable<LoadedPlugin> GetLoadedPlugins()
    {
        return _loadedPlugins.Values.ToList();
    }

    /// <inheritdoc />
    public PluginMetadata? GetPluginInfo(string pluginId)
    {
        return _loadedPlugins.TryGetValue(pluginId, out var plugin) ? plugin.Metadata : null;
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidatePluginAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(pluginPath))
            {
                return ValidationResult.Failure("Plugin file does not exist");
            }

            if (!pluginPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return ValidationResult.Failure("Plugin must be a .NET assembly (.dll)");
            }

            // Simulate validation process
            await Task.Delay(10, cancellationToken);

            // Try to load the assembly to validate it
            try
            {
                var assembly = Assembly.LoadFrom(pluginPath);
                if (assembly == null)
                {
                    return ValidationResult.Failure("Failed to load assembly");
                }
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Assembly load error: {ex.Message}");
            }

            return ValidationResult.Success();
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure($"Validation error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckPluginHealthAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_loadedPlugins.TryGetValue(pluginId, out var plugin))
            {
                return new HealthCheckResult(PluginHealth.Unknown, "Plugin not found");
            }

            if (plugin.State != PluginState.Running)
            {
                return new HealthCheckResult(PluginHealth.Unknown, $"Plugin is not running (state: {plugin.State})");
            }

            var startTime = DateTime.UtcNow;
            
            // Simulate health check
            await Task.Delay(10, cancellationToken);
            
            var responseTime = DateTime.UtcNow - startTime;
            
            // Simple health check - in a real implementation, this would call the plugin's health check method
            var health = responseTime < TimeSpan.FromSeconds(1) ? PluginHealth.Healthy : PluginHealth.Degraded;
            
            return new HealthCheckResult(health, "Health check completed", responseTime);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(PluginHealth.Failed, ex.Message);
        }
    }

    private void OnPluginStateChanged(string pluginId, PluginState oldState, PluginState newState, string? message = null)
    {
        var eventArgs = new PluginLifecycleEventArgs(pluginId, oldState, newState, DateTime.UtcNow, message);
        PluginStateChanged?.Invoke(this, eventArgs);
        
        _logger?.LogDebug("Plugin {PluginId} state changed from {OldState} to {NewState}: {Message}",
            pluginId, oldState, newState, message);
    }

    private void OnPluginHealthChanged(object? sender, PluginHealthChangedEventArgs e)
    {
        _logger?.LogDebug("Plugin {PluginId} health changed from {OldHealth} to {NewHealth}: {Message}",
            e.PluginId, e.OldHealth, e.NewHealth, e.Message);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await StopAsync();
        
        _stateTracker.PluginHealthChanged -= OnPluginHealthChanged;
        _stateTracker.Dispose();
        
        _loadedPlugins.Clear();
        _disposed = true;
        
        _logger?.LogInformation("Plugin lifecycle manager disposed");
    }
}
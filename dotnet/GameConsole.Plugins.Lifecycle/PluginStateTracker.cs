using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Tracks plugin states and provides health monitoring capabilities.
/// Monitors plugin health through watchdog timers and periodic checks.
/// </summary>
public class PluginStateTracker : IPluginStateTracker, IDisposable
{
    private readonly ILogger<PluginStateTracker> _logger;
    private readonly ConcurrentDictionary<IPlugin, PluginStateInfo> _pluginStates = new();
    private readonly ConcurrentDictionary<IPlugin, Timer> _healthCheckTimers = new();
    private readonly ConcurrentDictionary<IPlugin, bool> _unhealthyPlugins = new();
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _healthCheckTimeout = TimeSpan.FromSeconds(10);
    private bool _disposed;

    public PluginStateTracker(ILogger<PluginStateTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Event fired when a plugin's state changes.
    /// </summary>
    public event EventHandler<PluginStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Event fired when a plugin is determined to be unhealthy.
    /// </summary>
    public event EventHandler<PluginHealthResult>? PluginUnhealthy;

    /// <summary>
    /// Gets all currently tracked plugins.
    /// </summary>
    public IReadOnlyCollection<IPlugin> TrackedPlugins => _pluginStates.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Gets plugins in the specified state.
    /// </summary>
    /// <param name="state">The state to filter by.</param>
    /// <returns>Collection of plugins in the specified state.</returns>
    public IReadOnlyCollection<IPlugin> GetPluginsInState(PluginState state)
    {
        return _pluginStates
            .Where(kvp => kvp.Value.State == state)
            .Select(kvp => kvp.Key)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets plugins that are currently unhealthy.
    /// </summary>
    public IReadOnlyCollection<IPlugin> UnhealthyPlugins => _unhealthyPlugins.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Gets the current state of the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>The current plugin state, or null if not tracked.</returns>
    public PluginState? GetPluginState(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return _pluginStates.TryGetValue(plugin, out var stateInfo) ? stateInfo.State : null;
    }

    /// <summary>
    /// Sets the state of a plugin and records the transition.
    /// </summary>
    /// <param name="plugin">The plugin whose state to set.</param>
    /// <param name="newState">The new state.</param>
    /// <param name="exception">Optional exception associated with the state change.</param>
    public void SetPluginState(IPlugin plugin, PluginState newState, Exception? exception = null)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var previousState = GetPluginState(plugin) ?? PluginState.Discovered;
        
        var stateInfo = new PluginStateInfo(newState, DateTimeOffset.UtcNow, exception);
        _pluginStates.AddOrUpdate(plugin, stateInfo, (_, _) => stateInfo);

        _logger.LogInformation("Plugin {PluginName} state changed from {PreviousState} to {NewState}",
            plugin.Metadata.Name, previousState, newState);

        if (exception != null)
        {
            _logger.LogError(exception, "Plugin {PluginName} state changed to {NewState} due to error",
                plugin.Metadata.Name, newState);
        }

        try
        {
            StateChanged?.Invoke(this, new PluginStateChangedEventArgs(plugin, previousState, newState, exception));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking StateChanged event for plugin {PluginName}", plugin.Metadata.Name);
        }
    }

    /// <summary>
    /// Starts tracking a plugin's state and health.
    /// </summary>
    /// <param name="plugin">The plugin to start tracking.</param>
    /// <param name="initialState">The initial state of the plugin.</param>
    public void StartTracking(IPlugin plugin, PluginState initialState = PluginState.Discovered)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        SetPluginState(plugin, initialState);

        // Start health monitoring for running plugins
        if (initialState == PluginState.Running)
        {
            StartHealthMonitoring(plugin);
        }

        _logger.LogDebug("Started tracking plugin {PluginName} in state {State}",
            plugin.Metadata.Name, initialState);
    }

    /// <summary>
    /// Stops tracking a plugin's state and health.
    /// </summary>
    /// <param name="plugin">The plugin to stop tracking.</param>
    public void StopTracking(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        StopHealthMonitoring(plugin);
        _pluginStates.TryRemove(plugin, out _);
        _unhealthyPlugins.TryRemove(plugin, out _);

        _logger.LogDebug("Stopped tracking plugin {PluginName}", plugin.Metadata.Name);
    }

    /// <summary>
    /// Performs a health check on the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Health check result.</returns>
    public async Task<PluginHealthResult> PerformHealthCheckAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var timestamp = DateTimeOffset.UtcNow;
        Exception? error = null;
        bool isHealthy = false;
        Dictionary<string, object>? data = null;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_healthCheckTimeout);

            // Basic health check: verify the plugin is still running and responsive
            isHealthy = plugin.IsRunning;
            
            if (isHealthy)
            {
                // Add a small delay to make this truly async and simulate real health check work
                await Task.Delay(1, timeoutCts.Token);
                
                // Additional health checks could be added here if plugins support them
                data = new Dictionary<string, object>
                {
                    ["IsRunning"] = plugin.IsRunning,
                    ["HasContext"] = plugin.Context != null
                };
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Re-throw if it's the caller's cancellation
        }
        catch (OperationCanceledException)
        {
            error = new TimeoutException("Health check timed out");
            isHealthy = false;
        }
        catch (Exception ex)
        {
            error = ex;
            isHealthy = false;
        }

        stopwatch.Stop();
        var result = new PluginHealthResult
        {
            Plugin = plugin,
            IsHealthy = isHealthy,
            Timestamp = timestamp,
            ResponseTime = stopwatch.Elapsed,
            Error = error,
            Data = data
        };

        if (!isHealthy)
        {
            _unhealthyPlugins.TryAdd(plugin, true);
            _logger.LogWarning("Plugin {PluginName} health check failed: {Error}",
                plugin.Metadata.Name, error?.Message ?? "Unknown error");

            try
            {
                PluginUnhealthy?.Invoke(this, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking PluginUnhealthy event for plugin {PluginName}", plugin.Metadata.Name);
            }
        }
        else
        {
            // Remove from unhealthy set if it was there
            _unhealthyPlugins.TryRemove(plugin, out _);
        }

        return result;
    }

    private void StartHealthMonitoring(IPlugin plugin)
    {
        if (_healthCheckTimers.ContainsKey(plugin))
        {
            return; // Already monitoring
        }

        var timer = new Timer(async _ => await PerformHealthCheckCallback(plugin), 
            null, _healthCheckInterval, _healthCheckInterval);
        
        _healthCheckTimers.TryAdd(plugin, timer);
        
        _logger.LogDebug("Started health monitoring for plugin {PluginName}", plugin.Metadata.Name);
    }

    private void StopHealthMonitoring(IPlugin plugin)
    {
        if (_healthCheckTimers.TryRemove(plugin, out var timer))
        {
            timer.Dispose();
            _logger.LogDebug("Stopped health monitoring for plugin {PluginName}", plugin.Metadata.Name);
        }
    }

    private async Task PerformHealthCheckCallback(IPlugin plugin)
    {
        try
        {
            await PerformHealthCheckAsync(plugin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled health check for plugin {PluginName}", plugin.Metadata.Name);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (var timer in _healthCheckTimers.Values)
        {
            timer?.Dispose();
        }
        _healthCheckTimers.Clear();
        _pluginStates.Clear();
        _unhealthyPlugins.Clear();

        _logger.LogDebug("PluginStateTracker disposed");
    }

    private record PluginStateInfo(PluginState State, DateTimeOffset LastChanged, Exception? Exception);
}
using System.Collections.Concurrent;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Tracks plugin states and provides health monitoring capabilities.
/// Implements watchdog timers and periodic health checks for comprehensive plugin monitoring.
/// </summary>
public class PluginStateTracker : IPluginStateTracker, IDisposable
{
    private readonly ILogger<PluginStateTracker> _logger;
    private readonly ConcurrentDictionary<IPlugin, PluginTrackingInfo> _trackedPlugins = new();
    private readonly Timer _healthCheckTimer;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1); // Default health check every minute
    private readonly object _lockObject = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginStateTracker"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public PluginStateTracker(ILogger<PluginStateTracker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Start periodic health check timer
        _healthCheckTimer = new Timer(PerformPeriodicHealthChecks, null, _healthCheckInterval, _healthCheckInterval);
        
        _logger.LogInformation("PluginStateTracker initialized with health check interval: {Interval}", _healthCheckInterval);
    }

    /// <inheritdoc />
    public PluginState? GetPluginState(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        
        return _trackedPlugins.TryGetValue(plugin, out var info) ? info.CurrentState : null;
    }

    /// <inheritdoc />
    public void SetPluginState(IPlugin plugin, PluginState newState, Exception? exception = null)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        lock (_lockObject)
        {
            if (!_trackedPlugins.TryGetValue(plugin, out var info))
            {
                _logger.LogWarning("Attempted to set state for untracked plugin: {PluginId}", plugin.Metadata.Id);
                return;
            }

            var previousState = info.CurrentState;
            info.CurrentState = newState;
            info.LastStateChange = DateTimeOffset.UtcNow;
            info.LastException = exception;
            
            // Mark as unhealthy if state change is due to error
            if (exception != null)
            {
                info.IsHealthy = false;
                info.LastHealthCheck = DateTimeOffset.UtcNow;
            }

            _logger.LogInformation("Plugin {PluginId} state changed from {PreviousState} to {NewState}", 
                plugin.Metadata.Id, previousState, newState);

            // Fire state change event
            try
            {
                StateChanged?.Invoke(this, new PluginStateChangedEventArgs(plugin, previousState, newState, exception));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error firing StateChanged event for plugin {PluginId}", plugin.Metadata.Id);
            }

            // Fire unhealthy event if needed
            if (!info.IsHealthy)
            {
                try
                {
                    var healthResult = new PluginHealthResult
                    {
                        Plugin = plugin,
                        IsHealthy = false,
                        Timestamp = DateTimeOffset.UtcNow,
                        ResponseTime = TimeSpan.Zero,
                        Error = exception
                    };
                    PluginUnhealthy?.Invoke(this, healthResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error firing PluginUnhealthy event for plugin {PluginId}", plugin.Metadata.Id);
                }
            }
        }
    }

    /// <inheritdoc />
    public void StartTracking(IPlugin plugin, PluginState initialState = PluginState.Discovered)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var info = new PluginTrackingInfo
        {
            CurrentState = initialState,
            LastStateChange = DateTimeOffset.UtcNow,
            IsHealthy = true,
            LastHealthCheck = DateTimeOffset.UtcNow
        };

        if (_trackedPlugins.TryAdd(plugin, info))
        {
            _logger.LogInformation("Started tracking plugin {PluginId} with initial state {State}", 
                plugin.Metadata.Id, initialState);
        }
        else
        {
            _logger.LogWarning("Plugin {PluginId} is already being tracked", plugin.Metadata.Id);
        }
    }

    /// <inheritdoc />
    public void StopTracking(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (_trackedPlugins.TryRemove(plugin, out _))
        {
            _logger.LogInformation("Stopped tracking plugin {PluginId}", plugin.Metadata.Id);
        }
        else
        {
            _logger.LogWarning("Attempted to stop tracking plugin {PluginId} that was not being tracked", plugin.Metadata.Id);
        }
    }

    /// <inheritdoc />
    public Task<PluginHealthResult> PerformHealthCheckAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Basic health check: verify plugin is responsive and in expected state
            var isRunning = plugin.IsRunning;
            var currentState = GetPluginState(plugin);
            
            // Consider plugin healthy if it's running and in Running state, or not running but in valid stopped states
            var isHealthy = (isRunning && currentState == PluginState.Running) ||
                           (!isRunning && (currentState == PluginState.Stopped || 
                                          currentState == PluginState.Initialized ||
                                          currentState == PluginState.Configured));

            stopwatch.Stop();

            var result = new PluginHealthResult
            {
                Plugin = plugin,
                IsHealthy = isHealthy,
                Timestamp = startTime,
                ResponseTime = stopwatch.Elapsed
            };

            // Update tracking info
            if (_trackedPlugins.TryGetValue(plugin, out var info))
            {
                info.IsHealthy = isHealthy;
                info.LastHealthCheck = startTime;
                if (!isHealthy)
                {
                    info.LastException = new InvalidOperationException($"Plugin health check failed - Expected state mismatch");
                }
            }

            _logger.LogDebug("Health check completed for plugin {PluginId}: {IsHealthy} (took {ResponseTime}ms)", 
                plugin.Metadata.Id, isHealthy, stopwatch.ElapsedMilliseconds);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            var result = new PluginHealthResult
            {
                Plugin = plugin,
                IsHealthy = false,
                Timestamp = startTime,
                ResponseTime = stopwatch.Elapsed,
                Error = ex
            };

            // Update tracking info
            if (_trackedPlugins.TryGetValue(plugin, out var info))
            {
                info.IsHealthy = false;
                info.LastHealthCheck = startTime;
                info.LastException = ex;
            }

            _logger.LogWarning(ex, "Health check failed for plugin {PluginId} (took {ResponseTime}ms)", 
                plugin.Metadata.Id, stopwatch.ElapsedMilliseconds);

            return Task.FromResult(result);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IPlugin> TrackedPlugins => _trackedPlugins.Keys.ToList();

    /// <inheritdoc />
    public IReadOnlyCollection<IPlugin> GetPluginsInState(PluginState state)
    {
        return _trackedPlugins.Where(kvp => kvp.Value.CurrentState == state).Select(kvp => kvp.Key).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<IPlugin> UnhealthyPlugins => 
        _trackedPlugins.Where(kvp => !kvp.Value.IsHealthy).Select(kvp => kvp.Key).ToList();

    /// <inheritdoc />
    public event EventHandler<PluginStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<PluginHealthResult>? PluginUnhealthy;

    private async void PerformPeriodicHealthChecks(object? state)
    {
        if (_disposed)
            return;

        var pluginsToCheck = TrackedPlugins.ToList();
        
        _logger.LogDebug("Starting periodic health checks for {PluginCount} plugins", pluginsToCheck.Count);

        foreach (var plugin in pluginsToCheck)
        {
            try
            {
                var result = await PerformHealthCheckAsync(plugin);
                
                if (!result.IsHealthy)
                {
                    _logger.LogWarning("Plugin {PluginId} is unhealthy", plugin.Metadata.Id);
                    
                    try
                    {
                        PluginUnhealthy?.Invoke(this, result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error firing PluginUnhealthy event during periodic check for plugin {PluginId}", plugin.Metadata.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic health check for plugin {PluginId}", plugin.Metadata.Id);
            }
        }
        
        _logger.LogDebug("Completed periodic health checks");
    }

    /// <summary>
    /// Releases resources used by the PluginStateTracker.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _healthCheckTimer?.Dispose();
            _trackedPlugins.Clear();
            _disposed = true;
            
            _logger.LogInformation("PluginStateTracker disposed");
        }
    }

    private class PluginTrackingInfo
    {
        public PluginState CurrentState { get; set; }
        public DateTimeOffset LastStateChange { get; set; }
        public bool IsHealthy { get; set; }
        public DateTimeOffset LastHealthCheck { get; set; }
        public Exception? LastException { get; set; }
    }
}
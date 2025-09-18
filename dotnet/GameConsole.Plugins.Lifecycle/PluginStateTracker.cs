using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Options for configuring the plugin state tracker.
/// </summary>
public record PluginStateTrackerOptions(
    TimeSpan HealthCheckInterval = default,
    TimeSpan WatchdogTimeout = default,
    int MaxFailureCount = 3
)
{
    public PluginStateTrackerOptions() : this(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10), 3) { }
}

/// <summary>
/// Tracks plugin states and monitors their health.
/// </summary>
public class PluginStateTracker : IDisposable
{
    private readonly ILogger<PluginStateTracker>? _logger;
    private readonly PluginStateTrackerOptions _options;
    private readonly ConcurrentDictionary<string, LoadedPlugin> _plugins = new();
    private readonly ConcurrentDictionary<string, int> _failureCounts = new();
    private readonly Timer _healthCheckTimer;
    private bool _disposed;

    /// <summary>
    /// Event raised when a plugin health status changes.
    /// </summary>
    public event EventHandler<PluginHealthChangedEventArgs>? PluginHealthChanged;

    /// <summary>
    /// Event raised when a plugin failure is detected.
    /// </summary>
    public event EventHandler<PluginFailureEventArgs>? PluginFailureDetected;

    public PluginStateTracker(PluginStateTrackerOptions? options = null, ILogger<PluginStateTracker>? logger = null)
    {
        _options = options ?? new PluginStateTrackerOptions();
        _logger = logger;
        
        _healthCheckTimer = new Timer(
            PerformHealthChecks, 
            null, 
            _options.HealthCheckInterval, 
            _options.HealthCheckInterval
        );
    }

    /// <summary>
    /// Registers a plugin for tracking.
    /// </summary>
    public void RegisterPlugin(LoadedPlugin plugin)
    {
        _plugins.AddOrUpdate(plugin.Metadata.Id, plugin, (_, _) => plugin);
        _failureCounts.TryRemove(plugin.Metadata.Id, out _);
        
        _logger?.LogInformation("Registered plugin {PluginId} for state tracking", plugin.Metadata.Id);
    }

    /// <summary>
    /// Unregisters a plugin from tracking.
    /// </summary>
    public void UnregisterPlugin(string pluginId)
    {
        _plugins.TryRemove(pluginId, out _);
        _failureCounts.TryRemove(pluginId, out _);
        
        _logger?.LogInformation("Unregistered plugin {PluginId} from state tracking", pluginId);
    }

    /// <summary>
    /// Updates the state of a tracked plugin.
    /// </summary>
    public void UpdatePluginState(string pluginId, PluginState newState)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin))
        {
            var updatedPlugin = plugin with { State = newState };
            _plugins.TryUpdate(pluginId, updatedPlugin, plugin);
            
            _logger?.LogDebug("Updated plugin {PluginId} state to {State}", pluginId, newState);
        }
    }

    /// <summary>
    /// Updates the health status of a tracked plugin.
    /// </summary>
    public void UpdatePluginHealth(string pluginId, PluginHealth health, string? message = null)
    {
        if (_plugins.TryGetValue(pluginId, out var plugin))
        {
            var oldHealth = plugin.Health;
            var updatedPlugin = plugin with 
            { 
                Health = health, 
                LastHealthCheck = DateTime.UtcNow 
            };
            
            _plugins.TryUpdate(pluginId, updatedPlugin, plugin);

            if (oldHealth != health)
            {
                PluginHealthChanged?.Invoke(this, new PluginHealthChangedEventArgs(
                    pluginId, oldHealth, health, DateTime.UtcNow, message));
                
                _logger?.LogInformation("Plugin {PluginId} health changed from {OldHealth} to {NewHealth}: {Message}",
                    pluginId, oldHealth, health, message);
            }

            // Track failures
            if (health == PluginHealth.Failed || health == PluginHealth.Unhealthy)
            {
                var failureCount = _failureCounts.AddOrUpdate(pluginId, 1, (_, count) => count + 1);
                
                if (failureCount >= _options.MaxFailureCount)
                {
                    PluginFailureDetected?.Invoke(this, new PluginFailureEventArgs(
                        pluginId, failureCount, DateTime.UtcNow, message));
                    
                    _logger?.LogWarning("Plugin {PluginId} has failed {FailureCount} times, triggering failure event",
                        pluginId, failureCount);
                }
            }
            else if (health == PluginHealth.Healthy)
            {
                // Reset failure count on recovery
                _failureCounts.TryRemove(pluginId, out _);
            }
        }
    }

    /// <summary>
    /// Gets the current state of all tracked plugins.
    /// </summary>
    public IEnumerable<LoadedPlugin> GetTrackedPlugins() => _plugins.Values.ToList();

    /// <summary>
    /// Gets the current state of a specific plugin.
    /// </summary>
    public LoadedPlugin? GetPluginState(string pluginId) => 
        _plugins.TryGetValue(pluginId, out var plugin) ? plugin : null;

    /// <summary>
    /// Gets the failure count for a plugin.
    /// </summary>
    public int GetFailureCount(string pluginId) => 
        _failureCounts.TryGetValue(pluginId, out var count) ? count : 0;

    private async void PerformHealthChecks(object? state)
    {
        if (_disposed) return;

        try
        {
            var tasks = _plugins.Values
                .Where(p => p.State == PluginState.Running)
                .Select(async plugin =>
                {
                    try
                    {
                        // Simulate health check - in real implementation, this would call the plugin's health check method
                        await Task.Delay(100); // Simulate health check operation
                        
                        // For now, mark as healthy unless it's already failed
                        if (plugin.Health != PluginHealth.Failed)
                        {
                            UpdatePluginHealth(plugin.Metadata.Id, PluginHealth.Healthy);
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdatePluginHealth(plugin.Metadata.Id, PluginHealth.Failed, ex.Message);
                        _logger?.LogError(ex, "Health check failed for plugin {PluginId}", plugin.Metadata.Id);
                    }
                });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during health check cycle");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _healthCheckTimer?.Dispose();
        
        _logger?.LogInformation("Plugin state tracker disposed");
    }
}

/// <summary>
/// Event arguments for plugin health change events.
/// </summary>
public record PluginHealthChangedEventArgs(
    string PluginId,
    PluginHealth OldHealth,
    PluginHealth NewHealth,
    DateTime Timestamp,
    string? Message = null
);

/// <summary>
/// Event arguments for plugin failure events.
/// </summary>
public record PluginFailureEventArgs(
    string PluginId,
    int FailureCount,
    DateTime Timestamp,
    string? Message = null
);
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Manages plugin updates, versioning, and rollback operations.
/// Provides capabilities for hot-swapping plugins and version management.
/// </summary>
public class PluginUpdateManager : IPluginUpdateManager, IDisposable
{
    private readonly ILogger<PluginUpdateManager> _logger;
    private readonly ConcurrentDictionary<IPlugin, PluginVersionInfo> _versionHistory = new();
    private bool _disposed;

    public PluginUpdateManager(ILogger<PluginUpdateManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Event fired when a plugin update operation completes.
    /// </summary>
    public event EventHandler<PluginUpdateEventArgs>? UpdateCompleted;

    /// <summary>
    /// Updates a plugin to a newer version.
    /// Performs hot-swap if possible, otherwise graceful restart.
    /// </summary>
    /// <param name="plugin">The plugin to update.</param>
    /// <param name="newVersionPath">Path to the new plugin version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if update was successful, false if rolled back.</returns>
    public async Task<bool> UpdatePluginAsync(IPlugin plugin, string newVersionPath, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        if (string.IsNullOrEmpty(newVersionPath)) throw new ArgumentException("New version path cannot be null or empty", nameof(newVersionPath));

        _logger.LogInformation("Starting update for plugin {PluginName} from {NewVersionPath}",
            plugin.Metadata.Name, newVersionPath);

        try
        {
            // Store current version for rollback
            var currentVersionInfo = new PluginVersionInfo
            {
                Version = plugin.Metadata.Version.ToString(),
                Path = GetPluginPath(plugin),
                InstallDate = DateTimeOffset.UtcNow,
                IsActive = plugin.IsRunning
            };

            _versionHistory.AddOrUpdate(plugin, currentVersionInfo, (_, _) => currentVersionInfo);

            // Validate the update first
            var validationResult = await ValidateUpdateAsync(plugin, newVersionPath, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Plugin update validation failed for {PluginName}: {ValidationErrors}",
                    plugin.Metadata.Name, string.Join(", ", validationResult.Errors));
                return false;
            }

            // Attempt the update
            var wasRunning = plugin.IsRunning;
            
            // Stop the plugin if it's running
            if (wasRunning)
            {
                await plugin.StopAsync(cancellationToken);
            }

            // Perform the hot-swap
            // In a real implementation, this would involve assembly unloading and reloading
            // For now, we'll simulate the update process
            await SimulatePluginUpdateAsync(plugin, newVersionPath, cancellationToken);

            // Restart if it was running before
            if (wasRunning)
            {
                await plugin.InitializeAsync(cancellationToken);
                await plugin.StartAsync(cancellationToken);
            }

            _logger.LogInformation("Successfully updated plugin {PluginName} to version at {NewVersionPath}",
                plugin.Metadata.Name, newVersionPath);
            
            OnUpdateCompleted(plugin, true, null);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update plugin {PluginName}, attempting rollback",
                plugin.Metadata.Name);

            // Attempt rollback
            var rollbackSuccess = await RollbackPluginAsync(plugin, cancellationToken);
            if (!rollbackSuccess)
            {
                _logger.LogCritical("Failed to rollback plugin {PluginName} after failed update",
                    plugin.Metadata.Name);
            }

            OnUpdateCompleted(plugin, false, ex);
            return false;
        }
    }

    /// <summary>
    /// Rolls back a plugin to its previous version.
    /// </summary>
    /// <param name="plugin">The plugin to roll back.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rollback was successful.</returns>
    public async Task<bool> RollbackPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (!_versionHistory.TryGetValue(plugin, out var versionInfo))
        {
            _logger.LogWarning("No version history available for plugin {PluginName}, cannot rollback",
                plugin.Metadata.Name);
            return false;
        }

        _logger.LogInformation("Rolling back plugin {PluginName} to previous version {Version}",
            plugin.Metadata.Name, versionInfo.Version);

        try
        {
            var wasRunning = plugin.IsRunning;

            // Stop the plugin if it's running
            if (wasRunning)
            {
                await plugin.StopAsync(cancellationToken);
            }

            // Simulate rollback to previous version
            await SimulatePluginRollbackAsync(plugin, versionInfo, cancellationToken);

            // Restart if it was running before
            if (versionInfo.IsActive)
            {
                await plugin.InitializeAsync(cancellationToken);
                await plugin.StartAsync(cancellationToken);
            }

            _logger.LogInformation("Successfully rolled back plugin {PluginName} to version {Version}",
                plugin.Metadata.Name, versionInfo.Version);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback plugin {PluginName}", plugin.Metadata.Name);
            return false;
        }
    }

    /// <summary>
    /// Gets the version history for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to get version history for.</param>
    /// <returns>Collection of version information for the plugin.</returns>
    public IReadOnlyCollection<PluginVersionInfo> GetPluginVersionHistory(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        var versions = new List<PluginVersionInfo>();
        
        if (_versionHistory.TryGetValue(plugin, out var versionInfo))
        {
            versions.Add(versionInfo);
        }
        
        return versions.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified plugin can be rolled back to a previous version.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>True if the plugin can be rolled back, false otherwise.</returns>
    public bool CanRollback(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return _versionHistory.ContainsKey(plugin);
    }

    /// <summary>
    /// Creates a backup of the current plugin state for rollback purposes.
    /// </summary>
    /// <param name="plugin">The plugin to backup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backup was created successfully.</returns>
    public async Task<bool> CreateBackupAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            var versionInfo = new PluginVersionInfo
            {
                Version = plugin.Metadata.Version.ToString(),
                Path = GetPluginPath(plugin),
                InstallDate = DateTimeOffset.UtcNow,
                IsActive = plugin.IsRunning
            };

            _versionHistory.AddOrUpdate(plugin, versionInfo, (_, _) => versionInfo);
            
            _logger.LogDebug("Created backup for plugin {PluginName} version {Version}",
                plugin.Metadata.Name, plugin.Metadata.Version);
                
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for plugin {PluginName}", plugin.Metadata.Name);
            return false;
        }
    }

    /// <summary>
    /// Checks if an update is available for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update information if available, null otherwise.</returns>
    public async Task<PluginUpdateInfo?> CheckForUpdateAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            // In a real implementation, this would check a remote repository or update service
            // For now, we'll simulate checking for updates
            await Task.Delay(100, cancellationToken);

            // Simulate that there's no update available
            _logger.LogDebug("Checked for updates for plugin {PluginName}, no updates available",
                plugin.Metadata.Name);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates for plugin {PluginName}", plugin.Metadata.Name);
            return null;
        }
    }

    /// <summary>
    /// Validates that a plugin update is compatible and safe to install.
    /// </summary>
    /// <param name="plugin">The current plugin.</param>
    /// <param name="updatePath">Path to the update package.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating if the update is safe to install.</returns>
    public async Task<PluginUpdateValidationResult> ValidateUpdateAsync(IPlugin plugin, string updatePath, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        if (string.IsNullOrEmpty(updatePath)) throw new ArgumentException("Update path cannot be null or empty", nameof(updatePath));

        var errors = new List<string>();

        try
        {
            // Simulate validation logic
            await Task.Delay(50, cancellationToken);

            // Check if the update path exists
            if (!System.IO.File.Exists(updatePath) && !System.IO.Directory.Exists(updatePath))
            {
                errors.Add($"Update path '{updatePath}' does not exist");
            }

            // Additional validation checks would go here
            // - Version compatibility
            // - Dependency validation
            // - Security checks
            // - Assembly signature verification

            var isValid = errors.Count == 0;

            _logger.LogDebug("Validation for plugin {PluginName} update from {UpdatePath}: {Result}",
                plugin.Metadata.Name, updatePath, isValid ? "PASSED" : "FAILED");

            return new PluginUpdateValidationResult
            {
                IsValid = isValid,
                Errors = errors.AsReadOnly(),
                RequiresDependentRestart = false
            };
        }
        catch (Exception ex)
        {
            errors.Add($"Validation failed with exception: {ex.Message}");
            _logger.LogError(ex, "Exception during validation for plugin {PluginName}", plugin.Metadata.Name);

            return new PluginUpdateValidationResult
            {
                IsValid = false,
                Errors = errors.AsReadOnly(),
                RequiresDependentRestart = false
            };
        }
    }

    private async Task SimulatePluginUpdateAsync(IPlugin plugin, string newVersionPath, CancellationToken cancellationToken)
    {
        // Simulate update work
        await Task.Delay(500, cancellationToken);
        _logger.LogDebug("Simulated plugin update for {PluginName}", plugin.Metadata.Name);
    }

    private async Task SimulatePluginRollbackAsync(IPlugin plugin, PluginVersionInfo versionInfo, CancellationToken cancellationToken)
    {
        // Simulate rollback work
        await Task.Delay(300, cancellationToken);
        _logger.LogDebug("Simulated plugin rollback for {PluginName}", plugin.Metadata.Name);
    }

    private string GetPluginPath(IPlugin plugin)
    {
        // In a real implementation, this would return the actual plugin assembly path
        return $"/plugins/{plugin.Metadata.Name}.dll";
    }

    private void OnUpdateCompleted(IPlugin plugin, bool wasSuccessful, Exception? exception)
    {
        try
        {
            var fromVersion = "unknown";
            var toVersion = plugin.Metadata.Version.ToString();
            
            // Try to get the from version from version history
            if (_versionHistory.TryGetValue(plugin, out var versionInfo))
            {
                fromVersion = versionInfo.Version;
            }
            
            UpdateCompleted?.Invoke(this, new PluginUpdateEventArgs(plugin, fromVersion, toVersion, wasSuccessful, exception));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking UpdateCompleted event for plugin {PluginName}", plugin.Metadata.Name);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _versionHistory.Clear();

        _logger.LogDebug("PluginUpdateManager disposed");
    }
}
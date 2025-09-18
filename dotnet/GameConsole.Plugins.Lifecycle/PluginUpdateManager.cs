using Microsoft.Extensions.Logging;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Represents information about a plugin update.
/// </summary>
public record PluginUpdateInfo(
    string PluginId,
    Version CurrentVersion,
    Version UpdateVersion,
    string UpdatePath,
    string? ReleaseNotes = null
);

/// <summary>
/// Result of a plugin update operation.
/// </summary>
public record UpdateResult(
    bool Success,
    string? Message = null,
    Exception? Exception = null
);

/// <summary>
/// Options for configuring plugin update behavior.
/// </summary>
public record PluginUpdateOptions(
    bool CreateBackups = true,
    bool ValidateBeforeUpdate = true,
    bool RollbackOnFailure = true,
    TimeSpan UpdateTimeout = default
)
{
    public PluginUpdateOptions() : this(true, true, true, TimeSpan.FromMinutes(5)) { }
}

/// <summary>
/// Manages plugin updates and rollback operations.
/// </summary>
public class PluginUpdateManager : IDisposable
{
    private readonly IPluginLifecycleManager _lifecycleManager;
    private readonly PluginStateTracker _stateTracker;
    private readonly ILogger<PluginUpdateManager>? _logger;
    private readonly PluginUpdateOptions _options;
    private readonly Dictionary<string, PluginBackup> _backups = new();
    private bool _disposed;

    /// <summary>
    /// Event raised when a plugin update begins.
    /// </summary>
    public event EventHandler<PluginUpdateEventArgs>? UpdateStarted;

    /// <summary>
    /// Event raised when a plugin update completes.
    /// </summary>
    public event EventHandler<PluginUpdateCompletedEventArgs>? UpdateCompleted;

    /// <summary>
    /// Event raised when a plugin rollback occurs.
    /// </summary>
    public event EventHandler<PluginRollbackEventArgs>? RollbackPerformed;

    public PluginUpdateManager(
        IPluginLifecycleManager lifecycleManager,
        PluginStateTracker stateTracker,
        PluginUpdateOptions? options = null,
        ILogger<PluginUpdateManager>? logger = null)
    {
        _lifecycleManager = lifecycleManager ?? throw new ArgumentNullException(nameof(lifecycleManager));
        _stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        _options = options ?? new PluginUpdateOptions();
        _logger = logger;
    }

    /// <summary>
    /// Updates a plugin to a new version.
    /// </summary>
    public async Task<UpdateResult> UpdatePluginAsync(PluginUpdateInfo updateInfo, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        UpdateStarted?.Invoke(this, new PluginUpdateEventArgs(updateInfo.PluginId, updateInfo.CurrentVersion, updateInfo.UpdateVersion, startTime));

        _logger?.LogInformation("Starting update for plugin {PluginId} from version {CurrentVersion} to {UpdateVersion}",
            updateInfo.PluginId, updateInfo.CurrentVersion, updateInfo.UpdateVersion);

        try
        {
            // Validate the update
            if (_options.ValidateBeforeUpdate)
            {
                var validationResult = await ValidateUpdateAsync(updateInfo, cancellationToken);
                if (!validationResult.Success)
                {
                    var result = new UpdateResult(false, $"Update validation failed: {validationResult.Message}");
                    UpdateCompleted?.Invoke(this, new PluginUpdateCompletedEventArgs(
                        updateInfo.PluginId, false, DateTime.UtcNow, result.Message));
                    return result;
                }
            }

            // Create backup if enabled
            if (_options.CreateBackups)
            {
                var backupResult = await CreateBackupAsync(updateInfo.PluginId, cancellationToken);
                if (!backupResult)
                {
                    var result = new UpdateResult(false, "Failed to create backup before update");
                    UpdateCompleted?.Invoke(this, new PluginUpdateCompletedEventArgs(
                        updateInfo.PluginId, false, DateTime.UtcNow, result.Message));
                    return result;
                }
            }

            // Perform the update
            var updateResult = await PerformUpdateAsync(updateInfo, cancellationToken);
            
            if (!updateResult.Success && _options.RollbackOnFailure && _backups.ContainsKey(updateInfo.PluginId))
            {
                _logger?.LogWarning("Update failed for plugin {PluginId}, attempting rollback", updateInfo.PluginId);
                var rollbackResult = await RollbackPluginAsync(updateInfo.PluginId, cancellationToken);
                
                var message = $"Update failed: {updateResult.Message}. Rollback {(rollbackResult.Success ? "succeeded" : "failed")}.";
                var result = new UpdateResult(false, message, updateResult.Exception);
                
                UpdateCompleted?.Invoke(this, new PluginUpdateCompletedEventArgs(
                    updateInfo.PluginId, false, DateTime.UtcNow, result.Message));
                
                return result;
            }

            // Clean up backup on successful update
            if (updateResult.Success && _backups.ContainsKey(updateInfo.PluginId))
            {
                _backups.Remove(updateInfo.PluginId);
            }

            UpdateCompleted?.Invoke(this, new PluginUpdateCompletedEventArgs(
                updateInfo.PluginId, updateResult.Success, DateTime.UtcNow, updateResult.Message));

            return updateResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during plugin update for {PluginId}", updateInfo.PluginId);
            
            var result = new UpdateResult(false, $"Unexpected error: {ex.Message}", ex);
            UpdateCompleted?.Invoke(this, new PluginUpdateCompletedEventArgs(
                updateInfo.PluginId, false, DateTime.UtcNow, result.Message));
            
            return result;
        }
    }

    /// <summary>
    /// Rolls back a plugin to its previous version.
    /// </summary>
    public async Task<UpdateResult> RollbackPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        RollbackPerformed?.Invoke(this, new PluginRollbackEventArgs(pluginId, startTime));

        _logger?.LogInformation("Starting rollback for plugin {PluginId}", pluginId);

        try
        {
            if (!_backups.TryGetValue(pluginId, out var backup))
            {
                return new UpdateResult(false, "No backup available for rollback");
            }

            // Stop the current plugin
            await _lifecycleManager.StopPluginAsync(pluginId, cancellationToken);
            _stateTracker.UpdatePluginState(pluginId, PluginState.Updating);

            // Restore from backup
            var restoreResult = await RestoreFromBackupAsync(backup, cancellationToken);
            if (!restoreResult)
            {
                return new UpdateResult(false, "Failed to restore plugin from backup");
            }

            // Reload and restart the plugin
            var reloadSuccess = await _lifecycleManager.ReloadPluginAsync(pluginId, cancellationToken);
            if (!reloadSuccess)
            {
                return new UpdateResult(false, "Failed to reload plugin after rollback");
            }

            var startSuccess = await _lifecycleManager.StartPluginAsync(pluginId, cancellationToken);
            if (!startSuccess)
            {
                return new UpdateResult(false, "Failed to start plugin after rollback");
            }

            // Remove the backup after successful rollback
            _backups.Remove(pluginId);

            _logger?.LogInformation("Successfully rolled back plugin {PluginId}", pluginId);
            return new UpdateResult(true, "Plugin successfully rolled back");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during rollback for plugin {PluginId}", pluginId);
            return new UpdateResult(false, $"Rollback failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if a plugin has an available backup for rollback.
    /// </summary>
    public bool HasBackup(string pluginId) => _backups.ContainsKey(pluginId);

    /// <summary>
    /// Gets backup information for a plugin.
    /// </summary>
    public PluginBackup? GetBackupInfo(string pluginId) => 
        _backups.TryGetValue(pluginId, out var backup) ? backup : null;

    private async Task<UpdateResult> ValidateUpdateAsync(PluginUpdateInfo updateInfo, CancellationToken cancellationToken)
    {
        try
        {
            // Validate the update package
            if (!File.Exists(updateInfo.UpdatePath))
            {
                return new UpdateResult(false, "Update package not found");
            }

            // Validate version compatibility
            if (updateInfo.UpdateVersion <= updateInfo.CurrentVersion)
            {
                return new UpdateResult(false, "Update version must be newer than current version");
            }

            // Validate the plugin assembly
            var validationResult = await _lifecycleManager.ValidatePluginAsync(updateInfo.UpdatePath, cancellationToken);
            if (!validationResult.IsValid)
            {
                return new UpdateResult(false, $"Plugin validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            return new UpdateResult(true, "Update validation passed");
        }
        catch (Exception ex)
        {
            return new UpdateResult(false, $"Validation error: {ex.Message}", ex);
        }
    }

    private async Task<bool> CreateBackupAsync(string pluginId, CancellationToken cancellationToken)
    {
        try
        {
            var plugin = _lifecycleManager.GetPluginInfo(pluginId);
            if (plugin == null)
            {
                _logger?.LogWarning("Cannot create backup for unknown plugin {PluginId}", pluginId);
                return false;
            }

            // In a real implementation, this would copy the plugin files
            var backupPath = Path.Combine(Path.GetTempPath(), $"plugin_backup_{pluginId}_{DateTime.UtcNow:yyyyMMddHHmmss}");
            
            // Simulate backup creation
            await Task.Delay(100, cancellationToken);
            
            var backup = new PluginBackup(
                pluginId,
                plugin.Version,
                plugin.AssemblyPath,
                backupPath,
                DateTime.UtcNow
            );

            _backups[pluginId] = backup;
            
            _logger?.LogInformation("Created backup for plugin {PluginId} at {BackupPath}", pluginId, backupPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create backup for plugin {PluginId}", pluginId);
            return false;
        }
    }

    private async Task<UpdateResult> PerformUpdateAsync(PluginUpdateInfo updateInfo, CancellationToken cancellationToken)
    {
        try
        {
            // Stop the current plugin
            await _lifecycleManager.StopPluginAsync(updateInfo.PluginId, cancellationToken);
            _stateTracker.UpdatePluginState(updateInfo.PluginId, PluginState.Updating);

            // Unload the current plugin
            await _lifecycleManager.UnloadPluginAsync(updateInfo.PluginId, cancellationToken);

            // Load the new version
            var loadResult = await _lifecycleManager.LoadPluginAsync(updateInfo.UpdatePath, cancellationToken);
            if (!loadResult.Success)
            {
                return new UpdateResult(false, $"Failed to load updated plugin: {loadResult.ErrorMessage}");
            }

            // Start the updated plugin
            var startSuccess = await _lifecycleManager.StartPluginAsync(updateInfo.PluginId, cancellationToken);
            if (!startSuccess)
            {
                return new UpdateResult(false, "Failed to start updated plugin");
            }

            _logger?.LogInformation("Successfully updated plugin {PluginId} to version {Version}",
                updateInfo.PluginId, updateInfo.UpdateVersion);

            return new UpdateResult(true, $"Plugin updated to version {updateInfo.UpdateVersion}");
        }
        catch (Exception ex)
        {
            return new UpdateResult(false, $"Update failed: {ex.Message}", ex);
        }
    }

    private async Task<bool> RestoreFromBackupAsync(PluginBackup backup, CancellationToken cancellationToken)
    {
        try
        {
            // In a real implementation, this would restore the plugin files from backup
            await Task.Delay(100, cancellationToken);
            
            _logger?.LogInformation("Restored plugin {PluginId} from backup created at {BackupTime}",
                backup.PluginId, backup.CreatedAt);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to restore plugin {PluginId} from backup", backup.PluginId);
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        _backups.Clear();
        
        _logger?.LogInformation("Plugin update manager disposed");
    }
}

/// <summary>
/// Represents a plugin backup.
/// </summary>
public record PluginBackup(
    string PluginId,
    Version Version,
    string OriginalPath,
    string BackupPath,
    DateTime CreatedAt
);

/// <summary>
/// Event arguments for plugin update events.
/// </summary>
public record PluginUpdateEventArgs(
    string PluginId,
    Version FromVersion,
    Version ToVersion,
    DateTime Timestamp
);

/// <summary>
/// Event arguments for plugin update completed events.
/// </summary>
public record PluginUpdateCompletedEventArgs(
    string PluginId,
    bool Success,
    DateTime Timestamp,
    string? Message = null
);

/// <summary>
/// Event arguments for plugin rollback events.
/// </summary>
public record PluginRollbackEventArgs(
    string PluginId,
    DateTime Timestamp
);
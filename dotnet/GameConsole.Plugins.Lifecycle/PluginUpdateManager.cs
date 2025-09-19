using System.Collections.Concurrent;
using GameConsole.Plugins.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Manages plugin updates, versioning, and rollback operations.
/// Provides hot-swapping capabilities and comprehensive version management.
/// </summary>
public class PluginUpdateManager : IPluginUpdateManager, IDisposable
{
    private readonly ILogger<PluginUpdateManager> _logger;
    private readonly IPluginStateTracker _stateTracker;
    private readonly ConcurrentDictionary<IPlugin, PluginVersionHistory> _versionHistory = new();
    private readonly string _backupDirectory;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginUpdateManager"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="stateTracker">Plugin state tracker.</param>
    /// <param name="backupDirectory">Directory to store plugin backups (optional).</param>
    public PluginUpdateManager(ILogger<PluginUpdateManager> logger, IPluginStateTracker stateTracker, string? backupDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateTracker = stateTracker ?? throw new ArgumentNullException(nameof(stateTracker));
        _backupDirectory = backupDirectory ?? Path.Combine(Path.GetTempPath(), "GameConsole.Plugins.Backup");
        
        // Ensure backup directory exists
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
        
        _logger.LogInformation("PluginUpdateManager initialized with backup directory: {BackupDirectory}", _backupDirectory);
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePluginAsync(IPlugin plugin, string newVersionPath, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        if (string.IsNullOrWhiteSpace(newVersionPath)) throw new ArgumentException("New version path cannot be null or empty", nameof(newVersionPath));
        
        if (!File.Exists(newVersionPath))
        {
            throw new FileNotFoundException($"New version file not found: {newVersionPath}");
        }

        var currentVersion = plugin.Metadata.Version.ToString();
        string newVersion = "Unknown";
        
        try
        {
            // Extract version from new plugin (simplified - would normally parse assembly metadata)
            var fileName = Path.GetFileNameWithoutExtension(newVersionPath);
            newVersion = fileName.Contains("_v") ? fileName.Split("_v").Last() : "Unknown";
            
            _logger.LogInformation("Starting update for plugin {PluginId} from version {CurrentVersion} to {NewVersion}", 
                plugin.Metadata.Id, currentVersion, newVersion);

            // Validate update
            var validationResult = await ValidateUpdateAsync(plugin, newVersionPath, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogError("Update validation failed for plugin {PluginId}: {Errors}", 
                    plugin.Metadata.Id, string.Join(", ", validationResult.Errors));
                
                FireUpdateCompletedEvent(plugin, currentVersion, newVersion, false, 
                    new InvalidOperationException($"Update validation failed: {string.Join(", ", validationResult.Errors)}"));
                
                return false;
            }

            // Create backup
            var backupCreated = await CreateBackupAsync(plugin, cancellationToken);
            if (!backupCreated)
            {
                _logger.LogError("Failed to create backup for plugin {PluginId} before update", plugin.Metadata.Id);
                
                FireUpdateCompletedEvent(plugin, currentVersion, newVersion, false, 
                    new InvalidOperationException("Failed to create backup before update"));
                
                return false;
            }

            // Set updating state
            _stateTracker.SetPluginState(plugin, PluginState.Updating);

            // Perform hot-swap or restart update
            bool updateSuccess = await PerformUpdateAsync(plugin, newVersionPath, cancellationToken);
            
            if (updateSuccess)
            {
                // Record version history
                RecordVersionUpdate(plugin, currentVersion, newVersion);
                
                _logger.LogInformation("Successfully updated plugin {PluginId} from version {CurrentVersion} to {NewVersion}", 
                    plugin.Metadata.Id, currentVersion, newVersion);
                
                FireUpdateCompletedEvent(plugin, currentVersion, newVersion, true, null);
                
                return true;
            }
            else
            {
                _logger.LogError("Update failed for plugin {PluginId}, attempting rollback", plugin.Metadata.Id);
                
                // Attempt rollback
                var rollbackSuccess = await RollbackPluginAsync(plugin, cancellationToken);
                
                FireUpdateCompletedEvent(plugin, currentVersion, newVersion, false, 
                    new InvalidOperationException($"Update failed, rollback {(rollbackSuccess ? "successful" : "failed")}"));
                
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during plugin update for {PluginId}", plugin.Metadata.Id);
            
            // Attempt rollback on error
            try
            {
                await RollbackPluginAsync(plugin, cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Rollback failed after update error for plugin {PluginId}", plugin.Metadata.Id);
            }
            
            FireUpdateCompletedEvent(plugin, currentVersion, newVersion ?? "Unknown", false, ex);
            
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RollbackPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (!CanRollback(plugin))
        {
            _logger.LogWarning("Rollback not available for plugin {PluginId}", plugin.Metadata.Id);
            return false;
        }

        try
        {
            var history = _versionHistory[plugin];
            var previousVersion = history.Versions.Where(v => !v.IsActive).OrderByDescending(v => v.InstallDate).FirstOrDefault();
            
            if (previousVersion == null)
            {
                _logger.LogError("No previous version found for rollback of plugin {PluginId}", plugin.Metadata.Id);
                return false;
            }

            _logger.LogInformation("Rolling back plugin {PluginId} to version {PreviousVersion}", 
                plugin.Metadata.Id, previousVersion.Version);

            // Stop plugin if running
            if (plugin.IsRunning)
            {
                _stateTracker.SetPluginState(plugin, PluginState.Stopping);
                await plugin.StopAsync(cancellationToken);
                _stateTracker.SetPluginState(plugin, PluginState.Stopped);
            }

            // Restore previous version files
            await RestorePreviousVersionAsync(plugin, previousVersion, cancellationToken);

            // Mark versions appropriately
            var currentActiveVersion = history.Versions.FirstOrDefault(v => v.IsActive);
            if (currentActiveVersion != null)
            {
                currentActiveVersion.IsActive = false;
            }
            previousVersion.IsActive = true;

            // Restart plugin if it was running
            var targetState = _stateTracker.GetPluginState(plugin);
            if (targetState == PluginState.Stopped && plugin.IsRunning) // If we expect it to be running
            {
                _stateTracker.SetPluginState(plugin, PluginState.Starting);
                await plugin.StartAsync(cancellationToken);
                _stateTracker.SetPluginState(plugin, PluginState.Running);
            }

            _logger.LogInformation("Successfully rolled back plugin {PluginId} to version {PreviousVersion}", 
                plugin.Metadata.Id, previousVersion.Version);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rollback for plugin {PluginId}", plugin.Metadata.Id);
            _stateTracker.SetPluginState(plugin, PluginState.Failed, ex);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<PluginUpdateInfo?> CheckForUpdateAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        // Simplified implementation - in a real scenario, this would check update repositories, manifest files, etc.
        await Task.Delay(100, cancellationToken); // Simulate async operation
        
        // Return null for now - would implement actual update checking logic
        return null;
    }

    /// <inheritdoc />
    public async Task<PluginUpdateValidationResult> ValidateUpdateAsync(IPlugin plugin, string updatePath, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        if (string.IsNullOrWhiteSpace(updatePath)) throw new ArgumentException("Update path cannot be null or empty", nameof(updatePath));

        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            // Basic file existence check
            if (!File.Exists(updatePath))
            {
                errors.Add($"Update file not found: {updatePath}");
            }

            // Check file extension
            var extension = Path.GetExtension(updatePath).ToLowerInvariant();
            if (extension != ".dll" && extension != ".exe")
            {
                warnings.Add($"Unexpected file extension: {extension}");
            }

            // Check file size (basic sanity check)
            var fileInfo = new FileInfo(updatePath);
            if (fileInfo.Length == 0)
            {
                errors.Add("Update file is empty");
            }
            else if (fileInfo.Length > 100 * 1024 * 1024) // 100MB
            {
                warnings.Add("Update file is unusually large (>100MB)");
            }

            // In a real implementation, would check:
            // - Assembly compatibility
            // - Version compatibility
            // - Dependency requirements
            // - Digital signatures
            // - etc.

            await Task.Delay(50, cancellationToken); // Simulate validation work

            return new PluginUpdateValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors,
                Warnings = warnings,
                RequiresDependentRestart = false // Would determine based on actual analysis
            };
        }
        catch (Exception ex)
        {
            errors.Add($"Validation error: {ex.Message}");
            
            return new PluginUpdateValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings,
                RequiresDependentRestart = false
            };
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<PluginVersionInfo> GetPluginVersionHistory(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        return _versionHistory.TryGetValue(plugin, out var history) 
            ? history.Versions.OrderByDescending(v => v.InstallDate).ToList() 
            : Array.Empty<PluginVersionInfo>();
    }

    /// <inheritdoc />
    public bool CanRollback(IPlugin plugin)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        if (!_versionHistory.TryGetValue(plugin, out var history))
            return false;

        // Can rollback if there's at least one non-active version
        return history.Versions.Any(v => !v.IsActive);
    }

    /// <inheritdoc />
    public async Task<bool> CreateBackupAsync(IPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));

        try
        {
            var pluginDirectory = plugin.Context?.PluginDirectory;
            if (string.IsNullOrWhiteSpace(pluginDirectory) || !Directory.Exists(pluginDirectory))
            {
                _logger.LogWarning("Plugin directory not found for backup: {PluginId}", plugin.Metadata.Id);
                return false;
            }

            var backupPath = Path.Combine(_backupDirectory, $"{plugin.Metadata.Id}_{plugin.Metadata.Version}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}");
            
            _logger.LogDebug("Creating backup for plugin {PluginId} at {BackupPath}", plugin.Metadata.Id, backupPath);

            // Create backup directory
            Directory.CreateDirectory(backupPath);

            // Copy plugin files
            await CopyDirectoryAsync(pluginDirectory, backupPath, cancellationToken);

            // Record backup in version history
            if (!_versionHistory.TryGetValue(plugin, out var history))
            {
                history = new PluginVersionHistory();
                _versionHistory[plugin] = history;
            }

            var versionInfo = new PluginVersionInfo
            {
                Version = plugin.Metadata.Version.ToString(),
                InstallDate = DateTimeOffset.UtcNow,
                Path = backupPath,
                IsActive = true
            };

            // Mark other versions as inactive
            foreach (var version in history.Versions)
            {
                version.IsActive = false;
            }

            history.Versions.Add(versionInfo);

            _logger.LogDebug("Backup created successfully for plugin {PluginId}", plugin.Metadata.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup for plugin {PluginId}", plugin.Metadata.Id);
            return false;
        }
    }

    /// <inheritdoc />
    public event EventHandler<PluginUpdateEventArgs>? UpdateCompleted;

    private async Task<bool> PerformUpdateAsync(IPlugin plugin, string newVersionPath, CancellationToken cancellationToken)
    {
        try
        {
            var pluginDirectory = plugin.Context?.PluginDirectory;
            if (string.IsNullOrWhiteSpace(pluginDirectory))
            {
                throw new InvalidOperationException("Plugin directory not available");
            }

            // Stop plugin if running
            var wasRunning = plugin.IsRunning;
            if (wasRunning)
            {
                _stateTracker.SetPluginState(plugin, PluginState.Stopping);
                await plugin.StopAsync(cancellationToken);
                _stateTracker.SetPluginState(plugin, PluginState.Stopped);
            }

            // Copy new version files
            var targetFileName = Path.GetFileName(newVersionPath);
            var targetPath = Path.Combine(pluginDirectory, targetFileName);
            
            File.Copy(newVersionPath, targetPath, overwrite: true);

            // In a real implementation, would need to reload the plugin assembly
            // This simplified version assumes the plugin can be restarted with new files

            // Restart plugin if it was running
            if (wasRunning)
            {
                _stateTracker.SetPluginState(plugin, PluginState.Starting);
                await plugin.StartAsync(cancellationToken);
                _stateTracker.SetPluginState(plugin, PluginState.Running);
            }
            else
            {
                _stateTracker.SetPluginState(plugin, PluginState.Stopped);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing update for plugin {PluginId}", plugin.Metadata.Id);
            return false;
        }
    }

    private void RecordVersionUpdate(IPlugin plugin, string fromVersion, string toVersion)
    {
        if (!_versionHistory.TryGetValue(plugin, out var history))
        {
            history = new PluginVersionHistory();
            _versionHistory[plugin] = history;
        }

        // Mark all versions as inactive
        foreach (var version in history.Versions)
        {
            version.IsActive = false;
        }

        // Add new active version
        var newVersionInfo = new PluginVersionInfo
        {
            Version = toVersion,
            InstallDate = DateTimeOffset.UtcNow,
            Path = plugin.Context?.PluginDirectory ?? string.Empty,
            IsActive = true
        };

        history.Versions.Add(newVersionInfo);
    }

    private async Task RestorePreviousVersionAsync(IPlugin plugin, PluginVersionInfo previousVersion, CancellationToken cancellationToken)
    {
        var pluginDirectory = plugin.Context?.PluginDirectory;
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            throw new InvalidOperationException("Plugin directory not available");
        }

        // Copy files from backup to plugin directory
        await CopyDirectoryAsync(previousVersion.Path, pluginDirectory, cancellationToken);
    }

    private async Task CopyDirectoryAsync(string sourceDir, string targetDir, CancellationToken cancellationToken)
    {
        var source = new DirectoryInfo(sourceDir);
        var target = new DirectoryInfo(targetDir);

        if (!target.Exists)
        {
            target.Create();
        }

        // Copy files
        foreach (var file in source.GetFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetFile = Path.Combine(target.FullName, file.Name);
            file.CopyTo(targetFile, overwrite: true);
        }

        // Copy subdirectories
        foreach (var subDir in source.GetDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var targetSubDir = Path.Combine(target.FullName, subDir.Name);
            await CopyDirectoryAsync(subDir.FullName, targetSubDir, cancellationToken);
        }
    }

    private void FireUpdateCompletedEvent(IPlugin plugin, string fromVersion, string toVersion, bool wasSuccessful, Exception? exception)
    {
        try
        {
            var eventArgs = new PluginUpdateEventArgs(plugin, fromVersion, toVersion, wasSuccessful, exception);
            UpdateCompleted?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firing UpdateCompleted event for plugin {PluginId}", plugin.Metadata.Id);
        }
    }

    /// <summary>
    /// Releases resources used by the PluginUpdateManager.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _versionHistory.Clear();
            _disposed = true;
            
            _logger.LogInformation("PluginUpdateManager disposed");
        }
    }

    private class PluginVersionHistory
    {
        public List<PluginVersionInfo> Versions { get; } = new();
    }
}
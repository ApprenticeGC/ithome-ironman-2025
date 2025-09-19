using GameConsole.Plugins.Core;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Manages plugin updates, versioning, and rollback operations.
/// Provides capabilities for hot-swapping plugins and version management.
/// </summary>
public interface IPluginUpdateManager
{
    /// <summary>
    /// Updates a plugin to a newer version.
    /// Performs hot-swap if possible, otherwise graceful restart.
    /// </summary>
    /// <param name="plugin">The plugin to update.</param>
    /// <param name="newVersionPath">Path to the new plugin version.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if update was successful, false if rolled back.</returns>
    Task<bool> UpdatePluginAsync(IPlugin plugin, string newVersionPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back a plugin to its previous version.
    /// </summary>
    /// <param name="plugin">The plugin to roll back.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if rollback was successful.</returns>
    Task<bool> RollbackPluginAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an update is available for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update information if available, null otherwise.</returns>
    Task<PluginUpdateInfo?> CheckForUpdateAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a plugin update is compatible and safe to install.
    /// </summary>
    /// <param name="plugin">The current plugin.</param>
    /// <param name="updatePath">Path to the update package.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result.</returns>
    Task<PluginUpdateValidationResult> ValidateUpdateAsync(IPlugin plugin, string updatePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the version history for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to get history for.</param>
    /// <returns>Collection of version history entries.</returns>
    IReadOnlyCollection<PluginVersionInfo> GetPluginVersionHistory(IPlugin plugin);

    /// <summary>
    /// Gets whether rollback is available for the specified plugin.
    /// </summary>
    /// <param name="plugin">The plugin to check.</param>
    /// <returns>True if rollback is available.</returns>
    bool CanRollback(IPlugin plugin);

    /// <summary>
    /// Creates a backup of the current plugin version before update.
    /// </summary>
    /// <param name="plugin">The plugin to backup.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if backup was created successfully.</returns>
    Task<bool> CreateBackupAsync(IPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event fired when a plugin update operation completes.
    /// </summary>
    event EventHandler<PluginUpdateEventArgs> UpdateCompleted;
}

/// <summary>
/// Information about an available plugin update.
/// </summary>
public class PluginUpdateInfo
{
    /// <summary>
    /// Gets the current version of the plugin.
    /// </summary>
    public required string CurrentVersion { get; init; }

    /// <summary>
    /// Gets the available update version.
    /// </summary>
    public required string UpdateVersion { get; init; }

    /// <summary>
    /// Gets the URL or path where the update can be downloaded.
    /// </summary>
    public required string UpdateSource { get; init; }

    /// <summary>
    /// Gets the release notes for the update.
    /// </summary>
    public string? ReleaseNotes { get; init; }

    /// <summary>
    /// Gets whether this is a critical security update.
    /// </summary>
    public bool IsCriticalUpdate { get; init; }

    /// <summary>
    /// Gets the minimum host version required by the update.
    /// </summary>
    public string? MinimumHostVersion { get; init; }
}

/// <summary>
/// Result of plugin update validation.
/// </summary>
public class PluginUpdateValidationResult
{
    /// <summary>
    /// Gets whether the update is valid and safe to install.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets validation errors if any.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets validation warnings.
    /// </summary>
    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets whether the update requires a restart of dependent plugins.
    /// </summary>
    public bool RequiresDependentRestart { get; init; }
}

/// <summary>
/// Information about a plugin version.
/// </summary>
public class PluginVersionInfo
{
    /// <summary>
    /// Gets the version number.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets when this version was installed.
    /// </summary>
    public required DateTimeOffset InstallDate { get; init; }

    /// <summary>
    /// Gets the path to this version's files.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets whether this is the currently active version.
    /// </summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Event arguments for plugin update operations.
/// </summary>
public class PluginUpdateEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginUpdateEventArgs"/> class.
    /// </summary>
    /// <param name="plugin">The plugin that was updated.</param>
    /// <param name="fromVersion">The previous version.</param>
    /// <param name="toVersion">The new version.</param>
    /// <param name="wasSuccessful">Whether the update was successful.</param>
    /// <param name="exception">Optional exception if update failed.</param>
    public PluginUpdateEventArgs(IPlugin plugin, string fromVersion, string toVersion, bool wasSuccessful, Exception? exception = null)
    {
        Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        FromVersion = fromVersion ?? throw new ArgumentNullException(nameof(fromVersion));
        ToVersion = toVersion ?? throw new ArgumentNullException(nameof(toVersion));
        WasSuccessful = wasSuccessful;
        Exception = exception;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets the plugin that was updated.
    /// </summary>
    public IPlugin Plugin { get; }

    /// <summary>
    /// Gets the previous version.
    /// </summary>
    public string FromVersion { get; }

    /// <summary>
    /// Gets the new version.
    /// </summary>
    public string ToVersion { get; }

    /// <summary>
    /// Gets whether the update was successful.
    /// </summary>
    public bool WasSuccessful { get; }

    /// <summary>
    /// Gets the exception if update failed.
    /// </summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the timestamp when update completed.
    /// </summary>
    public DateTimeOffset Timestamp { get; }
}
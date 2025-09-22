using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Configuration;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Interface for secure configuration storage with encryption and integrity validation.
/// Provides encrypted storage of sensitive configuration data.
/// </summary>
public interface ISecureConfigurationStore : IService
{
    /// <summary>
    /// Stores a configuration value securely with encryption.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value to store.</param>
    /// <param name="isSensitive">Whether the value should be encrypted.</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the store operation.</returns>
    Task SetConfigurationAsync(string key, string value, bool isSensitive, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a configuration value, decrypting if necessary.
    /// </summary>
    /// <param name="key">The configuration key to retrieve.</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The configuration value, or null if not found.</returns>
    Task<string?> GetConfigurationAsync(string key, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a configuration value from secure storage.
    /// </summary>
    /// <param name="key">The configuration key to remove.</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the configuration was removed, false if it didn't exist.</returns>
    Task<bool> RemoveConfigurationAsync(string key, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a configuration key exists.
    /// </summary>
    /// <param name="key">The configuration key to check.</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the key exists, false otherwise.</returns>
    Task<bool> ConfigurationExistsAsync(string key, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configuration keys matching a pattern.
    /// </summary>
    /// <param name="pattern">The pattern to match (supports wildcards).</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of matching configuration keys.</returns>
    Task<IEnumerable<string>> GetConfigurationKeysAsync(string pattern, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets configuration metadata without retrieving the actual values.
    /// </summary>
    /// <param name="keys">The configuration keys to get metadata for.</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Configuration metadata for the specified keys.</returns>
    Task<IEnumerable<ConfigurationMetadata>> GetConfigurationMetadataAsync(IEnumerable<string> keys, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the integrity of all stored configurations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Integrity validation result.</returns>
    Task<IntegrityValidationResult> ValidateIntegrityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Backs up configuration data to a secure location.
    /// </summary>
    /// <param name="backupPath">The path to store the backup.</param>
    /// <param name="includeEncryption">Whether to keep data encrypted in the backup.</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Backup operation result.</returns>
    Task<BackupResult> BackupConfigurationAsync(string backupPath, bool includeEncryption, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores configuration data from a backup.
    /// </summary>
    /// <param name="backupPath">The path to restore the backup from.</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Restore operation result.</returns>
    Task<RestoreResult> RestoreConfigurationAsync(string backupPath, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports configuration from a standard .NET configuration source.
    /// </summary>
    /// <param name="configuration">The configuration to import.</param>
    /// <param name="sensitiveKeys">Keys that should be treated as sensitive and encrypted.</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Import operation result.</returns>
    Task<ImportResult> ImportConfigurationAsync(IConfiguration configuration, IEnumerable<string> sensitiveKeys, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports configuration to a standard .NET configuration format.
    /// </summary>
    /// <param name="keyPattern">Pattern to match configuration keys for export.</param>
    /// <param name="includeDecrypted">Whether to include decrypted values for sensitive data.</param>
    /// <param name="userId">The user performing the operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Export operation result with configuration data.</returns>
    Task<ExportResult> ExportConfigurationAsync(string keyPattern, bool includeDecrypted, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadata about a configuration entry.
/// </summary>
public record ConfigurationMetadata
{
    /// <summary>
    /// The configuration key.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Whether the configuration value is encrypted.
    /// </summary>
    public required bool IsEncrypted { get; init; }

    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the configuration was last modified.
    /// </summary>
    public required DateTimeOffset LastModified { get; init; }

    /// <summary>
    /// The user who created the configuration.
    /// </summary>
    public required string CreatedBy { get; init; }

    /// <summary>
    /// The user who last modified the configuration.
    /// </summary>
    public required string LastModifiedBy { get; init; }

    /// <summary>
    /// The key ID used for encryption (if encrypted).
    /// </summary>
    public string? KeyId { get; init; }

    /// <summary>
    /// The size of the configuration value in bytes.
    /// </summary>
    public int? SizeBytes { get; init; }

    /// <summary>
    /// Tags associated with the configuration.
    /// </summary>
    public IEnumerable<string>? Tags { get; init; }
}

/// <summary>
/// Result of an integrity validation operation.
/// </summary>
public record IntegrityValidationResult
{
    /// <summary>
    /// Whether all configurations passed integrity validation.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Total number of configurations validated.
    /// </summary>
    public required int TotalConfigurations { get; init; }

    /// <summary>
    /// Number of configurations that passed validation.
    /// </summary>
    public required int ValidConfigurations { get; init; }

    /// <summary>
    /// Number of configurations that failed validation.
    /// </summary>
    public required int InvalidConfigurations { get; init; }

    /// <summary>
    /// Details about validation failures.
    /// </summary>
    public IEnumerable<ValidationFailure>? ValidationFailures { get; init; }

    /// <summary>
    /// When the validation was performed.
    /// </summary>
    public required DateTimeOffset ValidatedAt { get; init; }
}

/// <summary>
/// Details about a validation failure.
/// </summary>
public record ValidationFailure
{
    /// <summary>
    /// The configuration key that failed validation.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// The reason for the validation failure.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Additional error details.
    /// </summary>
    public string? Details { get; init; }
}

/// <summary>
/// Result of a backup operation.
/// </summary>
public record BackupResult
{
    /// <summary>
    /// Whether the backup operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Path where the backup was stored.
    /// </summary>
    public required string BackupPath { get; init; }

    /// <summary>
    /// Number of configurations backed up.
    /// </summary>
    public required int ConfigurationsBackedUp { get; init; }

    /// <summary>
    /// Size of the backup file in bytes.
    /// </summary>
    public required long BackupSizeBytes { get; init; }

    /// <summary>
    /// When the backup was created.
    /// </summary>
    public required DateTimeOffset BackupTimestamp { get; init; }

    /// <summary>
    /// Error message if the backup failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of a restore operation.
/// </summary>
public record RestoreResult
{
    /// <summary>
    /// Whether the restore operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Number of configurations restored.
    /// </summary>
    public required int ConfigurationsRestored { get; init; }

    /// <summary>
    /// Number of configurations that were skipped (e.g., already exist).
    /// </summary>
    public int ConfigurationsSkipped { get; init; }

    /// <summary>
    /// Number of configurations that failed to restore.
    /// </summary>
    public int ConfigurationsFailed { get; init; }

    /// <summary>
    /// When the restore was performed.
    /// </summary>
    public required DateTimeOffset RestoreTimestamp { get; init; }

    /// <summary>
    /// Error message if the restore failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Details about any failures during restore.
    /// </summary>
    public IEnumerable<string>? FailureDetails { get; init; }
}

/// <summary>
/// Result of an import operation.
/// </summary>
public record ImportResult
{
    /// <summary>
    /// Whether the import operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Number of configurations imported.
    /// </summary>
    public required int ConfigurationsImported { get; init; }

    /// <summary>
    /// Number of configurations that were skipped.
    /// </summary>
    public int ConfigurationsSkipped { get; init; }

    /// <summary>
    /// When the import was performed.
    /// </summary>
    public required DateTimeOffset ImportTimestamp { get; init; }

    /// <summary>
    /// Error message if the import failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of an export operation.
/// </summary>
public record ExportResult
{
    /// <summary>
    /// Whether the export operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The exported configuration data.
    /// </summary>
    public Dictionary<string, string>? ConfigurationData { get; init; }

    /// <summary>
    /// Number of configurations exported.
    /// </summary>
    public required int ConfigurationsExported { get; init; }

    /// <summary>
    /// When the export was performed.
    /// </summary>
    public required DateTimeOffset ExportTimestamp { get; init; }

    /// <summary>
    /// Error message if the export failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
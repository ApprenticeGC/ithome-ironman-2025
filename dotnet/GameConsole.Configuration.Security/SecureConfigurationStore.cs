using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;
using System.Globalization;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Secure configuration store implementation with encryption and access control integration.
/// Provides encrypted storage of sensitive configuration data with comprehensive auditing.
/// </summary>
public class SecureConfigurationStore : ISecureConfigurationStore
{
    private readonly ILogger<SecureConfigurationStore> _logger;
    private readonly IConfigurationEncryption _encryption;
    private readonly IConfigurationAccessControl _accessControl;
    private readonly IConfigurationAuditLogger _auditLogger;
    private readonly ConcurrentDictionary<string, SecureConfigurationEntry> _store;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public SecureConfigurationStore(
        ILogger<SecureConfigurationStore> logger,
        IConfigurationEncryption encryption,
        IConfigurationAccessControl accessControl,
        IConfigurationAuditLogger auditLogger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
        _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
        _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
        _store = new ConcurrentDictionary<string, SecureConfigurationEntry>();
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Secure Configuration Store");
        _logger.LogInformation("Secure Configuration Store initialized");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Secure Configuration Store");
        _isRunning = true;
        _logger.LogInformation("Secure Configuration Store started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Secure Configuration Store");
        _isRunning = false;
        _logger.LogInformation("Secure Configuration Store stopped");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _isRunning = false;
        _store.Clear();
        return ValueTask.CompletedTask;
    }

    public async Task SetConfigurationAsync(string key, string value, bool isSensitive, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Configuration key cannot be null or empty.", nameof(key));
        if (value == null)
            throw new ArgumentNullException(nameof(value));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        var auditEntry = new ConfigurationAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ConfigurationPath = key,
            UserId = userId,
            OperationType = ConfigurationOperationType.Write,
            Timestamp = DateTimeOffset.UtcNow,
            Success = false
        };

        try
        {
            // Check write permissions
            if (!await _accessControl.CanWriteConfigurationAsync(key, userId, cancellationToken))
            {
                auditEntry = auditEntry with { ErrorMessage = "Access denied: insufficient write permissions" };
                await _auditLogger.LogWriteAsync(auditEntry, cancellationToken);
                throw new UnauthorizedAccessException($"User '{userId}' does not have write permission for configuration '{key}'.");
            }

            // Get previous value for auditing
            var previousValue = await GetConfigurationInternalAsync(key, userId, false, cancellationToken);

            // Encrypt value if sensitive
            EncryptedData? encryptedData = null;
            if (isSensitive)
            {
                encryptedData = await _encryption.EncryptAsync(value, cancellationToken: cancellationToken);
                _logger.LogDebug("Encrypted sensitive configuration value for key: {Key}", key);
            }

            var entry = new SecureConfigurationEntry
            {
                Key = key,
                Value = isSensitive ? null : value,
                EncryptedData = encryptedData,
                IsSensitive = isSensitive,
                CreatedAt = _store.ContainsKey(key) ? _store[key].CreatedAt : DateTimeOffset.UtcNow,
                LastModified = DateTimeOffset.UtcNow,
                CreatedBy = _store.ContainsKey(key) ? _store[key].CreatedBy : userId,
                LastModifiedBy = userId,
                Tags = new List<string>()
            };

            _store.AddOrUpdate(key, entry, (_, _) => entry);

            // Log successful audit entry
            auditEntry = auditEntry with 
            { 
                Success = true,
                PreviousValue = previousValue != null ? HashValue(previousValue) : null,
                NewValue = HashValue(value)
            };

            await _auditLogger.LogWriteAsync(auditEntry, cancellationToken);

            _logger.LogInformation("Set configuration '{Key}' (sensitive: {IsSensitive}) by user {UserId}", key, isSensitive, userId);
        }
        catch (Exception ex)
        {
            auditEntry = auditEntry with { ErrorMessage = ex.Message };
            await _auditLogger.LogWriteAsync(auditEntry, cancellationToken);
            _logger.LogError(ex, "Failed to set configuration '{Key}' for user {UserId}", key, userId);
            throw;
        }
    }

    public async Task<string?> GetConfigurationAsync(string key, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Configuration key cannot be null or empty.", nameof(key));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        return await GetConfigurationInternalAsync(key, userId, true, cancellationToken);
    }

    private async Task<string?> GetConfigurationInternalAsync(string key, string userId, bool logAudit, CancellationToken cancellationToken = default)
    {
        var auditEntry = new ConfigurationAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ConfigurationPath = key,
            UserId = userId,
            OperationType = ConfigurationOperationType.Read,
            Timestamp = DateTimeOffset.UtcNow,
            Success = false
        };

        try
        {
            // Check read permissions
            if (!await _accessControl.CanReadConfigurationAsync(key, userId, cancellationToken))
            {
                auditEntry = auditEntry with { ErrorMessage = "Access denied: insufficient read permissions" };
                if (logAudit) await _auditLogger.LogReadAsync(auditEntry, cancellationToken);
                throw new UnauthorizedAccessException($"User '{userId}' does not have read permission for configuration '{key}'.");
            }

            if (!_store.TryGetValue(key, out var entry))
            {
                auditEntry = auditEntry with { Success = true, ErrorMessage = "Configuration not found" };
                if (logAudit) await _auditLogger.LogReadAsync(auditEntry, cancellationToken);
                return null;
            }

            string value;
            if (entry.IsSensitive && entry.EncryptedData != null)
            {
                // Decrypt sensitive value
                value = await _encryption.DecryptAsync(entry.EncryptedData, cancellationToken);
                _logger.LogDebug("Decrypted sensitive configuration value for key: {Key}", key);
            }
            else
            {
                value = entry.Value!;
            }

            // Log successful audit entry
            auditEntry = auditEntry with { Success = true };
            if (logAudit) await _auditLogger.LogReadAsync(auditEntry, cancellationToken);

            return value;
        }
        catch (Exception ex)
        {
            auditEntry = auditEntry with { ErrorMessage = ex.Message };
            if (logAudit) await _auditLogger.LogReadAsync(auditEntry, cancellationToken);
            _logger.LogError(ex, "Failed to get configuration '{Key}' for user {UserId}", key, userId);
            throw;
        }
    }

    public async Task<bool> RemoveConfigurationAsync(string key, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Configuration key cannot be null or empty.", nameof(key));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        var auditEntry = new ConfigurationAuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            ConfigurationPath = key,
            UserId = userId,
            OperationType = ConfigurationOperationType.Delete,
            Timestamp = DateTimeOffset.UtcNow,
            Success = false
        };

        try
        {
            // Check delete permissions
            if (!await _accessControl.CanDeleteConfigurationAsync(key, userId, cancellationToken))
            {
                auditEntry = auditEntry with { ErrorMessage = "Access denied: insufficient delete permissions" };
                await _auditLogger.LogDeleteAsync(auditEntry, cancellationToken);
                throw new UnauthorizedAccessException($"User '{userId}' does not have delete permission for configuration '{key}'.");
            }

            bool removed = _store.TryRemove(key, out var removedEntry);

            // Log audit entry
            auditEntry = auditEntry with 
            { 
                Success = removed,
                PreviousValue = removedEntry?.IsSensitive == true ? "[ENCRYPTED]" : removedEntry?.Value ?? "[NOT_FOUND]"
            };

            await _auditLogger.LogDeleteAsync(auditEntry, cancellationToken);

            if (removed)
            {
                _logger.LogInformation("Removed configuration '{Key}' by user {UserId}", key, userId);
            }

            return removed;
        }
        catch (Exception ex)
        {
            auditEntry = auditEntry with { ErrorMessage = ex.Message };
            await _auditLogger.LogDeleteAsync(auditEntry, cancellationToken);
            _logger.LogError(ex, "Failed to remove configuration '{Key}' for user {UserId}", key, userId);
            throw;
        }
    }

    public async Task<bool> ConfigurationExistsAsync(string key, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Configuration key cannot be null or empty.", nameof(key));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            // Check read permissions to determine if key exists for this user
            if (!await _accessControl.CanReadConfigurationAsync(key, userId, cancellationToken))
            {
                return false; // User cannot read it, so for them it doesn't exist
            }

            return _store.ContainsKey(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if configuration '{Key}' exists for user {UserId}", key, userId);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetConfigurationKeysAsync(string pattern, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
            throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            var keys = new List<string>();

            foreach (var kvp in _store)
            {
                var key = kvp.Key;

                // Check if key matches pattern (simple wildcard support)
                if (MatchesPattern(key, pattern))
                {
                    // Check if user has read permission for this key
                    if (await _accessControl.CanReadConfigurationAsync(key, userId, cancellationToken))
                    {
                        keys.Add(key);
                    }
                }
            }

            _logger.LogDebug("Found {Count} configuration keys matching pattern '{Pattern}' for user {UserId}", keys.Count, pattern, userId);
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration keys with pattern '{Pattern}' for user {UserId}", pattern, userId);
            throw;
        }
    }

    public async Task<IEnumerable<ConfigurationMetadata>> GetConfigurationMetadataAsync(IEnumerable<string> keys, string userId, CancellationToken cancellationToken = default)
    {
        if (keys == null)
            throw new ArgumentNullException(nameof(keys));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            var metadata = new List<ConfigurationMetadata>();

            foreach (var key in keys)
            {
                if (!await _accessControl.CanReadConfigurationAsync(key, userId, cancellationToken))
                {
                    continue; // Skip keys user cannot read
                }

                if (_store.TryGetValue(key, out var entry))
                {
                    var meta = new ConfigurationMetadata
                    {
                        Key = key,
                        IsEncrypted = entry.IsSensitive,
                        CreatedAt = entry.CreatedAt,
                        LastModified = entry.LastModified,
                        CreatedBy = entry.CreatedBy,
                        LastModifiedBy = entry.LastModifiedBy,
                        KeyId = entry.EncryptedData?.KeyId,
                        SizeBytes = entry.IsSensitive 
                            ? entry.EncryptedData?.Data?.Length 
                            : Encoding.UTF8.GetByteCount(entry.Value ?? ""),
                        Tags = entry.Tags
                    };

                    metadata.Add(meta);
                }
            }

            _logger.LogDebug("Retrieved metadata for {Count} configurations for user {UserId}", metadata.Count, userId);
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration metadata for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IntegrityValidationResult> ValidateIntegrityAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting integrity validation of all stored configurations");

            var totalConfigurations = _store.Count;
            var validConfigurations = 0;
            var invalidConfigurations = 0;
            var validationFailures = new List<ValidationFailure>();

            foreach (var kvp in _store)
            {
                var key = kvp.Key;
                var entry = kvp.Value;

                try
                {
                    if (entry.IsSensitive && entry.EncryptedData != null)
                    {
                        // Validate encrypted data integrity
                        var isValid = await _encryption.ValidateIntegrityAsync(entry.EncryptedData, cancellationToken);
                        if (isValid)
                        {
                            validConfigurations++;
                        }
                        else
                        {
                            invalidConfigurations++;
                            validationFailures.Add(new ValidationFailure
                            {
                                Key = key,
                                Reason = "Encrypted data integrity validation failed",
                                Details = "HMAC verification failed"
                            });
                        }
                    }
                    else
                    {
                        // Non-encrypted data is always valid
                        validConfigurations++;
                    }
                }
                catch (Exception ex)
                {
                    invalidConfigurations++;
                    validationFailures.Add(new ValidationFailure
                    {
                        Key = key,
                        Reason = "Exception during validation",
                        Details = ex.Message
                    });
                }
            }

            var result = new IntegrityValidationResult
            {
                IsValid = invalidConfigurations == 0,
                TotalConfigurations = totalConfigurations,
                ValidConfigurations = validConfigurations,
                InvalidConfigurations = invalidConfigurations,
                ValidationFailures = validationFailures,
                ValidatedAt = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Integrity validation completed: {Valid}/{Total} configurations valid",
                validConfigurations, totalConfigurations);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during integrity validation");
            throw;
        }
    }

    public async Task<BackupResult> BackupConfigurationAsync(string backupPath, bool includeEncryption, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(backupPath))
            throw new ArgumentException("Backup path cannot be null or empty.", nameof(backupPath));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            _logger.LogInformation("Starting backup of configuration data to {BackupPath}", backupPath);

            var backupData = new Dictionary<string, object>();
            var configurationsBackedUp = 0;

            foreach (var kvp in _store)
            {
                var key = kvp.Key;
                var entry = kvp.Value;

                // Check if user has read permission for this configuration
                if (!await _accessControl.CanReadConfigurationAsync(key, userId, cancellationToken))
                {
                    continue;
                }

                if (includeEncryption && entry.IsSensitive)
                {
                    // Include encrypted data in backup
                    backupData[key] = new
                    {
                        IsEncrypted = true,
                        EncryptedData = entry.EncryptedData,
                        Metadata = new
                        {
                            entry.CreatedAt,
                            entry.LastModified,
                            entry.CreatedBy,
                            entry.LastModifiedBy,
                            entry.Tags
                        }
                    };
                }
                else if (!entry.IsSensitive)
                {
                    // Include plain text data in backup
                    backupData[key] = new
                    {
                        IsEncrypted = false,
                        Value = entry.Value,
                        Metadata = new
                        {
                            entry.CreatedAt,
                            entry.LastModified,
                            entry.CreatedBy,
                            entry.LastModifiedBy,
                            entry.Tags
                        }
                    };
                }
                // Skip sensitive data if includeEncryption is false

                configurationsBackedUp++;
            }

            var json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(backupPath, json, Encoding.UTF8, cancellationToken);

            var backupSize = new FileInfo(backupPath).Length;

            var result = new BackupResult
            {
                Success = true,
                BackupPath = backupPath,
                ConfigurationsBackedUp = configurationsBackedUp,
                BackupSizeBytes = backupSize,
                BackupTimestamp = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Backup completed successfully: {Count} configurations backed up to {Path} ({Size} bytes)",
                configurationsBackedUp, backupPath, backupSize);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup at {BackupPath}", backupPath);
            
            return new BackupResult
            {
                Success = false,
                BackupPath = backupPath,
                ConfigurationsBackedUp = 0,
                BackupSizeBytes = 0,
                BackupTimestamp = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<RestoreResult> RestoreConfigurationAsync(string backupPath, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(backupPath))
            throw new ArgumentException("Backup path cannot be null or empty.", nameof(backupPath));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            _logger.LogInformation("Starting restore of configuration data from {BackupPath}", backupPath);

            if (!File.Exists(backupPath))
            {
                throw new FileNotFoundException($"Backup file not found: {backupPath}");
            }

            var json = await File.ReadAllTextAsync(backupPath, Encoding.UTF8, cancellationToken);
            var backupData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

            var configurationsRestored = 0;
            var configurationsSkipped = 0;
            var configurationsFailed = 0;
            var failureDetails = new List<string>();

            if (backupData != null)
            {
                foreach (var kvp in backupData)
                {
                    var key = kvp.Key;
                    var data = kvp.Value;

                    try
                    {
                        // Check if user has write permission
                        if (!await _accessControl.CanWriteConfigurationAsync(key, userId, cancellationToken))
                        {
                            configurationsSkipped++;
                            continue;
                        }

                        // Skip if configuration already exists (could add overwrite option)
                        if (_store.ContainsKey(key))
                        {
                            configurationsSkipped++;
                            continue;
                        }

                        var isEncrypted = data.GetProperty("isEncrypted").GetBoolean();

                        if (isEncrypted)
                        {
                            var encryptedDataJson = data.GetProperty("encryptedData");
                            var encryptedData = JsonSerializer.Deserialize<EncryptedData>(encryptedDataJson.GetRawText());
                            
                            var entry = new SecureConfigurationEntry
                            {
                                Key = key,
                                Value = null,
                                EncryptedData = encryptedData,
                                IsSensitive = true,
                                CreatedAt = DateTimeOffset.UtcNow,
                                LastModified = DateTimeOffset.UtcNow,
                                CreatedBy = userId,
                                LastModifiedBy = userId,
                                Tags = new List<string>()
                            };

                            _store.TryAdd(key, entry);
                        }
                        else
                        {
                            var value = data.GetProperty("value").GetString();
                            
                            var entry = new SecureConfigurationEntry
                            {
                                Key = key,
                                Value = value,
                                EncryptedData = null,
                                IsSensitive = false,
                                CreatedAt = DateTimeOffset.UtcNow,
                                LastModified = DateTimeOffset.UtcNow,
                                CreatedBy = userId,
                                LastModifiedBy = userId,
                                Tags = new List<string>()
                            };

                            _store.TryAdd(key, entry);
                        }

                        configurationsRestored++;
                    }
                    catch (Exception ex)
                    {
                        configurationsFailed++;
                        failureDetails.Add($"Key '{key}': {ex.Message}");
                    }
                }
            }

            var result = new RestoreResult
            {
                Success = configurationsFailed == 0,
                ConfigurationsRestored = configurationsRestored,
                ConfigurationsSkipped = configurationsSkipped,
                ConfigurationsFailed = configurationsFailed,
                RestoreTimestamp = DateTimeOffset.UtcNow,
                FailureDetails = failureDetails
            };

            _logger.LogInformation("Restore completed: {Restored} restored, {Skipped} skipped, {Failed} failed",
                configurationsRestored, configurationsSkipped, configurationsFailed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup from {BackupPath}", backupPath);
            
            return new RestoreResult
            {
                Success = false,
                ConfigurationsRestored = 0,
                ConfigurationsSkipped = 0,
                ConfigurationsFailed = 0,
                RestoreTimestamp = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ImportResult> ImportConfigurationAsync(IConfiguration configuration, IEnumerable<string> sensitiveKeys, string userId, CancellationToken cancellationToken = default)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));
        if (sensitiveKeys == null)
            throw new ArgumentNullException(nameof(sensitiveKeys));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            _logger.LogInformation("Starting import of configuration data by user {UserId}", userId);

            var sensitiveKeySet = new HashSet<string>(sensitiveKeys, StringComparer.OrdinalIgnoreCase);
            var configurationsImported = 0;
            var configurationsSkipped = 0;

            foreach (var kvp in configuration.AsEnumerable())
            {
                var key = kvp.Key;
                var value = kvp.Value;

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                {
                    configurationsSkipped++;
                    continue;
                }

                try
                {
                    var isSensitive = sensitiveKeySet.Contains(key);
                    await SetConfigurationAsync(key, value, isSensitive, userId, cancellationToken);
                    configurationsImported++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to import configuration '{Key}'", key);
                    configurationsSkipped++;
                }
            }

            var result = new ImportResult
            {
                Success = true,
                ConfigurationsImported = configurationsImported,
                ConfigurationsSkipped = configurationsSkipped,
                ImportTimestamp = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Import completed: {Imported} imported, {Skipped} skipped",
                configurationsImported, configurationsSkipped);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing configuration data");
            
            return new ImportResult
            {
                Success = false,
                ConfigurationsImported = 0,
                ConfigurationsSkipped = 0,
                ImportTimestamp = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ExportResult> ExportConfigurationAsync(string keyPattern, bool includeDecrypted, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(keyPattern))
            throw new ArgumentException("Key pattern cannot be null or empty.", nameof(keyPattern));
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            _logger.LogInformation("Starting export of configuration data with pattern '{Pattern}' by user {UserId}", keyPattern, userId);

            var configurationData = new Dictionary<string, string>();
            var configurationsExported = 0;

            var matchingKeys = await GetConfigurationKeysAsync(keyPattern, userId, cancellationToken);

            foreach (var key in matchingKeys)
            {
                try
                {
                    if (_store.TryGetValue(key, out var entry))
                    {
                        if (entry.IsSensitive && !includeDecrypted)
                        {
                            configurationData[key] = "[ENCRYPTED]";
                        }
                        else
                        {
                            var value = await GetConfigurationAsync(key, userId, cancellationToken);
                            configurationData[key] = value ?? "[NULL]";
                        }
                        
                        configurationsExported++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to export configuration '{Key}'", key);
                    configurationData[key] = $"[ERROR: {ex.Message}]";
                }
            }

            var result = new ExportResult
            {
                Success = true,
                ConfigurationData = configurationData,
                ConfigurationsExported = configurationsExported,
                ExportTimestamp = DateTimeOffset.UtcNow
            };

            _logger.LogInformation("Export completed: {Exported} configurations exported", configurationsExported);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting configuration data with pattern '{Pattern}'", keyPattern);
            
            return new ExportResult
            {
                Success = false,
                ConfigurationData = null,
                ConfigurationsExported = 0,
                ExportTimestamp = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    private static bool MatchesPattern(string value, string pattern)
    {
        // Simple wildcard pattern matching (* and ?)
        if (pattern == "*")
            return true;

        if (!pattern.Contains('*') && !pattern.Contains('?'))
            return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);

        // Convert wildcard pattern to regex
        var regexPattern = "^" + 
            pattern.Replace("*", ".*")
                   .Replace("?", ".")
                   .Replace("[", "\\[")
                   .Replace("]", "\\]") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(value, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private static string HashValue(string value)
    {
        // Create a simple hash for audit logging (don't store actual sensitive values)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value ?? ""));
        return Convert.ToHexString(hash)[..16]; // First 16 characters of hex
    }

    /// <summary>
    /// Internal representation of a secure configuration entry.
    /// </summary>
    private record SecureConfigurationEntry
    {
        public required string Key { get; init; }
        public string? Value { get; init; }
        public EncryptedData? EncryptedData { get; init; }
        public required bool IsSensitive { get; init; }
        public required DateTimeOffset CreatedAt { get; init; }
        public required DateTimeOffset LastModified { get; init; }
        public required string CreatedBy { get; init; }
        public required string LastModifiedBy { get; init; }
        public required IEnumerable<string> Tags { get; init; }
    }
}
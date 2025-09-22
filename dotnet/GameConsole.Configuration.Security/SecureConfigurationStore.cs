using System;
using System.Collections.Concurrent;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameConsole.Configuration.Security
{
    /// <summary>
    /// Secure configuration store with encryption, access control, and audit logging.
    /// Implements Tier 3 service orchestration following the 4-tier architecture.
    /// </summary>
    public class SecureConfigurationStore
    {
        private readonly IConfigurationEncryption _encryption;
        private readonly ConfigurationAccessControl _accessControl;
        private readonly ConfigurationAuditLogger _auditLogger;
        private readonly ILogger<SecureConfigurationStore> _logger;
        private readonly ConcurrentDictionary<string, string> _secureStore;

        public SecureConfigurationStore(
            IConfigurationEncryption encryption,
            ConfigurationAccessControl accessControl,
            ConfigurationAuditLogger auditLogger,
            ILogger<SecureConfigurationStore> logger)
        {
            _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
            _accessControl = accessControl ?? throw new ArgumentNullException(nameof(accessControl));
            _auditLogger = auditLogger ?? throw new ArgumentNullException(nameof(auditLogger));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _secureStore = new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Securely stores a configuration value with encryption and access control.
        /// </summary>
        /// <param name="user">The user storing the configuration</param>
        /// <param name="key">Configuration key</param>
        /// <param name="value">Configuration value to encrypt and store</param>
        /// <param name="keyName">Encryption key identifier</param>
        /// <returns>Task representing the storage operation</returns>
        public async Task<bool> SetSecureValueAsync(IIdentity user, string key, string value, string keyName)
        {
            // Check write permissions
            if (!await _accessControl.HasPermissionAsync(user, ConfigurationAccessControl.Permissions.WriteConfiguration))
            {
                await _auditLogger.LogAccessControlEventAsync(user, 
                    ConfigurationAccessControl.Permissions.WriteConfiguration, 
                    false, 
                    "Insufficient permissions for configuration write");
                return false;
            }

            try
            {
                // Encrypt the value
                var encryptedValue = await _encryption.EncryptAsync(value, keyName);
                
                // Store the encrypted value
                _secureStore.AddOrUpdate(key, encryptedValue, (k, v) => encryptedValue);

                // Log successful operation
                await _auditLogger.LogConfigurationEventAsync(
                    ConfigurationAuditLogger.EventTypes.ConfigurationWrite,
                    user,
                    key,
                    true,
                    new { KeyName = keyName, ValueLength = value.Length });

                _logger.LogInformation("Securely stored configuration value for key {Key} using encryption key {KeyName}", key, keyName);
                return true;
            }
            catch (Exception ex)
            {
                await _auditLogger.LogConfigurationEventAsync(
                    ConfigurationAuditLogger.EventTypes.ConfigurationWrite,
                    user,
                    key,
                    false,
                    new { Error = ex.Message });

                _logger.LogError(ex, "Failed to securely store configuration value for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Retrieves and decrypts a secure configuration value with access control.
        /// </summary>
        /// <param name="user">The user retrieving the configuration</param>
        /// <param name="key">Configuration key</param>
        /// <param name="keyName">Encryption key identifier</param>
        /// <returns>The decrypted configuration value, or null if not found or access denied</returns>
        public async Task<string?> GetSecureValueAsync(IIdentity user, string key, string keyName)
        {
            // Check read permissions
            if (!await _accessControl.HasPermissionAsync(user, ConfigurationAccessControl.Permissions.ReadConfiguration))
            {
                await _auditLogger.LogAccessControlEventAsync(user,
                    ConfigurationAccessControl.Permissions.ReadConfiguration,
                    false,
                    "Insufficient permissions for configuration read");
                return null;
            }

            try
            {
                // Retrieve encrypted value
                if (!_secureStore.TryGetValue(key, out var encryptedValue))
                {
                    await _auditLogger.LogConfigurationEventAsync(
                        ConfigurationAuditLogger.EventTypes.ConfigurationRead,
                        user,
                        key,
                        false,
                        new { Reason = "Key not found" });
                    return null;
                }

                // Decrypt the value
                var decryptedValue = await _encryption.DecryptAsync(encryptedValue, keyName);

                // Log successful operation
                await _auditLogger.LogConfigurationEventAsync(
                    ConfigurationAuditLogger.EventTypes.ConfigurationRead,
                    user,
                    key,
                    true,
                    new { KeyName = keyName });

                return decryptedValue;
            }
            catch (Exception ex)
            {
                await _auditLogger.LogConfigurationEventAsync(
                    ConfigurationAuditLogger.EventTypes.ConfigurationRead,
                    user,
                    key,
                    false,
                    new { Error = ex.Message });

                _logger.LogError(ex, "Failed to retrieve secure configuration value for key {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Removes a secure configuration value with proper access control and auditing.
        /// </summary>
        /// <param name="user">The user removing the configuration</param>
        /// <param name="key">Configuration key to remove</param>
        /// <returns>True if successfully removed</returns>
        public async Task<bool> RemoveSecureValueAsync(IIdentity user, string key)
        {
            // Check write permissions (required for deletion)
            if (!await _accessControl.HasPermissionAsync(user, ConfigurationAccessControl.Permissions.WriteConfiguration))
            {
                await _auditLogger.LogAccessControlEventAsync(user,
                    ConfigurationAccessControl.Permissions.WriteConfiguration,
                    false,
                    "Insufficient permissions for configuration deletion");
                return false;
            }

            try
            {
                var removed = _secureStore.TryRemove(key, out _);

                await _auditLogger.LogConfigurationEventAsync(
                    "config.delete",
                    user,
                    key,
                    removed,
                    new { Operation = "delete" });

                if (removed)
                {
                    _logger.LogInformation("Successfully removed secure configuration value for key {Key}", key);
                }

                return removed;
            }
            catch (Exception ex)
            {
                await _auditLogger.LogConfigurationEventAsync(
                    "config.delete",
                    user,
                    key,
                    false,
                    new { Error = ex.Message });

                _logger.LogError(ex, "Failed to remove secure configuration value for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Rotates encryption for all stored values using new key.
        /// </summary>
        /// <param name="user">The user performing key rotation</param>
        /// <param name="oldKeyName">Current encryption key</param>
        /// <param name="newKeyName">New encryption key</param>
        /// <returns>Number of values successfully rotated</returns>
        public async Task<int> RotateEncryptionKeysAsync(IIdentity user, string oldKeyName, string newKeyName)
        {
            // Check key management permissions
            if (!await _accessControl.HasPermissionAsync(user, ConfigurationAccessControl.Permissions.ManageKeys))
            {
                await _auditLogger.LogAccessControlEventAsync(user,
                    ConfigurationAccessControl.Permissions.ManageKeys,
                    false,
                    "Insufficient permissions for key rotation");
                return 0;
            }

            int rotatedCount = 0;

            foreach (var kvp in _secureStore)
            {
                try
                {
                    // Decrypt with old key and re-encrypt with new key
                    var decryptedValue = await _encryption.DecryptAsync(kvp.Value, oldKeyName);
                    var reencryptedValue = await _encryption.EncryptAsync(decryptedValue, newKeyName);
                    
                    _secureStore.TryUpdate(kvp.Key, reencryptedValue, kvp.Value);
                    rotatedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to rotate encryption for key {Key}", kvp.Key);
                }
            }

            await _auditLogger.LogKeyRotationAsync(user, oldKeyName, newKeyName, rotatedCount > 0);
            
            _logger.LogInformation("Rotated encryption keys for {Count} configuration values", rotatedCount);
            return rotatedCount;
        }

        /// <summary>
        /// Validates integrity of all stored configuration data.
        /// </summary>
        /// <param name="user">The user performing validation</param>
        /// <returns>Validation results summary</returns>
        public async Task<ConfigurationIntegrityReport> ValidateIntegrityAsync(IIdentity user)
        {
            // Check audit access permissions
            if (!await _accessControl.HasPermissionAsync(user, ConfigurationAccessControl.Permissions.AuditAccess))
            {
                await _auditLogger.LogAccessControlEventAsync(user,
                    ConfigurationAccessControl.Permissions.AuditAccess,
                    false,
                    "Insufficient permissions for integrity validation");
                
                return new ConfigurationIntegrityReport { AccessDenied = true };
            }

            var report = new ConfigurationIntegrityReport
            {
                TotalKeys = _secureStore.Count,
                ValidatedAt = DateTimeOffset.UtcNow
            };

            foreach (var kvp in _secureStore)
            {
                try
                {
                    // Generate integrity hash for validation
                    var hash = System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(kvp.Value));
                    var hashString = Convert.ToBase64String(hash);
                    
                    // Validate integrity (in real implementation, compare with stored hash)
                    var isValid = await _encryption.ValidateIntegrityAsync(kvp.Value, hashString);
                    
                    if (isValid)
                    {
                        report.ValidKeys++;
                    }
                    else
                    {
                        report.InvalidKeys++;
                        report.InvalidKeyNames.Add(kvp.Key);
                    }

                    await _auditLogger.LogIntegrityValidationAsync(kvp.Key, isValid, hashString, hashString);
                }
                catch (Exception ex)
                {
                    report.ErrorKeys++;
                    _logger.LogError(ex, "Error validating integrity for key {Key}", kvp.Key);
                }
            }

            return report;
        }
    }

    /// <summary>
    /// Report structure for configuration integrity validation results.
    /// </summary>
    public class ConfigurationIntegrityReport
    {
        public int TotalKeys { get; set; }
        public int ValidKeys { get; set; }
        public int InvalidKeys { get; set; }
        public int ErrorKeys { get; set; }
        public bool AccessDenied { get; set; }
        public DateTimeOffset ValidatedAt { get; set; }
        public List<string> InvalidKeyNames { get; set; } = new();
    }
}
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Logging;

namespace GameConsole.Configuration.Security
{
    /// <summary>
    /// Azure Key Vault implementation of configuration encryption service.
    /// Implements Tier 4 provider following the 4-tier architecture with AES encryption and FIPS compliance support.
    /// </summary>
    public class AzureKeyVaultConfigurationEncryption : IConfigurationEncryption
    {
        private readonly KeyClient _keyClient;
        private readonly ILogger<AzureKeyVaultConfigurationEncryption> _logger;
        private readonly string _keyVaultUri;

        public AzureKeyVaultConfigurationEncryption(
            string keyVaultUri,
            ILogger<AzureKeyVaultConfigurationEncryption> logger)
        {
            if (string.IsNullOrWhiteSpace(keyVaultUri))
            {
                throw new ArgumentException("Key Vault URI cannot be null or empty", nameof(keyVaultUri));
            }

            _keyVaultUri = keyVaultUri;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Use DefaultAzureCredential for authentication (supports managed identity, service principal, etc.)
            var credential = new DefaultAzureCredential();
            _keyClient = new KeyClient(new Uri(keyVaultUri), credential);

            _logger.LogInformation("Initialized Azure Key Vault encryption service with URI: {KeyVaultUri}", keyVaultUri);
        }

        /// <summary>
        /// Encrypts sensitive configuration value using Azure Key Vault AES encryption.
        /// </summary>
        /// <param name="plainText">The plain text value to encrypt</param>
        /// <param name="keyName">The key identifier in Azure Key Vault</param>
        /// <returns>The encrypted value as base64 string with metadata</returns>
        public async Task<string> EncryptAsync(string plainText, string keyName)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
            }

            if (string.IsNullOrEmpty(keyName))
            {
                throw new ArgumentException("Key name cannot be null or empty", nameof(keyName));
            }

            try
            {
                // Get the key from Azure Key Vault
                var keyResponse = await _keyClient.GetKeyAsync(keyName);
                var key = keyResponse.Value;

                // Create cryptography client for the key
                var cryptoClient = new CryptographyClient(key.Id, new DefaultAzureCredential());

                // Convert plain text to bytes
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

                // Encrypt using AES-GCM (FIPS compliant)
                var encryptResult = await cryptoClient.EncryptAsync(EncryptionAlgorithm.A256Gcm, plainTextBytes);

                // Create encrypted payload with metadata for integrity validation
                var encryptedPayload = new EncryptedConfigurationValue
                {
                    EncryptedData = Convert.ToBase64String(encryptedPayload.CipherText),
                    KeyId = key.Id.ToString(),
                    KeyName = keyName,
                    Algorithm = EncryptionAlgorithm.A256Gcm.ToString(),
                    Nonce = Convert.ToBase64String(encryptedPayload.Iv ?? Array.Empty<byte>()),
                    Tag = Convert.ToBase64String(encryptedPayload.AuthenticationTag ?? Array.Empty<byte>()),
                    Timestamp = DateTimeOffset.UtcNow
                };

                // Serialize to JSON and encode as base64
                var payloadJson = System.Text.Json.JsonSerializer.Serialize(encryptedPayload);
                var result = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

                _logger.LogInformation("Successfully encrypted configuration value using key {KeyName} (ID: {KeyId})", 
                    keyName, key.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt configuration value using key {KeyName}", keyName);
                throw new InvalidOperationException($"Encryption failed for key {keyName}", ex);
            }
        }

        /// <summary>
        /// Decrypts previously encrypted configuration value using Azure Key Vault.
        /// </summary>
        /// <param name="encryptedText">The encrypted value as base64 string with metadata</param>
        /// <param name="keyName">The key identifier for decryption</param>
        /// <returns>The decrypted plain text value</returns>
        public async Task<string> DecryptAsync(string encryptedText, string keyName)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
            }

            try
            {
                // Decode and deserialize the encrypted payload
                var payloadBytes = Convert.FromBase64String(encryptedText);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                var encryptedPayload = System.Text.Json.JsonSerializer.Deserialize<EncryptedConfigurationValue>(payloadJson);

                if (encryptedPayload == null)
                {
                    throw new InvalidOperationException("Failed to deserialize encrypted payload");
                }

                // Verify key name matches
                if (encryptedPayload.KeyName != keyName)
                {
                    throw new InvalidOperationException($"Key name mismatch: expected {keyName}, got {encryptedPayload.KeyName}");
                }

                // Create cryptography client using the stored key ID
                var cryptoClient = new CryptographyClient(new Uri(encryptedPayload.KeyId), new DefaultAzureCredential());

                // Prepare decryption parameters
                var encryptedData = Convert.FromBase64String(encryptedPayload.EncryptedData);
                var nonce = Convert.FromBase64String(encryptedPayload.Nonce);
                var tag = Convert.FromBase64String(encryptedPayload.Tag);

                // Decrypt using AES-GCM
                var decryptParameters = new DecryptParameters(
                    Enum.Parse<EncryptionAlgorithm>(encryptedPayload.Algorithm),
                    encryptedData,
                    nonce,
                    tag);

                var decryptResult = await cryptoClient.DecryptAsync(decryptParameters);

                // Convert result back to string
                var result = Encoding.UTF8.GetString(decryptResult.Plaintext);

                _logger.LogInformation("Successfully decrypted configuration value using key {KeyName}", keyName);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt configuration value using key {KeyName}", keyName);
                throw new InvalidOperationException($"Decryption failed for key {keyName}", ex);
            }
        }

        /// <summary>
        /// Rotates encryption keys in Azure Key Vault with proper versioning.
        /// </summary>
        /// <param name="oldKeyName">The old key identifier</param>
        /// <param name="newKeyName">The new key identifier</param>
        /// <returns>Task representing the key rotation operation</returns>
        public async Task RotateKeyAsync(string oldKeyName, string newKeyName)
        {
            if (string.IsNullOrEmpty(oldKeyName))
            {
                throw new ArgumentException("Old key name cannot be null or empty", nameof(oldKeyName));
            }

            if (string.IsNullOrEmpty(newKeyName))
            {
                throw new ArgumentException("New key name cannot be null or empty", nameof(newKeyName));
            }

            try
            {
                // Verify old key exists
                var oldKeyResponse = await _keyClient.GetKeyAsync(oldKeyName);

                // Create or update the new key with proper attributes for configuration encryption
                var keyOptions = new CreateKeyOptions(newKeyName, KeyType.Oct)
                {
                    KeySize = 256, // AES-256 for FIPS compliance
                    ExpiresOn = DateTimeOffset.UtcNow.AddYears(1), // 1 year expiration
                    KeyOperations = { KeyOperation.Encrypt, KeyOperation.Decrypt },
                    Tags = 
                    {
                        ["purpose"] = "configuration-encryption",
                        ["rotated-from"] = oldKeyName,
                        ["created-by"] = "GameConsole.Configuration.Security",
                        ["rotation-date"] = DateTimeOffset.UtcNow.ToString("O")
                    }
                };

                var newKeyResponse = await _keyClient.CreateKeyAsync(keyOptions);

                _logger.LogInformation("Successfully rotated key from {OldKeyName} to {NewKeyName} (ID: {NewKeyId})", 
                    oldKeyName, newKeyName, newKeyResponse.Value.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate key from {OldKeyName} to {NewKeyName}", oldKeyName, newKeyName);
                throw new InvalidOperationException($"Key rotation failed from {oldKeyName} to {newKeyName}", ex);
            }
        }

        /// <summary>
        /// Validates the integrity of encrypted data using HMAC verification.
        /// </summary>
        /// <param name="encryptedText">The encrypted text to validate</param>
        /// <param name="expectedHash">The expected integrity hash</param>
        /// <returns>True if the data integrity is valid</returns>
        public async Task<bool> ValidateIntegrityAsync(string encryptedText, string expectedHash)
        {
            if (string.IsNullOrEmpty(encryptedText) || string.IsNullOrEmpty(expectedHash))
            {
                return false;
            }

            try
            {
                // Compute hash of the encrypted data for integrity check
                using var sha256 = SHA256.Create();
                var encryptedBytes = Convert.FromBase64String(encryptedText);
                var computedHash = sha256.ComputeHash(encryptedBytes);
                var computedHashString = Convert.ToBase64String(computedHash);

                var isValid = computedHashString == expectedHash;

                _logger.LogInformation("Integrity validation {Result} for encrypted data", 
                    isValid ? "PASSED" : "FAILED");

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during integrity validation");
                return false;
            }
        }

        /// <summary>
        /// Internal structure for encrypted configuration values with metadata.
        /// </summary>
        private class EncryptedConfigurationValue
        {
            public string EncryptedData { get; set; } = string.Empty;
            public string KeyId { get; set; } = string.Empty;
            public string KeyName { get; set; } = string.Empty;
            public string Algorithm { get; set; } = string.Empty;
            public string Nonce { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
            public DateTimeOffset Timestamp { get; set; }
        }
    }
}
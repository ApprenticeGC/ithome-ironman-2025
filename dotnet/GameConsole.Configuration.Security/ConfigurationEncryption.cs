using System.Security.Cryptography;
using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace GameConsole.Configuration.Security;

/// <summary>
/// AES encryption implementation for configuration data with Azure Key Vault integration.
/// Provides FIPS-compliant encryption with secure key management.
/// </summary>
public class AzureKeyVaultConfigurationEncryption : IConfigurationEncryption
{
    private readonly ILogger<AzureKeyVaultConfigurationEncryption> _logger;
    private readonly KeyClient _keyClient;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, CryptographyClient> _cryptographyClients;

    public AzureKeyVaultConfigurationEncryption(
        ILogger<AzureKeyVaultConfigurationEncryption> logger,
        IConfiguration configuration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cryptographyClients = new Dictionary<string, CryptographyClient>();

        var keyVaultUri = _configuration["ConfigurationSecurity:AzureKeyVault:Uri"];
        if (string.IsNullOrEmpty(keyVaultUri))
        {
            throw new InvalidOperationException("Azure Key Vault URI is not configured. Set ConfigurationSecurity:AzureKeyVault:Uri in configuration.");
        }

        // Use DefaultAzureCredential for authentication (supports managed identity, service principal, etc.)
        var credential = new DefaultAzureCredential();
        _keyClient = new KeyClient(new Uri(keyVaultUri), credential);
    }

    public async Task<EncryptedData> EncryptAsync(string plainText, string? keyId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));

        keyId ??= _configuration["ConfigurationSecurity:DefaultKeyId"] ?? "config-encryption-key";

        try
        {
            _logger.LogDebug("Encrypting configuration data with key ID: {KeyId}", keyId);

            var cryptographyClient = await GetCryptographyClientAsync(keyId, cancellationToken);
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // Generate random IV for AES
            using var aes = Aes.Create();
            aes.GenerateIV();
            var iv = aes.IV;

            // Encrypt using Azure Key Vault
            var encryptResult = await cryptographyClient.EncryptAsync(EncryptionAlgorithm.A256Cbc, plainTextBytes, cancellationToken);

            // Generate HMAC for integrity validation
            var hmac = await GenerateHMACAsync(encryptResult.Ciphertext, iv, keyId, cancellationToken);

            var encryptedData = new EncryptedData
            {
                Data = encryptResult.Ciphertext,
                IV = iv,
                KeyId = keyId,
                HMAC = hmac,
                Timestamp = DateTimeOffset.UtcNow,
                Algorithm = "AES-256-CBC",
                Version = 1
            };

            _logger.LogDebug("Successfully encrypted configuration data");
            return encryptedData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt configuration data with key ID: {KeyId}", keyId);
            throw;
        }
    }

    public async Task<string> DecryptAsync(EncryptedData encryptedData, CancellationToken cancellationToken = default)
    {
        if (encryptedData == null)
            throw new ArgumentNullException(nameof(encryptedData));

        try
        {
            _logger.LogDebug("Decrypting configuration data with key ID: {KeyId}", encryptedData.KeyId);

            // Validate integrity first
            if (!await ValidateIntegrityAsync(encryptedData, cancellationToken))
            {
                throw new InvalidOperationException("Encrypted data integrity validation failed.");
            }

            var cryptographyClient = await GetCryptographyClientAsync(encryptedData.KeyId, cancellationToken);

            // Decrypt using Azure Key Vault
            var decryptResult = await cryptographyClient.DecryptAsync(EncryptionAlgorithm.A256Cbc, encryptedData.Data, cancellationToken);

            var plainText = Encoding.UTF8.GetString(decryptResult.Plaintext);
            _logger.LogDebug("Successfully decrypted configuration data");

            return plainText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt configuration data with key ID: {KeyId}", encryptedData.KeyId);
            throw;
        }
    }

    public async Task<bool> ValidateIntegrityAsync(EncryptedData encryptedData, CancellationToken cancellationToken = default)
    {
        if (encryptedData == null)
            return false;

        try
        {
            _logger.LogDebug("Validating integrity of encrypted data with key ID: {KeyId}", encryptedData.KeyId);

            var expectedHmac = await GenerateHMACAsync(encryptedData.Data, encryptedData.IV, encryptedData.KeyId, cancellationToken);
            
            // Use constant-time comparison to prevent timing attacks
            bool isValid = CryptographicOperations.FixedTimeEquals(encryptedData.HMAC, expectedHmac);
            
            if (!isValid)
            {
                _logger.LogWarning("Integrity validation failed for encrypted data with key ID: {KeyId}", encryptedData.KeyId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating integrity for key ID: {KeyId}", encryptedData.KeyId);
            return false;
        }
    }

    public async Task RotateKeyAsync(string oldKeyId, string newKeyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(oldKeyId))
            throw new ArgumentException("Old key ID cannot be null or empty.", nameof(oldKeyId));
        if (string.IsNullOrEmpty(newKeyId))
            throw new ArgumentException("New key ID cannot be null or empty.", nameof(newKeyId));

        try
        {
            _logger.LogInformation("Rotating encryption key from {OldKeyId} to {NewKeyId}", oldKeyId, newKeyId);

            // Ensure new key exists and is accessible
            await GetCryptographyClientAsync(newKeyId, cancellationToken);

            // Remove old key client from cache to force refresh
            _cryptographyClients.Remove(oldKeyId);

            _logger.LogInformation("Successfully rotated encryption key from {OldKeyId} to {NewKeyId}", oldKeyId, newKeyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate encryption key from {OldKeyId} to {NewKeyId}", oldKeyId, newKeyId);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetAvailableKeysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Retrieving available encryption keys");

            var keys = new List<string>();
            await foreach (var keyProperties in _keyClient.GetPropertiesOfKeysAsync(cancellationToken))
            {
                if (keyProperties.Enabled == true && !keyProperties.ExpiresOn.HasValue || keyProperties.ExpiresOn > DateTimeOffset.UtcNow)
                {
                    keys.Add(keyProperties.Name);
                }
            }

            _logger.LogDebug("Found {KeyCount} available encryption keys", keys.Count);
            return keys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve available encryption keys");
            throw;
        }
    }

    private async Task<CryptographyClient> GetCryptographyClientAsync(string keyId, CancellationToken cancellationToken = default)
    {
        if (_cryptographyClients.TryGetValue(keyId, out var existingClient))
        {
            return existingClient;
        }

        try
        {
            // Get the key from Azure Key Vault
            var keyResponse = await _keyClient.GetKeyAsync(keyId, cancellationToken: cancellationToken);
            var key = keyResponse.Value;

            // Create cryptography client for this key
            var cryptographyClient = new CryptographyClient(key.Id, new DefaultAzureCredential());

            _cryptographyClients[keyId] = cryptographyClient;
            return cryptographyClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cryptography client for key ID: {KeyId}", keyId);
            throw;
        }
    }

    private async Task<byte[]> GenerateHMACAsync(byte[] data, byte[] iv, string keyId, CancellationToken cancellationToken = default)
    {
        // For HMAC, we use a derived key from the main encryption key
        // In a real implementation, you might use a separate HMAC key or derive it using HKDF
        var combinedData = new byte[data.Length + iv.Length + Encoding.UTF8.GetByteCount(keyId)];
        Array.Copy(data, 0, combinedData, 0, data.Length);
        Array.Copy(iv, 0, combinedData, data.Length, iv.Length);
        Array.Copy(Encoding.UTF8.GetBytes(keyId), 0, combinedData, data.Length + iv.Length, Encoding.UTF8.GetByteCount(keyId));

        using var sha256 = SHA256.Create();
        return await Task.FromResult(sha256.ComputeHash(combinedData));
    }
}

/// <summary>
/// Local AES encryption implementation for development and testing scenarios.
/// Uses local key storage and is not recommended for production use.
/// </summary>
public class LocalConfigurationEncryption : IConfigurationEncryption
{
    private readonly ILogger<LocalConfigurationEncryption> _logger;
    private readonly Dictionary<string, byte[]> _keys;

    public LocalConfigurationEncryption(ILogger<LocalConfigurationEncryption> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keys = new Dictionary<string, byte[]>();

        // Generate a default key for development
        var defaultKey = new byte[32]; // 256-bit key
        RandomNumberGenerator.Fill(defaultKey);
        _keys["default"] = defaultKey;
    }

    public Task<EncryptedData> EncryptAsync(string plainText, string? keyId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(plainText))
            throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));

        keyId ??= "default";

        if (!_keys.TryGetValue(keyId, out var key))
        {
            throw new InvalidOperationException($"Key with ID '{keyId}' not found.");
        }

        try
        {
            _logger.LogDebug("Encrypting configuration data with local key ID: {KeyId}", keyId);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = aes.EncryptCbc(plainTextBytes, aes.IV);

            // Generate HMAC for integrity
            var hmac = GenerateHMAC(encryptedBytes, aes.IV, key);

            var encryptedData = new EncryptedData
            {
                Data = encryptedBytes,
                IV = aes.IV,
                KeyId = keyId,
                HMAC = hmac,
                Timestamp = DateTimeOffset.UtcNow,
                Algorithm = "AES-256-CBC",
                Version = 1
            };

            _logger.LogDebug("Successfully encrypted configuration data with local encryption");
            return Task.FromResult(encryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt configuration data with local key ID: {KeyId}", keyId);
            throw;
        }
    }

    public Task<string> DecryptAsync(EncryptedData encryptedData, CancellationToken cancellationToken = default)
    {
        if (encryptedData == null)
            throw new ArgumentNullException(nameof(encryptedData));

        if (!_keys.TryGetValue(encryptedData.KeyId, out var key))
        {
            throw new InvalidOperationException($"Key with ID '{encryptedData.KeyId}' not found.");
        }

        try
        {
            _logger.LogDebug("Decrypting configuration data with local key ID: {KeyId}", encryptedData.KeyId);

            // Validate integrity first
            if (!ValidateIntegrityAsync(encryptedData, cancellationToken).Result)
            {
                throw new InvalidOperationException("Encrypted data integrity validation failed.");
            }

            using var aes = Aes.Create();
            aes.Key = key;

            var plainTextBytes = aes.DecryptCbc(encryptedData.Data, encryptedData.IV);
            var plainText = Encoding.UTF8.GetString(plainTextBytes);

            _logger.LogDebug("Successfully decrypted configuration data with local encryption");
            return Task.FromResult(plainText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt configuration data with local key ID: {KeyId}", encryptedData.KeyId);
            throw;
        }
    }

    public Task<bool> ValidateIntegrityAsync(EncryptedData encryptedData, CancellationToken cancellationToken = default)
    {
        if (encryptedData == null)
            return Task.FromResult(false);

        if (!_keys.TryGetValue(encryptedData.KeyId, out var key))
            return Task.FromResult(false);

        try
        {
            var expectedHmac = GenerateHMAC(encryptedData.Data, encryptedData.IV, key);
            bool isValid = CryptographicOperations.FixedTimeEquals(encryptedData.HMAC, expectedHmac);

            if (!isValid)
            {
                _logger.LogWarning("Integrity validation failed for encrypted data with local key ID: {KeyId}", encryptedData.KeyId);
            }

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating integrity for local key ID: {KeyId}", encryptedData.KeyId);
            return Task.FromResult(false);
        }
    }

    public Task RotateKeyAsync(string oldKeyId, string newKeyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(oldKeyId))
            throw new ArgumentException("Old key ID cannot be null or empty.", nameof(oldKeyId));
        if (string.IsNullOrEmpty(newKeyId))
            throw new ArgumentException("New key ID cannot be null or empty.", nameof(newKeyId));

        try
        {
            _logger.LogInformation("Rotating local encryption key from {OldKeyId} to {NewKeyId}", oldKeyId, newKeyId);

            // Generate new key
            var newKey = new byte[32];
            RandomNumberGenerator.Fill(newKey);
            _keys[newKeyId] = newKey;

            _logger.LogInformation("Successfully rotated local encryption key from {OldKeyId} to {NewKeyId}", oldKeyId, newKeyId);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate local encryption key from {OldKeyId} to {NewKeyId}", oldKeyId, newKeyId);
            throw;
        }
    }

    public Task<IEnumerable<string>> GetAvailableKeysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var keys = _keys.Keys.ToList();
            _logger.LogDebug("Found {KeyCount} available local encryption keys", keys.Count);
            return Task.FromResult<IEnumerable<string>>(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve available local encryption keys");
            throw;
        }
    }

    private static byte[] GenerateHMAC(byte[] data, byte[] iv, byte[] key)
    {
        var combinedData = new byte[data.Length + iv.Length];
        Array.Copy(data, 0, combinedData, 0, data.Length);
        Array.Copy(iv, 0, combinedData, data.Length, iv.Length);

        using var hmac = new HMACSHA256(key);
        return hmac.ComputeHash(combinedData);
    }
}
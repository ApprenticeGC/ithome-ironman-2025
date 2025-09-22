using System.Security.Cryptography;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Interface for encrypting and decrypting sensitive configuration data.
/// Provides AES encryption with support for external key management services.
/// </summary>
public interface IConfigurationEncryption
{
    /// <summary>
    /// Encrypts sensitive configuration data using AES encryption.
    /// </summary>
    /// <param name="plainText">The configuration data to encrypt.</param>
    /// <param name="keyId">Optional key identifier for key rotation support.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Encrypted configuration data with integrity validation.</returns>
    Task<EncryptedData> EncryptAsync(string plainText, string? keyId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts encrypted configuration data.
    /// </summary>
    /// <param name="encryptedData">The encrypted configuration data to decrypt.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The decrypted configuration data.</returns>
    Task<string> DecryptAsync(EncryptedData encryptedData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the integrity of encrypted configuration data.
    /// </summary>
    /// <param name="encryptedData">The encrypted data to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the data integrity is valid, false otherwise.</returns>
    Task<bool> ValidateIntegrityAsync(EncryptedData encryptedData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates encryption keys for enhanced security.
    /// </summary>
    /// <param name="oldKeyId">The old key identifier to replace.</param>
    /// <param name="newKeyId">The new key identifier to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the key rotation operation.</returns>
    Task RotateKeyAsync(string oldKeyId, string newKeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available encryption key identifiers.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of available key identifiers.</returns>
    Task<IEnumerable<string>> GetAvailableKeysAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents encrypted configuration data with integrity validation.
/// </summary>
public record EncryptedData
{
    /// <summary>
    /// The encrypted data bytes.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// The initialization vector used for encryption.
    /// </summary>
    public required byte[] IV { get; init; }

    /// <summary>
    /// The key identifier used for encryption.
    /// </summary>
    public required string KeyId { get; init; }

    /// <summary>
    /// HMAC for integrity validation.
    /// </summary>
    public required byte[] HMAC { get; init; }

    /// <summary>
    /// Timestamp when the data was encrypted.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Algorithm used for encryption.
    /// </summary>
    public required string Algorithm { get; init; } = "AES-256-CBC";

    /// <summary>
    /// Version of the encryption format.
    /// </summary>
    public required int Version { get; init; } = 1;
}
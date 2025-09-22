using System;
using System.Threading.Tasks;

namespace GameConsole.Configuration.Security
{
    /// <summary>
    /// Provides encryption and decryption services for sensitive configuration data.
    /// Implements Tier 1 contracts following the 4-tier architecture.
    /// </summary>
    public interface IConfigurationEncryption
    {
        /// <summary>
        /// Encrypts sensitive configuration value using AES encryption.
        /// </summary>
        /// <param name="plainText">The plain text value to encrypt</param>
        /// <param name="keyName">The key identifier for encryption</param>
        /// <returns>The encrypted value as base64 string</returns>
        Task<string> EncryptAsync(string plainText, string keyName);

        /// <summary>
        /// Decrypts previously encrypted configuration value.
        /// </summary>
        /// <param name="encryptedText">The encrypted value as base64 string</param>
        /// <param name="keyName">The key identifier for decryption</param>
        /// <returns>The decrypted plain text value</returns>
        Task<string> DecryptAsync(string encryptedText, string keyName);

        /// <summary>
        /// Rotates encryption keys for enhanced security.
        /// </summary>
        /// <param name="oldKeyName">The old key identifier</param>
        /// <param name="newKeyName">The new key identifier</param>
        /// <returns>Task representing the key rotation operation</returns>
        Task RotateKeyAsync(string oldKeyName, string newKeyName);

        /// <summary>
        /// Validates the integrity of encrypted data.
        /// </summary>
        /// <param name="encryptedText">The encrypted text to validate</param>
        /// <param name="expectedHash">The expected integrity hash</param>
        /// <returns>True if the data integrity is valid</returns>
        Task<bool> ValidateIntegrityAsync(string encryptedText, string expectedHash);
    }
}
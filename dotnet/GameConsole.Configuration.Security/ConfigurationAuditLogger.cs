using System;
using System.Security.Principal;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameConsole.Configuration.Security
{
    /// <summary>
    /// Audit logging for configuration changes and access events.
    /// Implements comprehensive tracking following security best practices.
    /// </summary>
    public class ConfigurationAuditLogger
    {
        private readonly ILogger<ConfigurationAuditLogger> _logger;

        /// <summary>
        /// Defines audit event types for configuration operations.
        /// </summary>
        public static class EventTypes
        {
            public const string ConfigurationRead = "config.read";
            public const string ConfigurationWrite = "config.write";
            public const string ConfigurationEncrypt = "config.encrypt";
            public const string ConfigurationDecrypt = "config.decrypt";
            public const string KeyRotation = "key.rotation";
            public const string AccessDenied = "access.denied";
            public const string IntegrityValidation = "integrity.validation";
            public const string RoleAssignment = "role.assignment";
        }

        public ConfigurationAuditLogger(ILogger<ConfigurationAuditLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Logs a configuration access event with full audit details.
        /// </summary>
        /// <param name="eventType">Type of configuration event</param>
        /// <param name="user">The user performing the operation</param>
        /// <param name="configurationKey">The configuration key being accessed</param>
        /// <param name="result">Success or failure result</param>
        /// <param name="additionalData">Additional context data</param>
        /// <returns>Task representing the logging operation</returns>
        public Task LogConfigurationEventAsync(
            string eventType,
            IIdentity? user,
            string configurationKey,
            bool result,
            object? additionalData = null)
        {
            var auditEvent = new ConfigurationAuditEvent
            {
                EventId = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                EventType = eventType,
                UserName = user?.Name ?? "anonymous",
                ConfigurationKey = configurationKey,
                Result = result ? "SUCCESS" : "FAILURE",
                AdditionalData = additionalData
            };

            var serializedEvent = JsonSerializer.Serialize(auditEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            if (result)
            {
                _logger.LogInformation("Configuration audit event: {AuditEvent}", serializedEvent);
            }
            else
            {
                _logger.LogWarning("Configuration security event (failed): {AuditEvent}", serializedEvent);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs encryption/decryption operations with key management details.
        /// </summary>
        /// <param name="operation">Encrypt or decrypt operation</param>
        /// <param name="user">The user performing the operation</param>
        /// <param name="keyName">The encryption key identifier</param>
        /// <param name="dataSize">Size of data being processed</param>
        /// <param name="success">Operation success status</param>
        /// <returns>Task representing the logging operation</returns>
        public Task LogEncryptionEventAsync(
            string operation,
            IIdentity? user,
            string keyName,
            int dataSize,
            bool success)
        {
            var encryptionData = new
            {
                Operation = operation,
                KeyName = keyName,
                DataSize = dataSize,
                Timestamp = DateTimeOffset.UtcNow
            };

            return LogConfigurationEventAsync(
                operation == "encrypt" ? EventTypes.ConfigurationEncrypt : EventTypes.ConfigurationDecrypt,
                user,
                $"key:{keyName}",
                success,
                encryptionData);
        }

        /// <summary>
        /// Logs key rotation events with security implications.
        /// </summary>
        /// <param name="user">The user performing key rotation</param>
        /// <param name="oldKeyName">The old key identifier</param>
        /// <param name="newKeyName">The new key identifier</param>
        /// <param name="success">Rotation success status</param>
        /// <returns>Task representing the logging operation</returns>
        public Task LogKeyRotationAsync(
            IIdentity? user,
            string oldKeyName,
            string newKeyName,
            bool success)
        {
            var rotationData = new
            {
                OldKeyName = oldKeyName,
                NewKeyName = newKeyName,
                RotationTimestamp = DateTimeOffset.UtcNow
            };

            return LogConfigurationEventAsync(
                EventTypes.KeyRotation,
                user,
                $"rotation:{oldKeyName}->{newKeyName}",
                success,
                rotationData);
        }

        /// <summary>
        /// Logs access control events including role assignments and permissions.
        /// </summary>
        /// <param name="user">The user being granted or denied access</param>
        /// <param name="permission">The permission being requested</param>
        /// <param name="granted">Whether access was granted</param>
        /// <param name="reason">Reason for access decision</param>
        /// <returns>Task representing the logging operation</returns>
        public Task LogAccessControlEventAsync(
            IIdentity? user,
            string permission,
            bool granted,
            string reason)
        {
            var accessData = new
            {
                Permission = permission,
                Granted = granted,
                Reason = reason,
                CheckTimestamp = DateTimeOffset.UtcNow
            };

            return LogConfigurationEventAsync(
                granted ? "access.granted" : EventTypes.AccessDenied,
                user,
                $"permission:{permission}",
                granted,
                accessData);
        }

        /// <summary>
        /// Logs integrity validation events for configuration data.
        /// </summary>
        /// <param name="configurationKey">The configuration key being validated</param>
        /// <param name="integrityValid">Whether integrity check passed</param>
        /// <param name="expectedHash">The expected integrity hash</param>
        /// <param name="actualHash">The actual computed hash</param>
        /// <returns>Task representing the logging operation</returns>
        public Task LogIntegrityValidationAsync(
            string configurationKey,
            bool integrityValid,
            string expectedHash,
            string actualHash)
        {
            var integrityData = new
            {
                ExpectedHash = expectedHash,
                ActualHash = actualHash,
                ValidationTimestamp = DateTimeOffset.UtcNow
            };

            return LogConfigurationEventAsync(
                EventTypes.IntegrityValidation,
                null,
                configurationKey,
                integrityValid,
                integrityData);
        }

        /// <summary>
        /// Internal audit event structure for consistent logging format.
        /// </summary>
        private class ConfigurationAuditEvent
        {
            public Guid EventId { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public string EventType { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string ConfigurationKey { get; set; } = string.Empty;
            public string Result { get; set; } = string.Empty;
            public object? AdditionalData { get; set; }
        }
    }
}
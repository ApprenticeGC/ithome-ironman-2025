using GameConsole.Core.Abstractions;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Interface for comprehensive audit logging of configuration changes.
/// Provides detailed tracking and compliance reporting capabilities.
/// </summary>
public interface IConfigurationAuditLogger : IService
{
    /// <summary>
    /// Logs a configuration read operation.
    /// </summary>
    /// <param name="audit">The audit information for the read operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the logging operation.</returns>
    Task LogReadAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a configuration write/update operation.
    /// </summary>
    /// <param name="audit">The audit information for the write operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the logging operation.</returns>
    Task LogWriteAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a configuration delete operation.
    /// </summary>
    /// <param name="audit">The audit information for the delete operation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the logging operation.</returns>
    Task LogDeleteAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an access control change operation.
    /// </summary>
    /// <param name="audit">The audit information for the access control change.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the logging operation.</returns>
    Task LogAccessControlChangeAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a key rotation operation.
    /// </summary>
    /// <param name="audit">The audit information for the key rotation.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the logging operation.</returns>
    Task LogKeyRotationAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs an authentication/authorization event.
    /// </summary>
    /// <param name="audit">The audit information for the authentication event.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the logging operation.</returns>
    Task LogAuthenticationAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit entries for a specific configuration path within a date range.
    /// </summary>
    /// <param name="configurationPath">The configuration path to retrieve audit entries for.</param>
    /// <param name="startDate">The start date for the audit query.</param>
    /// <param name="endDate">The end date for the audit query.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of audit entries matching the criteria.</returns>
    Task<IEnumerable<ConfigurationAuditEntry>> GetAuditEntriesAsync(string configurationPath, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit entries for a specific user within a date range.
    /// </summary>
    /// <param name="userId">The user identifier to retrieve audit entries for.</param>
    /// <param name="startDate">The start date for the audit query.</param>
    /// <param name="endDate">The end date for the audit query.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of audit entries matching the criteria.</returns>
    Task<IEnumerable<ConfigurationAuditEntry>> GetUserAuditEntriesAsync(string userId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit entries by operation type within a date range.
    /// </summary>
    /// <param name="operationType">The operation type to filter by.</param>
    /// <param name="startDate">The start date for the audit query.</param>
    /// <param name="endDate">The end date for the audit query.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of audit entries matching the criteria.</returns>
    Task<IEnumerable<ConfigurationAuditEntry>> GetAuditEntriesByOperationAsync(ConfigurationOperationType operationType, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a compliance report for audit entries within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date for the compliance report.</param>
    /// <param name="endDate">The end date for the compliance report.</param>
    /// <param name="reportFormat">The format for the compliance report.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The compliance report data.</returns>
    Task<ComplianceReport> GenerateComplianceReportAsync(DateTimeOffset startDate, DateTimeOffset endDate, ReportFormat reportFormat = ReportFormat.Json, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a configuration audit entry.
/// </summary>
public record ConfigurationAuditEntry
{
    /// <summary>
    /// Unique identifier for the audit entry.
    /// </summary>
    public required string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the operation occurred.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// The type of operation performed.
    /// </summary>
    public required ConfigurationOperationType OperationType { get; init; }

    /// <summary>
    /// The configuration path that was accessed.
    /// </summary>
    public required string ConfigurationPath { get; init; }

    /// <summary>
    /// The user who performed the operation.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// The user's role at the time of the operation.
    /// </summary>
    public ConfigurationRole? UserRole { get; init; }

    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The IP address from which the operation was performed.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// The user agent or client information.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// Session identifier for the user session.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// Previous value (for update operations, may be hashed for sensitive data).
    /// </summary>
    public string? PreviousValue { get; init; }

    /// <summary>
    /// New value (for update operations, may be hashed for sensitive data).
    /// </summary>
    public string? NewValue { get; init; }

    /// <summary>
    /// Additional metadata related to the operation.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Risk level associated with the operation.
    /// </summary>
    public AuditRiskLevel RiskLevel { get; init; } = AuditRiskLevel.Low;
}

/// <summary>
/// Types of configuration operations that can be audited.
/// </summary>
public enum ConfigurationOperationType
{
    /// <summary>Read operation.</summary>
    Read,
    /// <summary>Write/Create operation.</summary>
    Write,
    /// <summary>Update operation.</summary>
    Update,
    /// <summary>Delete operation.</summary>
    Delete,
    /// <summary>Access control change.</summary>
    AccessControlChange,
    /// <summary>Key rotation operation.</summary>
    KeyRotation,
    /// <summary>Authentication event.</summary>
    Authentication,
    /// <summary>Authorization event.</summary>
    Authorization,
    /// <summary>System event.</summary>
    System
}

/// <summary>
/// Risk levels for audit entries.
/// </summary>
public enum AuditRiskLevel
{
    /// <summary>Low risk operation.</summary>
    Low,
    /// <summary>Medium risk operation.</summary>
    Medium,
    /// <summary>High risk operation.</summary>
    High,
    /// <summary>Critical risk operation.</summary>
    Critical
}

/// <summary>
/// Compliance report data structure.
/// </summary>
public record ComplianceReport
{
    /// <summary>
    /// The start date of the report period.
    /// </summary>
    public required DateTimeOffset StartDate { get; init; }

    /// <summary>
    /// The end date of the report period.
    /// </summary>
    public required DateTimeOffset EndDate { get; init; }

    /// <summary>
    /// Total number of audit entries in the period.
    /// </summary>
    public required int TotalEntries { get; init; }

    /// <summary>
    /// Number of successful operations.
    /// </summary>
    public required int SuccessfulOperations { get; init; }

    /// <summary>
    /// Number of failed operations.
    /// </summary>
    public required int FailedOperations { get; init; }

    /// <summary>
    /// Breakdown of operations by type.
    /// </summary>
    public required Dictionary<ConfigurationOperationType, int> OperationBreakdown { get; init; }

    /// <summary>
    /// Breakdown of operations by risk level.
    /// </summary>
    public required Dictionary<AuditRiskLevel, int> RiskLevelBreakdown { get; init; }

    /// <summary>
    /// Top users by number of operations.
    /// </summary>
    public required Dictionary<string, int> TopUsers { get; init; }

    /// <summary>
    /// Most frequently accessed configuration paths.
    /// </summary>
    public required Dictionary<string, int> TopConfigurationPaths { get; init; }

    /// <summary>
    /// Report generation timestamp.
    /// </summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// Report format.
    /// </summary>
    public required ReportFormat Format { get; init; }
}

/// <summary>
/// Available report formats.
/// </summary>
public enum ReportFormat
{
    /// <summary>JSON format.</summary>
    Json,
    /// <summary>XML format.</summary>
    Xml,
    /// <summary>CSV format.</summary>
    Csv,
    /// <summary>HTML format.</summary>
    Html
}
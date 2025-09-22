using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;

namespace GameConsole.Configuration.Security;

/// <summary>
/// Implementation of comprehensive audit logging for configuration changes.
/// Provides structured logging with compliance reporting capabilities.
/// </summary>
public class ConfigurationAuditLogger : IConfigurationAuditLogger
{
    private readonly ILogger<ConfigurationAuditLogger> _logger;
    private readonly ConcurrentQueue<ConfigurationAuditEntry> _auditEntries;
    private readonly SemaphoreSlim _auditSemaphore;
    private bool _isRunning;

    public bool IsRunning => _isRunning;

    public ConfigurationAuditLogger(ILogger<ConfigurationAuditLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditEntries = new ConcurrentQueue<ConfigurationAuditEntry>();
        _auditSemaphore = new SemaphoreSlim(1, 1);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing Configuration Audit Logger");
        _logger.LogInformation("Configuration Audit Logger initialized");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Configuration Audit Logger");
        _isRunning = true;
        _logger.LogInformation("Configuration Audit Logger started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Configuration Audit Logger");
        _isRunning = false;
        _logger.LogInformation("Configuration Audit Logger stopped");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _isRunning = false;
        _auditSemaphore.Dispose();
        return ValueTask.CompletedTask;
    }

    public async Task LogReadAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default)
    {
        if (audit == null)
            throw new ArgumentNullException(nameof(audit));

        audit = audit with { OperationType = ConfigurationOperationType.Read };
        await LogAuditEntryAsync(audit, cancellationToken);
    }

    public async Task LogWriteAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default)
    {
        if (audit == null)
            throw new ArgumentNullException(nameof(audit));

        audit = audit with { OperationType = ConfigurationOperationType.Write };
        await LogAuditEntryAsync(audit, cancellationToken);
    }

    public async Task LogDeleteAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default)
    {
        if (audit == null)
            throw new ArgumentNullException(nameof(audit));

        audit = audit with { OperationType = ConfigurationOperationType.Delete };
        await LogAuditEntryAsync(audit, cancellationToken);
    }

    public async Task LogAccessControlChangeAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default)
    {
        if (audit == null)
            throw new ArgumentNullException(nameof(audit));

        audit = audit with { OperationType = ConfigurationOperationType.AccessControlChange };
        await LogAuditEntryAsync(audit, cancellationToken);
    }

    public async Task LogKeyRotationAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default)
    {
        if (audit == null)
            throw new ArgumentNullException(nameof(audit));

        audit = audit with { OperationType = ConfigurationOperationType.KeyRotation };
        await LogAuditEntryAsync(audit, cancellationToken);
    }

    public async Task LogAuthenticationAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken = default)
    {
        if (audit == null)
            throw new ArgumentNullException(nameof(audit));

        audit = audit with { OperationType = ConfigurationOperationType.Authentication };
        await LogAuditEntryAsync(audit, cancellationToken);
    }

    public Task<IEnumerable<ConfigurationAuditEntry>> GetAuditEntriesAsync(string configurationPath, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(configurationPath))
            throw new ArgumentException("Configuration path cannot be null or empty.", nameof(configurationPath));

        try
        {
            var entries = _auditEntries
                .Where(e => e.ConfigurationPath.Equals(configurationPath, StringComparison.OrdinalIgnoreCase) &&
                           e.Timestamp >= startDate &&
                           e.Timestamp <= endDate)
                .OrderByDescending(e => e.Timestamp)
                .ToList();

            _logger.LogDebug("Retrieved {Count} audit entries for path {Path} between {StartDate} and {EndDate}",
                entries.Count, configurationPath, startDate, endDate);

            return Task.FromResult<IEnumerable<ConfigurationAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit entries for path {Path}", configurationPath);
            throw;
        }
    }

    public Task<IEnumerable<ConfigurationAuditEntry>> GetUserAuditEntriesAsync(string userId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        try
        {
            var entries = _auditEntries
                .Where(e => e.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) &&
                           e.Timestamp >= startDate &&
                           e.Timestamp <= endDate)
                .OrderByDescending(e => e.Timestamp)
                .ToList();

            _logger.LogDebug("Retrieved {Count} audit entries for user {UserId} between {StartDate} and {EndDate}",
                entries.Count, userId, startDate, endDate);

            return Task.FromResult<IEnumerable<ConfigurationAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit entries for user {UserId}", userId);
            throw;
        }
    }

    public Task<IEnumerable<ConfigurationAuditEntry>> GetAuditEntriesByOperationAsync(ConfigurationOperationType operationType, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = _auditEntries
                .Where(e => e.OperationType == operationType &&
                           e.Timestamp >= startDate &&
                           e.Timestamp <= endDate)
                .OrderByDescending(e => e.Timestamp)
                .ToList();

            _logger.LogDebug("Retrieved {Count} audit entries for operation {OperationType} between {StartDate} and {EndDate}",
                entries.Count, operationType, startDate, endDate);

            return Task.FromResult<IEnumerable<ConfigurationAuditEntry>>(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit entries for operation {OperationType}", operationType);
            throw;
        }
    }

    public Task<ComplianceReport> GenerateComplianceReportAsync(DateTimeOffset startDate, DateTimeOffset endDate, ReportFormat reportFormat = ReportFormat.Json, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating compliance report from {StartDate} to {EndDate} in format {Format}",
                startDate, endDate, reportFormat);

            var entriesInRange = _auditEntries
                .Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate)
                .ToList();

            var totalEntries = entriesInRange.Count;
            var successfulOperations = entriesInRange.Count(e => e.Success);
            var failedOperations = totalEntries - successfulOperations;

            var operationBreakdown = entriesInRange
                .GroupBy(e => e.OperationType)
                .ToDictionary(g => g.Key, g => g.Count());

            var riskLevelBreakdown = entriesInRange
                .GroupBy(e => e.RiskLevel)
                .ToDictionary(g => g.Key, g => g.Count());

            var topUsers = entriesInRange
                .GroupBy(e => e.UserId)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            var topConfigurationPaths = entriesInRange
                .GroupBy(e => e.ConfigurationPath)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            var report = new ComplianceReport
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalEntries = totalEntries,
                SuccessfulOperations = successfulOperations,
                FailedOperations = failedOperations,
                OperationBreakdown = operationBreakdown,
                RiskLevelBreakdown = riskLevelBreakdown,
                TopUsers = topUsers,
                TopConfigurationPaths = topConfigurationPaths,
                GeneratedAt = DateTimeOffset.UtcNow,
                Format = reportFormat
            };

            _logger.LogInformation("Generated compliance report with {TotalEntries} entries ({SuccessfulOperations} successful, {FailedOperations} failed)",
                totalEntries, successfulOperations, failedOperations);

            return Task.FromResult(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report from {StartDate} to {EndDate}", startDate, endDate);
            throw;
        }
    }

    private async Task LogAuditEntryAsync(ConfigurationAuditEntry audit, CancellationToken cancellationToken)
    {
        await _auditSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Ensure timestamp is set
            if (audit.Timestamp == default)
            {
                audit = audit with { Timestamp = DateTimeOffset.UtcNow };
            }

            // Determine risk level if not explicitly set
            if (audit.RiskLevel == AuditRiskLevel.Low)
            {
                audit = audit with { RiskLevel = DetermineRiskLevel(audit) };
            }

            // Add to in-memory store
            _auditEntries.Enqueue(audit);

            // Log structured audit entry
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["AuditId"] = audit.Id,
                ["OperationType"] = audit.OperationType,
                ["ConfigurationPath"] = audit.ConfigurationPath,
                ["UserId"] = audit.UserId,
                ["Success"] = audit.Success,
                ["RiskLevel"] = audit.RiskLevel
            });

            if (audit.Success)
            {
                _logger.LogInformation("Configuration operation {OperationType} on {Path} by user {UserId} - SUCCESS",
                    audit.OperationType, audit.ConfigurationPath, audit.UserId);
            }
            else
            {
                _logger.LogWarning("Configuration operation {OperationType} on {Path} by user {UserId} - FAILED: {ErrorMessage}",
                    audit.OperationType, audit.ConfigurationPath, audit.UserId, audit.ErrorMessage);
            }

            // For high-risk operations, also log as structured JSON
            if (audit.RiskLevel >= AuditRiskLevel.High)
            {
                var auditJson = JsonSerializer.Serialize(audit, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogWarning("HIGH-RISK Configuration Audit: {AuditEntry}", auditJson);
            }

            // Clean up old entries to prevent memory growth (keep last 10,000 entries)
            while (_auditEntries.Count > 10000)
            {
                _auditEntries.TryDequeue(out _);
            }
        }
        finally
        {
            _auditSemaphore.Release();
        }
    }

    private static AuditRiskLevel DetermineRiskLevel(ConfigurationAuditEntry audit)
    {
        // Determine risk level based on operation type and configuration path
        var riskLevel = AuditRiskLevel.Low;

        // Higher risk for write/delete operations
        if (audit.OperationType == ConfigurationOperationType.Delete ||
            audit.OperationType == ConfigurationOperationType.AccessControlChange ||
            audit.OperationType == ConfigurationOperationType.KeyRotation)
        {
            riskLevel = AuditRiskLevel.Medium;
        }

        // Higher risk for sensitive configuration paths
        var lowerPath = audit.ConfigurationPath.ToLowerInvariant();
        if (lowerPath.Contains("password") || lowerPath.Contains("secret") || 
            lowerPath.Contains("key") || lowerPath.Contains("token") ||
            lowerPath.Contains("connectionstring") || lowerPath.Contains("credential"))
        {
            riskLevel = riskLevel == AuditRiskLevel.Low ? AuditRiskLevel.Medium : AuditRiskLevel.High;
        }

        // Failed operations are higher risk
        if (!audit.Success)
        {
            riskLevel = riskLevel == AuditRiskLevel.Low ? AuditRiskLevel.Medium : AuditRiskLevel.High;
        }

        // System-level operations are critical
        if (audit.OperationType == ConfigurationOperationType.System)
        {
            riskLevel = AuditRiskLevel.Critical;
        }

        return riskLevel;
    }
}
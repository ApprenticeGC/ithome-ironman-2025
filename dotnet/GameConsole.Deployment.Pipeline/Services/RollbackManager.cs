using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Pipeline.Services;

/// <summary>
/// Manages rollback operations for deployment recovery.
/// Handles automatic and manual rollback scenarios with versioning support.
/// </summary>
public class RollbackManager : IRollbackManager
{
    private readonly Dictionary<string, DeploymentVersion> _versionHistory = new();
    private readonly Dictionary<string, RollbackStatus> _rollbackStatuses = new();
    private bool _isRunning;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Event raised when a rollback operation status changes.
    /// </summary>
    public event EventHandler<RollbackStatusChangedEventArgs>? StatusChanged;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Initialize rollback manager
        LoadVersionHistory();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<RollbackResult> RollbackAsync(string deploymentId, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var rollbackId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        try
        {
            // Update rollback status
            UpdateRollbackStatus(rollbackId, RollbackStatus.Pending, RollbackStatus.InProgress);

            // Find the previous stable version
            var rollbackOptions = await GetRollbackOptionsAsync("production", cancellationToken);
            var previousVersion = rollbackOptions.FirstOrDefault(v => v.IsRollbackEligible && !v.IsActive);

            if (previousVersion == null)
            {
                var failedResult = new RollbackResult
                {
                    RollbackId = rollbackId,
                    DeploymentId = deploymentId,
                    Success = false,
                    Status = RollbackStatus.Failed,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow,
                    ErrorMessage = "No suitable version found for rollback",
                    Reason = reason
                };

                UpdateRollbackStatus(rollbackId, RollbackStatus.InProgress, RollbackStatus.Failed);
                return failedResult;
            }

            // Perform the actual rollback
            var rollbackResult = await PerformRollbackAsync(deploymentId, previousVersion, rollbackId, reason, cancellationToken);
            
            UpdateRollbackStatus(rollbackId, RollbackStatus.InProgress, 
                rollbackResult.Success ? RollbackStatus.Succeeded : RollbackStatus.Failed);

            return rollbackResult;
        }
        catch (Exception ex)
        {
            UpdateRollbackStatus(rollbackId, RollbackStatus.InProgress, RollbackStatus.Failed);
            
            return new RollbackResult
            {
                RollbackId = rollbackId,
                DeploymentId = deploymentId,
                Success = false,
                Status = RollbackStatus.Failed,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                ErrorMessage = ex.Message,
                Reason = reason
            };
        }
    }

    /// <inheritdoc />
    public async Task<RollbackResult> RollbackToVersionAsync(string deploymentId, string targetVersion, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var rollbackId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        try
        {
            UpdateRollbackStatus(rollbackId, RollbackStatus.Pending, RollbackStatus.InProgress);

            // Find the target version
            if (!_versionHistory.TryGetValue(targetVersion, out var targetVersionInfo))
            {
                var notFoundResult = new RollbackResult
                {
                    RollbackId = rollbackId,
                    DeploymentId = deploymentId,
                    Success = false,
                    Status = RollbackStatus.Failed,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow,
                    ErrorMessage = $"Target version '{targetVersion}' not found",
                    Reason = reason
                };

                UpdateRollbackStatus(rollbackId, RollbackStatus.InProgress, RollbackStatus.Failed);
                return notFoundResult;
            }

            if (!targetVersionInfo.IsRollbackEligible)
            {
                var notEligibleResult = new RollbackResult
                {
                    RollbackId = rollbackId,
                    DeploymentId = deploymentId,
                    Success = false,
                    Status = RollbackStatus.Failed,
                    StartTime = startTime,
                    EndTime = DateTime.UtcNow,
                    ErrorMessage = $"Target version '{targetVersion}' is not eligible for rollback",
                    Reason = reason
                };

                UpdateRollbackStatus(rollbackId, RollbackStatus.InProgress, RollbackStatus.Failed);
                return notEligibleResult;
            }

            // Perform the rollback to specific version
            var rollbackResult = await PerformRollbackAsync(deploymentId, targetVersionInfo, rollbackId, reason, cancellationToken);
            
            UpdateRollbackStatus(rollbackId, RollbackStatus.InProgress, 
                rollbackResult.Success ? RollbackStatus.Succeeded : RollbackStatus.Failed);

            return rollbackResult;
        }
        catch (Exception ex)
        {
            UpdateRollbackStatus(rollbackId, RollbackStatus.InProgress, RollbackStatus.Failed);
            
            return new RollbackResult
            {
                RollbackId = rollbackId,
                DeploymentId = deploymentId,
                Success = false,
                Status = RollbackStatus.Failed,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                ErrorMessage = ex.Message,
                Reason = reason
            };
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<DeploymentVersion>> GetRollbackOptionsAsync(string environment, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        var options = _versionHistory.Values
            .Where(v => v.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase) && v.IsRollbackEligible)
            .OrderByDescending(v => v.DeployedAt)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<DeploymentVersion>>(options.AsReadOnly());
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateRollbackAsync(RollbackConfig rollbackConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rollbackConfig);

        var result = new ValidationResult { IsValid = true };

        // Validate deployment exists
        if (string.IsNullOrWhiteSpace(rollbackConfig.DeploymentId))
        {
            result.Errors.Add("Deployment ID is required for rollback.");
            result.IsValid = false;
        }

        // Validate target version if specified
        if (!string.IsNullOrWhiteSpace(rollbackConfig.TargetVersion))
        {
            if (!_versionHistory.ContainsKey(rollbackConfig.TargetVersion))
            {
                result.Errors.Add($"Target version '{rollbackConfig.TargetVersion}' not found in version history.");
                result.IsValid = false;
            }
            else if (!_versionHistory[rollbackConfig.TargetVersion].IsRollbackEligible)
            {
                result.Errors.Add($"Target version '{rollbackConfig.TargetVersion}' is not eligible for rollback.");
                result.IsValid = false;
            }
        }

        // Validate reason is provided
        if (string.IsNullOrWhiteSpace(rollbackConfig.Reason))
        {
            result.Errors.Add("Reason is required for rollback operations.");
            result.IsValid = false;
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<RollbackStatus> GetRollbackStatusAsync(string rollbackId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rollbackId);

        var status = _rollbackStatuses.TryGetValue(rollbackId, out var rollbackStatus) 
            ? rollbackStatus 
            : RollbackStatus.Pending;

        return Task.FromResult(status);
    }

    /// <inheritdoc />
    public Task ConfigureAutoRollbackAsync(RollbackTriggers triggers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(triggers);

        // In a real implementation, this would configure monitoring systems
        // to automatically trigger rollbacks based on the specified conditions
        
        // For now, we'll just store the configuration
        // This could integrate with health monitoring, error rate tracking, etc.
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a deployment version to the version history.
    /// </summary>
    /// <param name="version">The deployment version to add.</param>
    public void AddVersion(DeploymentVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);
        _versionHistory[version.Version] = version;
    }

    private async Task<RollbackResult> PerformRollbackAsync(string deploymentId, DeploymentVersion targetVersion, string rollbackId, string reason, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Simulate rollback operation
            await Task.Delay(1000, cancellationToken); // Simulate rollback time

            // Update version states
            var currentActive = _versionHistory.Values.FirstOrDefault(v => v.IsActive && v.Environment == targetVersion.Environment);
            if (currentActive != null)
            {
                currentActive.IsActive = false;
            }

            targetVersion.IsActive = true;

            return new RollbackResult
            {
                RollbackId = rollbackId,
                DeploymentId = deploymentId,
                Success = true,
                Status = RollbackStatus.Succeeded,
                RolledBackToVersion = targetVersion.Version,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                Reason = reason
            };
        }
        catch (Exception ex)
        {
            return new RollbackResult
            {
                RollbackId = rollbackId,
                DeploymentId = deploymentId,
                Success = false,
                Status = RollbackStatus.Failed,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                ErrorMessage = ex.Message,
                Reason = reason
            };
        }
    }

    private void UpdateRollbackStatus(string rollbackId, RollbackStatus previousStatus, RollbackStatus newStatus)
    {
        _rollbackStatuses[rollbackId] = newStatus;
        StatusChanged?.Invoke(this, new RollbackStatusChangedEventArgs(rollbackId, previousStatus, newStatus));
    }

    private void LoadVersionHistory()
    {
        // In a real implementation, this would load from persistent storage
        // For now, we'll create some sample data
        
        var version1 = new DeploymentVersion
        {
            Version = "1.0.0",
            DeploymentId = "deploy-1",
            Environment = "production",
            DeployedAt = DateTime.UtcNow.AddDays(-7),
            CommitSha = "abc123",
            IsActive = false,
            IsRollbackEligible = true
        };

        var version2 = new DeploymentVersion
        {
            Version = "1.1.0",
            DeploymentId = "deploy-2",
            Environment = "production",
            DeployedAt = DateTime.UtcNow.AddDays(-3),
            CommitSha = "def456",
            IsActive = true,
            IsRollbackEligible = true
        };

        _versionHistory[version1.Version] = version1;
        _versionHistory[version2.Version] = version2;
    }
}
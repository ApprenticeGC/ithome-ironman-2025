using Microsoft.Extensions.Logging;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Manages rollback operations for deployments.
/// </summary>
public class RollbackManager : IRollbackManager
{
    private readonly ILogger<RollbackManager> _logger;
    private readonly Dictionary<string, List<DeploymentContext>> _deploymentHistory = new();
    private readonly Dictionary<string, List<RollbackTrigger>> _autoRollbackTriggers = new();

    /// <inheritdoc />
    public event EventHandler<RollbackStatusChangedEventArgs>? RollbackStatusChanged;

    public RollbackManager(ILogger<RollbackManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RollbackResult> InitiateRollbackAsync(
        string deploymentId,
        string? targetVersion = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var rollbackId = Guid.NewGuid().ToString();
        var result = new RollbackResult
        {
            OriginalDeploymentId = deploymentId,
            RollbackId = rollbackId,
            StartedAt = DateTimeOffset.UtcNow,
            Reason = reason
        };

        try
        {
            _logger.LogInformation("Initiating rollback for deployment {DeploymentId}, rollback ID: {RollbackId}",
                deploymentId, rollbackId);

            OnRollbackStatusChanged(rollbackId, deploymentId, "Starting", "Rollback operation initiated");

            // Check if rollback is possible
            if (!await CanRollbackAsync(deploymentId, cancellationToken))
            {
                result.ErrorMessage = "Rollback not available for this deployment";
                OnRollbackStatusChanged(rollbackId, deploymentId, "Failed", result.ErrorMessage);
                return result;
            }

            // Get rollback options
            var options = await GetRollbackOptionsAsync(deploymentId, cancellationToken);
            var targetOption = options.FirstOrDefault(o => o.Version == targetVersion) ?? options.FirstOrDefault(o => o.IsRecommended);

            if (targetOption == null)
            {
                result.ErrorMessage = "No suitable rollback target found";
                OnRollbackStatusChanged(rollbackId, deploymentId, "Failed", result.ErrorMessage);
                return result;
            }

            result.RolledBackToVersion = targetOption.Version;

            OnRollbackStatusChanged(rollbackId, deploymentId, "InProgress", $"Rolling back to version {targetOption.Version}");

            // Simulate rollback execution
            await ExecuteRollbackAsync(deploymentId, targetOption, result, cancellationToken);

            result.Success = true;
            result.CompletedAt = DateTimeOffset.UtcNow;

            _logger.LogInformation("Successfully completed rollback for deployment {DeploymentId} to version {Version}",
                deploymentId, targetOption.Version);

            OnRollbackStatusChanged(rollbackId, deploymentId, "Completed", 
                $"Rollback completed successfully to version {targetOption.Version}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback deployment {DeploymentId}", deploymentId);
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTimeOffset.UtcNow;
            OnRollbackStatusChanged(rollbackId, deploymentId, "Failed", ex.Message);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> CanRollbackAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if we have deployment history for rollback
            var options = await GetRollbackOptionsAsync(deploymentId, cancellationToken);
            return options.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check rollback availability for deployment {DeploymentId}", deploymentId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<RollbackOption>> GetRollbackOptionsAsync(
        string deploymentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(100, cancellationToken);

            // For demo purposes, simulate rollback options based on deployment ID
            var options = new List<RollbackOption>();

            // Add some mock rollback options
            if (deploymentId.Length > 10)
            {
                options.Add(new RollbackOption
                {
                    Version = "1.2.3",
                    DisplayName = "Previous Stable Release (1.2.3)",
                    IsRecommended = true,
                    Description = "Last known stable version with full functionality"
                });

                options.Add(new RollbackOption
                {
                    Version = "1.2.2",
                    DisplayName = "Earlier Release (1.2.2)",
                    IsRecommended = false,
                    Description = "Older stable version, may have missing features"
                });
            }

            return options.AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rollback options for deployment {DeploymentId}", deploymentId);
            return Array.Empty<RollbackOption>();
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<RollbackResult>> GetRollbackHistoryAsync(
        string? environment = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // For demo purposes, return empty history
            // In production, this would query a database or storage system
            return Task.FromResult<IReadOnlyCollection<RollbackResult>>(Array.Empty<RollbackResult>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rollback history for environment {Environment}", environment);
            return Task.FromResult<IReadOnlyCollection<RollbackResult>>(Array.Empty<RollbackResult>());
        }
    }

    /// <inheritdoc />
    public async Task<bool> ConfigureAutoRollbackAsync(
        string deploymentId,
        IReadOnlyCollection<RollbackTrigger> triggers,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Yield(); // Make it properly async
            _autoRollbackTriggers[deploymentId] = triggers.ToList();

            _logger.LogInformation("Configured {Count} auto-rollback triggers for deployment {DeploymentId}",
                triggers.Count, deploymentId);

            // In a real implementation, you would set up monitoring and alerting
            // to watch for these trigger conditions

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure auto-rollback for deployment {DeploymentId}", deploymentId);
            return false;
        }
    }

    public void RegisterDeployment(DeploymentContext context)
    {
        try
        {
            var environment = context.Environment;
            if (!_deploymentHistory.ContainsKey(environment))
            {
                _deploymentHistory[environment] = new List<DeploymentContext>();
            }

            _deploymentHistory[environment].Add(context);

            // Keep only the last 10 deployments per environment
            if (_deploymentHistory[environment].Count > 10)
            {
                _deploymentHistory[environment].RemoveAt(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register deployment {DeploymentId} in history", context.DeploymentId);
        }
    }

    private async Task ExecuteRollbackAsync(
        string deploymentId,
        RollbackOption targetOption,
        RollbackResult result,
        CancellationToken cancellationToken)
    {
        // Simulate rollback steps
        var steps = new[]
        {
            "Stopping current application instances",
            "Backing up current configuration",
            "Downloading rollback artifacts",
            "Updating deployment configuration",
            "Deploying previous version",
            "Validating rollback deployment",
            "Updating load balancer configuration",
            "Running post-rollback verification"
        };

        foreach (var step in steps)
        {
            _logger.LogDebug("Rollback step: {Step}", step);
            OnRollbackStatusChanged(result.RollbackId, deploymentId, "InProgress", step);

            // Simulate step execution time
            await Task.Delay(Random.Shared.Next(500, 1500), cancellationToken);

            result.Metadata[step.Replace(" ", "_").ToLower()] = DateTimeOffset.UtcNow;
        }
    }

    private void OnRollbackStatusChanged(string rollbackId, string deploymentId, string status, string? message)
    {
        try
        {
            RollbackStatusChanged?.Invoke(this, new RollbackStatusChangedEventArgs(rollbackId, deploymentId, status, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error raising rollback status changed event");
        }
    }
}
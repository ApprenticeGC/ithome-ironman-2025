using GameConsole.Deployment.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Manages rollback operations for failed deployments.
/// </summary>
public class RollbackManager : BaseDeploymentService, IRollbackManager
{
    private readonly ConcurrentDictionary<string, DeploymentStatus> _rollbackStatus = new();
    private readonly ConcurrentDictionary<string, string> _rollbackToOriginal = new();
    private readonly ConcurrentDictionary<string, List<string>> _rollbackTargets = new();

    public event EventHandler<RollbackStatusChangedEventArgs>? RollbackStatusChanged;

    public RollbackManager(ILogger<RollbackManager> logger) : base(logger)
    {
    }

    public async Task<string> InitiateRollbackAsync(string deploymentId, string? targetVersion = null, CancellationToken cancellationToken = default)
    {
        var rollbackId = $"rollback-{deploymentId}-{Guid.NewGuid():N}";
        
        _logger.LogInformation("Initiating rollback {RollbackId} for deployment {DeploymentId} to version {TargetVersion}", 
            rollbackId, deploymentId, targetVersion ?? "previous");

        _rollbackStatus[rollbackId] = DeploymentStatus.InProgress;
        _rollbackToOriginal[rollbackId] = deploymentId;

        try
        {
            // Simulate rollback process
            await PerformRollbackAsync(deploymentId, targetVersion, cancellationToken);
            
            await UpdateRollbackStatusAsync(rollbackId, DeploymentStatus.Succeeded);
            _logger.LogInformation("Rollback {RollbackId} completed successfully", rollbackId);
        }
        catch (OperationCanceledException)
        {
            await UpdateRollbackStatusAsync(rollbackId, DeploymentStatus.Cancelled);
            _logger.LogWarning("Rollback {RollbackId} was cancelled", rollbackId);
        }
        catch (Exception ex)
        {
            await UpdateRollbackStatusAsync(rollbackId, DeploymentStatus.Failed);
            _logger.LogError(ex, "Rollback {RollbackId} failed", rollbackId);
        }

        return rollbackId;
    }

    public Task<DeploymentStatus> GetRollbackStatusAsync(string rollbackId, CancellationToken cancellationToken = default)
    {
        var status = _rollbackStatus.TryGetValue(rollbackId, out var currentStatus) 
            ? currentStatus 
            : DeploymentStatus.Failed;

        return Task.FromResult(status);
    }

    public Task<bool> CanRollbackAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        // For this implementation, we assume rollback is always possible for non-production environments
        // In a real implementation, this would check deployment history, environment state, etc.
        var canRollback = !string.IsNullOrEmpty(deploymentId);
        return Task.FromResult(canRollback);
    }

    public Task<IEnumerable<string>> GetRollbackTargetsAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        if (_rollbackTargets.TryGetValue(deploymentId, out var targets))
        {
            return Task.FromResult<IEnumerable<string>>(targets);
        }

        // Return some default rollback targets for demo purposes
        var defaultTargets = new List<string> { "v1.0.0", "v0.9.0", "v0.8.0" };
        _rollbackTargets[deploymentId] = defaultTargets;
        
        return Task.FromResult<IEnumerable<string>>(defaultTargets);
    }

    private async Task PerformRollbackAsync(string deploymentId, string? targetVersion, CancellationToken cancellationToken)
    {
        // Simulate rollback operations
        _logger.LogDebug("Performing rollback for deployment {DeploymentId}", deploymentId);
        
        // Simulate different rollback stages
        await Task.Delay(1000, cancellationToken); // Stop current deployment
        await Task.Delay(1500, cancellationToken); // Restore previous version
        await Task.Delay(2000, cancellationToken); // Verify rollback
        
        _logger.LogDebug("Rollback operations completed for deployment {DeploymentId}", deploymentId);
    }

    private async Task UpdateRollbackStatusAsync(string rollbackId, DeploymentStatus newStatus)
    {
        var originalDeploymentId = _rollbackToOriginal.TryGetValue(rollbackId, out var id) ? id : string.Empty;
        var previousStatus = _rollbackStatus.TryGetValue(rollbackId, out var prevStatus) ? prevStatus : DeploymentStatus.Pending;

        _rollbackStatus[rollbackId] = newStatus;

        var args = new RollbackStatusChangedEventArgs
        {
            RollbackId = rollbackId,
            OriginalDeploymentId = originalDeploymentId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Timestamp = DateTime.UtcNow
        };

        RollbackStatusChanged?.Invoke(this, args);
        await Task.CompletedTask;
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing rollback manager");
        return Task.CompletedTask;
    }

    protected override ValueTask OnDisposeAsync()
    {
        _rollbackStatus.Clear();
        _rollbackToOriginal.Clear();
        _rollbackTargets.Clear();
        return ValueTask.CompletedTask;
    }
}
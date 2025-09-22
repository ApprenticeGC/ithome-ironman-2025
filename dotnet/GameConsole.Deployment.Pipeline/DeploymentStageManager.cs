using GameConsole.Deployment.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Manages stage-based deployments with approval gates and execution orchestration.
/// </summary>
public class DeploymentStageManager : BaseDeploymentService
{
    private readonly ConcurrentDictionary<string, DeploymentStageResult> _stageResults = new();
    private readonly ConcurrentDictionary<string, List<DeploymentStage>> _deploymentStages = new();

    public DeploymentStageManager(ILogger<DeploymentStageManager> logger) : base(logger)
    {
    }

    /// <summary>
    /// Executes a deployment stage asynchronously.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="stage">The stage to execute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the stage result.</returns>
    public async Task<DeploymentStageResult> ExecuteStageAsync(string deploymentId, DeploymentStage stage, CancellationToken cancellationToken = default)
    {
        var stageKey = $"{deploymentId}:{stage.Id}";
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Starting deployment stage {StageId} for deployment {DeploymentId}", stage.Id, deploymentId);

        var result = new DeploymentStageResult
        {
            Stage = stage,
            Status = StageStatus.Running,
            StartTime = startTime
        };

        _stageResults[stageKey] = result;

        try
        {
            // Handle approval requirement
            if (stage.RequiresApproval)
            {
                result = result with { Status = StageStatus.PendingApproval };
                _stageResults[stageKey] = result;
                _logger.LogInformation("Stage {StageId} requires approval", stage.Id);
                return result;
            }

            // Execute stage based on environment
            var output = await ExecuteStageImplementationAsync(stage, cancellationToken);

            result = result with
            {
                Status = StageStatus.Completed,
                EndTime = DateTime.UtcNow,
                Output = output
            };

            _logger.LogInformation("Completed deployment stage {StageId} for deployment {DeploymentId}", stage.Id, deploymentId);
        }
        catch (OperationCanceledException)
        {
            result = result with
            {
                Status = StageStatus.Failed,
                EndTime = DateTime.UtcNow,
                ErrorMessage = "Stage execution was cancelled"
            };
            _logger.LogWarning("Stage {StageId} was cancelled", stage.Id);
        }
        catch (Exception ex)
        {
            result = result with
            {
                Status = StageStatus.Failed,
                EndTime = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
            _logger.LogError(ex, "Failed to execute deployment stage {StageId}", stage.Id);
        }

        _stageResults[stageKey] = result;
        return result;
    }

    /// <summary>
    /// Approves a pending deployment stage.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="stageId">The stage identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ApproveStageAsync(string deploymentId, string stageId, CancellationToken cancellationToken = default)
    {
        var stageKey = $"{deploymentId}:{stageId}";
        
        if (!_stageResults.TryGetValue(stageKey, out var result))
        {
            throw new InvalidOperationException($"Stage {stageId} not found for deployment {deploymentId}");
        }

        if (result.Status != StageStatus.PendingApproval)
        {
            throw new InvalidOperationException($"Stage {stageId} is not pending approval (current status: {result.Status})");
        }

        _logger.LogInformation("Approving stage {StageId} for deployment {DeploymentId}", stageId, deploymentId);

        // Continue execution after approval
        try
        {
            var output = await ExecuteStageImplementationAsync(result.Stage, cancellationToken);

            result = result with
            {
                Status = StageStatus.Completed,
                EndTime = DateTime.UtcNow,
                Output = output
            };

            _logger.LogInformation("Stage {StageId} completed after approval", stageId);
        }
        catch (Exception ex)
        {
            result = result with
            {
                Status = StageStatus.Failed,
                EndTime = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
            _logger.LogError(ex, "Stage {StageId} failed after approval", stageId);
        }

        _stageResults[stageKey] = result;
    }

    /// <summary>
    /// Gets the result of a specific deployment stage.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <param name="stageId">The stage identifier.</param>
    /// <returns>The stage result if found, null otherwise.</returns>
    public DeploymentStageResult? GetStageResult(string deploymentId, string stageId)
    {
        var stageKey = $"{deploymentId}:{stageId}";
        return _stageResults.TryGetValue(stageKey, out var result) ? result : null;
    }

    /// <summary>
    /// Gets all stage results for a deployment.
    /// </summary>
    /// <param name="deploymentId">The deployment identifier.</param>
    /// <returns>All stage results for the deployment.</returns>
    public IEnumerable<DeploymentStageResult> GetAllStageResults(string deploymentId)
    {
        return _stageResults.Values.Where(r => r.Stage.Id.StartsWith(deploymentId));
    }

    private async Task<string> ExecuteStageImplementationAsync(DeploymentStage stage, CancellationToken cancellationToken)
    {
        // Simulate stage execution based on environment
        var delay = stage.Environment switch
        {
            DeploymentEnvironment.Development => 1000,
            DeploymentEnvironment.Testing => 2000,
            DeploymentEnvironment.Staging => 3000,
            DeploymentEnvironment.Production => 5000,
            _ => 1000
        };

        await Task.Delay(delay, cancellationToken);

        return $"Successfully deployed to {stage.Environment} environment";
    }
}
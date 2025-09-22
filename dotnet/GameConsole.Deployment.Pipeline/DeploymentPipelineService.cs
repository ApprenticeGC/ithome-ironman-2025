using GameConsole.Core.Abstractions;
using GameConsole.Deployment.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Main deployment pipeline service that orchestrates CI/CD integration and stage-based deployments.
/// </summary>
public class DeploymentPipelineService : BaseDeploymentService, IDeploymentPipeline
{
    private readonly DeploymentStageManager _stageManager;
    private readonly IRollbackManager _rollbackManager;
    private readonly ICIPipelineProvider _ciProvider;
    
    private readonly ConcurrentDictionary<string, DeploymentStatus> _deploymentStatus = new();
    private readonly ConcurrentDictionary<string, List<DeploymentStage>> _deploymentStages = new();

    public event EventHandler<DeploymentStatusChangedEventArgs>? DeploymentStatusChanged;
    public event EventHandler<StageStatusChangedEventArgs>? StageStatusChanged;

    public DeploymentPipelineService(
        DeploymentStageManager stageManager,
        IRollbackManager rollbackManager,
        ICIPipelineProvider ciProvider,
        ILogger<DeploymentPipelineService> logger) : base(logger)
    {
        _stageManager = stageManager ?? throw new ArgumentNullException(nameof(stageManager));
        _rollbackManager = rollbackManager ?? throw new ArgumentNullException(nameof(rollbackManager));
        _ciProvider = ciProvider ?? throw new ArgumentNullException(nameof(ciProvider));
    }

    #region IDeploymentPipeline Implementation

    public async Task<string> StartDeploymentAsync(IEnumerable<DeploymentStage> stages, CancellationToken cancellationToken = default)
    {
        var deploymentId = $"deploy-{Guid.NewGuid():N}";
        var stageList = stages.OrderBy(s => s.Order).ToList();

        _logger.LogInformation("Starting deployment {DeploymentId} with {StageCount} stages", deploymentId, stageList.Count);

        _deploymentStages[deploymentId] = stageList;
        await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.InProgress);

        // Start deployment execution in background
        _ = Task.Run(async () =>
        {
            try
            {
                await ExecuteDeploymentAsync(deploymentId, stageList, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deployment {DeploymentId} failed unexpectedly", deploymentId);
                await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.Failed);
            }
        }, cancellationToken);

        return deploymentId;
    }

    public Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        var status = _deploymentStatus.TryGetValue(deploymentId, out var currentStatus) 
            ? currentStatus 
            : DeploymentStatus.Failed;

        return Task.FromResult(status);
    }

    public Task<IEnumerable<DeploymentStageResult>> GetStageResultsAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        var results = _stageManager.GetAllStageResults(deploymentId);
        return Task.FromResult(results);
    }

    public async Task CancelDeploymentAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling deployment {DeploymentId}", deploymentId);
        await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.Cancelled);
    }

    public async Task ApproveStageAsync(string deploymentId, string stageId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Approving stage {StageId} for deployment {DeploymentId}", stageId, deploymentId);
        await _stageManager.ApproveStageAsync(deploymentId, stageId, cancellationToken);
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new List<Type>
        {
            typeof(IDeploymentPipeline)
        };

        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(IDeploymentPipeline));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return Task.FromResult(typeof(T) == typeof(IDeploymentPipeline) ? this as T : null);
    }

    #endregion

    private async Task ExecuteDeploymentAsync(string deploymentId, List<DeploymentStage> stages, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var stage in stages)
            {
                // Check if deployment was cancelled
                if (_deploymentStatus.TryGetValue(deploymentId, out var status) && status == DeploymentStatus.Cancelled)
                {
                    _logger.LogInformation("Deployment {DeploymentId} was cancelled", deploymentId);
                    return;
                }

                // Execute stage
                var stageResult = await _stageManager.ExecuteStageAsync(deploymentId, stage, cancellationToken);
                
                // Raise stage status changed event
                await RaiseStageStatusChangedEventAsync(deploymentId, stage.Id, StageStatus.Waiting, stageResult.Status);

                // Handle stage failures
                if (stageResult.Status == StageStatus.Failed)
                {
                    _logger.LogError("Stage {StageId} failed for deployment {DeploymentId}. Initiating rollback.", 
                        stage.Id, deploymentId);
                    
                    await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.Failed);
                    
                    // Initiate rollback
                    await _rollbackManager.InitiateRollbackAsync(deploymentId, cancellationToken: cancellationToken);
                    return;
                }

                // Handle pending approval
                if (stageResult.Status == StageStatus.PendingApproval)
                {
                    _logger.LogInformation("Stage {StageId} is pending approval", stage.Id);
                    // Deployment will wait for external approval call
                    return;
                }
            }

            // All stages completed successfully
            await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.Succeeded);
            _logger.LogInformation("Deployment {DeploymentId} completed successfully", deploymentId);
        }
        catch (OperationCanceledException)
        {
            await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.Cancelled);
            _logger.LogWarning("Deployment {DeploymentId} was cancelled", deploymentId);
        }
        catch (Exception ex)
        {
            await UpdateDeploymentStatusAsync(deploymentId, DeploymentStatus.Failed);
            _logger.LogError(ex, "Deployment {DeploymentId} failed", deploymentId);
            
            // Initiate rollback on failure
            await _rollbackManager.InitiateRollbackAsync(deploymentId, cancellationToken: cancellationToken);
        }
    }

    private async Task UpdateDeploymentStatusAsync(string deploymentId, DeploymentStatus newStatus)
    {
        var previousStatus = _deploymentStatus.TryGetValue(deploymentId, out var prevStatus) ? prevStatus : DeploymentStatus.Pending;
        _deploymentStatus[deploymentId] = newStatus;

        var args = new DeploymentStatusChangedEventArgs
        {
            DeploymentId = deploymentId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Timestamp = DateTime.UtcNow
        };

        DeploymentStatusChanged?.Invoke(this, args);
        await Task.CompletedTask;
    }

    private async Task RaiseStageStatusChangedEventAsync(string deploymentId, string stageId, StageStatus previousStatus, StageStatus newStatus)
    {
        var args = new StageStatusChangedEventArgs
        {
            DeploymentId = deploymentId,
            StageId = stageId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Timestamp = DateTime.UtcNow
        };

        StageStatusChanged?.Invoke(this, args);
        await Task.CompletedTask;
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        await _stageManager.InitializeAsync(cancellationToken);
        await _rollbackManager.InitializeAsync(cancellationToken);
        await _ciProvider.InitializeAsync(cancellationToken);
        
        _logger.LogDebug("Deployment pipeline service initialized");
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken = default)
    {
        await _stageManager.StartAsync(cancellationToken);
        await _rollbackManager.StartAsync(cancellationToken);
        await _ciProvider.StartAsync(cancellationToken);
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken = default)
    {
        await _stageManager.StopAsync(cancellationToken);
        await _rollbackManager.StopAsync(cancellationToken);
        await _ciProvider.StopAsync(cancellationToken);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        _deploymentStatus.Clear();
        _deploymentStages.Clear();
        
        await _stageManager.DisposeAsync();
        await _rollbackManager.DisposeAsync();
        await _ciProvider.DisposeAsync();
    }
}
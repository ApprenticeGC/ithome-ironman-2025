using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Main implementation of the deployment pipeline orchestration.
/// </summary>
public class DeploymentPipeline : IDeploymentPipeline
{
    private readonly ILogger<DeploymentPipeline> _logger;
    private readonly IDeploymentStageManager _stageManager;
    private readonly IRollbackManager _rollbackManager;
    private readonly ICIPipelineProvider _ciPipelineProvider;
    private readonly Dictionary<string, DeploymentResult> _activeDeployments = new();
    private bool _isRunning;

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public event EventHandler<DeploymentStatusChangedEventArgs>? DeploymentStatusChanged;

    public DeploymentPipeline(
        ILogger<DeploymentPipeline> logger,
        IDeploymentStageManager stageManager,
        IRollbackManager rollbackManager,
        ICIPipelineProvider ciPipelineProvider)
    {
        _logger = logger;
        _stageManager = stageManager;
        _rollbackManager = rollbackManager;
        _ciPipelineProvider = ciPipelineProvider;
    }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing deployment pipeline");

        // Initialize dependencies if needed
        
        _logger.LogInformation("Deployment pipeline initialized successfully");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting deployment pipeline service");
        await Task.Yield(); // Make it properly async
        _isRunning = true;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping deployment pipeline service");
        _isRunning = false;

        // Cancel any active deployments
        var activeTasks = _activeDeployments.Values
            .Where(d => d.Status == DeploymentStatus.InProgress)
            .Select(d => CancelDeploymentAsync(d.Context.DeploymentId, cancellationToken))
            .ToArray();

        if (activeTasks.Any())
        {
            await Task.WhenAll(activeTasks);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task<DeploymentResult> DeployAsync(DeploymentContext context, CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
        {
            throw new InvalidOperationException("Deployment pipeline is not running");
        }

        var result = new DeploymentResult
        {
            Context = context,
            Status = DeploymentStatus.NotStarted,
            StartedAt = DateTimeOffset.UtcNow
        };

        _activeDeployments[context.DeploymentId] = result;

        try
        {
            _logger.LogInformation("Starting deployment {DeploymentId} for {ArtifactName} v{Version} to {Environment}",
                context.DeploymentId, context.ArtifactName, context.Version, context.Environment);

            // Register deployment with rollback manager for history tracking
            if (_rollbackManager is RollbackManager rollbackManager)
            {
                rollbackManager.RegisterDeployment(context);
            }

            OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.NotStarted, DeploymentStatus.InProgress,
                DeploymentStage.Validation, "Deployment started");

            result.Status = DeploymentStatus.InProgress;

            // Get stage configurations (in production, these would come from configuration)
            var stageConfigurations = GetStageConfigurations(context);

            // Execute each stage in sequence
            foreach (var stageConfig in stageConfigurations)
            {
                try
                {
                    result.CompletedStage = stageConfig.Stage;

                    _logger.LogInformation("Executing stage {Stage} for deployment {DeploymentId}",
                        stageConfig.Stage, context.DeploymentId);

                    // Check for approval if required
                    var validation = await _stageManager.ValidateStageAsync(context, stageConfig.Stage, stageConfig, cancellationToken);
                    if (!validation.CanProceed)
                    {
                        if (validation.WaitingForApproval)
                        {
                            result.Status = DeploymentStatus.PendingApproval;
                            OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.InProgress,
                                DeploymentStatus.PendingApproval, stageConfig.Stage, validation.BlockingReason);

                            // In a real implementation, this would wait for approval through external mechanism
                            // For demo, we'll simulate automatic approval after a delay
                            await Task.Delay(1000, cancellationToken);
                            await _stageManager.RecordApprovalAsync(context.DeploymentId, stageConfig.Stage,
                                "system-auto-approval", cancellationToken);
                            result.Status = DeploymentStatus.InProgress;
                        }
                        else
                        {
                            throw new InvalidOperationException(validation.BlockingReason);
                        }
                    }

                    // Execute the stage
                    var stageResult = await _stageManager.ExecuteStageAsync(context, stageConfig.Stage, stageConfig, cancellationToken);

                    if (!stageResult.Success)
                    {
                        throw new InvalidOperationException($"Stage {stageConfig.Stage} failed: {stageResult.ErrorMessage}");
                    }

                    // Add stage logs and outputs to deployment result
                    result.Logs.AddRange(stageResult.Logs);
                    foreach (var output in stageResult.Outputs)
                    {
                        result.Artifacts[output.Key] = output.Value;
                    }

                    // Add performance metrics
                    result.Metrics[$"stage_{stageConfig.Stage.ToString().ToLower()}_duration_ms"] = stageResult.Duration.TotalMilliseconds;

                    OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.InProgress, DeploymentStatus.InProgress,
                        stageConfig.Stage, $"Stage {stageConfig.Stage} completed successfully");
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Stage {Stage} failed for deployment {DeploymentId}", stageConfig.Stage, context.DeploymentId);

                    // Check if rollback should be initiated
                    if (ShouldAutoRollback(context, stageConfig.Stage))
                    {
                        _logger.LogWarning("Initiating automatic rollback for deployment {DeploymentId}", context.DeploymentId);

                        result.Status = DeploymentStatus.RollingBack;
                        OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.InProgress,
                            DeploymentStatus.RollingBack, stageConfig.Stage, "Automatic rollback initiated");

                        var rollbackResult = await _rollbackManager.InitiateRollbackAsync(context.DeploymentId,
                            reason: $"Auto-rollback due to {stageConfig.Stage} stage failure: {ex.Message}",
                            cancellationToken: cancellationToken);

                        if (rollbackResult.Success)
                        {
                            result.Status = DeploymentStatus.RolledBack;
                            OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.RollingBack,
                                DeploymentStatus.RolledBack, stageConfig.Stage, "Rollback completed successfully");
                        }
                        else
                        {
                            result.Status = DeploymentStatus.RollbackFailed;
                            OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.RollingBack,
                                DeploymentStatus.RollbackFailed, stageConfig.Stage, "Rollback failed");
                        }
                    }
                    else
                    {
                        result.Status = DeploymentStatus.Failed;
                        OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.InProgress,
                            DeploymentStatus.Failed, stageConfig.Stage, ex.Message);
                    }

                    result.ErrorMessage = ex.Message;
                    result.Exception = ex;
                    result.CompletedAt = DateTimeOffset.UtcNow;
                    return result;
                }
            }

            // Deployment completed successfully
            result.Status = DeploymentStatus.Succeeded;
            result.CompletedAt = DateTimeOffset.UtcNow;

            _logger.LogInformation("Successfully completed deployment {DeploymentId} in {Duration}",
                context.DeploymentId, result.Duration);

            OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.InProgress, DeploymentStatus.Succeeded,
                DeploymentStage.Cleanup, "Deployment completed successfully");
        }
        catch (OperationCanceledException)
        {
            result.Status = DeploymentStatus.Cancelled;
            result.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogWarning("Deployment {DeploymentId} was cancelled", context.DeploymentId);
            OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.InProgress, DeploymentStatus.Cancelled,
                result.CompletedStage, "Deployment was cancelled");
        }
        catch (Exception ex)
        {
            result.Status = DeploymentStatus.Failed;
            result.CompletedAt = DateTimeOffset.UtcNow;
            result.ErrorMessage = ex.Message;
            result.Exception = ex;
            _logger.LogError(ex, "Deployment {DeploymentId} failed unexpectedly", context.DeploymentId);
            OnDeploymentStatusChanged(context.DeploymentId, DeploymentStatus.InProgress, DeploymentStatus.Failed,
                result.CompletedStage, ex.Message);
        }

        return result;
    }

    /// <inheritdoc />
    public Task<DeploymentResult?> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_activeDeployments.TryGetValue(deploymentId, out var result) ? result : null);
    }

    /// <inheritdoc />
    public async Task<bool> CancelDeploymentAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Yield(); // Make it properly async
            if (_activeDeployments.TryGetValue(deploymentId, out var deployment) &&
                deployment.Status == DeploymentStatus.InProgress)
            {
                deployment.Status = DeploymentStatus.Cancelled;
                deployment.CompletedAt = DateTimeOffset.UtcNow;

                _logger.LogInformation("Cancelled deployment {DeploymentId}", deploymentId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel deployment {DeploymentId}", deploymentId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ApproveStageAsync(string deploymentId, DeploymentStage stage, string approvedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _stageManager.RecordApprovalAsync(deploymentId, stage, approvedBy, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Stage {Stage} approved by {ApprovedBy} for deployment {DeploymentId}",
                    stage, approvedBy, deploymentId);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to approve stage {Stage} for deployment {DeploymentId}", stage, deploymentId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<DeploymentResult>> GetDeploymentHistoryAsync(
        string? environment = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Make it properly async

        var query = _activeDeployments.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(environment))
        {
            query = query.Where(d => d.Context.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(d => d.StartedAt)
            .Take(limit)
            .ToList()
            .AsReadOnly();
    }

    private List<StageConfiguration> GetStageConfigurations(DeploymentContext context)
    {
        // In production, these configurations would come from external configuration
        return new List<StageConfiguration>
        {
            new() { Stage = DeploymentStage.Validation, Name = "Validation", TimeoutMinutes = 5 },
            new() { Stage = DeploymentStage.Build, Name = "Build", TimeoutMinutes = 15 },
            new() { Stage = DeploymentStage.Test, Name = "Test", TimeoutMinutes = 20 },
            new() { Stage = DeploymentStage.Security, Name = "Security Scan", TimeoutMinutes = 10 },
            new() { Stage = DeploymentStage.Staging, Name = "Deploy to Staging", RequiresApproval = false, TimeoutMinutes = 10 },
            new() { Stage = DeploymentStage.UAT, Name = "User Acceptance Testing", RequiresApproval = true, Approvers = new List<string> { "qa-team", "product-owner" }, TimeoutMinutes = 60 },
            new() { Stage = DeploymentStage.Canary, Name = "Canary Deployment", RequiresApproval = context.Environment == "production", Approvers = new List<string> { "ops-team" }, TimeoutMinutes = 15 },
            new() { Stage = DeploymentStage.Production, Name = "Production Deployment", RequiresApproval = true, Approvers = new List<string> { "ops-team", "tech-lead" }, TimeoutMinutes = 30 },
            new() { Stage = DeploymentStage.Verification, Name = "Post-Deploy Verification", TimeoutMinutes = 10 },
            new() { Stage = DeploymentStage.Cleanup, Name = "Cleanup", TimeoutMinutes = 5 }
        };
    }

    private bool ShouldAutoRollback(DeploymentContext context, DeploymentStage failedStage)
    {
        // In production, this would check configuration and policies
        return failedStage == DeploymentStage.Production || failedStage == DeploymentStage.Canary;
    }

    private void OnDeploymentStatusChanged(
        string deploymentId,
        DeploymentStatus previousStatus,
        DeploymentStatus currentStatus,
        DeploymentStage currentStage,
        string? message)
    {
        try
        {
            DeploymentStatusChanged?.Invoke(this, new DeploymentStatusChangedEventArgs(
                deploymentId, previousStatus, currentStatus, currentStage, message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error raising deployment status changed event");
        }
    }
}
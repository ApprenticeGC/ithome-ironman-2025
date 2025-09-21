using GameConsole.Core.Abstractions;
using GameConsole.Deployment.Pipeline.Services;
using GameConsole.Deployment.Pipeline.Providers;
using GameConsole.Plugins.Lifecycle;

namespace GameConsole.Deployment.Pipeline.Services;

/// <summary>
/// Main deployment pipeline orchestrator that coordinates deployment operations.
/// Integrates stage management, rollback capabilities, and CI/CD providers.
/// </summary>
public class DeploymentPipeline : IDeploymentPipeline
{
    private readonly DeploymentStageManager _stageManager;
    private readonly IRollbackManager _rollbackManager;
    private readonly IDeploymentProvider _deploymentProvider;
    private readonly Dictionary<string, DeploymentStatus> _deploymentStatuses = new();
    private readonly Dictionary<string, DeploymentMetrics> _deploymentMetrics = new();
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentPipeline"/> class.
    /// </summary>
    /// <param name="pluginLifecycleManager">The plugin lifecycle manager for integration.</param>
    /// <param name="rollbackManager">The rollback manager for deployment recovery.</param>
    /// <param name="deploymentProvider">The deployment provider for CI/CD integration.</param>
    public DeploymentPipeline(
        IPluginLifecycleManager pluginLifecycleManager,
        IRollbackManager rollbackManager,
        IDeploymentProvider deploymentProvider)
    {
        _stageManager = new DeploymentStageManager(pluginLifecycleManager ?? throw new ArgumentNullException(nameof(pluginLifecycleManager)));
        _rollbackManager = rollbackManager ?? throw new ArgumentNullException(nameof(rollbackManager));
        _deploymentProvider = deploymentProvider ?? throw new ArgumentNullException(nameof(deploymentProvider));

        // Subscribe to stage status changes
        _stageManager.StageStatusChanged += OnStageStatusChanged;
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Event raised when deployment status changes.
    /// </summary>
    public event EventHandler<DeploymentStatusChangedEventArgs>? StatusChanged;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _stageManager.InitializeAsync(cancellationToken);
        await _rollbackManager.InitializeAsync(cancellationToken);
        await _deploymentProvider.InitializeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _stageManager.StartAsync(cancellationToken);
        await _rollbackManager.StartAsync(cancellationToken);
        await _deploymentProvider.StartAsync(cancellationToken);
        _isRunning = true;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        await _stageManager.StopAsync(cancellationToken);
        await _rollbackManager.StopAsync(cancellationToken);
        await _deploymentProvider.StopAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _stageManager.DisposeAsync();
        await _rollbackManager.DisposeAsync();
        await _deploymentProvider.DisposeAsync();
    }

    /// <inheritdoc />
    public async Task<DeploymentResult> ExecutePipelineAsync(DeploymentConfig deploymentConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deploymentConfig);

        var startTime = DateTime.UtcNow;
        var deploymentId = deploymentConfig.DeploymentId;

        // Initialize deployment status
        var status = new DeploymentStatus
        {
            DeploymentId = deploymentId,
            Status = DeploymentStatusValue.Queued,
            StartTime = startTime,
            LastUpdated = startTime,
            ProgressPercentage = 0
        };
        _deploymentStatuses[deploymentId] = status;

        // Initialize metrics
        var metrics = new DeploymentMetrics
        {
            DeploymentId = deploymentId
        };
        _deploymentMetrics[deploymentId] = metrics;

        try
        {
            // Update status to in progress
            UpdateDeploymentStatus(deploymentId, DeploymentStatusValue.Queued, DeploymentStatusValue.InProgress);

            // Validate deployment configuration
            var validation = await ValidateDeploymentAsync(deploymentConfig, cancellationToken);
            if (!validation.IsValid)
            {
                UpdateDeploymentStatus(deploymentId, DeploymentStatusValue.InProgress, DeploymentStatusValue.Failed);
                return CreateFailedResult(deploymentId, startTime, $"Validation failed: {string.Join(", ", validation.Errors)}");
            }

            // Execute all stages
            var stageResults = await _stageManager.ExecuteAllStagesAsync(deploymentConfig, cancellationToken);
            
            // Update metrics
            metrics.StagesExecuted = stageResults.Count;
            metrics.SuccessfulStages = stageResults.Count(r => r.Success);
            metrics.FailedStages = stageResults.Count(r => !r.Success);
            metrics.TotalDuration = DateTime.UtcNow - startTime;
            metrics.StageMetrics = stageResults.Select(CreateStageMetrics).ToList();

            // Determine final result
            var success = stageResults.All(r => r.Success);
            var finalStatus = success ? DeploymentStatusValue.Succeeded : DeploymentStatusValue.Failed;
            
            UpdateDeploymentStatus(deploymentId, DeploymentStatusValue.InProgress, finalStatus);

            var result = new DeploymentResult
            {
                DeploymentId = deploymentId,
                Success = success,
                Status = finalStatus,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                StageResults = stageResults,
                Metrics = metrics
            };

            // Trigger automatic rollback if deployment failed and configured
            if (!success && deploymentConfig.RollbackConfig?.EnableAutoRollback == true)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000, CancellationToken.None); // Brief delay
                    await TriggerAutoRollbackAsync(deploymentConfig, "Deployment failed", CancellationToken.None);
                }, CancellationToken.None);
            }

            return result;
        }
        catch (Exception ex)
        {
            UpdateDeploymentStatus(deploymentId, DeploymentStatusValue.InProgress, DeploymentStatusValue.Failed);
            metrics.TotalDuration = DateTime.UtcNow - startTime;
            
            return CreateFailedResult(deploymentId, startTime, ex.Message, metrics);
        }
    }

    /// <inheritdoc />
    public async Task<StageResult> ExecuteStageAsync(StageConfig stageConfig, CancellationToken cancellationToken = default)
    {
        return await _stageManager.ExecuteStageAsync(stageConfig, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RollbackResult> RollbackAsync(RollbackConfig rollbackConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rollbackConfig);

        // Update deployment status to rolling back
        if (_deploymentStatuses.ContainsKey(rollbackConfig.DeploymentId))
        {
            UpdateDeploymentStatus(rollbackConfig.DeploymentId, 
                _deploymentStatuses[rollbackConfig.DeploymentId].Status, 
                DeploymentStatusValue.RollingBack);
        }

        var rollbackResult = string.IsNullOrWhiteSpace(rollbackConfig.TargetVersion)
            ? await _rollbackManager.RollbackAsync(rollbackConfig.DeploymentId, rollbackConfig.Reason, cancellationToken)
            : await _rollbackManager.RollbackToVersionAsync(rollbackConfig.DeploymentId, rollbackConfig.TargetVersion, rollbackConfig.Reason, cancellationToken);

        // Update deployment status based on rollback result
        if (_deploymentStatuses.ContainsKey(rollbackConfig.DeploymentId))
        {
            var newStatus = rollbackResult.Success ? DeploymentStatusValue.RolledBack : DeploymentStatusValue.Failed;
            UpdateDeploymentStatus(rollbackConfig.DeploymentId, DeploymentStatusValue.RollingBack, newStatus);
        }

        return rollbackResult;
    }

    /// <inheritdoc />
    public Task<ValidationResult> ValidateDeploymentAsync(DeploymentConfig deploymentConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deploymentConfig);

        var result = new ValidationResult { IsValid = true };

        // Validate required fields
        if (string.IsNullOrWhiteSpace(deploymentConfig.DeploymentId))
        {
            result.Errors.Add("Deployment ID is required.");
            result.IsValid = false;
        }

        if (string.IsNullOrWhiteSpace(deploymentConfig.Name))
        {
            result.Errors.Add("Deployment name is required.");
            result.IsValid = false;
        }

        if (string.IsNullOrWhiteSpace(deploymentConfig.Version))
        {
            result.Errors.Add("Version is required.");
            result.IsValid = false;
        }

        if (string.IsNullOrWhiteSpace(deploymentConfig.TargetEnvironment))
        {
            result.Errors.Add("Target environment is required.");
            result.IsValid = false;
        }

        // Validate stages
        if (deploymentConfig.Stages.Count == 0)
        {
            result.Errors.Add("At least one deployment stage is required.");
            result.IsValid = false;
        }
        else
        {
            var duplicateIds = deploymentConfig.Stages
                .GroupBy(s => s.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            
            foreach (var duplicateId in duplicateIds)
            {
                result.Errors.Add($"Duplicate stage ID: {duplicateId}");
                result.IsValid = false;
            }
        }

        // Validate timeout
        if (deploymentConfig.Timeout <= TimeSpan.Zero)
        {
            result.Warnings.Add("Deployment timeout should be greater than zero. Using default timeout.");
        }

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentId);

        if (_deploymentStatuses.TryGetValue(deploymentId, out var status))
        {
            status.LastUpdated = DateTime.UtcNow;
            return Task.FromResult(status);
        }

        // Return not found status
        return Task.FromResult(new DeploymentStatus
        {
            DeploymentId = deploymentId,
            Status = DeploymentStatusValue.Failed,
            StartTime = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            StatusMessage = "Deployment not found"
        });
    }

    /// <inheritdoc />
    public Task<DeploymentMetrics> GetDeploymentMetricsAsync(string deploymentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentId);

        if (_deploymentMetrics.TryGetValue(deploymentId, out var metrics))
        {
            return Task.FromResult(metrics);
        }

        // Return empty metrics
        return Task.FromResult(new DeploymentMetrics
        {
            DeploymentId = deploymentId
        });
    }

    private void UpdateDeploymentStatus(string deploymentId, DeploymentStatusValue previousStatus, DeploymentStatusValue newStatus)
    {
        if (_deploymentStatuses.TryGetValue(deploymentId, out var status))
        {
            status.Status = newStatus;
            status.LastUpdated = DateTime.UtcNow;
            
            // Update progress based on status
            status.ProgressPercentage = newStatus switch
            {
                DeploymentStatusValue.Queued => 0,
                DeploymentStatusValue.InProgress => 50,
                DeploymentStatusValue.WaitingForApproval => 25,
                DeploymentStatusValue.Succeeded => 100,
                DeploymentStatusValue.Failed => 0,
                DeploymentStatusValue.RollingBack => 75,
                DeploymentStatusValue.RolledBack => 100,
                _ => status.ProgressPercentage
            };
        }

        StatusChanged?.Invoke(this, new DeploymentStatusChangedEventArgs(deploymentId, previousStatus, newStatus));
    }

    private void OnStageStatusChanged(object? sender, StageStatusChangedEventArgs e)
    {
        // Update deployment status based on stage changes
        if (!string.IsNullOrEmpty(e.DeploymentId) && _deploymentStatuses.ContainsKey(e.DeploymentId))
        {
            var status = _deploymentStatuses[e.DeploymentId];
            status.CurrentStage = e.StageId;
            status.LastUpdated = DateTime.UtcNow;
        }
    }

    private async Task TriggerAutoRollbackAsync(DeploymentConfig deploymentConfig, string reason, CancellationToken cancellationToken)
    {
        if (deploymentConfig.RollbackConfig != null)
        {
            try
            {
                deploymentConfig.RollbackConfig.Reason = reason;
                await RollbackAsync(deploymentConfig.RollbackConfig, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log rollback failure (in real implementation)
                Console.WriteLine($"Auto-rollback failed for deployment {deploymentConfig.DeploymentId}: {ex.Message}");
            }
        }
    }

    private static DeploymentResult CreateFailedResult(string deploymentId, DateTime startTime, string errorMessage, DeploymentMetrics? metrics = null)
    {
        return new DeploymentResult
        {
            DeploymentId = deploymentId,
            Success = false,
            Status = DeploymentStatusValue.Failed,
            StartTime = startTime,
            EndTime = DateTime.UtcNow,
            ErrorMessage = errorMessage,
            Metrics = metrics
        };
    }

    private static StageMetrics CreateStageMetrics(StageResult stageResult)
    {
        return new StageMetrics
        {
            StageId = stageResult.StageId,
            Duration = stageResult.Duration,
            Success = stageResult.Success,
            RetryAttempts = 0 // Could be enhanced to track actual retries
        };
    }
}
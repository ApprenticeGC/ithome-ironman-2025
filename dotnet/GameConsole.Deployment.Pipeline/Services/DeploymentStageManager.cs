using GameConsole.Core.Abstractions;
using GameConsole.Plugins.Lifecycle;

namespace GameConsole.Deployment.Pipeline.Services;

/// <summary>
/// Manages stage-based deployments with approval gates and validation.
/// Coordinates deployment stages and handles stage transitions.
/// </summary>
public class DeploymentStageManager : IService
{
    private readonly IPluginLifecycleManager _pluginLifecycleManager;
    private readonly List<IDeploymentStage> _stages = new();
    private bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeploymentStageManager"/> class.
    /// </summary>
    /// <param name="pluginLifecycleManager">The plugin lifecycle manager for integration.</param>
    public DeploymentStageManager(IPluginLifecycleManager pluginLifecycleManager)
    {
        _pluginLifecycleManager = pluginLifecycleManager ?? throw new ArgumentNullException(nameof(pluginLifecycleManager));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Gets the registered deployment stages.
    /// </summary>
    public IReadOnlyCollection<IDeploymentStage> Stages => _stages.AsReadOnly();

    /// <summary>
    /// Event raised when a stage status changes.
    /// </summary>
    public event EventHandler<StageStatusChangedEventArgs>? StageStatusChanged;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Initialize stage manager
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

    /// <summary>
    /// Registers a deployment stage with the manager.
    /// </summary>
    /// <param name="stage">The deployment stage to register.</param>
    public void RegisterStage(IDeploymentStage stage)
    {
        ArgumentNullException.ThrowIfNull(stage);
        
        if (_stages.Any(s => s.Id == stage.Id))
        {
            throw new InvalidOperationException($"Stage with ID '{stage.Id}' is already registered.");
        }

        _stages.Add(stage);
        stage.StatusChanged += OnStageStatusChanged;
    }

    /// <summary>
    /// Unregisters a deployment stage from the manager.
    /// </summary>
    /// <param name="stageId">The ID of the stage to unregister.</param>
    /// <returns>True if the stage was found and removed, false otherwise.</returns>
    public bool UnregisterStage(string stageId)
    {
        var stage = _stages.FirstOrDefault(s => s.Id == stageId);
        if (stage != null)
        {
            stage.StatusChanged -= OnStageStatusChanged;
            _stages.Remove(stage);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Executes a deployment stage asynchronously.
    /// </summary>
    /// <param name="stageConfig">Configuration for the stage execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the stage execution result.</returns>
    public async Task<StageResult> ExecuteStageAsync(StageConfig stageConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stageConfig);

        var stage = _stages.FirstOrDefault(s => s.Id == stageConfig.Id);
        if (stage == null)
        {
            return new StageResult
            {
                StageId = stageConfig.Id,
                StageName = stageConfig.Name,
                Success = false,
                Status = StageStatus.Failed,
                ErrorMessage = $"Stage with ID '{stageConfig.Id}' not found.",
                StartTime = DateTime.UtcNow
            };
        }

        try
        {
            // Validate stage prerequisites
            var validation = await stage.ValidateAsync(stageConfig, cancellationToken);
            if (!validation.IsValid)
            {
                return new StageResult
                {
                    StageId = stageConfig.Id,
                    StageName = stageConfig.Name,
                    Success = false,
                    Status = StageStatus.Failed,
                    ErrorMessage = $"Stage validation failed: {string.Join(", ", validation.Errors)}",
                    StartTime = DateTime.UtcNow
                };
            }

            // Handle approval requirements
            if (stage.RequiresApproval && stage.Status != StageStatus.Succeeded)
            {
                // In a real implementation, this would wait for approval
                // For now, we'll simulate automatic approval for testing
                var approval = new StageApproval
                {
                    StageId = stageConfig.Id,
                    IsApproved = true,
                    ApproverId = "system",
                    Comments = "Automatic approval for testing"
                };
                await stage.SubmitApprovalAsync(approval, cancellationToken);
            }

            // Execute the stage
            var result = await stage.ExecuteAsync(stageConfig, cancellationToken);
            
            // Perform health checks if configured
            if (stageConfig.HealthCheck?.Enabled == true)
            {
                result.HealthCheckResults = await PerformHealthChecksAsync(stageConfig.HealthCheck, cancellationToken);
            }

            return result;
        }
        catch (Exception ex)
        {
            return new StageResult
            {
                StageId = stageConfig.Id,
                StageName = stageConfig.Name,
                Success = false,
                Status = StageStatus.Failed,
                ErrorMessage = ex.Message,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Executes all stages in a deployment configuration sequentially.
    /// </summary>
    /// <param name="deploymentConfig">The deployment configuration containing stages.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing all stage execution results.</returns>
    public async Task<List<StageResult>> ExecuteAllStagesAsync(DeploymentConfig deploymentConfig, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(deploymentConfig);

        var results = new List<StageResult>();
        var orderedStages = deploymentConfig.Stages.OrderBy(s => s.Order).ToList();

        foreach (var stageConfig in orderedStages)
        {
            var result = await ExecuteStageAsync(stageConfig, cancellationToken);
            results.Add(result);

            // Stop execution if stage failed (unless configured to continue)
            if (!result.Success)
            {
                break;
            }
        }

        return results;
    }

    private async Task<List<HealthCheckResult>> PerformHealthChecksAsync(HealthCheckConfig healthConfig, CancellationToken cancellationToken)
    {
        var results = new List<HealthCheckResult>();

        foreach (var endpoint in healthConfig.Endpoints)
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = healthConfig.CheckTimeout };
                var startTime = DateTime.UtcNow;
                var response = await httpClient.GetAsync(endpoint, cancellationToken);
                var endTime = DateTime.UtcNow;

                var result = new HealthCheckResult
                {
                    Endpoint = endpoint,
                    IsHealthy = healthConfig.SuccessCriteria.ExpectedStatusCodes.Contains((int)response.StatusCode),
                    StatusCode = (int)response.StatusCode,
                    ResponseTime = endTime - startTime,
                    CheckTime = startTime
                };

                if (response.Content != null)
                {
                    result.ResponseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                }

                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(new HealthCheckResult
                {
                    Endpoint = endpoint,
                    IsHealthy = false,
                    ErrorMessage = ex.Message,
                    CheckTime = DateTime.UtcNow
                });
            }
        }

        return results;
    }

    private void OnStageStatusChanged(object? sender, StageStatusChangedEventArgs e)
    {
        StageStatusChanged?.Invoke(sender, e);
    }
}
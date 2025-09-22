using GameConsole.Core.Abstractions;
using GameConsole.Deployment.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// CI/CD Pipeline provider for GitHub Actions integration.
/// </summary>
public class GitHubActionsCIPipelineProvider : BaseDeploymentService, ICIPipelineProvider
{
    private readonly ConcurrentDictionary<string, PipelineStatus> _pipelineStatus = new();
    private readonly ConcurrentDictionary<string, string> _pipelineLogs = new();

    public string PlatformName => "GitHub Actions";

    public event EventHandler<PipelineStatusChangedEventArgs>? PipelineStatusChanged;

    public GitHubActionsCIPipelineProvider(ILogger<GitHubActionsCIPipelineProvider> logger) : base(logger)
    {
    }

    #region ICIPipelineProvider Implementation

    public async Task<string> TriggerPipelineAsync(PipelineConfiguration pipelineConfig, CancellationToken cancellationToken = default)
    {
        var runId = $"run-{Guid.NewGuid():N}";
        
        _logger.LogInformation("Triggering GitHub Actions pipeline for repository {Repository} on branch {Branch}", 
            pipelineConfig.Repository, pipelineConfig.Branch);

        _pipelineStatus[runId] = PipelineStatus.Queued;
        await UpdatePipelineStatusAsync(runId, PipelineStatus.Queued);

        // Simulate pipeline execution
        _ = Task.Run(async () =>
        {
            try
            {
                await SimulatePipelineExecutionAsync(runId, pipelineConfig, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipeline {RunId} failed unexpectedly", runId);
                await UpdatePipelineStatusAsync(runId, PipelineStatus.Failed);
            }
        }, cancellationToken);

        return runId;
    }

    public Task<PipelineStatus> GetPipelineStatusAsync(string runId, CancellationToken cancellationToken = default)
    {
        var status = _pipelineStatus.TryGetValue(runId, out var currentStatus) 
            ? currentStatus 
            : PipelineStatus.Failed;

        return Task.FromResult(status);
    }

    public async Task CancelPipelineAsync(string runId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling pipeline {RunId}", runId);
        
        if (_pipelineStatus.TryGetValue(runId, out var status) && 
            (status == PipelineStatus.Queued || status == PipelineStatus.Running))
        {
            await UpdatePipelineStatusAsync(runId, PipelineStatus.Cancelled);
        }
    }

    public Task<string> GetPipelineLogsAsync(string runId, CancellationToken cancellationToken = default)
    {
        var logs = _pipelineLogs.TryGetValue(runId, out var currentLogs) 
            ? currentLogs 
            : $"No logs available for pipeline run {runId}";

        return Task.FromResult(logs);
    }

    #endregion

    #region ICapabilityProvider Implementation

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new List<Type>
        {
            typeof(ICIPipelineProvider)
        };

        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(ICIPipelineProvider));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return Task.FromResult(typeof(T) == typeof(ICIPipelineProvider) ? this as T : null);
    }

    #endregion

    private async Task SimulatePipelineExecutionAsync(string runId, PipelineConfiguration config, CancellationToken cancellationToken)
    {
        var logs = new List<string>
        {
            $"Starting GitHub Actions pipeline for {config.Repository}:{config.Branch}",
            $"Using workflow file: {config.WorkflowFile}",
            $"Target environment: {config.Environment}"
        };

        // Simulate pipeline stages
        await UpdatePipelineStatusAsync(runId, PipelineStatus.Running);
        logs.Add("Pipeline execution started");

        // Build stage
        await Task.Delay(2000, cancellationToken);
        logs.Add("Build stage completed successfully");

        // Test stage
        await Task.Delay(1500, cancellationToken);
        logs.Add("Test stage completed successfully");

        // Deploy stage (varies by environment)
        var deployDelay = config.Environment switch
        {
            DeploymentEnvironment.Development => 1000,
            DeploymentEnvironment.Testing => 2000,
            DeploymentEnvironment.Staging => 3000,
            DeploymentEnvironment.Production => 5000,
            _ => 1000
        };

        await Task.Delay(deployDelay, cancellationToken);
        logs.Add($"Deployment to {config.Environment} completed successfully");

        _pipelineLogs[runId] = string.Join(Environment.NewLine, logs);
        await UpdatePipelineStatusAsync(runId, PipelineStatus.Succeeded);
    }

    private async Task UpdatePipelineStatusAsync(string runId, PipelineStatus newStatus)
    {
        var previousStatus = _pipelineStatus.TryGetValue(runId, out var prevStatus) ? prevStatus : PipelineStatus.Queued;
        _pipelineStatus[runId] = newStatus;

        var args = new PipelineStatusChangedEventArgs
        {
            RunId = runId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            Timestamp = DateTime.UtcNow
        };

        PipelineStatusChanged?.Invoke(this, args);
        await Task.CompletedTask;
    }

    protected override Task OnInitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Initializing GitHub Actions CI pipeline provider");
        return Task.CompletedTask;
    }

    protected override ValueTask OnDisposeAsync()
    {
        _pipelineStatus.Clear();
        _pipelineLogs.Clear();
        return ValueTask.CompletedTask;
    }
}
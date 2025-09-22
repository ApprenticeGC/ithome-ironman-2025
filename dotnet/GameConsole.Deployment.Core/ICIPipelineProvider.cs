using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Core;

/// <summary>
/// Interface for CI/CD pipeline providers that integrate with external systems.
/// </summary>
public interface ICIPipelineProvider : IService, ICapabilityProvider
{
    /// <summary>
    /// Gets the name of the CI/CD platform (e.g., "GitHub Actions", "Azure DevOps", "Jenkins").
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Triggers a CI/CD pipeline run.
    /// </summary>
    /// <param name="pipelineConfig">Configuration for the pipeline run.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns a unique run identifier.</returns>
    Task<string> TriggerPipelineAsync(PipelineConfiguration pipelineConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a pipeline run.
    /// </summary>
    /// <param name="runId">The unique identifier of the pipeline run.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the pipeline status.</returns>
    Task<PipelineStatus> GetPipelineStatusAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running pipeline.
    /// </summary>
    /// <param name="runId">The unique identifier of the pipeline run to cancel.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task CancelPipelineAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the logs for a pipeline run.
    /// </summary>
    /// <param name="runId">The unique identifier of the pipeline run.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the pipeline logs.</returns>
    Task<string> GetPipelineLogsAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when a pipeline status changes.
    /// </summary>
    event EventHandler<PipelineStatusChangedEventArgs>? PipelineStatusChanged;
}

/// <summary>
/// Configuration for a CI/CD pipeline run.
/// </summary>
public record PipelineConfiguration
{
    /// <summary>
    /// Gets the repository or project identifier.
    /// </summary>
    public required string Repository { get; init; }

    /// <summary>
    /// Gets the branch or reference to build from.
    /// </summary>
    public required string Branch { get; init; }

    /// <summary>
    /// Gets the workflow or pipeline definition file.
    /// </summary>
    public required string WorkflowFile { get; init; }

    /// <summary>
    /// Gets the target environment for the pipeline.
    /// </summary>
    public required DeploymentEnvironment Environment { get; init; }

    /// <summary>
    /// Gets additional parameters for the pipeline run.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Represents the status of a CI/CD pipeline run.
/// </summary>
public enum PipelineStatus
{
    /// <summary>
    /// Pipeline is queued and waiting to start.
    /// </summary>
    Queued,

    /// <summary>
    /// Pipeline is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Pipeline completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// Pipeline failed during execution.
    /// </summary>
    Failed,

    /// <summary>
    /// Pipeline was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Pipeline is waiting for manual intervention.
    /// </summary>
    Waiting
}

/// <summary>
/// Event arguments for pipeline status changes.
/// </summary>
public class PipelineStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the pipeline run identifier.
    /// </summary>
    public required string RunId { get; init; }

    /// <summary>
    /// Gets the previous status.
    /// </summary>
    public required PipelineStatus PreviousStatus { get; init; }

    /// <summary>
    /// Gets the new status.
    /// </summary>
    public required PipelineStatus NewStatus { get; init; }

    /// <summary>
    /// Gets the timestamp of the status change.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
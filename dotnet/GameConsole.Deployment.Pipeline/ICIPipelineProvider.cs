namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Provides integration with continuous integration and continuous deployment platforms.
/// </summary>
public interface ICIPipelineProvider
{
    /// <summary>
    /// Gets the name of the CI/CD platform this provider supports.
    /// </summary>
    string PlatformName { get; }

    /// <summary>
    /// Gets whether this provider is currently available and configured.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Triggers a CI/CD pipeline execution.
    /// </summary>
    /// <param name="pipelineConfig">The pipeline configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The result of the pipeline trigger operation.</returns>
    Task<CIPipelineResult> TriggerPipelineAsync(
        CIPipelineConfiguration pipelineConfig,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a running CI/CD pipeline.
    /// </summary>
    /// <param name="pipelineId">The pipeline execution identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current pipeline status and details.</returns>
    Task<CIPipelineStatus?> GetPipelineStatusAsync(
        string pipelineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running CI/CD pipeline.
    /// </summary>
    /// <param name="pipelineId">The pipeline execution identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the pipeline was successfully cancelled.</returns>
    Task<bool> CancelPipelineAsync(string pipelineId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the logs from a CI/CD pipeline execution.
    /// </summary>
    /// <param name="pipelineId">The pipeline execution identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The pipeline execution logs.</returns>
    Task<IReadOnlyCollection<string>> GetPipelineLogsAsync(
        string pipelineId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the artifacts produced by a CI/CD pipeline execution.
    /// </summary>
    /// <param name="pipelineId">The pipeline execution identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Information about available artifacts.</returns>
    Task<IReadOnlyCollection<CIArtifact>> GetPipelineArtifactsAsync(
        string pipelineId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for a CI/CD pipeline execution.
/// </summary>
public class CIPipelineConfiguration
{
    /// <summary>
    /// Gets or sets the name or identifier of the pipeline to execute.
    /// </summary>
    public string PipelineName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the repository or project context.
    /// </summary>
    public string Repository { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the branch or tag to build from.
    /// </summary>
    public string Branch { get; set; } = "main";

    /// <summary>
    /// Gets or sets the environment variables for the pipeline.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets custom parameters for the pipeline.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout for the pipeline execution in minutes.
    /// </summary>
    public int TimeoutMinutes { get; set; } = 60;
}

/// <summary>
/// Represents the result of triggering a CI/CD pipeline.
/// </summary>
public class CIPipelineResult
{
    /// <summary>
    /// Gets or sets whether the trigger was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the pipeline execution identifier.
    /// </summary>
    public string? PipelineId { get; set; }

    /// <summary>
    /// Gets or sets the URL to view the pipeline execution.
    /// </summary>
    public string? ViewUrl { get; set; }

    /// <summary>
    /// Gets or sets the error message if triggering failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the pipeline execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Represents the status of a CI/CD pipeline execution.
/// </summary>
public class CIPipelineStatus
{
    /// <summary>
    /// Gets or sets the pipeline execution identifier.
    /// </summary>
    public string PipelineId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current status of the pipeline.
    /// </summary>
    public CIPipelineState State { get; set; }

    /// <summary>
    /// Gets or sets when the pipeline started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Gets or sets when the pipeline completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Gets the duration of the pipeline execution.
    /// </summary>
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;

    /// <summary>
    /// Gets or sets the current step or job being executed.
    /// </summary>
    public string? CurrentStep { get; set; }

    /// <summary>
    /// Gets or sets the result message or error details.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the URL to view the pipeline execution.
    /// </summary>
    public string? ViewUrl { get; set; }
}

/// <summary>
/// States of a CI/CD pipeline execution.
/// </summary>
public enum CIPipelineState
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
    /// Pipeline failed with errors.
    /// </summary>
    Failed,

    /// <summary>
    /// Pipeline was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Pipeline timed out.
    /// </summary>
    TimedOut
}

/// <summary>
/// Represents an artifact produced by a CI/CD pipeline.
/// </summary>
public class CIArtifact
{
    /// <summary>
    /// Gets or sets the name of the artifact.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type or format of the artifact.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the artifact in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the URL to download the artifact.
    /// </summary>
    public string? DownloadUrl { get; set; }

    /// <summary>
    /// Gets or sets when the artifact was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the checksum or hash of the artifact.
    /// </summary>
    public string? Checksum { get; set; }
}
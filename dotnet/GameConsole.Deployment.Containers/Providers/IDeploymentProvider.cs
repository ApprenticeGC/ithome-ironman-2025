using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Containers.Providers;

/// <summary>
/// Base interface for container deployment providers.
/// </summary>
public interface IDeploymentProvider : IService
{
    /// <summary>
    /// Gets the provider name (e.g., "Docker", "Kubernetes").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the supported deployment features.
    /// </summary>
    IReadOnlySet<string> SupportedFeatures { get; }

    /// <summary>
    /// Checks if the provider is available and configured.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>True if the provider is ready for use.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a deployment configuration for this provider.
    /// </summary>
    /// <param name="configuration">The deployment configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Validation result with errors if any.</returns>
    Task<ValidationResult> ValidateConfigurationAsync(DeploymentConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deploys a containerized application.
    /// </summary>
    /// <param name="configuration">The deployment configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The deployment result.</returns>
    Task<DeploymentResult> DeployAsync(DeploymentConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scales a deployed application.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="replicas">Target number of replicas.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The scaling operation result.</returns>
    Task<ScalingResult> ScaleAsync(string deploymentId, int replicas, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a deployed application.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The removal operation result.</returns>
    Task<OperationResult> RemoveAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The current deployment status.</returns>
    Task<DeploymentStatus> GetStatusAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all active deployments managed by this provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Collection of active deployment information.</returns>
    Task<IEnumerable<DeploymentInfo>> ListDeploymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets logs for a specific deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="options">Log retrieval options.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Log entries for the deployment.</returns>
    Task<IEnumerable<LogEntry>> GetLogsAsync(string deploymentId, LogOptions? options = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for container health monitoring providers.
/// </summary>
public interface IHealthMonitorProvider : IService
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Monitors the health of a specific deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Observable stream of health status updates.</returns>
    IObservable<HealthStatus> MonitorHealthAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Current health check result.</returns>
    Task<HealthCheckResult> CheckHealthAsync(string deploymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Configures health check parameters for a deployment.
    /// </summary>
    /// <param name="deploymentId">Unique identifier of the deployment.</param>
    /// <param name="configuration">Health check configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task representing the configuration operation.</returns>
    Task ConfigureHealthCheckAsync(string deploymentId, HealthCheckConfiguration configuration, CancellationToken cancellationToken = default);
}

/// <summary>
/// Validation result for deployment configurations.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Whether the validation passed.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors if any.
    /// </summary>
    public List<ValidationError> Errors { get; init; } = new();

    /// <summary>
    /// Validation warnings if any.
    /// </summary>
    public List<ValidationWarning> Warnings { get; init; } = new();

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>Valid validation result.</returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with errors.
    /// </summary>
    /// <param name="errors">Validation errors.</param>
    /// <returns>Invalid validation result.</returns>
    public static ValidationResult Failure(params ValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Validation error information.
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Property or field that failed validation.
    /// </summary>
    public required string Property { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public string? Code { get; init; }
}

/// <summary>
/// Validation warning information.
/// </summary>
public record ValidationWarning
{
    /// <summary>
    /// Property or field that triggered the warning.
    /// </summary>
    public required string Property { get; init; }

    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Warning code for programmatic handling.
    /// </summary>
    public string? Code { get; init; }
}
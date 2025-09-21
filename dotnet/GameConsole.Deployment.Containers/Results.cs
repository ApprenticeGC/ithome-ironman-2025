namespace GameConsole.Deployment.Containers;

/// <summary>
/// Result of a deployment operation.
/// </summary>
public record DeploymentResult
{
    /// <summary>
    /// Unique identifier of the deployment.
    /// </summary>
    public required string DeploymentId { get; init; }

    /// <summary>
    /// Success status of the deployment.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Status message or error description.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Timestamp of the deployment operation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional metadata about the deployment.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// Deployed endpoints and services.
    /// </summary>
    public List<ServiceEndpoint> Endpoints { get; init; } = new();
}

/// <summary>
/// Result of a scaling operation.
/// </summary>
public record ScalingResult
{
    /// <summary>
    /// Unique identifier of the deployment.
    /// </summary>
    public required string DeploymentId { get; init; }

    /// <summary>
    /// Success status of the scaling operation.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Previous number of replicas.
    /// </summary>
    public required int PreviousReplicas { get; init; }

    /// <summary>
    /// Target number of replicas.
    /// </summary>
    public required int TargetReplicas { get; init; }

    /// <summary>
    /// Current number of replicas.
    /// </summary>
    public required int CurrentReplicas { get; init; }

    /// <summary>
    /// Status message or error description.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Timestamp of the scaling operation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Generic operation result.
/// </summary>
public record OperationResult
{
    /// <summary>
    /// Success status of the operation.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Status message or error description.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Timestamp of the operation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional operation metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Blue-green deployment operation result.
/// </summary>
public record BlueGreenResult
{
    /// <summary>
    /// Unique identifier of the blue-green deployment.
    /// </summary>
    public required string DeploymentId { get; init; }

    /// <summary>
    /// Blue deployment result.
    /// </summary>
    public required DeploymentResult BlueDeployment { get; init; }

    /// <summary>
    /// Green deployment result.
    /// </summary>
    public required DeploymentResult GreenDeployment { get; init; }

    /// <summary>
    /// Current active deployment (blue or green).
    /// </summary>
    public required string ActiveDeployment { get; init; }

    /// <summary>
    /// Traffic split percentage (0-100).
    /// </summary>
    public required int TrafficSplitPercentage { get; init; }

    /// <summary>
    /// Success status of the blue-green deployment.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Status message or error description.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Timestamp of the operation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Deployment status information.
/// </summary>
public record DeploymentStatus
{
    /// <summary>
    /// Unique identifier of the deployment.
    /// </summary>
    public required string DeploymentId { get; init; }

    /// <summary>
    /// Current phase of the deployment.
    /// </summary>
    public required DeploymentPhase Phase { get; init; }

    /// <summary>
    /// Number of desired replicas.
    /// </summary>
    public required int DesiredReplicas { get; init; }

    /// <summary>
    /// Number of current replicas.
    /// </summary>
    public required int CurrentReplicas { get; init; }

    /// <summary>
    /// Number of ready replicas.
    /// </summary>
    public required int ReadyReplicas { get; init; }

    /// <summary>
    /// Number of available replicas.
    /// </summary>
    public required int AvailableReplicas { get; init; }

    /// <summary>
    /// Health status of the deployment.
    /// </summary>
    public required HealthStatus HealthStatus { get; init; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Status conditions and events.
    /// </summary>
    public List<StatusCondition> Conditions { get; init; } = new();
}

/// <summary>
/// Deployment information summary.
/// </summary>
public record DeploymentInfo
{
    /// <summary>
    /// Unique identifier of the deployment.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Name of the deployment.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Container image being deployed.
    /// </summary>
    public required string Image { get; init; }

    /// <summary>
    /// Current deployment phase.
    /// </summary>
    public required DeploymentPhase Phase { get; init; }

    /// <summary>
    /// Number of desired replicas.
    /// </summary>
    public required int DesiredReplicas { get; init; }

    /// <summary>
    /// Number of ready replicas.
    /// </summary>
    public required int ReadyReplicas { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Service endpoints.
    /// </summary>
    public List<ServiceEndpoint> Endpoints { get; init; } = new();
}

/// <summary>
/// Service endpoint information.
/// </summary>
public record ServiceEndpoint
{
    /// <summary>
    /// Name of the endpoint.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Protocol (HTTP, HTTPS, TCP, UDP).
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// Host or IP address.
    /// </summary>
    public required string Host { get; init; }

    /// <summary>
    /// Port number.
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// Optional path for HTTP endpoints.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Full URL of the endpoint.
    /// </summary>
    public string Url => Path != null 
        ? $"{Protocol.ToLower()}://{Host}:{Port}{Path}" 
        : $"{Protocol.ToLower()}://{Host}:{Port}";
}

/// <summary>
/// Health status information.
/// </summary>
public record HealthStatus
{
    /// <summary>
    /// Overall health status.
    /// </summary>
    public required HealthState Status { get; init; }

    /// <summary>
    /// Health check results for individual components.
    /// </summary>
    public List<ComponentHealth> Components { get; init; } = new();

    /// <summary>
    /// Last health check timestamp.
    /// </summary>
    public DateTimeOffset LastChecked { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Health status message.
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Health check result.
/// </summary>
public record HealthCheckResult
{
    /// <summary>
    /// Health status.
    /// </summary>
    public required HealthState Status { get; init; }

    /// <summary>
    /// Health check message or error.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Response time of the health check.
    /// </summary>
    public TimeSpan ResponseTime { get; init; }

    /// <summary>
    /// Timestamp of the health check.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional health check data.
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// Component health information.
/// </summary>
public record ComponentHealth
{
    /// <summary>
    /// Name of the component.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Health status of the component.
    /// </summary>
    public required HealthState Status { get; init; }

    /// <summary>
    /// Component health message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Last check timestamp.
    /// </summary>
    public DateTimeOffset LastChecked { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Service mesh metrics data.
/// </summary>
public record ServiceMeshMetrics
{
    /// <summary>
    /// Request rate (requests per second).
    /// </summary>
    public double RequestRate { get; init; }

    /// <summary>
    /// Error rate percentage.
    /// </summary>
    public double ErrorRate { get; init; }

    /// <summary>
    /// Average response time in milliseconds.
    /// </summary>
    public double ResponseTime { get; init; }

    /// <summary>
    /// Success rate percentage.
    /// </summary>
    public double SuccessRate { get; init; }

    /// <summary>
    /// Timestamp of the metrics.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional custom metrics.
    /// </summary>
    public Dictionary<string, double> CustomMetrics { get; init; } = new();
}

/// <summary>
/// Status condition information.
/// </summary>
public record StatusCondition
{
    /// <summary>
    /// Type of condition.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Status of the condition.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Reason for the condition.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Human-readable message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Last transition time.
    /// </summary>
    public DateTimeOffset LastTransitionTime { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Log entry information.
/// </summary>
public record LogEntry
{
    /// <summary>
    /// Log timestamp.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Log level (INFO, WARN, ERROR, etc.).
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// Log message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Source component or container.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Additional log metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}

/// <summary>
/// Deployment phases.
/// </summary>
public enum DeploymentPhase
{
    /// <summary>
    /// Deployment is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// Deployment is in progress.
    /// </summary>
    Progressing,

    /// <summary>
    /// Deployment completed successfully.
    /// </summary>
    Complete,

    /// <summary>
    /// Deployment failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Deployment is being scaled.
    /// </summary>
    Scaling,

    /// <summary>
    /// Deployment is being removed.
    /// </summary>
    Terminating
}

/// <summary>
/// Health states.
/// </summary>
public enum HealthState
{
    /// <summary>
    /// Health status is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// Component is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// Component is unhealthy.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Component is starting up.
    /// </summary>
    Starting,

    /// <summary>
    /// Component is degraded but functional.
    /// </summary>
    Degraded
}
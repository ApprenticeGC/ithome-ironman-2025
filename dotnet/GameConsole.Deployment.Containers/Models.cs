namespace GameConsole.Deployment.Containers;

/// <summary>
/// Configuration for container deployment.
/// </summary>
public record DeploymentConfiguration
{
    /// <summary>
    /// Unique identifier for the deployment.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Name of the deployment.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Container image to deploy.
    /// </summary>
    public required string Image { get; init; }

    /// <summary>
    /// Image tag or version.
    /// </summary>
    public string Tag { get; init; } = "latest";

    /// <summary>
    /// Number of replicas to deploy.
    /// </summary>
    public int Replicas { get; init; } = 1;

    /// <summary>
    /// Environment variables for the container.
    /// </summary>
    public Dictionary<string, string> Environment { get; init; } = new();

    /// <summary>
    /// Port mappings for the container.
    /// </summary>
    public List<PortMapping> Ports { get; init; } = new();

    /// <summary>
    /// Volume mounts for the container.
    /// </summary>
    public List<VolumeMount> Volumes { get; init; } = new();

    /// <summary>
    /// Resource requirements and limits.
    /// </summary>
    public ResourceConfiguration Resources { get; init; } = new();

    /// <summary>
    /// Health check configuration.
    /// </summary>
    public HealthCheckConfiguration? HealthCheck { get; init; }

    /// <summary>
    /// Labels and metadata for the deployment.
    /// </summary>
    public Dictionary<string, string> Labels { get; init; } = new();
}

/// <summary>
/// Port mapping configuration.
/// </summary>
public record PortMapping
{
    /// <summary>
    /// Container port.
    /// </summary>
    public required int ContainerPort { get; init; }

    /// <summary>
    /// Host port (optional for auto-assignment).
    /// </summary>
    public int? HostPort { get; init; }

    /// <summary>
    /// Protocol (TCP/UDP).
    /// </summary>
    public string Protocol { get; init; } = "TCP";

    /// <summary>
    /// Name of the port mapping.
    /// </summary>
    public string? Name { get; init; }
}

/// <summary>
/// Volume mount configuration.
/// </summary>
public record VolumeMount
{
    /// <summary>
    /// Source path on the host.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Target path in the container.
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Mount type (bind, volume, tmpfs).
    /// </summary>
    public string Type { get; init; } = "bind";

    /// <summary>
    /// Read-only mount.
    /// </summary>
    public bool ReadOnly { get; init; }
}

/// <summary>
/// Resource configuration for containers.
/// </summary>
public record ResourceConfiguration
{
    /// <summary>
    /// CPU limits in millicores (e.g., 1000 = 1 CPU).
    /// </summary>
    public int? CpuLimit { get; init; }

    /// <summary>
    /// CPU requests in millicores.
    /// </summary>
    public int? CpuRequest { get; init; }

    /// <summary>
    /// Memory limits in bytes.
    /// </summary>
    public long? MemoryLimit { get; init; }

    /// <summary>
    /// Memory requests in bytes.
    /// </summary>
    public long? MemoryRequest { get; init; }
}

/// <summary>
/// Health check configuration.
/// </summary>
public record HealthCheckConfiguration
{
    /// <summary>
    /// Health check endpoint or command.
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Interval between health checks.
    /// </summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Timeout for each health check.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Number of consecutive failures before marking unhealthy.
    /// </summary>
    public int FailureThreshold { get; init; } = 3;

    /// <summary>
    /// Number of consecutive successes before marking healthy.
    /// </summary>
    public int SuccessThreshold { get; init; } = 1;

    /// <summary>
    /// Initial delay before starting health checks.
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Service mesh configuration.
/// </summary>
public record ServiceMeshConfiguration
{
    /// <summary>
    /// Enable service mesh sidecar injection.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Service mesh type (Istio, Linkerd, etc.).
    /// </summary>
    public string MeshType { get; init; } = "Istio";

    /// <summary>
    /// Traffic policy configuration.
    /// </summary>
    public TrafficPolicy? TrafficPolicy { get; init; }

    /// <summary>
    /// Security policy configuration.
    /// </summary>
    public SecurityPolicy? SecurityPolicy { get; init; }
}

/// <summary>
/// Traffic policy configuration for service mesh.
/// </summary>
public record TrafficPolicy
{
    /// <summary>
    /// Load balancing strategy.
    /// </summary>
    public string LoadBalancer { get; init; } = "ROUND_ROBIN";

    /// <summary>
    /// Circuit breaker configuration.
    /// </summary>
    public CircuitBreakerConfig? CircuitBreaker { get; init; }

    /// <summary>
    /// Retry policy configuration.
    /// </summary>
    public RetryPolicy? Retry { get; init; }
}

/// <summary>
/// Circuit breaker configuration.
/// </summary>
public record CircuitBreakerConfig
{
    /// <summary>
    /// Maximum number of requests per connection pool.
    /// </summary>
    public int MaxRequests { get; init; } = 100;

    /// <summary>
    /// Maximum number of connections.
    /// </summary>
    public int MaxConnections { get; init; } = 10;

    /// <summary>
    /// Request timeout.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Retry policy configuration.
/// </summary>
public record RetryPolicy
{
    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int Attempts { get; init; } = 3;

    /// <summary>
    /// Timeout per retry attempt.
    /// </summary>
    public TimeSpan PerTryTimeout { get; init; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Security policy configuration.
/// </summary>
public record SecurityPolicy
{
    /// <summary>
    /// Enable mutual TLS.
    /// </summary>
    public bool MutualTls { get; init; } = true;

    /// <summary>
    /// Authorization policy.
    /// </summary>
    public string? AuthorizationPolicy { get; init; }
}

/// <summary>
/// Metrics query parameters.
/// </summary>
public record MetricsQuery
{
    /// <summary>
    /// Start time for metrics query.
    /// </summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    /// End time for metrics query.
    /// </summary>
    public DateTimeOffset EndTime { get; init; }

    /// <summary>
    /// Metrics to retrieve.
    /// </summary>
    public List<string> Metrics { get; init; } = new();

    /// <summary>
    /// Aggregation function (avg, sum, max, min).
    /// </summary>
    public string Aggregation { get; init; } = "avg";
}

/// <summary>
/// Log retrieval options.
/// </summary>
public record LogOptions
{
    /// <summary>
    /// Number of log lines to retrieve.
    /// </summary>
    public int? Lines { get; init; }

    /// <summary>
    /// Start time for log retrieval.
    /// </summary>
    public DateTimeOffset? Since { get; init; }

    /// <summary>
    /// Follow log output (streaming).
    /// </summary>
    public bool Follow { get; init; }

    /// <summary>
    /// Include timestamps in log output.
    /// </summary>
    public bool Timestamps { get; init; } = true;
}
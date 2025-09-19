namespace GameConsole.Deployment.Containers;

/// <summary>
/// Configuration for container deployment.
/// </summary>
public record ContainerConfiguration
{
    /// <summary>
    /// Gets or sets the container image name and tag.
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port mappings for the container.
    /// </summary>
    public Dictionary<int, int> PortMappings { get; set; } = new();

    /// <summary>
    /// Gets or sets the environment variables for the container.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    /// Gets or sets the volume mounts for the container.
    /// </summary>
    public Dictionary<string, string> VolumeMounts { get; set; } = new();

    /// <summary>
    /// Gets or sets the resource limits for the container.
    /// </summary>
    public ResourceLimits? ResourceLimits { get; set; }

    /// <summary>
    /// Gets or sets the health check configuration.
    /// </summary>
    public HealthCheckConfiguration? HealthCheck { get; set; }

    /// <summary>
    /// Gets or sets the number of replicas to deploy.
    /// </summary>
    public int Replicas { get; set; } = 1;

    /// <summary>
    /// Gets or sets the deployment strategy.
    /// </summary>
    public DeploymentStrategy Strategy { get; set; } = DeploymentStrategy.RollingUpdate;

    /// <summary>
    /// Gets or sets labels to apply to the container/service.
    /// </summary>
    public Dictionary<string, string> Labels { get; set; } = new();

    /// <summary>
    /// Gets or sets additional configuration specific to the orchestrator.
    /// </summary>
    public Dictionary<string, object> AdditionalConfiguration { get; set; } = new();
}

/// <summary>
/// Resource limits for containers.
/// </summary>
public class ResourceLimits
{
    /// <summary>
    /// Gets or sets the CPU limit (in CPU units, e.g., 0.5 for half a CPU).
    /// </summary>
    public double? CpuLimit { get; set; }

    /// <summary>
    /// Gets or sets the memory limit in bytes.
    /// </summary>
    public long? MemoryLimit { get; set; }

    /// <summary>
    /// Gets or sets the CPU request (minimum guaranteed CPU).
    /// </summary>
    public double? CpuRequest { get; set; }

    /// <summary>
    /// Gets or sets the memory request in bytes (minimum guaranteed memory).
    /// </summary>
    public long? MemoryRequest { get; set; }
}

/// <summary>
/// Health check configuration for containers.
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// Gets or sets the health check endpoint path.
    /// </summary>
    public string Path { get; set; } = "/health";

    /// <summary>
    /// Gets or sets the port to use for health checks.
    /// </summary>
    public int Port { get; set; } = 80;

    /// <summary>
    /// Gets or sets the interval between health checks.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the timeout for each health check.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the number of consecutive failed health checks before marking unhealthy.
    /// </summary>
    public int FailureThreshold { get; set; } = 3;

    /// <summary>
    /// Gets or sets the number of consecutive successful health checks after being unhealthy.
    /// </summary>
    public int SuccessThreshold { get; set; } = 1;

    /// <summary>
    /// Gets or sets the initial delay before starting health checks.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Deployment strategy options.
/// </summary>
public enum DeploymentStrategy
{
    /// <summary>
    /// Rolling update strategy - gradually replace instances.
    /// </summary>
    RollingUpdate,

    /// <summary>
    /// Blue-green deployment strategy - deploy new version alongside old, then switch.
    /// </summary>
    BlueGreen,

    /// <summary>
    /// Recreate strategy - terminate all old instances before creating new ones.
    /// </summary>
    Recreate,

    /// <summary>
    /// Canary deployment strategy - deploy small percentage of new version first.
    /// </summary>
    Canary
}
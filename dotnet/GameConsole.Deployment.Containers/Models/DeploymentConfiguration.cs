namespace GameConsole.Deployment.Containers.Models;

/// <summary>
/// Configuration for container deployments specifying image, resources, and environment.
/// </summary>
public class DeploymentConfiguration
{
    /// <summary>
    /// Name of the deployment.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Container image to deploy.
    /// </summary>
    public string Image { get; set; } = string.Empty;

    /// <summary>
    /// Number of replicas/instances to deploy.
    /// </summary>
    public int Replicas { get; set; } = 1;

    /// <summary>
    /// Environment variables for the container.
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>
    /// Port mappings (host port -> container port).
    /// </summary>
    public Dictionary<int, int> PortMappings { get; set; } = new();

    /// <summary>
    /// Labels to apply to the deployment.
    /// </summary>
    public Dictionary<string, string> Labels { get; set; } = new();

    /// <summary>
    /// Resource limits for the deployment.
    /// </summary>
    public ResourceLimits? ResourceLimits { get; set; }

    /// <summary>
    /// Health check configuration.
    /// </summary>
    public HealthCheckConfiguration? HealthCheck { get; set; }

    /// <summary>
    /// Additional metadata for the deployment.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Resource limits for container deployment.
/// </summary>
public class ResourceLimits
{
    /// <summary>
    /// CPU limit in millicores (e.g., 1000 = 1 CPU core).
    /// </summary>
    public int? CpuLimit { get; set; }

    /// <summary>
    /// Memory limit in MB.
    /// </summary>
    public int? MemoryLimit { get; set; }

    /// <summary>
    /// CPU request in millicores.
    /// </summary>
    public int? CpuRequest { get; set; }

    /// <summary>
    /// Memory request in MB.
    /// </summary>
    public int? MemoryRequest { get; set; }
}

/// <summary>
/// Health check configuration for containers.
/// </summary>
public class HealthCheckConfiguration
{
    /// <summary>
    /// Health check endpoint path.
    /// </summary>
    public string Path { get; set; } = "/health";

    /// <summary>
    /// Port to use for health checks.
    /// </summary>
    public int Port { get; set; } = 80;

    /// <summary>
    /// Initial delay before starting health checks.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Interval between health checks.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Timeout for each health check.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Number of consecutive failures before marking unhealthy.
    /// </summary>
    public int FailureThreshold { get; set; } = 3;
}
namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Represents the context and configuration for a deployment operation.
/// </summary>
public class DeploymentContext
{
    /// <summary>
    /// Gets or sets the unique identifier for this deployment.
    /// </summary>
    public string DeploymentId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name or identifier of the artifact being deployed.
    /// </summary>
    public string ArtifactName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the artifact being deployed.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target environment for deployment.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment strategy to use.
    /// </summary>
    public DeploymentStrategy Strategy { get; set; } = DeploymentStrategy.Rolling;

    /// <summary>
    /// Gets or sets the configuration parameters for this deployment.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the user or system that initiated this deployment.
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the deployment was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets custom metadata for this deployment.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
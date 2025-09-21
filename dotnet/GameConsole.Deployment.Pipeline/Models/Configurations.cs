namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Configuration for a deployment operation.
/// </summary>
public class DeploymentConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for this deployment.
    /// </summary>
    public required string DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets the name of the deployment.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the version being deployed.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets the target environment for the deployment.
    /// </summary>
    public required string TargetEnvironment { get; set; }

    /// <summary>
    /// Gets or sets the deployment stages to execute.
    /// </summary>
    public List<StageConfig> Stages { get; set; } = new();

    /// <summary>
    /// Gets or sets the deployment provider configuration.
    /// </summary>
    public Dictionary<string, object> ProviderConfig { get; set; } = new();

    /// <summary>
    /// Gets or sets the rollback configuration.
    /// </summary>
    public RollbackConfig? RollbackConfig { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the deployment.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout for the entire deployment operation.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Configuration for a deployment stage.
/// </summary>
public class StageConfig
{
    /// <summary>
    /// Gets or sets the unique identifier for this stage.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the stage.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the execution order for this stage.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets whether this stage requires manual approval.
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Gets or sets the target environment for this stage.
    /// </summary>
    public required string TargetEnvironment { get; set; }

    /// <summary>
    /// Gets or sets the workflow configuration for this stage.
    /// </summary>
    public WorkflowConfig? WorkflowConfig { get; set; }

    /// <summary>
    /// Gets or sets stage-specific configuration parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout for this stage.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Gets or sets the health check configuration for this stage.
    /// </summary>
    public HealthCheckConfig? HealthCheck { get; set; }
}

/// <summary>
/// Configuration for workflow execution.
/// </summary>
public class WorkflowConfig
{
    /// <summary>
    /// Gets or sets the workflow name or identifier.
    /// </summary>
    public required string WorkflowId { get; set; }

    /// <summary>
    /// Gets or sets the provider type (e.g., "GitHubActions", "AzureDevOps", "Jenkins").
    /// </summary>
    public required string ProviderType { get; set; }

    /// <summary>
    /// Gets or sets the repository or project context.
    /// </summary>
    public required string Repository { get; set; }

    /// <summary>
    /// Gets or sets the branch or reference to deploy.
    /// </summary>
    public required string Reference { get; set; }

    /// <summary>
    /// Gets or sets workflow input parameters.
    /// </summary>
    public Dictionary<string, object> Inputs { get; set; } = new();

    /// <summary>
    /// Gets or sets environment variables for the workflow.
    /// </summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>
    /// Gets or sets authentication configuration.
    /// </summary>
    public Dictionary<string, string> Authentication { get; set; } = new();
}
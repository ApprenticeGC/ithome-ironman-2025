namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Configuration for rollback operations.
/// </summary>
public class RollbackConfig
{
    /// <summary>
    /// Gets or sets the rollback identifier.
    /// </summary>
    public required string RollbackId { get; set; }

    /// <summary>
    /// Gets or sets the deployment to rollback.
    /// </summary>
    public required string DeploymentId { get; set; }

    /// <summary>
    /// Gets or sets the target version to rollback to.
    /// </summary>
    public string? TargetVersion { get; set; }

    /// <summary>
    /// Gets or sets the reason for the rollback.
    /// </summary>
    public required string Reason { get; set; }

    /// <summary>
    /// Gets or sets whether to enable automatic rollback triggers.
    /// </summary>
    public bool EnableAutoRollback { get; set; } = true;

    /// <summary>
    /// Gets or sets rollback trigger configuration.
    /// </summary>
    public RollbackTriggers? Triggers { get; set; }

    /// <summary>
    /// Gets or sets the timeout for rollback operations.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Configuration for automatic rollback triggers.
/// </summary>
public class RollbackTriggers
{
    /// <summary>
    /// Gets or sets whether to rollback on health check failures.
    /// </summary>
    public bool OnHealthCheckFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to rollback on deployment errors.
    /// </summary>
    public bool OnDeploymentError { get; set; } = true;

    /// <summary>
    /// Gets or sets the error rate threshold for triggering rollback.
    /// </summary>
    public double ErrorRateThreshold { get; set; } = 0.05; // 5%

    /// <summary>
    /// Gets or sets the time window for monitoring error rates.
    /// </summary>
    public TimeSpan MonitoringWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets custom rollback conditions.
    /// </summary>
    public List<string> CustomConditions { get; set; } = new();
}

/// <summary>
/// Configuration for health checks during deployment.
/// </summary>
public class HealthCheckConfig
{
    /// <summary>
    /// Gets or sets whether health checks are enabled for this stage.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the health check endpoint URLs.
    /// </summary>
    public List<string> Endpoints { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout for individual health checks.
    /// </summary>
    public TimeSpan CheckTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the interval between health check attempts.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the number of retries for failed health checks.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the success criteria for health checks.
    /// </summary>
    public HealthCheckCriteria SuccessCriteria { get; set; } = new();
}

/// <summary>
/// Success criteria for health check validation.
/// </summary>
public class HealthCheckCriteria
{
    /// <summary>
    /// Gets or sets the required success rate for health checks.
    /// </summary>
    public double RequiredSuccessRate { get; set; } = 1.0; // 100%

    /// <summary>
    /// Gets or sets the minimum number of successful checks required.
    /// </summary>
    public int MinSuccessfulChecks { get; set; } = 1;

    /// <summary>
    /// Gets or sets the expected HTTP status codes for success.
    /// </summary>
    public List<int> ExpectedStatusCodes { get; set; } = new() { 200, 204 };

    /// <summary>
    /// Gets or sets custom validation rules for health check responses.
    /// </summary>
    public List<string> ValidationRules { get; set; } = new();
}
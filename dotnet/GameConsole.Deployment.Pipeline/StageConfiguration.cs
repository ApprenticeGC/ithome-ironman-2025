namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Configuration for a deployment stage, including approval gates and validation rules.
/// </summary>
public class StageConfiguration
{
    /// <summary>
    /// Gets or sets the deployment stage this configuration applies to.
    /// </summary>
    public DeploymentStage Stage { get; set; }

    /// <summary>
    /// Gets or sets the display name for this stage.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this stage requires manual approval before proceeding.
    /// </summary>
    public bool RequiresApproval { get; set; }

    /// <summary>
    /// Gets or sets the list of users or roles that can approve this stage.
    /// </summary>
    public List<string> Approvers { get; set; } = new();

    /// <summary>
    /// Gets or sets the timeout for this stage in minutes.
    /// </summary>
    public int TimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether this stage can be skipped on failure.
    /// </summary>
    public bool AllowSkipOnFailure { get; set; }

    /// <summary>
    /// Gets or sets the retry policy for this stage.
    /// </summary>
    public RetryPolicy RetryPolicy { get; set; } = new();

    /// <summary>
    /// Gets or sets the validation tests to run during this stage.
    /// </summary>
    public List<ValidationTest> ValidationTests { get; set; } = new();

    /// <summary>
    /// Gets or sets custom configuration for this stage.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Defines retry behavior for deployment stages.
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts in seconds.
    /// </summary>
    public int DelaySeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to use exponential backoff for retry delays.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}

/// <summary>
/// Defines a validation test to be executed during deployment.
/// </summary>
public class ValidationTest
{
    /// <summary>
    /// Gets or sets the name of this validation test.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of validation test.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration for this test.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets whether this test is required for the deployment to succeed.
    /// </summary>
    public bool IsRequired { get; set; } = true;
}
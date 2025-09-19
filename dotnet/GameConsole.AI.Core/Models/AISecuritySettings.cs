namespace GameConsole.AI.Models;

/// <summary>
/// Represents security settings for AI execution contexts.
/// </summary>
public class AISecuritySettings
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable sandboxing for AI operations.
    /// </summary>
    public bool EnableSandboxing { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum execution time allowed for operations.
    /// </summary>
    public TimeSpan MaxExecutionTime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum memory allocation allowed.
    /// </summary>
    public long MaxMemoryAllocation { get; set; } = 1024 * 1024 * 1024; // 1 GB

    /// <summary>
    /// Gets or sets the list of allowed operations.
    /// </summary>
    public IList<string> AllowedOperations { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of blocked operations.
    /// </summary>
    public IList<string> BlockedOperations { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets a value indicating whether to enable network access.
    /// </summary>
    public bool EnableNetworkAccess { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enable file system access.
    /// </summary>
    public bool EnableFileSystemAccess { get; set; } = false;

    /// <summary>
    /// Gets or sets the allowed file system paths (if file system access is enabled).
    /// </summary>
    public IList<string> AllowedFilePaths { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets additional security-specific settings.
    /// </summary>
    public IDictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents settings for initializing an AI context.
/// </summary>
public class AIContextSettings
{
    /// <summary>
    /// Gets or sets the resource requirements for the context.
    /// </summary>
    public AIResourceRequirements ResourceRequirements { get; set; } = new AIResourceRequirements();

    /// <summary>
    /// Gets or sets the security settings for the context.
    /// </summary>
    public AISecuritySettings SecuritySettings { get; set; } = new AISecuritySettings();

    /// <summary>
    /// Gets or sets the performance monitoring settings.
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval for performance metrics collection.
    /// </summary>
    public TimeSpan PerformanceMonitoringInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets a value indicating whether to enable logging.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the logging level for AI operations.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Gets or sets additional context-specific settings.
    /// </summary>
    public IDictionary<string, object> AdditionalSettings { get; set; } = new Dictionary<string, object>();
}
namespace GameConsole.AI.Orchestration.Models;

/// <summary>
/// Represents a workflow definition that can be executed by the orchestration system.
/// </summary>
public record WorkflowDefinition
{
    /// <summary>
    /// Unique identifier for the workflow definition.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name for the workflow.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of what the workflow accomplishes.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The tasks that comprise this workflow.
    /// </summary>
    public required IReadOnlyList<TaskDefinition> Tasks { get; init; }

    /// <summary>
    /// Execution strategy for the workflow tasks.
    /// </summary>
    public WorkflowExecutionStrategy ExecutionStrategy { get; init; } = WorkflowExecutionStrategy.Sequential;

    /// <summary>
    /// Maximum timeout for the entire workflow execution.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Retry policy configuration for workflow failures.
    /// </summary>
    public RetryPolicy? RetryPolicy { get; init; }

    /// <summary>
    /// Resource requirements for the workflow.
    /// </summary>
    public ResourceRequirements? ResourceRequirements { get; init; }
}

/// <summary>
/// Represents a single task within a workflow.
/// </summary>
public record TaskDefinition
{
    /// <summary>
    /// Unique identifier for the task.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name for the task.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Type of AI agent or service to execute this task.
    /// </summary>
    public required string AgentType { get; init; }

    /// <summary>
    /// Input parameters for the task.
    /// </summary>
    public required IReadOnlyDictionary<string, object> Parameters { get; init; }

    /// <summary>
    /// Dependencies - tasks that must complete before this one can start.
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Maximum timeout for this task.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Priority level for task scheduling.
    /// </summary>
    public TaskPriority Priority { get; init; } = TaskPriority.Normal;
}

/// <summary>
/// Represents the result of a workflow execution.
/// </summary>
public record WorkflowResult
{
    /// <summary>
    /// Unique identifier for the workflow instance.
    /// </summary>
    public required string WorkflowId { get; init; }

    /// <summary>
    /// The workflow definition that was executed.
    /// </summary>
    public required WorkflowDefinition WorkflowDefinition { get; init; }

    /// <summary>
    /// Current status of the workflow.
    /// </summary>
    public required WorkflowStatus Status { get; init; }

    /// <summary>
    /// Results from individual tasks.
    /// </summary>
    public IReadOnlyDictionary<string, TaskResult> TaskResults { get; init; } = new Dictionary<string, TaskResult>();

    /// <summary>
    /// Aggregated final result if workflow completed successfully.
    /// </summary>
    public object? FinalResult { get; init; }

    /// <summary>
    /// Error information if workflow failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// When the workflow started.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// When the workflow completed or failed.
    /// </summary>
    public DateTime? EndTime { get; init; }

    /// <summary>
    /// Total execution duration.
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTime.UtcNow.Subtract(StartTime);
}

/// <summary>
/// Represents the result of a single task execution.
/// </summary>
public record TaskResult
{
    /// <summary>
    /// Unique identifier for the task.
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    /// Current status of the task.
    /// </summary>
    public required TaskStatus Status { get; init; }

    /// <summary>
    /// The result data from task execution.
    /// </summary>
    public object? Result { get; init; }

    /// <summary>
    /// Error message if the task failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// When the task started execution.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// When the task completed or failed.
    /// </summary>
    public DateTime? EndTime { get; init; }

    /// <summary>
    /// Task execution duration.
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? DateTime.UtcNow.Subtract(StartTime);
}

/// <summary>
/// Workflow execution status.
/// </summary>
public enum WorkflowStatus
{
    /// <summary>
    /// Workflow is waiting to be started.
    /// </summary>
    Pending,

    /// <summary>
    /// Workflow is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// Workflow completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Workflow failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Workflow was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Workflow is paused.
    /// </summary>
    Paused
}

/// <summary>
/// Task execution status.
/// </summary>
public enum TaskStatus
{
    /// <summary>
    /// Task is waiting to be executed.
    /// </summary>
    Pending,

    /// <summary>
    /// Task is currently executing.
    /// </summary>
    Running,

    /// <summary>
    /// Task completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Task failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Task was cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Task is waiting for dependencies.
    /// </summary>
    WaitingForDependencies
}

/// <summary>
/// Workflow execution strategies.
/// </summary>
public enum WorkflowExecutionStrategy
{
    /// <summary>
    /// Execute tasks one after another in sequence.
    /// </summary>
    Sequential,

    /// <summary>
    /// Execute all independent tasks in parallel.
    /// </summary>
    Parallel,

    /// <summary>
    /// Execute tasks based on their dependency graph.
    /// </summary>
    DependencyBased
}

/// <summary>
/// Task priority levels for scheduling.
/// </summary>
public enum TaskPriority
{
    /// <summary>
    /// Low priority task.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority task.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority task.
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority task.
    /// </summary>
    Critical = 3
}

/// <summary>
/// Retry policy configuration for handling failures.
/// </summary>
public record RetryPolicy
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Delay between retry attempts.
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether to use exponential backoff for retry delays.
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;

    /// <summary>
    /// Maximum delay between retries when using exponential backoff.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Resource requirements for workflows and tasks.
/// </summary>
public record ResourceRequirements
{
    /// <summary>
    /// Minimum CPU cores required.
    /// </summary>
    public int? MinCpuCores { get; init; }

    /// <summary>
    /// Minimum memory required in MB.
    /// </summary>
    public int? MinMemoryMB { get; init; }

    /// <summary>
    /// Maximum concurrent tasks allowed.
    /// </summary>
    public int? MaxConcurrentTasks { get; init; }

    /// <summary>
    /// Required agent types that must be available.
    /// </summary>
    public IReadOnlyList<string> RequiredAgentTypes { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Orchestration performance and operational metrics.
/// </summary>
public record OrchestrationMetrics
{
    /// <summary>
    /// Total number of workflows executed.
    /// </summary>
    public long TotalWorkflowsExecuted { get; init; }

    /// <summary>
    /// Number of workflows currently running.
    /// </summary>
    public int RunningWorkflows { get; init; }

    /// <summary>
    /// Number of workflows in the queue.
    /// </summary>
    public int QueuedWorkflows { get; init; }

    /// <summary>
    /// Average workflow execution time.
    /// </summary>
    public TimeSpan AverageWorkflowDuration { get; init; }

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate { get; init; }

    /// <summary>
    /// Current resource utilization.
    /// </summary>
    public ResourceUtilization ResourceUtilization { get; init; } = new();

    /// <summary>
    /// Timestamp when metrics were collected.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Information about available system resources.
/// </summary>
public record ResourceInfo
{
    /// <summary>
    /// Number of available CPU cores.
    /// </summary>
    public int AvailableCpuCores { get; init; }

    /// <summary>
    /// Available memory in MB.
    /// </summary>
    public long AvailableMemoryMB { get; init; }

    /// <summary>
    /// Currently active agent types and their counts.
    /// </summary>
    public IReadOnlyDictionary<string, int> AvailableAgents { get; init; } = new Dictionary<string, int>();

    /// <summary>
    /// Maximum concurrent workflows supported.
    /// </summary>
    public int MaxConcurrentWorkflows { get; init; }

    /// <summary>
    /// Current resource utilization.
    /// </summary>
    public ResourceUtilization Utilization { get; init; } = new();
}

/// <summary>
/// Current resource utilization metrics.
/// </summary>
public record ResourceUtilization
{
    /// <summary>
    /// CPU utilization as a percentage (0-100).
    /// </summary>
    public double CpuUtilization { get; init; }

    /// <summary>
    /// Memory utilization as a percentage (0-100).
    /// </summary>
    public double MemoryUtilization { get; init; }

    /// <summary>
    /// Number of active tasks currently executing.
    /// </summary>
    public int ActiveTasks { get; init; }

    /// <summary>
    /// Agent utilization by type.
    /// </summary>
    public IReadOnlyDictionary<string, double> AgentUtilization { get; init; } = new Dictionary<string, double>();
}
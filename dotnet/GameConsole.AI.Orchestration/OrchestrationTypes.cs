namespace GameConsole.AI.Orchestration.Services;

#region Workflow Types

/// <summary>
/// Configuration for creating an AI workflow.
/// </summary>
public class WorkflowConfiguration
{
    public string Name { get; set; } = string.Empty;
    public WorkflowType Type { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<WorkflowStep> Steps { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxRetries { get; set; } = 3;
    public bool EnablePersistence { get; set; } = false;
}

/// <summary>
/// Represents a step in an AI workflow.
/// </summary>
public class WorkflowStep
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
    public bool IsParallel { get; set; } = false;
}

/// <summary>
/// Result of workflow execution.
/// </summary>
public class WorkflowResult
{
    public string WorkflowId { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; }
    public object? Result { get; set; }
    public Exception? Error { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public DateTime CompletedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of AI workflows supported.
/// </summary>
public enum WorkflowType
{
    Sequential,
    Parallel,
    Conditional,
    Pipeline,
    MapReduce
}

/// <summary>
/// Status of workflow execution.
/// </summary>
public enum WorkflowStatus
{
    Created,
    Running,
    Completed,
    Failed,
    Cancelled,
    Paused
}

#endregion

#region Task Types

/// <summary>
/// Represents an AI task to be executed.
/// </summary>
public class AITask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public object Input { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxRetries { get; set; } = 2;
    public List<string> RequiredCapabilities { get; set; } = new();
}

/// <summary>
/// Priority levels for task scheduling.
/// </summary>
public enum TaskPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Status information for a scheduled task.
/// </summary>
public class TaskStatus
{
    public string TaskId { get; set; } = string.Empty;
    public TaskExecutionStatus Status { get; set; }
    public object? Result { get; set; }
    public Exception? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
    public TimeSpan? ExecutionTime { get; set; }
}

/// <summary>
/// Execution status of a task.
/// </summary>
public enum TaskExecutionStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Cancelled,
    Retry
}

#endregion

#region Resource Types

/// <summary>
/// Resource utilization metrics.
/// </summary>
public class ResourceMetrics
{
    public int ActiveTasks { get; set; }
    public int QueuedTasks { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public int AvailableAgents { get; set; }
    public int TotalAgents { get; set; }
    public Dictionary<string, int> AgentTypeDistribution { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Resource allocation request.
/// </summary>
public class ResourceRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string RequiredAgentType { get; set; } = string.Empty;
    public int RequiredInstances { get; set; } = 1;
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, object> Requirements { get; set; } = new();
}

/// <summary>
/// Result of resource allocation.
/// </summary>
public class ResourceAllocation
{
    public string RequestId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public List<string> AllocatedAgents { get; set; } = new();
    public TimeSpan WaitTime { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
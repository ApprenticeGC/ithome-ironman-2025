using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Orchestration.Services;

/// <summary>
/// Capability interface for workflow coordination operations.
/// </summary>
public interface IWorkflowCoordinatorCapability : ICapabilityProvider
{
    /// <summary>
    /// Orchestrates a complex AI workflow with multiple steps.
    /// </summary>
    /// <param name="workflow">Workflow configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Workflow execution result.</returns>
    Task<WorkflowResult> OrchestateWorkflowAsync(WorkflowConfiguration workflow, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Pauses a running workflow.
    /// </summary>
    /// <param name="workflowId">Workflow identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task PauseWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Resumes a paused workflow.
    /// </summary>
    /// <param name="workflowId">Workflow identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ResumeWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets workflow execution status.
    /// </summary>
    /// <param name="workflowId">Workflow identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Workflow status information.</returns>
    Task<WorkflowResult> GetWorkflowStatusAsync(string workflowId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for task scheduling and load balancing operations.
/// </summary>
public interface ITaskSchedulerCapability : ICapabilityProvider
{
    /// <summary>
    /// Distributes tasks across available AI agents based on load and capabilities.
    /// </summary>
    /// <param name="tasks">Collection of tasks to distribute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task distribution results.</returns>
    Task<IEnumerable<TaskDistributionResult>> DistributeTasksAsync(IEnumerable<AITask> tasks, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets available agents for task execution.
    /// </summary>
    /// <param name="agentType">Optional filter by agent type.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>List of available agents.</returns>
    Task<IEnumerable<AgentInfo>> GetAvailableAgentsAsync(string? agentType = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets current load balancing metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Load balancing metrics.</returns>
    Task<LoadBalancingMetrics> GetLoadMetricsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for aggregating AI results from multiple sources.
/// </summary>
public interface IResultAggregatorCapability : ICapabilityProvider
{
    /// <summary>
    /// Combines partial AI responses into a coherent result.
    /// </summary>
    /// <param name="partialResults">Collection of partial results to aggregate.</param>
    /// <param name="aggregationStrategy">Strategy for combining results.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Aggregated result.</returns>
    Task<object> AggregateResultsAsync(IEnumerable<object> partialResults, AggregationStrategy aggregationStrategy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates and filters results before aggregation.
    /// </summary>
    /// <param name="results">Results to validate.</param>
    /// <param name="validationCriteria">Validation criteria.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Valid results ready for aggregation.</returns>
    Task<IEnumerable<object>> ValidateResultsAsync(IEnumerable<object> results, ValidationCriteria validationCriteria, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets aggregation performance metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Aggregation metrics.</returns>
    Task<AggregationMetrics> GetAggregationMetricsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Capability interface for AI resource management and optimization.
/// </summary>
public interface IResourceManagerCapability : ICapabilityProvider
{
    /// <summary>
    /// Optimizes resource allocation based on current workload and requirements.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Resource optimization results.</returns>
    Task<ResourceOptimizationResult> OptimizeResourcesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Monitors resource health and performance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Resource health status.</returns>
    Task<ResourceHealthStatus> MonitorResourceHealthAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Scales resources up or down based on demand.
    /// </summary>
    /// <param name="scalingRequest">Scaling configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Scaling operation result.</returns>
    Task<ScalingResult> ScaleResourcesAsync(ScalingRequest scalingRequest, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Releases allocated resources.
    /// </summary>
    /// <param name="allocationId">Resource allocation identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ReleaseResourcesAsync(string allocationId, CancellationToken cancellationToken = default);
}

#region Supporting Types

/// <summary>
/// Result of task distribution operation.
/// </summary>
public class TaskDistributionResult
{
    public string TaskId { get; set; } = string.Empty;
    public string AssignedAgent { get; set; } = string.Empty;
    public TimeSpan EstimatedExecutionTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Information about an available agent.
/// </summary>
public class AgentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public AgentStatus Status { get; set; }
    public double CurrentLoad { get; set; }
    public List<string> Capabilities { get; set; } = new();
    public DateTime LastHeartbeat { get; set; }
}

/// <summary>
/// Status of an agent.
/// </summary>
public enum AgentStatus
{
    Available,
    Busy,
    Offline,
    Error
}

/// <summary>
/// Load balancing metrics.
/// </summary>
public class LoadBalancingMetrics
{
    public double AverageLoad { get; set; }
    public int TotalAgents { get; set; }
    public int ActiveAgents { get; set; }
    public Dictionary<string, double> LoadByAgentType { get; set; } = new();
    public int TasksInQueue { get; set; }
    public TimeSpan AverageWaitTime { get; set; }
}

/// <summary>
/// Strategies for aggregating results.
/// </summary>
public enum AggregationStrategy
{
    Merge,
    Consensus,
    WeightedAverage,
    BestResult,
    Custom
}

/// <summary>
/// Criteria for validating results.
/// </summary>
public class ValidationCriteria
{
    public double MinConfidenceScore { get; set; } = 0.5;
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(5);
    public List<string> RequiredFields { get; set; } = new();
    public Dictionary<string, object> CustomValidators { get; set; } = new();
}

/// <summary>
/// Metrics for result aggregation operations.
/// </summary>
public class AggregationMetrics
{
    public int TotalAggregations { get; set; }
    public int SuccessfulAggregations { get; set; }
    public TimeSpan AverageAggregationTime { get; set; }
    public double AverageResultQuality { get; set; }
    public Dictionary<AggregationStrategy, int> StrategyUsage { get; set; } = new();
}

/// <summary>
/// Result of resource optimization.
/// </summary>
public class ResourceOptimizationResult
{
    public bool OptimizationPerformed { get; set; }
    public double EfficiencyGain { get; set; }
    public List<string> OptimizationActions { get; set; } = new();
    public ResourceMetrics PostOptimizationMetrics { get; set; } = new();
}

/// <summary>
/// Health status of resources.
/// </summary>
public class ResourceHealthStatus
{
    public HealthLevel OverallHealth { get; set; }
    public Dictionary<string, HealthLevel> ComponentHealth { get; set; } = new();
    public List<string> HealthIssues { get; set; } = new();
    public DateTime LastHealthCheck { get; set; }
}

/// <summary>
/// Health levels for resources.
/// </summary>
public enum HealthLevel
{
    Healthy,
    Warning,
    Critical,
    Offline
}

/// <summary>
/// Request for scaling resources.
/// </summary>
public class ScalingRequest
{
    public string ResourceType { get; set; } = string.Empty;
    public ScalingDirection Direction { get; set; }
    public int TargetInstances { get; set; }
    public Dictionary<string, object> ScalingParameters { get; set; } = new();
}

/// <summary>
/// Direction for scaling operations.
/// </summary>
public enum ScalingDirection
{
    Up,
    Down,
    Maintain
}

/// <summary>
/// Result of scaling operation.
/// </summary>
public class ScalingResult
{
    public bool Success { get; set; }
    public int PreviousInstances { get; set; }
    public int CurrentInstances { get; set; }
    public TimeSpan ScalingTime { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
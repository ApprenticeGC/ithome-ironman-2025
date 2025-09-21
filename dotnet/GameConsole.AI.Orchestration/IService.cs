using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Orchestration.Services;

/// <summary>
/// Core AI orchestration service interface for workflow management and task coordination.
/// Provides unified interface for AI workflow orchestration using Akka.NET actors.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService, ICapabilityProvider
{
    #region Workflow Management
    
    /// <summary>
    /// Creates a new AI workflow with the specified configuration.
    /// </summary>
    /// <param name="workflowConfig">Workflow configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Workflow identifier.</returns>
    Task<string> CreateWorkflowAsync(WorkflowConfiguration workflowConfig, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a workflow asynchronously.
    /// </summary>
    /// <param name="workflowId">Workflow identifier.</param>
    /// <param name="input">Input data for the workflow.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Workflow execution result.</returns>
    Task<WorkflowResult> ExecuteWorkflowAsync(string workflowId, object input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops a running workflow.
    /// </summary>
    /// <param name="workflowId">Workflow identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task StopWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);
    
    #endregion

    #region Task Scheduling
    
    /// <summary>
    /// Schedules an AI task for execution.
    /// </summary>
    /// <param name="task">AI task to schedule.</param>
    /// <param name="priority">Task priority level.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task identifier.</returns>
    Task<string> ScheduleTaskAsync(AITask task, TaskPriority priority = TaskPriority.Normal, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the status of a scheduled task.
    /// </summary>
    /// <param name="taskId">Task identifier.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Task status information.</returns>
    Task<TaskStatus> GetTaskStatusAsync(string taskId, CancellationToken cancellationToken = default);
    
    #endregion

    #region Resource Management
    
    /// <summary>
    /// Gets current resource utilization metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Resource metrics.</returns>
    Task<ResourceMetrics> GetResourceMetricsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Allocates resources for AI processing.
    /// </summary>
    /// <param name="resourceRequest">Resource allocation request.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>Resource allocation result.</returns>
    Task<ResourceAllocation> AllocateResourcesAsync(ResourceRequest resourceRequest, CancellationToken cancellationToken = default);
    
    #endregion

    #region Capability Providers
    
    /// <summary>
    /// Gets the workflow coordinator capability.
    /// </summary>
    IWorkflowCoordinatorCapability? WorkflowCoordinator { get; }
    
    /// <summary>
    /// Gets the task scheduler capability.
    /// </summary>
    ITaskSchedulerCapability? TaskScheduler { get; }
    
    /// <summary>
    /// Gets the result aggregator capability.
    /// </summary>
    IResultAggregatorCapability? ResultAggregator { get; }
    
    /// <summary>
    /// Gets the resource manager capability.
    /// </summary>
    IResourceManagerCapability? ResourceManager { get; }
    
    #endregion
}
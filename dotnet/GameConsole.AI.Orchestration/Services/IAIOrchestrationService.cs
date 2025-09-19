namespace GameConsole.AI.Orchestration.Services;

using GameConsole.Core.Abstractions;
using GameConsole.AI.Orchestration.Models;

/// <summary>
/// Service interface for AI workflow orchestration and task coordination.
/// Provides capabilities for managing complex AI pipelines, load balancing,
/// result aggregation, and resource allocation.
/// </summary>
public interface IAIOrchestrationService : IService, ICapabilityProvider
{
    /// <summary>
    /// Executes a workflow asynchronously with the provided definition.
    /// </summary>
    /// <param name="workflowDefinition">The workflow to execute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the workflow execution result.</returns>
    Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a workflow asynchronously and streams intermediate results.
    /// </summary>
    /// <param name="workflowDefinition">The workflow to execute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable of intermediate workflow results.</returns>
    IAsyncEnumerable<WorkflowResult> ExecuteWorkflowStreamAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a running workflow.
    /// </summary>
    /// <param name="workflowId">The unique identifier of the workflow.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the workflow status.</returns>
    Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running workflow.
    /// </summary>
    /// <param name="workflowId">The unique identifier of the workflow to cancel.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async cancellation operation.</returns>
    Task<bool> CancelWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets performance metrics for workflow executions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns orchestration metrics.</returns>
    Task<OrchestrationMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about available resources for AI processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns resource information.</returns>
    Task<ResourceInfo> GetResourceInfoAsync(CancellationToken cancellationToken = default);
}
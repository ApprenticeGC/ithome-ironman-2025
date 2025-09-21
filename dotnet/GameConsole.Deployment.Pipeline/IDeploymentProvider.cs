using GameConsole.Core.Abstractions;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Defines a deployment provider interface for different CI/CD platforms.
/// Abstracts platform-specific deployment operations for GitHub Actions, Azure DevOps, Jenkins, etc.
/// </summary>
public interface IDeploymentProvider : IService
{
    /// <summary>
    /// Gets the provider name (e.g., "GitHub Actions", "Azure DevOps", "Jenkins").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the supported platform types for this provider.
    /// </summary>
    IReadOnlyCollection<string> SupportedPlatforms { get; }

    /// <summary>
    /// Triggers a deployment workflow on the target platform.
    /// </summary>
    /// <param name="workflowConfig">Configuration for the workflow execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the workflow execution result.</returns>
    Task<WorkflowResult> TriggerWorkflowAsync(WorkflowConfig workflowConfig, CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors the status of a running deployment workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the current workflow status.</returns>
    Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running deployment workflow.
    /// </summary>
    /// <param name="workflowId">The workflow identifier to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task indicating completion of the cancellation request.</returns>
    Task CancelWorkflowAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the provider can execute the specified workflow configuration.
    /// </summary>
    /// <param name="workflowConfig">Configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing validation results.</returns>
    Task<ValidationResult> ValidateWorkflowAsync(WorkflowConfig workflowConfig, CancellationToken cancellationToken = default);
}
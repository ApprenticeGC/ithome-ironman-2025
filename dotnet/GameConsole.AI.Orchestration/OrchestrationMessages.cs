using Akka.Actor;

namespace GameConsole.AI.Orchestration.Messages;

/// <summary>
/// Base class for all AI orchestration messages.
/// </summary>
public abstract record OrchestrationMessage;

#region Workflow Coordinator Messages

/// <summary>
/// Message to start workflow execution.
/// </summary>
/// <param name="WorkflowId">Unique workflow identifier.</param>
/// <param name="Configuration">Workflow configuration.</param>
/// <param name="Input">Input data for the workflow.</param>
/// <param name="Sender">Actor reference that sent the request.</param>
public record StartWorkflow(
    string WorkflowId,
    Services.WorkflowConfiguration Configuration,
    object Input,
    IActorRef Sender
) : OrchestrationMessage;

/// <summary>
/// Message indicating workflow execution has completed.
/// </summary>
/// <param name="WorkflowId">Workflow identifier.</param>
/// <param name="Result">Execution result.</param>
/// <param name="ExecutionTime">Time taken to execute.</param>
public record WorkflowCompleted(
    string WorkflowId,
    Services.WorkflowResult Result,
    TimeSpan ExecutionTime
) : OrchestrationMessage;

/// <summary>
/// Message indicating workflow execution has failed.
/// </summary>
/// <param name="WorkflowId">Workflow identifier.</param>
/// <param name="Error">Exception that caused the failure.</param>
/// <param name="RetryCount">Number of retry attempts made.</param>
public record WorkflowFailed(
    string WorkflowId,
    Exception Error,
    int RetryCount
) : OrchestrationMessage;

/// <summary>
/// Message to pause workflow execution.
/// </summary>
/// <param name="WorkflowId">Workflow identifier.</param>
public record PauseWorkflow(string WorkflowId) : OrchestrationMessage;

/// <summary>
/// Message to resume workflow execution.
/// </summary>
/// <param name="WorkflowId">Workflow identifier.</param>
public record ResumeWorkflow(string WorkflowId) : OrchestrationMessage;

/// <summary>
/// Message to stop workflow execution.
/// </summary>
/// <param name="WorkflowId">Workflow identifier.</param>
/// <param name="Reason">Reason for stopping the workflow.</param>
public record StopWorkflow(string WorkflowId, string Reason = "User requested") : OrchestrationMessage;

/// <summary>
/// Message to get workflow status.
/// </summary>
/// <param name="WorkflowId">Workflow identifier.</param>
/// <param name="Sender">Actor reference that sent the request.</param>
public record GetWorkflowStatus(string WorkflowId, IActorRef Sender) : OrchestrationMessage;

#endregion

#region Task Scheduler Messages

/// <summary>
/// Message to schedule a new AI task.
/// </summary>
/// <param name="Task">AI task to schedule.</param>
/// <param name="Priority">Task priority level.</param>
/// <param name="Sender">Actor reference that sent the request.</param>
public record ScheduleTask(
    Services.AITask Task,
    Services.TaskPriority Priority,
    IActorRef Sender
) : OrchestrationMessage;

/// <summary>
/// Message indicating task has been scheduled.
/// </summary>
/// <param name="TaskId">Task identifier.</param>
/// <param name="AssignedAgent">Agent assigned to execute the task.</param>
/// <param name="EstimatedStartTime">Estimated start time.</param>
public record TaskScheduled(
    string TaskId,
    string AssignedAgent,
    DateTime EstimatedStartTime
) : OrchestrationMessage;

/// <summary>
/// Message indicating task execution has started.
/// </summary>
/// <param name="TaskId">Task identifier.</param>
/// <param name="AgentId">Agent executing the task.</param>
/// <param name="StartTime">Task start time.</param>
public record TaskStarted(
    string TaskId,
    string AgentId,
    DateTime StartTime
) : OrchestrationMessage;

/// <summary>
/// Message indicating task execution has completed.
/// </summary>
/// <param name="TaskId">Task identifier.</param>
/// <param name="Result">Task execution result.</param>
/// <param name="ExecutionTime">Time taken to execute.</param>
public record TaskCompleted(
    string TaskId,
    object Result,
    TimeSpan ExecutionTime
) : OrchestrationMessage;

/// <summary>
/// Message indicating task execution has failed.
/// </summary>
/// <param name="TaskId">Task identifier.</param>
/// <param name="Error">Exception that caused the failure.</param>
/// <param name="RetryCount">Number of retry attempts made.</param>
public record TaskFailed(
    string TaskId,
    Exception Error,
    int RetryCount
) : OrchestrationMessage;

/// <summary>
/// Message to get task status.
/// </summary>
/// <param name="TaskId">Task identifier.</param>
/// <param name="Sender">Actor reference that sent the request.</param>
public record GetTaskStatus(string TaskId, IActorRef Sender) : OrchestrationMessage;

/// <summary>
/// Message to request load balancing metrics.
/// </summary>
/// <param name="Sender">Actor reference that sent the request.</param>
public record GetLoadMetrics(IActorRef Sender) : OrchestrationMessage;

#endregion

#region Result Aggregator Messages

/// <summary>
/// Message to aggregate partial results.
/// </summary>
/// <param name="AggregationId">Unique aggregation identifier.</param>
/// <param name="PartialResults">Collection of partial results.</param>
/// <param name="Strategy">Aggregation strategy to use.</param>
/// <param name="Sender">Actor reference that sent the request.</param>
public record AggregateResults(
    string AggregationId,
    IEnumerable<object> PartialResults,
    Services.AggregationStrategy Strategy,
    IActorRef Sender
) : OrchestrationMessage;

/// <summary>
/// Message indicating result aggregation has completed.
/// </summary>
/// <param name="AggregationId">Aggregation identifier.</param>
/// <param name="AggregatedResult">Final aggregated result.</param>
/// <param name="ProcessingTime">Time taken to aggregate.</param>
public record AggregationCompleted(
    string AggregationId,
    object AggregatedResult,
    TimeSpan ProcessingTime
) : OrchestrationMessage;

/// <summary>
/// Message indicating result aggregation has failed.
/// </summary>
/// <param name="AggregationId">Aggregation identifier.</param>
/// <param name="Error">Exception that caused the failure.</param>
public record AggregationFailed(
    string AggregationId,
    Exception Error
) : OrchestrationMessage;

/// <summary>
/// Message to add partial result to ongoing aggregation.
/// </summary>
/// <param name="AggregationId">Aggregation identifier.</param>
/// <param name="PartialResult">Partial result to add.</param>
/// <param name="Source">Source of the partial result.</param>
public record AddPartialResult(
    string AggregationId,
    object PartialResult,
    string Source
) : OrchestrationMessage;

#endregion

#region Resource Manager Messages

/// <summary>
/// Message to allocate resources.
/// </summary>
/// <param name="Request">Resource allocation request.</param>
/// <param name="Sender">Actor reference that sent the request.</param>
public record AllocateResources(
    Services.ResourceRequest Request,
    IActorRef Sender
) : OrchestrationMessage;

/// <summary>
/// Message indicating resources have been allocated.
/// </summary>
/// <param name="RequestId">Request identifier.</param>
/// <param name="Allocation">Resource allocation result.</param>
public record ResourcesAllocated(
    string RequestId,
    Services.ResourceAllocation Allocation
) : OrchestrationMessage;

/// <summary>
/// Message indicating resource allocation has failed.
/// </summary>
/// <param name="RequestId">Request identifier.</param>
/// <param name="Error">Reason for allocation failure.</param>
public record ResourceAllocationFailed(
    string RequestId,
    string Error
) : OrchestrationMessage;

/// <summary>
/// Message to release allocated resources.
/// </summary>
/// <param name="AllocationId">Allocation identifier.</param>
public record ReleaseResources(string AllocationId) : OrchestrationMessage;

/// <summary>
/// Message to get resource metrics.
/// </summary>
/// <param name="Sender">Actor reference that sent the request.</param>
public record GetResourceMetrics(IActorRef Sender) : OrchestrationMessage;

/// <summary>
/// Message to optimize resource allocation.
/// </summary>
/// <param name="Sender">Actor reference that sent the request.</param>
public record OptimizeResources(IActorRef Sender) : OrchestrationMessage;

/// <summary>
/// Message indicating agent is available for work.
/// </summary>
/// <param name="AgentId">Agent identifier.</param>
/// <param name="AgentType">Type of agent.</param>
/// <param name="Capabilities">Agent capabilities.</param>
public record AgentAvailable(
    string AgentId,
    string AgentType,
    List<string> Capabilities
) : OrchestrationMessage;

/// <summary>
/// Message indicating agent is no longer available.
/// </summary>
/// <param name="AgentId">Agent identifier.</param>
/// <param name="Reason">Reason for unavailability.</param>
public record AgentUnavailable(
    string AgentId,
    string Reason
) : OrchestrationMessage;

/// <summary>
/// Message with agent heartbeat information.
/// </summary>
/// <param name="AgentId">Agent identifier.</param>
/// <param name="CurrentLoad">Current load percentage.</param>
/// <param name="Status">Current agent status.</param>
/// <param name="Timestamp">Heartbeat timestamp.</param>
public record AgentHeartbeat(
    string AgentId,
    double CurrentLoad,
    Services.AgentStatus Status,
    DateTime Timestamp
) : OrchestrationMessage;

#endregion

#region Circuit Breaker Messages

/// <summary>
/// Message to open circuit breaker for a service.
/// </summary>
/// <param name="ServiceName">Name of the service.</param>
/// <param name="Reason">Reason for opening the circuit.</param>
public record OpenCircuitBreaker(string ServiceName, string Reason) : OrchestrationMessage;

/// <summary>
/// Message to close circuit breaker for a service.
/// </summary>
/// <param name="ServiceName">Name of the service.</param>
public record CloseCircuitBreaker(string ServiceName) : OrchestrationMessage;

/// <summary>
/// Message to transition circuit breaker to half-open state.
/// </summary>
/// <param name="ServiceName">Name of the service.</param>
public record HalfOpenCircuitBreaker(string ServiceName) : OrchestrationMessage;

/// <summary>
/// Message indicating circuit breaker state has changed.
/// </summary>
/// <param name="ServiceName">Name of the service.</param>
/// <param name="State">New circuit breaker state.</param>
/// <param name="Timestamp">Time of state change.</param>
public record CircuitBreakerStateChanged(
    string ServiceName,
    CircuitBreakerState State,
    DateTime Timestamp
) : OrchestrationMessage;

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}

#endregion
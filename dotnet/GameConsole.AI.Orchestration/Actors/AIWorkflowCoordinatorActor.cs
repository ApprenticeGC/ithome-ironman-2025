using Akka.Actor;
using Akka.Event;
using GameConsole.AI.Orchestration.Messages;
using GameConsole.AI.Orchestration.Services;

namespace GameConsole.AI.Orchestration.Actors;

/// <summary>
/// Actor responsible for coordinating AI workflow execution.
/// Manages complex multi-step workflows with support for parallel and sequential execution.
/// </summary>
public class AIWorkflowCoordinatorActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Dictionary<string, WorkflowState> _activeWorkflows = new();
    private readonly Dictionary<string, IActorRef> _workflowActors = new();

    public AIWorkflowCoordinatorActor()
    {
        Receive<StartWorkflow>(Handle);
        Receive<PauseWorkflow>(Handle);
        Receive<ResumeWorkflow>(Handle);
        Receive<StopWorkflow>(Handle);
        Receive<GetWorkflowStatus>(Handle);
        Receive<WorkflowCompleted>(Handle);
        Receive<WorkflowFailed>(Handle);
        Receive<Terminated>(Handle);
    }

    private void Handle(StartWorkflow message)
    {
        _log.Info("Starting workflow {WorkflowId} with {StepCount} steps", 
            message.WorkflowId, message.Configuration.Steps.Count);

        try
        {
            var workflowState = new WorkflowState
            {
                Id = message.WorkflowId,
                Configuration = message.Configuration,
                Status = WorkflowStatus.Running,
                StartTime = DateTime.UtcNow,
                Requester = message.Sender,
                Input = message.Input
            };

            _activeWorkflows[message.WorkflowId] = workflowState;

            // Create a child actor to handle the specific workflow execution
            var workflowActor = Context.ActorOf(
                Props.Create(() => new WorkflowExecutionActor(workflowState)),
                $"workflow-{message.WorkflowId}");

            _workflowActors[message.WorkflowId] = workflowActor;
            Context.Watch(workflowActor);

            workflowActor.Tell(new WorkflowExecutionActor.ExecuteWorkflow(message.Input));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to start workflow {WorkflowId}", message.WorkflowId);
            message.Sender.Tell(new WorkflowFailed(message.WorkflowId, ex, 0));
        }
    }

    private void Handle(PauseWorkflow message)
    {
        if (_workflowActors.TryGetValue(message.WorkflowId, out var workflowActor))
        {
            _log.Info("Pausing workflow {WorkflowId}", message.WorkflowId);
            workflowActor.Tell(new WorkflowExecutionActor.PauseExecution());
            
            if (_activeWorkflows.TryGetValue(message.WorkflowId, out var state))
            {
                state.Status = WorkflowStatus.Paused;
            }
        }
        else
        {
            _log.Warning("Cannot pause workflow {WorkflowId} - workflow not found", message.WorkflowId);
        }
    }

    private void Handle(ResumeWorkflow message)
    {
        if (_workflowActors.TryGetValue(message.WorkflowId, out var workflowActor))
        {
            _log.Info("Resuming workflow {WorkflowId}", message.WorkflowId);
            workflowActor.Tell(new WorkflowExecutionActor.ResumeExecution());
            
            if (_activeWorkflows.TryGetValue(message.WorkflowId, out var state))
            {
                state.Status = WorkflowStatus.Running;
            }
        }
        else
        {
            _log.Warning("Cannot resume workflow {WorkflowId} - workflow not found", message.WorkflowId);
        }
    }

    private void Handle(StopWorkflow message)
    {
        if (_workflowActors.TryGetValue(message.WorkflowId, out var workflowActor))
        {
            _log.Info("Stopping workflow {WorkflowId}: {Reason}", message.WorkflowId, message.Reason);
            Context.Stop(workflowActor);
            
            if (_activeWorkflows.TryGetValue(message.WorkflowId, out var state))
            {
                state.Status = WorkflowStatus.Cancelled;
                state.EndTime = DateTime.UtcNow;
                
                var result = new WorkflowResult
                {
                    WorkflowId = message.WorkflowId,
                    Status = WorkflowStatus.Cancelled,
                    ExecutionTime = (state.EndTime ?? DateTime.UtcNow) - state.StartTime,
                    CompletedAt = state.EndTime ?? DateTime.UtcNow,
                    Error = new OperationCanceledException(message.Reason)
                };
                
                state.Requester?.Tell(result);
            }
        }
        else
        {
            _log.Warning("Cannot stop workflow {WorkflowId} - workflow not found", message.WorkflowId);
        }
    }

    private void Handle(GetWorkflowStatus message)
    {
        if (_activeWorkflows.TryGetValue(message.WorkflowId, out var state))
        {
            var result = new WorkflowResult
            {
                WorkflowId = message.WorkflowId,
                Status = state.Status,
                ExecutionTime = (state.EndTime.HasValue ? state.EndTime.Value : DateTime.UtcNow) - state.StartTime,
                CompletedAt = state.EndTime.HasValue ? state.EndTime.Value : DateTime.UtcNow,
                Result = state.Result,
                Error = state.Error
            };
            
            message.Sender.Tell(result);
        }
        else
        {
            message.Sender.Tell(new WorkflowResult
            {
                WorkflowId = message.WorkflowId,
                Status = WorkflowStatus.Failed,
                Error = new InvalidOperationException($"Workflow {message.WorkflowId} not found")
            });
        }
    }

    private void Handle(WorkflowCompleted message)
    {
        _log.Info("Workflow {WorkflowId} completed in {ExecutionTime}", 
            message.WorkflowId, message.ExecutionTime);

        if (_activeWorkflows.TryGetValue(message.WorkflowId, out var state))
        {
            state.Status = WorkflowStatus.Completed;
            state.EndTime = DateTime.UtcNow;
            state.Result = message.Result.Result;
            
            state.Requester?.Tell(message.Result);
        }

        CleanupWorkflow(message.WorkflowId);
    }

    private void Handle(WorkflowFailed message)
    {
        _log.Error(message.Error, "Workflow {WorkflowId} failed after {RetryCount} retries", 
            message.WorkflowId, message.RetryCount);

        if (_activeWorkflows.TryGetValue(message.WorkflowId, out var state))
        {
            state.Status = WorkflowStatus.Failed;
            state.EndTime = DateTime.UtcNow;
            state.Error = message.Error;
            
            var result = new WorkflowResult
            {
                WorkflowId = message.WorkflowId,
                Status = WorkflowStatus.Failed,
                ExecutionTime = (state.EndTime ?? DateTime.UtcNow) - state.StartTime,
                CompletedAt = state.EndTime ?? DateTime.UtcNow,
                Error = message.Error
            };
            
            state.Requester?.Tell(result);
        }

        CleanupWorkflow(message.WorkflowId);
    }

    private void Handle(Terminated message)
    {
        // Find and clean up the terminated workflow actor
        var workflowId = _workflowActors
            .Where(kv => kv.Value.Equals(message.ActorRef))
            .Select(kv => kv.Key)
            .FirstOrDefault();

        if (workflowId != null)
        {
            _log.Info("Workflow actor for {WorkflowId} terminated", workflowId);
            CleanupWorkflow(workflowId);
        }
    }

    private void CleanupWorkflow(string workflowId)
    {
        _activeWorkflows.Remove(workflowId);
        _workflowActors.Remove(workflowId);
    }

    protected override void PreStart()
    {
        _log.Info("AIWorkflowCoordinatorActor started");
    }

    protected override void PostStop()
    {
        _log.Info("AIWorkflowCoordinatorActor stopped");
    }

    #region Internal Messages and State

    /// <summary>
    /// State information for an active workflow.
    /// </summary>
    public class WorkflowState
    {
        public string Id { get; set; } = string.Empty;
        public WorkflowConfiguration Configuration { get; set; } = new();
        public WorkflowStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public object? Input { get; set; }
        public object? Result { get; set; }
        public Exception? Error { get; set; }
        public IActorRef? Requester { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
    }

    #endregion
}

/// <summary>
/// Actor responsible for executing a specific workflow instance.
/// Handles step-by-step execution, parallel processing, and error handling.
/// </summary>
public class WorkflowExecutionActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly AIWorkflowCoordinatorActor.WorkflowState _workflowState;
    private readonly Dictionary<string, object> _stepResults = new();
    private bool _isPaused = false;
    private int _completedSteps = 0;

    /// <summary>
    /// Internal message to execute workflow.
    /// </summary>
    /// <param name="Input">Workflow input data.</param>
    public record ExecuteWorkflow(object Input);

    /// <summary>
    /// Internal message to pause workflow execution.
    /// </summary>
    public record PauseExecution();

    /// <summary>
    /// Internal message to resume workflow execution.
    /// </summary>
    public record ResumeExecution();

    /// <summary>
    /// Internal message indicating a step has completed.
    /// </summary>
    /// <param name="StepId">Step identifier.</param>
    /// <param name="Result">Step execution result.</param>
    public record StepCompleted(string StepId, object Result);

    /// <summary>
    /// Internal message indicating a step has failed.
    /// </summary>
    /// <param name="StepId">Step identifier.</param>
    /// <param name="Error">Exception that caused the failure.</param>
    public record StepFailed(string StepId, Exception Error);

    public WorkflowExecutionActor(AIWorkflowCoordinatorActor.WorkflowState workflowState)
    {
        _workflowState = workflowState;
        
        Receive<ExecuteWorkflow>(Handle);
        Receive<PauseExecution>(Handle);
        Receive<ResumeExecution>(Handle);
        Receive<StepCompleted>(Handle);
        Receive<StepFailed>(Handle);
    }

    private void Handle(ExecuteWorkflow message)
    {
        if (_isPaused)
        {
            _log.Info("Workflow execution paused, queuing execution request");
            return;
        }

        try
        {
            _log.Info("Executing workflow {WorkflowId} with {StepCount} steps", 
                _workflowState.Id, _workflowState.Configuration.Steps.Count);

            ExecuteNextSteps();
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to execute workflow {WorkflowId}", _workflowState.Id);
            Context.Parent.Tell(new WorkflowFailed(_workflowState.Id, ex, 0));
        }
    }

    private void Handle(PauseExecution message)
    {
        _log.Info("Pausing workflow execution for {WorkflowId}", _workflowState.Id);
        _isPaused = true;
    }

    private void Handle(ResumeExecution message)
    {
        _log.Info("Resuming workflow execution for {WorkflowId}", _workflowState.Id);
        _isPaused = false;
        ExecuteNextSteps();
    }

    private void Handle(StepCompleted message)
    {
        _log.Info("Step {StepId} completed for workflow {WorkflowId}", 
            message.StepId, _workflowState.Id);

        _stepResults[message.StepId] = message.Result;
        _completedSteps++;

        if (_completedSteps >= _workflowState.Configuration.Steps.Count)
        {
            // All steps completed
            var finalResult = AggregateStepResults();
            var workflowResult = new WorkflowResult
            {
                WorkflowId = _workflowState.Id,
                Status = WorkflowStatus.Completed,
                Result = finalResult,
                ExecutionTime = DateTime.UtcNow - _workflowState.StartTime,
                CompletedAt = DateTime.UtcNow
            };

            Context.Parent.Tell(new WorkflowCompleted(_workflowState.Id, workflowResult, workflowResult.ExecutionTime));
        }
        else
        {
            // Continue with next steps
            ExecuteNextSteps();
        }
    }

    private void Handle(StepFailed message)
    {
        _log.Error(message.Error, "Step {StepId} failed for workflow {WorkflowId}", 
            message.StepId, _workflowState.Id);

        // For now, fail the entire workflow if any step fails
        // In a more sophisticated implementation, we could support retry logic and error recovery
        Context.Parent.Tell(new WorkflowFailed(_workflowState.Id, message.Error, 0));
    }

    private void ExecuteNextSteps()
    {
        if (_isPaused)
            return;

        // Simple sequential execution for now
        // In a more sophisticated implementation, this would handle parallel execution,
        // dependency resolution, and conditional steps
        var pendingSteps = _workflowState.Configuration.Steps
            .Where(step => !_stepResults.ContainsKey(step.Id))
            .ToList();

        foreach (var step in pendingSteps)
        {
            // Simulate step execution
            // In a real implementation, this would delegate to the appropriate AI agent
            ExecuteStep(step);
        }
    }

    private void ExecuteStep(WorkflowStep step)
    {
        _log.Info("Executing step {StepId} ({StepName}) for workflow {WorkflowId}", 
            step.Id, step.Name, _workflowState.Id);

        // Simulate async step execution
        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(100); // Simulate processing time
                
                var result = new
                {
                    StepId = step.Id,
                    Result = $"Mock result for step {step.Name}",
                    ProcessedAt = DateTime.UtcNow
                };

                Self.Tell(new StepCompleted(step.Id, result));
            }
            catch (Exception ex)
            {
                Self.Tell(new StepFailed(step.Id, ex));
            }
        });
    }

    private object AggregateStepResults()
    {
        // Simple aggregation - combine all step results
        return new
        {
            WorkflowId = _workflowState.Id,
            StepResults = _stepResults,
            CompletedSteps = _completedSteps,
            ProcessedAt = DateTime.UtcNow
        };
    }

}
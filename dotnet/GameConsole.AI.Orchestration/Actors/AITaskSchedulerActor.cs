using Akka.Actor;
using Akka.Event;
using GameConsole.AI.Orchestration.Messages;
using GameConsole.AI.Orchestration.Services;

namespace GameConsole.AI.Orchestration.Actors;

/// <summary>
/// Actor responsible for scheduling and load balancing AI tasks across available agents.
/// Implements intelligent task distribution based on agent capabilities and current load.
/// </summary>
public class AITaskSchedulerActor : ReceiveActor
{
    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly Dictionary<string, AgentInfo> _availableAgents = new();
    private readonly Dictionary<string, TaskInfo> _scheduledTasks = new();
    private readonly PriorityQueue<TaskInfo, int> _taskQueue = new();
    private readonly Dictionary<string, CircuitBreakerInfo> _circuitBreakers = new();

    public AITaskSchedulerActor()
    {
        Receive<ScheduleTask>(Handle);
        Receive<GetTaskStatus>(Handle);
        Receive<GetLoadMetrics>(Handle);
        Receive<TaskStarted>(Handle);
        Receive<TaskCompleted>(Handle);
        Receive<TaskFailed>(Handle);
        Receive<AgentAvailable>(Handle);
        Receive<AgentUnavailable>(Handle);
        Receive<AgentHeartbeat>(Handle);
        Receive<OpenCircuitBreaker>(Handle);
        Receive<CloseCircuitBreaker>(Handle);
        Receive<CheckTaskQueue>(Handle);

        // Start periodic task queue processing
        Context.System.Scheduler.ScheduleTellRepeatedly(
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            Self,
            new CheckTaskQueue(),
            ActorRefs.NoSender);
    }

    private void Handle(ScheduleTask message)
    {
        _log.Info("Scheduling task {TaskId} with priority {Priority}", 
            message.Task.Id, message.Priority);

        try
        {
            var taskInfo = new TaskInfo
            {
                Task = message.Task,
                Priority = message.Priority,
                Requester = message.Sender,
                ScheduledAt = DateTime.UtcNow,
                Status = TaskExecutionStatus.Queued
            };

            _scheduledTasks[message.Task.Id] = taskInfo;

            // Try immediate scheduling, otherwise add to queue
            if (!TryScheduleImmediately(taskInfo))
            {
                var priorityValue = (int)message.Priority * -1; // Higher priority = lower value for min-heap
                _taskQueue.Enqueue(taskInfo, priorityValue);
                _log.Info("Task {TaskId} added to queue", message.Task.Id);
            }

            message.Sender.Tell(new TaskScheduled(
                message.Task.Id,
                taskInfo.AssignedAgent ?? "queued",
                taskInfo.EstimatedStartTime ?? DateTime.UtcNow.AddMinutes(1)));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to schedule task {TaskId}", message.Task.Id);
            message.Sender.Tell(new TaskFailed(message.Task.Id, ex, 0));
        }
    }

    private void Handle(GetTaskStatus message)
    {
        if (_scheduledTasks.TryGetValue(message.TaskId, out var taskInfo))
        {
            var status = new Services.TaskStatus
            {
                TaskId = message.TaskId,
                Status = taskInfo.Status,
                CreatedAt = taskInfo.ScheduledAt,
                StartedAt = taskInfo.StartedAt,
                CompletedAt = taskInfo.CompletedAt,
                RetryCount = taskInfo.RetryCount,
                Result = taskInfo.Result,
                Error = taskInfo.Error,
                ExecutionTime = taskInfo.CompletedAt.HasValue ? 
                    taskInfo.CompletedAt.Value - (taskInfo.StartedAt ?? taskInfo.ScheduledAt) : null
            };

            message.Sender.Tell(status);
        }
        else
        {
            message.Sender.Tell(new Services.TaskStatus
            {
                TaskId = message.TaskId,
                Status = TaskExecutionStatus.Failed,
                Error = new InvalidOperationException($"Task {message.TaskId} not found")
            });
        }
    }

    private void Handle(GetLoadMetrics message)
    {
        var totalAgents = _availableAgents.Count;
        var activeAgents = _availableAgents.Count(a => a.Value.Status == AgentStatus.Available);
        var averageLoad = _availableAgents.Values
            .Where(a => a.Status == AgentStatus.Available)
            .Select(a => a.CurrentLoad)
            .DefaultIfEmpty(0)
            .Average();

        var loadByType = _availableAgents.Values
            .GroupBy(a => a.Type)
            .ToDictionary(g => g.Key, g => g.Average(a => a.CurrentLoad));

        var runningTasks = _scheduledTasks.Values.Count(t => t.Status == TaskExecutionStatus.Running);
        var queuedTasks = _taskQueue.Count;

        var metrics = new LoadBalancingMetrics
        {
            AverageLoad = averageLoad,
            TotalAgents = totalAgents,
            ActiveAgents = activeAgents,
            LoadByAgentType = loadByType,
            TasksInQueue = queuedTasks,
            AverageWaitTime = CalculateAverageWaitTime()
        };

        message.Sender.Tell(metrics);
    }

    private void Handle(TaskStarted message)
    {
        if (_scheduledTasks.TryGetValue(message.TaskId, out var taskInfo))
        {
            taskInfo.Status = TaskExecutionStatus.Running;
            taskInfo.StartedAt = message.StartTime;
            
            // Update agent load
            if (taskInfo.AssignedAgent != null && _availableAgents.TryGetValue(taskInfo.AssignedAgent, out var agent))
            {
                agent.CurrentLoad += 0.2; // Approximate load increase
                agent.Status = AgentStatus.Busy;
            }

            _log.Info("Task {TaskId} started on agent {AgentId}", message.TaskId, message.AgentId);
        }
    }

    private void Handle(TaskCompleted message)
    {
        if (_scheduledTasks.TryGetValue(message.TaskId, out var taskInfo))
        {
            taskInfo.Status = TaskExecutionStatus.Completed;
            taskInfo.CompletedAt = DateTime.UtcNow;
            taskInfo.Result = message.Result;

            // Update agent load
            if (taskInfo.AssignedAgent != null && _availableAgents.TryGetValue(taskInfo.AssignedAgent, out var agent))
            {
                agent.CurrentLoad = Math.Max(0, agent.CurrentLoad - 0.2);
                if (agent.CurrentLoad < 0.5)
                {
                    agent.Status = AgentStatus.Available;
                }
            }

            _log.Info("Task {TaskId} completed in {ExecutionTime}", 
                message.TaskId, message.ExecutionTime);

            // Notify requester
            taskInfo.Requester?.Tell(message);

            // Try to schedule queued tasks
            ProcessTaskQueue();
        }
    }

    private void Handle(TaskFailed message)
    {
        if (_scheduledTasks.TryGetValue(message.TaskId, out var taskInfo))
        {
            taskInfo.RetryCount++;
            taskInfo.Error = message.Error;

            // Update agent load
            if (taskInfo.AssignedAgent != null && _availableAgents.TryGetValue(taskInfo.AssignedAgent, out var agent))
            {
                agent.CurrentLoad = Math.Max(0, agent.CurrentLoad - 0.2);
                if (agent.CurrentLoad < 0.5)
                {
                    agent.Status = AgentStatus.Available;
                }
            }

            // Check if we should retry
            if (taskInfo.RetryCount < taskInfo.Task.MaxRetries)
            {
                _log.Info("Retrying task {TaskId} (attempt {RetryCount}/{MaxRetries})", 
                    message.TaskId, taskInfo.RetryCount, taskInfo.Task.MaxRetries);

                taskInfo.Status = TaskExecutionStatus.Retry;
                taskInfo.AssignedAgent = null;

                // Re-queue for retry
                var priorityValue = (int)taskInfo.Priority * -1;
                _taskQueue.Enqueue(taskInfo, priorityValue);
            }
            else
            {
                taskInfo.Status = TaskExecutionStatus.Failed;
                taskInfo.CompletedAt = DateTime.UtcNow;

                _log.Error(message.Error, "Task {TaskId} failed after {RetryCount} attempts", 
                    message.TaskId, taskInfo.RetryCount);

                // Notify requester
                taskInfo.Requester?.Tell(message);
            }
        }
    }

    private void Handle(AgentAvailable message)
    {
        var agentInfo = new AgentInfo
        {
            Id = message.AgentId,
            Type = message.AgentType,
            Status = AgentStatus.Available,
            CurrentLoad = 0.0,
            Capabilities = message.Capabilities,
            LastHeartbeat = DateTime.UtcNow
        };

        _availableAgents[message.AgentId] = agentInfo;
        _log.Info("Agent {AgentId} of type {AgentType} is now available", 
            message.AgentId, message.AgentType);

        // Try to schedule queued tasks
        ProcessTaskQueue();
    }

    private void Handle(AgentUnavailable message)
    {
        if (_availableAgents.Remove(message.AgentId))
        {
            _log.Info("Agent {AgentId} is no longer available: {Reason}", 
                message.AgentId, message.Reason);

            // Reschedule any tasks assigned to this agent
            RescheduleTasksForAgent(message.AgentId);
        }
    }

    private void Handle(AgentHeartbeat message)
    {
        if (_availableAgents.TryGetValue(message.AgentId, out var agent))
        {
            agent.CurrentLoad = message.CurrentLoad;
            agent.Status = message.Status;
            agent.LastHeartbeat = message.Timestamp;
        }
    }

    private void Handle(OpenCircuitBreaker message)
    {
        _circuitBreakers[message.ServiceName] = new CircuitBreakerInfo
        {
            ServiceName = message.ServiceName,
            State = CircuitBreakerState.Open,
            LastStateChange = DateTime.UtcNow,
            FailureReason = message.Reason
        };

        _log.Warning("Circuit breaker opened for service {ServiceName}: {Reason}", 
            message.ServiceName, message.Reason);
    }

    private void Handle(CloseCircuitBreaker message)
    {
        if (_circuitBreakers.TryGetValue(message.ServiceName, out var cb))
        {
            cb.State = CircuitBreakerState.Closed;
            cb.LastStateChange = DateTime.UtcNow;

            _log.Info("Circuit breaker closed for service {ServiceName}", message.ServiceName);
        }
    }

    private void Handle(CheckTaskQueue message)
    {
        ProcessTaskQueue();
        CheckAgentHealth();
    }

    private bool TryScheduleImmediately(TaskInfo taskInfo)
    {
        var suitableAgent = FindBestAgent(taskInfo.Task);
        if (suitableAgent != null)
        {
            AssignTaskToAgent(taskInfo, suitableAgent);
            return true;
        }
        return false;
    }

    private void ProcessTaskQueue()
    {
        while (_taskQueue.Count > 0)
        {
            if (!_taskQueue.TryPeek(out var nextTask, out _))
                break;

            var suitableAgent = FindBestAgent(nextTask.Task);
            if (suitableAgent != null)
            {
                _taskQueue.Dequeue();
                AssignTaskToAgent(nextTask, suitableAgent);
            }
            else
            {
                break; // No suitable agents available, try again later
            }
        }
    }

    private AgentInfo? FindBestAgent(AITask task)
    {
        var candidateAgents = _availableAgents.Values
            .Where(agent => 
                agent.Status == AgentStatus.Available &&
                agent.CurrentLoad < 0.8 && // Don't overload agents
                (string.IsNullOrEmpty(task.AgentType) || agent.Type == task.AgentType) &&
                task.RequiredCapabilities.All(cap => agent.Capabilities.Contains(cap)))
            .OrderBy(agent => agent.CurrentLoad)
            .ToList();

        return candidateAgents.FirstOrDefault();
    }

    private void AssignTaskToAgent(TaskInfo taskInfo, AgentInfo agent)
    {
        taskInfo.AssignedAgent = agent.Id;
        taskInfo.EstimatedStartTime = DateTime.UtcNow;

        _log.Info("Assigned task {TaskId} to agent {AgentId}", 
            taskInfo.Task.Id, agent.Id);

        // In a real implementation, this would send the task to the actual agent
        // For now, simulate task execution
        SimulateTaskExecution(taskInfo);
    }

    private void SimulateTaskExecution(TaskInfo taskInfo)
    {
        // Simulate task execution with async processing
        Task.Run(async () =>
        {
            try
            {
                Self.Tell(new TaskStarted(taskInfo.Task.Id, taskInfo.AssignedAgent!, DateTime.UtcNow));

                // Simulate processing time
                await Task.Delay(Random.Shared.Next(1000, 5000));

                var result = new
                {
                    TaskId = taskInfo.Task.Id,
                    Result = $"Mock result for task {taskInfo.Task.Name}",
                    ProcessedBy = taskInfo.AssignedAgent,
                    ProcessedAt = DateTime.UtcNow
                };

                Self.Tell(new TaskCompleted(taskInfo.Task.Id, result, DateTime.UtcNow - taskInfo.ScheduledAt));
            }
            catch (Exception ex)
            {
                Self.Tell(new TaskFailed(taskInfo.Task.Id, ex, taskInfo.RetryCount));
            }
        });
    }

    private void RescheduleTasksForAgent(string agentId)
    {
        var tasksToReschedule = _scheduledTasks.Values
            .Where(t => t.AssignedAgent == agentId && 
                       (t.Status == TaskExecutionStatus.Running || t.Status == TaskExecutionStatus.Queued))
            .ToList();

        foreach (var task in tasksToReschedule)
        {
            task.AssignedAgent = null;
            task.Status = TaskExecutionStatus.Queued;
            
            var priorityValue = (int)task.Priority * -1;
            _taskQueue.Enqueue(task, priorityValue);
        }

        if (tasksToReschedule.Any())
        {
            _log.Info("Rescheduled {Count} tasks due to agent {AgentId} unavailability", 
                tasksToReschedule.Count, agentId);
        }
    }

    private void CheckAgentHealth()
    {
        var staleAgents = _availableAgents.Values
            .Where(agent => DateTime.UtcNow - agent.LastHeartbeat > TimeSpan.FromMinutes(2))
            .ToList();

        foreach (var agent in staleAgents)
        {
            _log.Warning("Agent {AgentId} appears to be stale (last heartbeat: {LastHeartbeat})", 
                agent.Id, agent.LastHeartbeat);
            
            agent.Status = AgentStatus.Offline;
            RescheduleTasksForAgent(agent.Id);
        }
    }

    private TimeSpan CalculateAverageWaitTime()
    {
        var queuedTasks = _scheduledTasks.Values
            .Where(t => t.Status == TaskExecutionStatus.Queued)
            .ToList();

        if (!queuedTasks.Any())
            return TimeSpan.Zero;

        var totalWaitTime = queuedTasks
            .Sum(t => (DateTime.UtcNow - t.ScheduledAt).TotalSeconds);

        return TimeSpan.FromSeconds(totalWaitTime / queuedTasks.Count);
    }

    protected override void PreStart()
    {
        _log.Info("AITaskSchedulerActor started");
    }

    protected override void PostStop()
    {
        _log.Info("AITaskSchedulerActor stopped");
    }

    #region Internal Messages and State

    /// <summary>
    /// Internal message to check and process the task queue.
    /// </summary>
    private record CheckTaskQueue();

    /// <summary>
    /// Information about a scheduled task.
    /// </summary>
    private class TaskInfo
    {
        public AITask Task { get; set; } = new();
        public TaskPriority Priority { get; set; }
        public TaskExecutionStatus Status { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? EstimatedStartTime { get; set; }
        public string? AssignedAgent { get; set; }
        public IActorRef? Requester { get; set; }
        public int RetryCount { get; set; }
        public object? Result { get; set; }
        public Exception? Error { get; set; }
    }

    /// <summary>
    /// Information about circuit breaker state.
    /// </summary>
    private class CircuitBreakerInfo
    {
        public string ServiceName { get; set; } = string.Empty;
        public CircuitBreakerState State { get; set; }
        public DateTime LastStateChange { get; set; }
        public string? FailureReason { get; set; }
    }

    #endregion
}
# GameConsole.AI.Orchestration

AI Workflow Orchestration system for the GameConsole framework, implementing specialized actors for AI task coordination using Akka.NET.

## Overview

This project implements GAME-RFC-009-02, providing a comprehensive AI orchestration system with four main actor types:

- **AIWorkflowCoordinator** - Manages complex AI workflows with support for parallel and sequential execution
- **AITaskScheduler** - Distributes AI tasks across available agents with intelligent load balancing
- **AIResultAggregator** - Combines partial AI responses using various aggregation strategies
- **AIResourceManager** - Optimizes AI resource allocation and monitors system health

## Architecture

The system follows the GameConsole 4-tier architecture pattern:

- **Tier 1**: Core service contracts (`IService`)
- **Tier 2**: Service proxy implementation (`OrchestrationService`)
- **Tier 3**: Actor-based implementation (specialized actors)
- **Tier 4**: Akka.NET provider integration

## Key Features

### Workflow Orchestration
- Sequential and parallel workflow execution
- Step dependency resolution
- Workflow pause/resume/stop capabilities
- Fault tolerance with automatic retry
- Workflow persistence and recovery support

### Task Scheduling
- Priority-based task queuing
- Intelligent load balancing across agents
- Circuit breaker pattern for fault tolerance
- Agent health monitoring and heartbeat tracking
- Automatic task rescheduling on agent failure

### Result Aggregation
- Multiple aggregation strategies (Merge, Consensus, WeightedAverage, BestResult, Custom)
- Result validation and quality filtering
- Performance metrics and analytics
- Configurable aggregation parameters

### Resource Management
- Dynamic resource allocation and deallocation
- Resource pool management by agent type
- Health monitoring and status reporting
- Resource optimization and scaling
- Automatic cleanup of stale allocations

## Usage

### Basic Service Usage

```csharp
using GameConsole.AI.Orchestration.Services;
using Microsoft.Extensions.Logging;

// Create and configure the service
var logger = loggerFactory.CreateLogger<OrchestrationService>();
var orchestrationService = new OrchestrationService(logger);

// Initialize and start
await orchestrationService.InitializeAsync();
await orchestrationService.StartAsync();

// Create a workflow
var workflowConfig = new WorkflowConfiguration
{
    Name = "Content Generation Workflow",
    Type = WorkflowType.Sequential,
    Steps = new List<WorkflowStep>
    {
        new WorkflowStep
        {
            Id = "generate",
            Name = "Generate Content",
            AgentType = "DirectorAgent",
            Parameters = new Dictionary<string, object> { { "theme", "fantasy" } }
        }
    }
};

var workflowId = await orchestrationService.CreateWorkflowAsync(workflowConfig);
var result = await orchestrationService.ExecuteWorkflowAsync(workflowId, inputData);
```

### Capability-Based Access

```csharp
// Access workflow coordinator directly
if (orchestrationService.WorkflowCoordinator != null)
{
    await orchestrationService.WorkflowCoordinator.PauseWorkflowAsync(workflowId);
    await orchestrationService.WorkflowCoordinator.ResumeWorkflowAsync(workflowId);
}

// Access task scheduler for load balancing
if (orchestrationService.TaskScheduler != null)
{
    var loadMetrics = await orchestrationService.TaskScheduler.GetLoadMetricsAsync();
    var agents = await orchestrationService.TaskScheduler.GetAvailableAgentsAsync();
}

// Access result aggregator for combining results
if (orchestrationService.ResultAggregator != null)
{
    var aggregated = await orchestrationService.ResultAggregator.AggregateResultsAsync(
        partialResults, AggregationStrategy.Consensus);
}

// Access resource manager for optimization
if (orchestrationService.ResourceManager != null)
{
    var healthStatus = await orchestrationService.ResourceManager.MonitorResourceHealthAsync();
    var optimization = await orchestrationService.ResourceManager.OptimizeResourcesAsync();
}
```

### Running the Demo

```csharp
using GameConsole.AI.Orchestration;

// Run the comprehensive demo
await OrchestrationDemo.RunDemoAsync();
```

## Message Types

The system uses a comprehensive set of messages for actor communication:

- **Workflow Messages**: `StartWorkflow`, `WorkflowCompleted`, `WorkflowFailed`, `PauseWorkflow`, `ResumeWorkflow`
- **Task Messages**: `ScheduleTask`, `TaskScheduled`, `TaskStarted`, `TaskCompleted`, `TaskFailed`
- **Aggregation Messages**: `AggregateResults`, `AggregationCompleted`, `AddPartialResult`
- **Resource Messages**: `AllocateResources`, `ReleaseResources`, `AgentAvailable`, `AgentHeartbeat`
- **Circuit Breaker Messages**: `OpenCircuitBreaker`, `CloseCircuitBreaker`, `HalfOpenCircuitBreaker`

## Configuration

### Workflow Configuration

```csharp
public class WorkflowConfiguration
{
    public string Name { get; set; }
    public WorkflowType Type { get; set; } // Sequential, Parallel, Conditional, Pipeline, MapReduce
    public List<WorkflowStep> Steps { get; set; }
    public TimeSpan Timeout { get; set; }
    public int MaxRetries { get; set; }
    public bool EnablePersistence { get; set; }
}
```

### Task Configuration

```csharp
public class AITask
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string AgentType { get; set; }
    public object Input { get; set; }
    public TimeSpan Timeout { get; set; }
    public int MaxRetries { get; set; }
    public List<string> RequiredCapabilities { get; set; }
}
```

## Dependencies

- **Akka.NET 1.5.13** - Actor framework for concurrent, distributed systems
- **Akka.Streams 1.5.13** - Data pipeline processing support
- **Microsoft.Extensions.Logging** - Logging infrastructure
- **GameConsole.Core.Abstractions** - Base service contracts

## Testing

The implementation includes comprehensive error handling, fault tolerance, and monitoring capabilities. All existing tests pass, ensuring no regressions in the broader GameConsole framework.

## Performance Considerations

- Uses actor-based concurrency for high throughput
- Implements circuit breaker pattern to prevent cascading failures
- Includes resource pooling and optimization
- Supports horizontal scaling through actor distribution
- Provides comprehensive metrics and monitoring

## Future Enhancements

- Integration with actual AI backend services
- Workflow templating and reusability
- Advanced scheduling algorithms
- Distributed actor system support
- Enhanced monitoring and analytics dashboards
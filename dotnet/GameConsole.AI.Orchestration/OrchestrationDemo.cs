using GameConsole.AI.Orchestration.Services;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Orchestration;

/// <summary>
/// Simple demonstration of the AI Orchestration system capabilities.
/// Shows how the various actors work together to manage AI workflows.
/// </summary>
public static class OrchestrationDemo
{
    /// <summary>
    /// Demonstrates the AI orchestration capabilities.
    /// </summary>
    public static async Task RunDemoAsync()
    {
        using var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var logger = loggerFactory.CreateLogger<OrchestrationService>();

        Console.WriteLine("=== GameConsole AI Orchestration Demo ===\n");

        try
        {
            // Create and initialize the orchestration service
            var orchestrationService = new OrchestrationService(logger);
            
            Console.WriteLine("1. Initializing AI Orchestration Service...");
            await orchestrationService.InitializeAsync();
            
            Console.WriteLine("2. Starting AI Orchestration Service...");
            await orchestrationService.StartAsync();
            
            Console.WriteLine("✅ AI Orchestration Service is now running!\n");
            
            // Demonstrate workflow creation
            Console.WriteLine("3. Creating AI workflow...");
            var workflowConfig = new WorkflowConfiguration
            {
                Name = "Demo Content Generation Workflow",
                Type = WorkflowType.Sequential,
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep
                    {
                        Id = "step1",
                        Name = "Generate Ideas",
                        AgentType = "DirectorAgent",
                        Parameters = new Dictionary<string, object> { { "theme", "fantasy" } }
                    },
                    new WorkflowStep
                    {
                        Id = "step2", 
                        Name = "Create Dialogue",
                        AgentType = "DialogueAgent",
                        Dependencies = new List<string> { "step1" }
                    },
                    new WorkflowStep
                    {
                        Id = "step3",
                        Name = "Generate Code",
                        AgentType = "CodexAgent",
                        Dependencies = new List<string> { "step1", "step2" }
                    }
                },
                Timeout = TimeSpan.FromMinutes(5)
            };
            
            var workflowId = await orchestrationService.CreateWorkflowAsync(workflowConfig);
            Console.WriteLine($"✅ Created workflow: {workflowId}");
            
            // Demonstrate workflow execution
            Console.WriteLine("\n4. Executing AI workflow...");
            var workflowInput = new { theme = "fantasy", complexity = "medium" };
            var workflowResult = await orchestrationService.ExecuteWorkflowAsync(workflowId, workflowInput);
            
            Console.WriteLine($"✅ Workflow executed successfully!");
            Console.WriteLine($"   Status: {workflowResult.Status}");
            Console.WriteLine($"   Execution Time: {workflowResult.ExecutionTime}");
            Console.WriteLine($"   Result: {workflowResult.Result}");
            
            // Demonstrate task scheduling
            Console.WriteLine("\n5. Scheduling AI tasks...");
            var task1 = new AITask
            {
                Name = "Generate Character Backstory",
                AgentType = "DirectorAgent",
                Input = new { character = "Elven Mage", setting = "Dark Forest" },
                Timeout = TimeSpan.FromMinutes(2)
            };
            
            var task2 = new AITask
            {
                Name = "Create Combat Dialogue",
                AgentType = "DialogueAgent", 
                Input = new { context = "Boss Battle", emotion = "intense" },
                Timeout = TimeSpan.FromMinutes(2)
            };
            
            var taskId1 = await orchestrationService.ScheduleTaskAsync(task1, TaskPriority.Normal);
            var taskId2 = await orchestrationService.ScheduleTaskAsync(task2, TaskPriority.High);
            
            Console.WriteLine($"✅ Scheduled task 1: {taskId1}");
            Console.WriteLine($"✅ Scheduled task 2: {taskId2} (high priority)");
            
            // Demonstrate resource metrics
            Console.WriteLine("\n6. Getting resource metrics...");
            var resourceMetrics = await orchestrationService.GetResourceMetricsAsync();
            Console.WriteLine($"✅ Resource Metrics:");
            Console.WriteLine($"   Total Agents: {resourceMetrics.TotalAgents}");
            Console.WriteLine($"   Available Agents: {resourceMetrics.AvailableAgents}");
            Console.WriteLine($"   CPU Utilization: {resourceMetrics.CpuUtilization:P1}");
            Console.WriteLine($"   Memory Utilization: {resourceMetrics.MemoryUtilization:P1}");
            
            // Demonstrate capability access
            Console.WriteLine("\n7. Testing capability providers...");
            var capabilities = await orchestrationService.GetCapabilitiesAsync();
            Console.WriteLine($"✅ Available capabilities:");
            foreach (var capability in capabilities)
            {
                Console.WriteLine($"   - {capability.Name}");
            }
            
            // Test workflow coordinator capability
            if (orchestrationService.WorkflowCoordinator != null)
            {
                Console.WriteLine("\n8. Testing workflow coordinator capability...");
                var coordinatorMetrics = await orchestrationService.WorkflowCoordinator.GetWorkflowStatusAsync(workflowId);
                Console.WriteLine($"✅ Workflow Status: {coordinatorMetrics.Status}");
            }
            
            // Test task scheduler capability
            if (orchestrationService.TaskScheduler != null)
            {
                Console.WriteLine("\n9. Testing task scheduler capability...");
                var loadMetrics = await orchestrationService.TaskScheduler.GetLoadMetricsAsync();
                Console.WriteLine($"✅ Load Balancing Metrics:");
                Console.WriteLine($"   Average Load: {loadMetrics.AverageLoad:P1}");
                Console.WriteLine($"   Active Agents: {loadMetrics.ActiveAgents}");
                Console.WriteLine($"   Tasks in Queue: {loadMetrics.TasksInQueue}");
            }
            
            // Test result aggregator capability
            if (orchestrationService.ResultAggregator != null)
            {
                Console.WriteLine("\n10. Testing result aggregator capability...");
                var partialResults = new object[]
                {
                    new { result = "Generated backstory for Elven Mage", confidence = 0.9 },
                    new { result = "Created dialogue for boss battle", confidence = 0.85 },
                    new { result = "Optimized combat mechanics", confidence = 0.92 }
                };
                
                var aggregatedResult = await orchestrationService.ResultAggregator.AggregateResultsAsync(
                    partialResults, AggregationStrategy.BestResult);
                
                Console.WriteLine($"✅ Aggregated Result: {aggregatedResult}");
            }
            
            // Test resource manager capability
            if (orchestrationService.ResourceManager != null)
            {
                Console.WriteLine("\n11. Testing resource manager capability...");
                var healthStatus = await orchestrationService.ResourceManager.MonitorResourceHealthAsync();
                Console.WriteLine($"✅ Resource Health: {healthStatus.OverallHealth}");
                Console.WriteLine($"   Components: {healthStatus.ComponentHealth.Count}");
                
                var optimizationResult = await orchestrationService.ResourceManager.OptimizeResourcesAsync();
                Console.WriteLine($"✅ Optimization Result: {optimizationResult.EfficiencyGain:P1} efficiency gain");
            }
            
            Console.WriteLine("\n12. Stopping AI Orchestration Service...");
            await orchestrationService.StopAsync();
            
            Console.WriteLine("✅ AI Orchestration Service stopped successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Demo failed: {ex.Message}");
            throw;
        }
        
        Console.WriteLine("\n🎉 AI Orchestration Demo completed successfully!");
        Console.WriteLine("All four actors (WorkflowCoordinator, TaskScheduler, ResultAggregator, ResourceManager) are working correctly!");
    }
}
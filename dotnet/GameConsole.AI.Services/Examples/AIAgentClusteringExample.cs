using GameConsole.AI.Services;
using GameConsole.Engine.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services.Examples;

/// <summary>
/// Example demonstrating how to use the AI Agent Actor Clustering system.
/// This shows the complete workflow from system setup through task distribution.
/// </summary>
public class AIAgentClusteringExample
{
    private readonly ILoggerFactory _loggerFactory;

    public AIAgentClusteringExample(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Demonstrates the complete AI Agent Actor Clustering workflow.
    /// </summary>
    public async Task RunExampleAsync()
    {
        Console.WriteLine("=== AI Agent Actor Clustering Example ===\n");

        // Step 1: Create the actor system (Tier 3)
        Console.WriteLine("1. Creating Actor System...");
        var actorSystem = new BasicActorSystem("example-system", _loggerFactory);
        await actorSystem.InitializeAsync();
        await actorSystem.StartAsync();
        Console.WriteLine($"   ✓ Actor system '{actorSystem.SystemName}' started in {actorSystem.Mode} mode\n");

        // Step 2: Create the agent manager (Tier 2)
        Console.WriteLine("2. Setting up Agent Manager...");
        var agentManager = new DefaultAgentManager(_loggerFactory.CreateLogger<DefaultAgentManager>());
        await agentManager.SetActorSystemAsync(actorSystem);
        await agentManager.InitializeAsync();
        await agentManager.StartAsync();
        Console.WriteLine("   ✓ Agent manager initialized and started\n");

        // Step 3: Create clusters
        Console.WriteLine("3. Creating Agent Clusters...");
        await agentManager.CreateClusterAsync("math-cluster", LoadBalancingStrategy.RoundRobin);
        await agentManager.CreateClusterAsync("text-cluster", LoadBalancingStrategy.LoadBased);
        
        var clusterStatus = await agentManager.GetClusterStatusAsync();
        foreach (var (clusterId, health) in clusterStatus)
        {
            Console.WriteLine($"   ✓ Cluster '{clusterId}' created with health: {health}");
        }
        Console.WriteLine();

        // Step 4: Spawn agents
        Console.WriteLine("4. Spawning AI Agents...");
        var mathAgent1 = await agentManager.SpawnAgentAsync("math-cluster", "calculator", "calc-001");
        var mathAgent2 = await agentManager.SpawnAgentAsync("math-cluster", "calculator", "calc-002");
        var textAgent1 = await agentManager.SpawnAgentAsync("text-cluster", "processor", "text-001");
        var textAgent2 = await agentManager.SpawnAgentAsync("text-cluster", "processor", "text-002");

        Console.WriteLine($"   ✓ Math agents spawned: {mathAgent1}, {mathAgent2}");
        Console.WriteLine($"   ✓ Text agents spawned: {textAgent1}, {textAgent2}");

        // Show system metrics
        var systemMetrics = await agentManager.GetMetricsAsync();
        Console.WriteLine($"   ✓ System now has {systemMetrics["total_clusters"]} clusters and {systemMetrics["total_agents"]} agents\n");

        // Step 5: Distribute tasks to agents
        Console.WriteLine("5. Distributing Tasks to Agents...");
        
        // Math tasks
        var mathTasks = new[] { "2 + 2", "5 * 3", "10 - 4", "15 / 3" };
        foreach (var task in mathTasks)
        {
            var (result, agentId) = await agentManager.SubmitTaskAsync("math-cluster", task, "calculator");
            Console.WriteLine($"   ✓ Math task '{task}' → Agent {agentId}: {result}");
        }

        // Text tasks
        var textTasks = new[] { "Hello World", "Process this text", "Convert to uppercase", "Count characters" };
        foreach (var task in textTasks)
        {
            var (result, agentId) = await agentManager.SubmitTaskAsync("text-cluster", task, "processor");
            Console.WriteLine($"   ✓ Text task '{task}' → Agent {agentId}: {result}");
        }
        Console.WriteLine();

        // Step 6: Health monitoring
        Console.WriteLine("6. Monitoring Cluster Health...");
        await agentManager.PerformHealthCheckAsync();
        
        var mathMetrics = await agentManager.GetMetricsAsync("math-cluster");
        var textMetrics = await agentManager.GetMetricsAsync("text-cluster");
        
        Console.WriteLine($"   ✓ Math cluster: {mathMetrics["agent_count"]} agents, {mathMetrics["cluster_health"]} health");
        Console.WriteLine($"   ✓ Text cluster: {textMetrics["agent_count"]} agents, {textMetrics["cluster_health"]} health\n");

        // Step 7: Demonstrate different actor system modes
        Console.WriteLine("7. Demonstrating Actor System Modes...");
        Console.WriteLine($"   Current mode: {actorSystem.Mode}");
        
        await actorSystem.SetModeAsync(GameConsole.Core.Abstractions.ActorSystemMode.Clustered);
        Console.WriteLine($"   ✓ Changed to: {actorSystem.Mode}");
        
        await actorSystem.SetModeAsync(GameConsole.Core.Abstractions.ActorSystemMode.Hybrid);
        Console.WriteLine($"   ✓ Changed to: {actorSystem.Mode}\n");

        // Step 8: Rebalancing
        Console.WriteLine("8. Performing Load Rebalancing...");
        await agentManager.RebalanceAsync();
        Console.WriteLine("   ✓ Load rebalancing completed\n");

        // Step 9: Cleanup
        Console.WriteLine("9. Cleaning up...");
        await agentManager.TerminateAgentAsync(mathAgent1, graceful: true);
        await agentManager.TerminateAgentAsync(mathAgent2, graceful: true);
        await agentManager.TerminateAgentAsync(textAgent1, graceful: true);
        await agentManager.TerminateAgentAsync(textAgent2, graceful: true);
        
        await agentManager.DestroyClusterAsync("math-cluster", graceful: true);
        await agentManager.DestroyClusterAsync("text-cluster", graceful: true);
        
        await agentManager.StopAsync();
        await agentManager.DisposeAsync();
        
        await actorSystem.StopAsync();
        await actorSystem.DisposeAsync();
        
        Console.WriteLine("   ✓ All resources cleaned up successfully");
        Console.WriteLine("\n=== Example completed successfully! ===");
    }
}

/// <summary>
/// Console application to run the AI Agent Actor Clustering example.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));

        try
        {
            var example = new AIAgentClusteringExample(loggerFactory);
            await example.RunExampleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running example: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
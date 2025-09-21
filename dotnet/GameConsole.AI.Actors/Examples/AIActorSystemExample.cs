using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GameConsole.AI.Actors.Configuration;
using GameConsole.AI.Actors.System;
using GameConsole.AI.Actors.Messages;
using GameConsole.AI.Actors.Examples;
using Akka.Actor;

namespace GameConsole.AI.Actors.Examples;

/// <summary>
/// Example demonstrating how to set up and use the AI Actor System.
/// </summary>
public static class AIActorSystemExample
{
    /// <summary>
    /// Complete example showing AI Actor System setup, agent registration, and usage.
    /// </summary>
    public static async Task RunExampleAsync()
    {
        Console.WriteLine("=== AI Actor System Example ===\n");

        // 1. Set up dependency injection and services
        var services = new ServiceCollection();
        services.AddLogging(builder => builder
            .AddConsole()
            .SetMinimumLevel(LogLevel.Information));

        // Configure AI Actor System
        var actorSystemConfig = new ActorSystemConfiguration
        {
            SystemName = "ExampleAI",
            Clustering = new ClusterConfig
            {
                Enabled = false // Disable clustering for this example
            },
            Logging = new LoggingConfig
            {
                LogLevel = "INFO",
                LogActorLifecycle = true,
                LogDeadLetters = false
            }
        };
        
        services.AddSingleton(actorSystemConfig);
        
        var serviceProvider = services.BuildServiceProvider();

        // 2. Create and initialize the AI Actor System
        var logger = serviceProvider.GetRequiredService<ILogger<AIActorSystem>>();
        var aiActorSystem = new AIActorSystem(logger, actorSystemConfig, serviceProvider);

        try
        {
            Console.WriteLine("1. Initializing AI Actor System...");
            await aiActorSystem.InitializeAsync();
            
            Console.WriteLine("2. Starting AI Actor System...");
            await aiActorSystem.StartAsync();

            // 3. Register sample AI agents
            Console.WriteLine("3. Registering AI agents...");
            
            // Create Echo Agent
            var echoAgentProps = Props.Create(() => new EchoAIAgent(
                serviceProvider.GetRequiredService<ILogger<EchoAIAgent>>(),
                "echo-agent"));
            
            var echoMetadata = new AgentMetadata(
                "echo-agent", "Echo Agent", "Echoes user input", "1.0.0",
                new[] { "echo", "testing" }, true);
            
            await aiActorSystem.RegisterAgentAsync("echo-agent", echoAgentProps, echoMetadata);

            // Create Text Analysis Agent
            var analysisAgentProps = Props.Create(() => new TextAnalysisAIAgent(
                serviceProvider.GetRequiredService<ILogger<TextAnalysisAIAgent>>(),
                "text-analysis-agent"));
            
            var analysisMetadata = new AgentMetadata(
                "text-analysis-agent", "Text Analysis Agent", "Analyzes text content", "1.0.0",
                new[] { "text-analysis", "nlp" }, true);
            
            await aiActorSystem.RegisterAgentAsync("text-analysis-agent", analysisAgentProps, analysisMetadata);

            // 4. Demonstrate agent usage
            await DemonstrateAgentUsage(aiActorSystem);

            Console.WriteLine("\n9. Stopping AI Actor System...");
            await aiActorSystem.StopAsync();
            
            Console.WriteLine("Example completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
        finally
        {
            await aiActorSystem.DisposeAsync();
            serviceProvider.Dispose();
        }
    }

    private static async Task DemonstrateAgentUsage(AIActorSystem aiActorSystem)
    {
        var agentDirector = aiActorSystem.GetAgentDirector();
        if (agentDirector == null)
        {
            throw new InvalidOperationException("Agent director not available");
        }

        Console.WriteLine("4. Getting available agents...");
        var availableAgentsResponse = await agentDirector.Ask<AvailableAgentsResponse>(
            new GetAvailableAgents(), TimeSpan.FromSeconds(5));
        
        Console.WriteLine($"Available agents: {string.Join(", ", availableAgentsResponse.AgentIds)}");

        Console.WriteLine("\n5. Getting agent information...");
        foreach (var agentId in availableAgentsResponse.AgentIds)
        {
            var agentInfoResponse = await agentDirector.Ask<AgentInfoResponse>(
                new GetAgentInfo(agentId), TimeSpan.FromSeconds(5));
            
            if (agentInfoResponse.Metadata != null)
            {
                Console.WriteLine($"Agent: {agentInfoResponse.Metadata.Name} ({agentInfoResponse.Metadata.Id})");
                Console.WriteLine($"  Description: {agentInfoResponse.Metadata.Description}");
                Console.WriteLine($"  Capabilities: {string.Join(", ", agentInfoResponse.Metadata.Capabilities)}");
            }
        }

        Console.WriteLine("\n6. Testing Echo Agent...");
        var echoResponse = await agentDirector.Ask<AgentResponse>(
            new InvokeAgent("echo-agent", "Hello, AI Actor System!"),
            TimeSpan.FromSeconds(10));
        
        Console.WriteLine($"Echo response: {echoResponse.Output}");

        Console.WriteLine("\n7. Testing Text Analysis Agent...");
        var analysisResponse = await agentDirector.Ask<AgentResponse>(
            new InvokeAgent("text-analysis-agent", "This is a sample text for analysis. It contains multiple sentences! How interesting?"),
            TimeSpan.FromSeconds(10));
        
        Console.WriteLine($"Analysis response:\n{analysisResponse.Output}");

        Console.WriteLine("\n8. Testing streaming response...");
        // Note: In a real application, you would handle streaming responses differently
        // This is a simplified example for demonstration purposes
        agentDirector.Tell(new StreamAgent("echo-agent", "streaming test with multiple words"));
        await Task.Delay(1000); // Allow time for streaming to complete
    }
}

/// <summary>
/// Simple console application entry point for running the example.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            await AIActorSystemExample.RunExampleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex}");
            Environment.Exit(1);
        }
    }
}
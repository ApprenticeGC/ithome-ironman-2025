using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;

namespace GameConsole.Core.Registry.Examples;

/// <summary>
/// Example demonstration of AI Agent Discovery and Registration system.
/// This example shows how to register agents using attributes and discover them by capabilities.
/// </summary>
public static class AgentDiscoveryExample
{
    public static async Task RunExampleAsync()
    {
        Console.WriteLine("=== AI Agent Discovery and Registration Example ===");
        Console.WriteLine();

        // Create service provider
        using var serviceProvider = new ServiceProvider();

        // Register agents from the current assembly
        Console.WriteLine("1. Registering agents from assembly...");
        serviceProvider.RegisterAgentsFromAttributes(Assembly.GetExecutingAssembly());

        // Get all registered agents
        var allAgents = serviceProvider.GetRegisteredAgents();
        Console.WriteLine($"   Found {allAgents.Count()} agents total");

        // Discover agents by capability
        Console.WriteLine("\n2. Discovering agents by capability...");
        var planningAgents = serviceProvider.GetAgentsWithCapability("Planning");
        Console.WriteLine($"   Found {planningAgents.Count()} agents with 'Planning' capability");

        var learningAgents = serviceProvider.GetAgentsWithCapability("Learning");
        Console.WriteLine($"   Found {learningAgents.Count()} agents with 'Learning' capability");

        // Discover agents by category
        Console.WriteLine("\n3. Discovering agents by category...");
        var strategicAgents = serviceProvider.GetAgentsByCategory("Strategic");
        Console.WriteLine($"   Found {strategicAgents.Count()} agents in 'Strategic' category");

        var tacticalAgents = serviceProvider.GetAgentsByCategory("Tactical");
        Console.WriteLine($"   Found {tacticalAgents.Count()} agents in 'Tactical' category");

        // Create and activate agents
        Console.WriteLine("\n4. Creating and activating agents...");
        var strategicAgent = serviceProvider.GetService<StrategicPlannerAgent>();
        if (strategicAgent != null)
        {
            await strategicAgent.InitializeAsync();
            await strategicAgent.ActivateAsync();
            Console.WriteLine($"   Activated agent: {strategicAgent.AgentId} ({strategicAgent.Metadata.Name})");
            await strategicAgent.DeactivateAsync();
        }

        var tacticalAgent = serviceProvider.GetService<TacticalExecutorAgent>();
        if (tacticalAgent != null)
        {
            await tacticalAgent.InitializeAsync();
            await tacticalAgent.ActivateAsync();
            Console.WriteLine($"   Activated agent: {tacticalAgent.AgentId} ({tacticalAgent.Metadata.Name})");
            await tacticalAgent.DeactivateAsync();
        }

        Console.WriteLine("\n5. Agent lifecycle complete!");
    }
}

// Example agent implementations

[Agent("Strategic Planner", "1.0.0", "An AI agent focused on high-level strategic planning",
       Categories = new[] { "Strategic", "AI", "Planning" },
       Capabilities = new[] { "Planning", "Strategy", "Decision Making" },
       Lifetime = ServiceLifetime.Singleton)]
public class StrategicPlannerAgent : IAgent
{
    public string AgentId { get; } = "strategic-planner-001";
    public bool IsActive { get; private set; }
    public IAgentMetadata Metadata { get; }

    public StrategicPlannerAgent()
    {
        Metadata = new AgentMetadata(
            "Strategic Planner",
            "1.0.0",
            "An AI agent focused on high-level strategic planning",
            new[] { "Strategic", "AI", "Planning" },
            new[] { "Planning", "Strategy", "Decision Making" },
            new Dictionary<string, object>
            {
                { "MaxPlanningDepth", 10 },
                { "PlanningAlgorithm", "Monte Carlo Tree Search" }
            });
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"   Initializing {Metadata.Name}...");
        return Task.CompletedTask;
    }

    public Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"   Activating {Metadata.Name}...");
        IsActive = true;
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"   Deactivating {Metadata.Name}...");
        IsActive = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Console.WriteLine($"   Disposing {Metadata.Name}...");
        return ValueTask.CompletedTask;
    }
}

[Agent("Tactical Executor", "1.0.0", "An AI agent focused on tactical execution",
       Categories = new[] { "Tactical", "AI", "Execution" },
       Capabilities = new[] { "Execution", "Learning", "Adaptation" },
       Lifetime = ServiceLifetime.Scoped)]
public class TacticalExecutorAgent : IAgent
{
    public string AgentId { get; } = "tactical-executor-001";
    public bool IsActive { get; private set; }
    public IAgentMetadata Metadata { get; }

    public TacticalExecutorAgent()
    {
        Metadata = new AgentMetadata(
            "Tactical Executor",
            "1.0.0",
            "An AI agent focused on tactical execution",
            new[] { "Tactical", "AI", "Execution" },
            new[] { "Execution", "Learning", "Adaptation" },
            new Dictionary<string, object>
            {
                { "MaxExecutionThreads", 4 },
                { "LearningRate", 0.1f }
            });
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"   Initializing {Metadata.Name}...");
        return Task.CompletedTask;
    }

    public Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"   Activating {Metadata.Name}...");
        IsActive = true;
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"   Deactivating {Metadata.Name}...");
        IsActive = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Console.WriteLine($"   Disposing {Metadata.Name}...");
        return ValueTask.CompletedTask;
    }
}

// Example implementation of IAgentMetadata
internal class AgentMetadata : IAgentMetadata
{
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }
    public IEnumerable<string> Categories { get; }
    public IEnumerable<string> Capabilities { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }

    public AgentMetadata(
        string name,
        string version,
        string description,
        IEnumerable<string> categories,
        IEnumerable<string> capabilities,
        IReadOnlyDictionary<string, object> properties)
    {
        Name = name;
        Version = version;
        Description = description;
        Categories = categories;
        Capabilities = capabilities;
        Properties = properties;
    }
}
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry.Examples;
using Xunit;

namespace GameConsole.Core.Registry.Tests;

public class AgentDiscoveryExampleTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public AgentDiscoveryExampleTests()
    {
        _serviceProvider = new ServiceProvider();
        // Register agents from the Core.Registry assembly (where the example agents are)
        _serviceProvider.RegisterAgentsFromAttributes(typeof(ServiceProvider).Assembly);
        // Also register from the test assembly (for TestAgent and AnotherTestAgent)
        _serviceProvider.RegisterAgentsFromAttributes(typeof(AgentDiscoveryExampleTests).Assembly);
    }

    [Fact]
    public void ExampleAgents_AreRegisteredCorrectly()
    {
        // Act
        var allAgents = _serviceProvider.GetRegisteredAgents();
        
        // Assert
        Assert.NotEmpty(allAgents);
        
        var strategicAgents = allAgents.Where(a => 
            a.ImplementationType?.Name == nameof(StrategicPlannerAgent));
        var tacticalAgents = allAgents.Where(a => 
            a.ImplementationType?.Name == nameof(TacticalExecutorAgent));
            
        Assert.NotEmpty(strategicAgents);
        Assert.NotEmpty(tacticalAgents);
    }

    [Fact]
    public void ExampleAgents_CanBeDiscoveredByCapability()
    {
        // Act
        var planningAgents = _serviceProvider.GetAgentsWithCapability("Planning");
        var learningAgents = _serviceProvider.GetAgentsWithCapability("Learning");
        var executionAgents = _serviceProvider.GetAgentsWithCapability("Execution");
        
        // Assert
        Assert.NotEmpty(planningAgents);
        Assert.NotEmpty(learningAgents);
        Assert.NotEmpty(executionAgents);
    }

    [Fact]
    public void ExampleAgents_CanBeDiscoveredByCategory()
    {
        // Act
        var strategicAgents = _serviceProvider.GetAgentsByCategory("Strategic");
        var tacticalAgents = _serviceProvider.GetAgentsByCategory("Tactical");
        var aiAgents = _serviceProvider.GetAgentsByCategory("AI");
        
        // Assert
        Assert.NotEmpty(strategicAgents);
        Assert.NotEmpty(tacticalAgents);
        Assert.NotEmpty(aiAgents);
    }

    [Fact]
    public async Task ExampleAgents_CanBeCreatedAndActivated()
    {
        // Act
        var strategicAgent = _serviceProvider.GetService<StrategicPlannerAgent>();
        var tacticalAgent = _serviceProvider.GetService<TacticalExecutorAgent>();
        
        // Assert
        Assert.NotNull(strategicAgent);
        Assert.NotNull(tacticalAgent);
        
        // Test lifecycle
        Assert.False(strategicAgent.IsActive);
        await strategicAgent.InitializeAsync();
        await strategicAgent.ActivateAsync();
        Assert.True(strategicAgent.IsActive);
        await strategicAgent.DeactivateAsync();
        Assert.False(strategicAgent.IsActive);

        Assert.False(tacticalAgent.IsActive);
        await tacticalAgent.InitializeAsync();
        await tacticalAgent.ActivateAsync();
        Assert.True(tacticalAgent.IsActive);
        await tacticalAgent.DeactivateAsync();
        Assert.False(tacticalAgent.IsActive);
    }

    [Fact]
    public void ExampleAgents_HaveCorrectMetadata()
    {
        // Act
        var strategicAgent = _serviceProvider.GetService<StrategicPlannerAgent>();
        var tacticalAgent = _serviceProvider.GetService<TacticalExecutorAgent>();
        
        // Assert
        Assert.NotNull(strategicAgent);
        Assert.NotNull(tacticalAgent);
        
        Assert.Equal("Strategic Planner", strategicAgent.Metadata.Name);
        Assert.Contains("Strategic", strategicAgent.Metadata.Categories);
        Assert.Contains("Planning", strategicAgent.Metadata.Capabilities);
        
        Assert.Equal("Tactical Executor", tacticalAgent.Metadata.Name);
        Assert.Contains("Tactical", tacticalAgent.Metadata.Categories);
        Assert.Contains("Learning", tacticalAgent.Metadata.Capabilities);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
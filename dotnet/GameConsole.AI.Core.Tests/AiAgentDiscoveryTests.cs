using GameConsole.AI.Core;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.AI.Core.Tests;

public class AiAgentDiscoveryTests
{
    [Fact]
    public async Task DiscoverAllAsync_WithMultipleAgents_ShouldReturnAllAgents()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var discovery = new AiAgentDiscovery(registry);
        
        var agent1 = new TestAiAgent("agent-1", "Agent 1", "First agent", "capability-1");
        var agent2 = new TestAiAgent("agent-2", "Agent 2", "Second agent", "capability-2");
        
        await registry.RegisterAsync(agent1);
        await registry.RegisterAsync(agent2);

        // Act
        var result = await discovery.DiscoverAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.AgentId == "agent-1");
        Assert.Contains(result, a => a.AgentId == "agent-2");
    }

    [Fact]
    public async Task DiscoverByCapabilityAsync_WithMatchingCapability_ShouldReturnMatchingAgents()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var discovery = new AiAgentDiscovery(registry);
        
        var agent1 = new TestAiAgent("agent-1", "Agent 1", "First agent", "capability-1", "capability-common");
        var agent2 = new TestAiAgent("agent-2", "Agent 2", "Second agent", "capability-2", "capability-common");
        var agent3 = new TestAiAgent("agent-3", "Agent 3", "Third agent", "capability-3");
        
        await registry.RegisterAsync(agent1);
        await registry.RegisterAsync(agent2);
        await registry.RegisterAsync(agent3);

        // Act
        var result = await discovery.DiscoverByCapabilityAsync("capability-common");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.AgentId == "agent-1");
        Assert.Contains(result, a => a.AgentId == "agent-2");
        Assert.DoesNotContain(result, a => a.AgentId == "agent-3");
    }

    [Fact]
    public async Task DiscoverByCapabilityAsync_WithNoMatchingCapability_ShouldReturnEmptyList()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var discovery = new AiAgentDiscovery(registry);
        
        var agent = new TestAiAgent("agent-1", "Agent 1", "First agent", "capability-1");
        await registry.RegisterAsync(agent);

        // Act
        var result = await discovery.DiscoverByCapabilityAsync("non-existing-capability");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task DiscoverByStatusAsync_WithMatchingStatus_ShouldReturnMatchingAgents()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var discovery = new AiAgentDiscovery(registry);
        
        var agent1 = new TestAiAgent("agent-1", "Agent 1", "First agent", "capability-1");
        var agent2 = new TestAiAgent("agent-2", "Agent 2", "Second agent", "capability-2");
        
        await registry.RegisterAsync(agent1);
        await registry.RegisterAsync(agent2);
        
        // Start only agent1
        await agent1.InitializeAsync();
        await agent1.StartAsync();

        // Act
        var readyAgents = await discovery.DiscoverByStatusAsync(AiAgentStatus.Ready);
        var inactiveAgents = await discovery.DiscoverByStatusAsync(AiAgentStatus.Inactive);

        // Assert
        Assert.Single(readyAgents);
        Assert.Equal("agent-1", readyAgents[0].AgentId);
        
        Assert.Single(inactiveAgents);
        Assert.Equal("agent-2", inactiveAgents[0].AgentId);
    }

    [Fact]
    public async Task DiscoverByIdAsync_WithExistingAgent_ShouldReturnAgent()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var discovery = new AiAgentDiscovery(registry);
        
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");
        await registry.RegisterAsync(agent);

        // Act
        var result = await discovery.DiscoverByIdAsync("test-agent");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-agent", result.AgentId);
        Assert.Equal("Test Agent", result.Name);
    }

    [Fact]
    public async Task DiscoverByIdAsync_WithNonExistingAgent_ShouldReturnNull()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var discovery = new AiAgentDiscovery(registry);

        // Act
        var result = await discovery.DiscoverByIdAsync("non-existing");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AgentDiscovered_Event_ShouldBeRaisedWhenAgentRegistered()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var discovery = new AiAgentDiscovery(registry);
        await discovery.InitializeAsync();
        
        AiAgentDiscoveredEventArgs? eventArgs = null;
        discovery.AgentDiscovered += (_, args) => eventArgs = args;
        
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");

        // Act
        await registry.RegisterAsync(agent);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("test-agent", eventArgs.Agent.AgentId);
    }

    [Fact]
    public async Task AgentUnavailable_Event_ShouldBeRaisedWhenAgentUnregistered()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var discovery = new AiAgentDiscovery(registry);
        await discovery.InitializeAsync();
        
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");
        await registry.RegisterAsync(agent);
        
        AiAgentUnavailableEventArgs? eventArgs = null;
        discovery.AgentUnavailable += (_, args) => eventArgs = args;

        // Act
        await registry.UnregisterAsync("test-agent");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("test-agent", eventArgs.AgentId);
    }
}
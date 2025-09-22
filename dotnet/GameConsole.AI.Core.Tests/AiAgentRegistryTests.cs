using GameConsole.AI.Core;
using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.AI.Core.Tests;

public class AiAgentRegistryTests
{
    [Fact]
    public async Task RegisterAsync_WithNewAgent_ShouldReturnTrue()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");

        // Act
        var result = await registry.RegisterAsync(agent);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingAgent_ShouldReturnFalse()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");
        await registry.RegisterAsync(agent);

        // Act
        var result = await registry.RegisterAsync(agent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAsync_WithExistingAgent_ShouldReturnAgent()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");
        await registry.RegisterAsync(agent);

        // Act
        var result = await registry.GetAsync("test-agent");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-agent", result.AgentId);
        Assert.Equal("Test Agent", result.Name);
    }

    [Fact]
    public async Task GetAsync_WithNonExistingAgent_ShouldReturnNull()
    {
        // Arrange
        var registry = new AiAgentRegistry();

        // Act
        var result = await registry.GetAsync("non-existing");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UnregisterAsync_WithExistingAgent_ShouldReturnTrue()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");
        await registry.RegisterAsync(agent);

        // Act
        var result = await registry.UnregisterAsync("test-agent");

        // Assert
        Assert.True(result);
        
        // Verify agent is no longer registered
        var retrievedAgent = await registry.GetAsync("test-agent");
        Assert.Null(retrievedAgent);
    }

    [Fact]
    public async Task UnregisterAsync_WithNonExistingAgent_ShouldReturnFalse()
    {
        // Arrange
        var registry = new AiAgentRegistry();

        // Act
        var result = await registry.UnregisterAsync("non-existing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsRegisteredAsync_WithExistingAgent_ShouldReturnTrue()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");
        await registry.RegisterAsync(agent);

        // Act
        var result = await registry.IsRegisteredAsync("test-agent");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsRegisteredAsync_WithNonExistingAgent_ShouldReturnFalse()
    {
        // Arrange
        var registry = new AiAgentRegistry();

        // Act
        var result = await registry.IsRegisteredAsync("non-existing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllAsync_WithMultipleAgents_ShouldReturnAllAgents()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var agent1 = new TestAiAgent("agent-1", "Agent 1", "First agent", "capability-1");
        var agent2 = new TestAiAgent("agent-2", "Agent 2", "Second agent", "capability-2");
        await registry.RegisterAsync(agent1);
        await registry.RegisterAsync(agent2);

        // Act
        var result = await registry.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.AgentId == "agent-1");
        Assert.Contains(result, a => a.AgentId == "agent-2");
    }

    [Fact]
    public async Task AgentRegistered_Event_ShouldBeRaised()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");
        AiAgentRegisteredEventArgs? eventArgs = null;
        registry.AgentRegistered += (_, args) => eventArgs = args;

        // Act
        await registry.RegisterAsync(agent);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("test-agent", eventArgs.Agent.AgentId);
    }

    [Fact]
    public async Task AgentUnregistered_Event_ShouldBeRaised()
    {
        // Arrange
        var registry = new AiAgentRegistry();
        var agent = new TestAiAgent("test-agent", "Test Agent", "A test agent", "test-capability");
        await registry.RegisterAsync(agent);
        
        AiAgentUnregisteredEventArgs? eventArgs = null;
        registry.AgentUnregistered += (_, args) => eventArgs = args;

        // Act
        await registry.UnregisterAsync("test-agent");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("test-agent", eventArgs.AgentId);
    }
}
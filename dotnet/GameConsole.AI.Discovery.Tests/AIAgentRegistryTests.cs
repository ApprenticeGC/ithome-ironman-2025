using GameConsole.AI.Discovery;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.AI.Discovery.Tests;

public class AIAgentRegistryTests
{
    private readonly AIAgentRegistry _registry;
    private readonly AgentMetadata _testAgent;

    public AIAgentRegistryTests()
    {
        _registry = new AIAgentRegistry(NullLogger<AIAgentRegistry>.Instance);
        _testAgent = new AgentMetadata
        {
            Id = "test-agent",
            Name = "Test Agent",
            AgentType = typeof(TestAIAgent),
            Priority = 5,
            Tags = new[] { "test" }.AsReadOnly()
        };
    }

    [Fact]
    public async Task RegisterAgentAsync_WithValidAgent_RegistersSuccessfully()
    {
        // Act
        await _registry.RegisterAgentAsync(_testAgent);

        // Assert
        var retrievedAgent = await _registry.GetAgentByIdAsync(_testAgent.Id);
        Assert.NotNull(retrievedAgent);
        Assert.Equal(_testAgent.Id, retrievedAgent.Id);
        Assert.Equal(_testAgent.Name, retrievedAgent.Name);
    }

    [Fact]
    public async Task RegisterAgentAsync_WithNullAgent_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _registry.RegisterAgentAsync(null!));
    }

    [Fact]
    public async Task UnregisterAgentAsync_WithExistingAgent_ReturnsTrue()
    {
        // Arrange
        await _registry.RegisterAgentAsync(_testAgent);

        // Act
        var result = await _registry.UnregisterAgentAsync(_testAgent.Id);

        // Assert
        Assert.True(result);
        var retrievedAgent = await _registry.GetAgentByIdAsync(_testAgent.Id);
        Assert.Null(retrievedAgent);
    }

    [Fact]
    public async Task UnregisterAgentAsync_WithNonExistentAgent_ReturnsFalse()
    {
        // Act
        var result = await _registry.UnregisterAgentAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    public async Task UnregisterAgentAsync_WithInvalidId_ThrowsException(string agentId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _registry.UnregisterAgentAsync(agentId));
    }

    [Fact]
    public async Task UnregisterAgentAsync_WithNullId_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _registry.UnregisterAgentAsync(null!));
    }

    [Fact]
    public async Task GetAllAgentsAsync_WithRegisteredAgents_ReturnsAll()
    {
        // Arrange
        var agent1 = new AgentMetadata { Id = "agent1", Name = "Agent 1", AgentType = typeof(TestAIAgent) };
        var agent2 = new AgentMetadata { Id = "agent2", Name = "Agent 2", AgentType = typeof(BasicTestAgent) };

        await _registry.RegisterAgentAsync(agent1);
        await _registry.RegisterAgentAsync(agent2);

        // Act
        var allAgents = await _registry.GetAllAgentsAsync();

        // Assert
        Assert.Equal(2, allAgents.Count());
        Assert.Contains(allAgents, a => a.Id == "agent1");
        Assert.Contains(allAgents, a => a.Id == "agent2");
    }

    [Fact]
    public async Task GetAgentsByTagsAsync_WithMatchingTags_ReturnsMatchingAgents()
    {
        // Arrange
        var agent1 = new AgentMetadata 
        { 
            Id = "agent1", 
            Name = "Agent 1", 
            AgentType = typeof(TestAIAgent),
            Tags = new[] { "test", "basic" }.AsReadOnly()
        };
        var agent2 = new AgentMetadata 
        { 
            Id = "agent2", 
            Name = "Agent 2", 
            AgentType = typeof(BasicTestAgent),
            Tags = new[] { "advanced" }.AsReadOnly()
        };

        await _registry.RegisterAgentAsync(agent1);
        await _registry.RegisterAgentAsync(agent2);

        // Act
        var testTaggedAgents = await _registry.GetAgentsByTagsAsync(new[] { "test" });

        // Assert
        Assert.Single(testTaggedAgents);
        Assert.Equal("agent1", testTaggedAgents.First().Id);
    }

    [Fact]
    public async Task GetAgentsByTagsAsync_WithRequireAll_ReturnsExactMatches()
    {
        // Arrange
        var agent1 = new AgentMetadata 
        { 
            Id = "agent1", 
            Name = "Agent 1", 
            AgentType = typeof(TestAIAgent),
            Tags = new[] { "test", "basic" }.AsReadOnly()
        };
        var agent2 = new AgentMetadata 
        { 
            Id = "agent2", 
            Name = "Agent 2", 
            AgentType = typeof(BasicTestAgent),
            Tags = new[] { "test" }.AsReadOnly()
        };

        await _registry.RegisterAgentAsync(agent1);
        await _registry.RegisterAgentAsync(agent2);

        // Act
        var exactMatches = await _registry.GetAgentsByTagsAsync(new[] { "test", "basic" }, requireAll: true);

        // Assert
        Assert.Single(exactMatches);
        Assert.Equal("agent1", exactMatches.First().Id);
    }

    [Fact]
    public async Task UpdateAgentAvailabilityAsync_WithExistingAgent_UpdatesSuccessfully()
    {
        // Arrange
        await _registry.RegisterAgentAsync(_testAgent);

        // Act
        var result = await _registry.UpdateAgentAvailabilityAsync(_testAgent.Id, false);

        // Assert
        Assert.True(result);
        var updatedAgent = await _registry.GetAgentByIdAsync(_testAgent.Id);
        Assert.NotNull(updatedAgent);
        Assert.False(updatedAgent.IsAvailable);
    }

    [Fact]
    public async Task UpdateAgentAvailabilityAsync_WithNonExistentAgent_ReturnsFalse()
    {
        // Act
        var result = await _registry.UpdateAgentAvailabilityAsync("non-existent", false);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("")]
    public async Task UpdateAgentAvailabilityAsync_WithInvalidId_ThrowsException(string agentId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _registry.UpdateAgentAvailabilityAsync(agentId, true));
    }

    [Fact]
    public async Task UpdateAgentAvailabilityAsync_WithNullId_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _registry.UpdateAgentAvailabilityAsync(null!, true));
    }

    [Fact]
    public async Task GetAgentByIdAsync_WithNullTags_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _registry.GetAgentsByTagsAsync(null!));
    }

    [Theory]
    [InlineData("")]
    public async Task GetAgentByIdAsync_WithInvalidId_ThrowsException(string agentId)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _registry.GetAgentByIdAsync(agentId));
    }

    [Fact]
    public async Task GetAgentByIdAsync_WithNullId_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _registry.GetAgentByIdAsync(null!));
    }
}
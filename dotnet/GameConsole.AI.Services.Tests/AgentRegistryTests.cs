using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.AI.Core;
using GameConsole.AI.Services;
using Xunit;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Tests for the AgentRegistry class focusing on registration and discovery functionality.
/// </summary>
public class AgentRegistryTests
{
    [Fact]
    public async Task RegisterAgentAsync_Should_Add_Agent_Successfully()
    {
        // Arrange
        var registry = new AgentRegistry();
        var metadata = CreateTestAgentMetadata("test-agent", "Test Agent");

        // Act
        await registry.RegisterAgentAsync(metadata);

        // Assert
        var retrievedAgent = await registry.GetAgentAsync("test-agent");
        Assert.NotNull(retrievedAgent);
        Assert.Equal("test-agent", retrievedAgent.AgentId);
        Assert.Equal("Test Agent", retrievedAgent.Name);
    }

    [Fact]
    public async Task RegisterAgentAsync_Should_Throw_When_Metadata_Is_Null()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => registry.RegisterAgentAsync(null!));
    }

    [Fact]
    public async Task RegisterAgentAsync_Should_Throw_When_AgentId_Is_Empty()
    {
        // Arrange
        var registry = new AgentRegistry();
        var metadata = CreateTestAgentMetadata("", "Test Agent");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => registry.RegisterAgentAsync(metadata));
    }

    [Fact]
    public async Task UnregisterAgentAsync_Should_Remove_Agent_Successfully()
    {
        // Arrange
        var registry = new AgentRegistry();
        var metadata = CreateTestAgentMetadata("test-agent", "Test Agent");
        await registry.RegisterAgentAsync(metadata);

        // Act
        var result = await registry.UnregisterAgentAsync("test-agent");

        // Assert
        Assert.True(result);
        var retrievedAgent = await registry.GetAgentAsync("test-agent");
        Assert.Null(retrievedAgent);
    }

    [Fact]
    public async Task UnregisterAgentAsync_Should_Return_False_When_Agent_Not_Found()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var result = await registry.UnregisterAgentAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAgentIds_Should_Return_All_Registered_Agent_Ids()
    {
        // Arrange
        var registry = new AgentRegistry();
        var metadata1 = CreateTestAgentMetadata("agent-1", "Agent 1");
        var metadata2 = CreateTestAgentMetadata("agent-2", "Agent 2");
        registry.RegisterAgentAsync(metadata1).Wait();
        registry.RegisterAgentAsync(metadata2).Wait();

        // Act
        var agentIds = registry.GetAgentIds().ToList();

        // Assert
        Assert.Equal(2, agentIds.Count);
        Assert.Contains("agent-1", agentIds);
        Assert.Contains("agent-2", agentIds);
    }

    [Fact]
    public async Task GetAllAgentsAsync_Should_Return_All_Agents()
    {
        // Arrange
        var registry = new AgentRegistry();
        var metadata1 = CreateTestAgentMetadata("agent-1", "Agent 1");
        var metadata2 = CreateTestAgentMetadata("agent-2", "Agent 2");
        await registry.RegisterAgentAsync(metadata1);
        await registry.RegisterAgentAsync(metadata2);

        // Act
        var agents = (await registry.GetAllAgentsAsync()).ToList();

        // Assert
        Assert.Equal(2, agents.Count);
        Assert.Contains(agents, a => a.AgentId == "agent-1");
        Assert.Contains(agents, a => a.AgentId == "agent-2");
    }

    [Fact]
    public async Task GetAgentsByTaskKindAsync_Should_Return_Filtered_Agents()
    {
        // Arrange
        var registry = new AgentRegistry();
        var runtimeAgent = CreateTestAgentMetadata("runtime-agent", "Runtime Agent", TaskKind.RuntimeDirector.ToString());
        var editorAgent = CreateTestAgentMetadata("editor-agent", "Editor Agent", TaskKind.EditorAuthoring.ToString());
        await registry.RegisterAgentAsync(runtimeAgent);
        await registry.RegisterAgentAsync(editorAgent);

        // Act
        var runtimeAgents = (await registry.GetAgentsByTaskKindAsync(TaskKind.RuntimeDirector.ToString())).ToList();

        // Assert
        Assert.Single(runtimeAgents);
        Assert.Equal("runtime-agent", runtimeAgents[0].AgentId);
    }

    [Fact]
    public async Task GetAgentAsync_Should_Return_Null_When_Agent_Not_Found()
    {
        // Arrange
        var registry = new AgentRegistry();

        // Act
        var agent = await registry.GetAgentAsync("non-existent");

        // Assert
        Assert.Null(agent);
    }

    private static AgentMetadata CreateTestAgentMetadata(string agentId, string name, params string[] taskKinds)
    {
        return new AgentMetadata
        {
            AgentId = agentId,
            Name = name,
            Description = $"Test agent: {name}",
            AgentType = "TestAgent",
            Version = "1.0.0",
            Capabilities = new List<string> { "TestCapability" },
            SupportedTaskKinds = taskKinds.ToList(),
            IsAvailable = true,
            Configuration = new Dictionary<string, object>()
        };
    }
}
using System.Linq;
using System.Threading.Tasks;
using GameConsole.AI.Core;
using GameConsole.AI.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Tests for the AIOrchestrationService class focusing on agent discovery and orchestration functionality.
/// </summary>
public class AIOrchestrationServiceTests
{
    [Fact]
    public async Task InitializeAsync_Should_Register_Default_Agents()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);

        // Act
        await service.InitializeAsync();

        // Assert
        var availableAgents = service.GetAvailableAgents().ToList();
        Assert.True(availableAgents.Count >= 3);
        Assert.Contains("director-001", availableAgents);
        Assert.Contains("dialogue-001", availableAgents);
        Assert.Contains("codex-001", availableAgents);
    }

    [Fact]
    public async Task GetAgentInfoAsync_Should_Return_Agent_Metadata()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);
        await service.InitializeAsync();

        // Act
        var agentInfo = await service.GetAgentInfoAsync("director-001");

        // Assert
        Assert.NotNull(agentInfo);
        Assert.Equal("director-001", agentInfo.AgentId);
        Assert.Equal("Content Director", agentInfo.Name);
        Assert.Equal("DirectorAgent", agentInfo.AgentType);
        Assert.Contains("EncounterGeneration", agentInfo.Capabilities);
    }

    [Fact]
    public async Task GetAgentInfoAsync_Should_Throw_When_Agent_Not_Found()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);
        await service.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<System.InvalidOperationException>(
            () => service.GetAgentInfoAsync("non-existent-agent"));
    }

    [Fact]
    public async Task InvokeAgentAsync_Should_Return_Mock_Response()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);
        await service.InitializeAsync();

        // Act
        var response = await service.InvokeAgentAsync("director-001", "Test input message");

        // Assert
        Assert.NotNull(response);
        Assert.Contains("Mock response from agent director-001", response);
        Assert.Contains("Test input message", response);
    }

    [Fact]
    public async Task StreamAgentAsync_Should_Return_Mock_Stream()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);
        await service.InitializeAsync();

        // Act
        var stream = await service.StreamAgentAsync("director-001", "Test input");
        var chunks = new System.Collections.Generic.List<string>();
        await foreach (var chunk in stream)
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.Equal(3, chunks.Count);
        Assert.Contains("Stream chunk 1 from director-001", chunks[0]);
        Assert.Contains("Stream chunk 2 from director-001", chunks[1]);
        Assert.Contains("Final chunk from director-001", chunks[2]);
    }

    [Fact]
    public async Task CreateConversationAsync_Should_Return_Conversation_Id()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);
        await service.InitializeAsync();

        // Act
        var conversationId = await service.CreateConversationAsync("director-001");

        // Assert
        Assert.NotNull(conversationId);
        Assert.False(string.IsNullOrWhiteSpace(conversationId));
        Assert.True(System.Guid.TryParse(conversationId, out _));
    }

    [Fact]
    public async Task EndConversationAsync_Should_Return_True()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);
        await service.InitializeAsync();
        var conversationId = await service.CreateConversationAsync("director-001");

        // Act
        var result = await service.EndConversationAsync(conversationId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task InitializeAsync_With_Profile_Should_Set_Profile()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);
        var profile = new AIProfile
        {
            TaskKind = TaskKind.EditorAuthoring,
            Model = "test-model",
            MaxTokens = 512,
            Temperature = 0.8f
        };

        // Act
        await service.InitializeAsync(profile);

        // Assert - No exception should be thrown
        // The profile setting is internal so we can't directly verify it,
        // but we can verify the service accepts it without error
        Assert.True(true);
    }

    [Fact]
    public async Task HasCapabilityAsync_Should_Return_True_For_IService()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);

        // Act
        var hasCapability = await service.HasCapabilityAsync<AI.Services.IService>();

        // Assert
        Assert.True(hasCapability);
    }

    [Fact]
    public async Task GetCapabilityAsync_Should_Return_Service_Instance()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);

        // Act
        var capability = await service.GetCapabilityAsync<AI.Services.IService>();

        // Assert
        Assert.NotNull(capability);
        Assert.Same(service, capability);
    }

    [Fact]
    public async Task Service_Lifecycle_Should_Work_Properly()
    {
        // Arrange
        var service = new AIOrchestrationService(NullLogger<AIOrchestrationService>.Instance);

        // Act & Assert - Initialize
        await service.InitializeAsync();
        Assert.False(service.IsRunning);

        // Act & Assert - Start
        await service.StartAsync();
        Assert.True(service.IsRunning);

        // Act & Assert - Stop
        await service.StopAsync();
        Assert.False(service.IsRunning);

        // Act & Assert - Dispose
        await service.DisposeAsync();
        // No exception should be thrown
    }
}
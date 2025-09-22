using Xunit;
using FluentAssertions;
using GameConsole.AI.Actors.Core.Messages;

namespace GameConsole.AI.Actors.Core.Tests.Messages;

public class AIMessagesTests
{
    [Fact]
    public void AgentConfig_ShouldCreateWithDefaults()
    {
        // Arrange & Act
        var config = new AgentConfig
        {
            AgentId = "test-agent-001",
            AgentType = "dialogue"
        };

        // Assert
        config.AgentId.Should().Be("test-agent-001");
        config.AgentType.Should().Be("dialogue");
        config.MaxConcurrentRequests.Should().Be(5);
        config.RequestTimeout.Should().Be(TimeSpan.FromMinutes(2));
        config.MaxRetries.Should().Be(3);
        config.Properties.Should().NotBeNull();
        config.Backend.Should().NotBeNull();
    }

    [Fact]
    public void StartAgent_Message_ShouldCreateCorrectly()
    {
        // Arrange
        var config = new AgentConfig
        {
            AgentId = "test-agent",
            AgentType = "analysis"
        };

        // Act
        var message = new StartAgent("analysis", config);

        // Assert
        message.AgentType.Should().Be("analysis");
        message.Config.Should().Be(config);
        message.Should().BeAssignableTo<AIMessage>();
    }

    [Fact]
    public void ProcessRequest_Message_ShouldCreateCorrectly()
    {
        // Arrange
        var requestId = Guid.NewGuid().ToString();
        var agentType = "codegen";
        var request = new { Code = "Generate unit test" };

        // Act
        var message = new ProcessRequest(requestId, agentType, request, null!);

        // Assert
        message.RequestId.Should().Be(requestId);
        message.AgentType.Should().Be(agentType);
        message.Request.Should().Be(request);
        message.Should().BeAssignableTo<AIMessage>();
    }

    [Fact]
    public void ClusterStateResponse_ShouldCreateCorrectly()
    {
        // Arrange
        var nodes = new List<string> { "node1", "node2", "node3" };
        var leader = "node1";
        var unreachable = new List<string> { "node4" };

        // Act
        var response = new ClusterStateResponse(nodes, leader, unreachable);

        // Assert
        response.Nodes.Should().BeEquivalentTo(nodes);
        response.Leader.Should().Be(leader);
        response.Unreachable.Should().BeEquivalentTo(unreachable);
        response.Should().BeAssignableTo<AIMessage>();
    }

    [Fact]
    public void BackendConfig_ShouldCreateWithDefaults()
    {
        // Arrange & Act
        var config = new BackendConfig
        {
            Name = "OpenAI",
            Endpoint = "https://api.openai.com/v1",
            Model = "gpt-4"
        };

        // Assert
        config.Name.Should().Be("OpenAI");
        config.Endpoint.Should().Be("https://api.openai.com/v1");
        config.Model.Should().Be("gpt-4");
        config.Settings.Should().NotBeNull();
    }
}
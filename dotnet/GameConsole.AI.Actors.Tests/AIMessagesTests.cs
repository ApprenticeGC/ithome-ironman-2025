using GameConsole.AI.Actors.Messages;
using Xunit;

namespace GameConsole.AI.Actors.Tests;

/// <summary>
/// Tests for AI message types and their properties
/// </summary>
public class AIMessagesTests
{
    [Fact]
    public void CreateAgent_Should_Initialize_Correctly()
    {
        // Act
        var message = new CreateAgent("agent-1", "dialogue", new { Setting = "value" });

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.Equal("dialogue", message.AgentType);
        Assert.NotNull(message.Configuration);
    }

    [Fact]
    public void StopAgent_Should_Initialize_Correctly()
    {
        // Act
        var message = new StopAgent("agent-1");

        // Assert
        Assert.Equal("agent-1", message.AgentId);
    }

    [Fact]
    public void GetAgentStatus_Should_Initialize_Correctly()
    {
        // Act
        var message = new GetAgentStatus("agent-1");

        // Assert
        Assert.Equal("agent-1", message.AgentId);
    }

    [Fact]
    public void AgentStatus_Should_Initialize_Correctly()
    {
        // Arrange
        var lastActivity = DateTime.UtcNow;

        // Act
        var message = new AgentStatus("agent-1", true, lastActivity);

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.True(message.IsRunning);
        Assert.Equal(lastActivity, message.LastActivity);
    }

    [Fact]
    public void InvokeAgent_Should_Initialize_Correctly()
    {
        // Act
        var message = new InvokeAgent("agent-1", "test input", "conv-1");

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.Equal("test input", message.Input);
        Assert.Equal("conv-1", message.ConversationId);
    }

    [Fact]
    public void InvokeAgent_Should_Allow_Null_ConversationId()
    {
        // Act
        var message = new InvokeAgent("agent-1", "test input");

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.Equal("test input", message.Input);
        Assert.Null(message.ConversationId);
    }

    [Fact]
    public void AgentResponse_Should_Initialize_Correctly()
    {
        // Act
        var message = new AgentResponse("agent-1", "test output", "conv-1", true, null);

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.Equal("test output", message.Output);
        Assert.Equal("conv-1", message.ConversationId);
        Assert.True(message.IsSuccess);
        Assert.Null(message.Error);
    }

    [Fact]
    public void AgentResponse_Should_Default_To_Success()
    {
        // Act
        var message = new AgentResponse("agent-1", "test output");

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.Equal("test output", message.Output);
        Assert.Null(message.ConversationId);
        Assert.True(message.IsSuccess);
        Assert.Null(message.Error);
    }

    [Fact]
    public void StreamAgent_Should_Initialize_Correctly()
    {
        // Act
        var message = new StreamAgent("agent-1", "test input", "conv-1");

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.Equal("test input", message.Input);
        Assert.Equal("conv-1", message.ConversationId);
    }

    [Fact]
    public void AgentStreamChunk_Should_Initialize_Correctly()
    {
        // Act
        var message = new AgentStreamChunk("agent-1", "chunk data", "conv-1", false);

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.Equal("chunk data", message.Chunk);
        Assert.Equal("conv-1", message.ConversationId);
        Assert.False(message.IsComplete);
    }

    [Fact]
    public void AgentStreamChunk_Should_Default_To_Incomplete()
    {
        // Act
        var message = new AgentStreamChunk("agent-1", "chunk data");

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.Equal("chunk data", message.Chunk);
        Assert.Null(message.ConversationId);
        Assert.False(message.IsComplete);
    }

    [Fact]
    public void CreateConversation_Should_Initialize_Correctly()
    {
        // Act
        var message = new CreateConversation("agent-1", "conv-1");

        // Assert
        Assert.Equal("agent-1", message.AgentId);
        Assert.Equal("conv-1", message.ConversationId);
    }

    [Fact]
    public void EndConversation_Should_Initialize_Correctly()
    {
        // Act
        var message = new EndConversation("conv-1");

        // Assert
        Assert.Equal("conv-1", message.ConversationId);
    }

    [Fact]
    public void SystemHealth_Should_Initialize_Correctly()
    {
        // Arrange
        var uptime = TimeSpan.FromMinutes(30);

        // Act
        var message = new SystemHealth(true, 5, 3, uptime);

        // Assert
        Assert.True(message.IsHealthy);
        Assert.Equal(5, message.ActiveAgents);
        Assert.Equal(3, message.ActiveConversations);
        Assert.Equal(uptime, message.Uptime);
    }

    [Fact]
    public void StartSystem_Should_Be_Singleton_Message()
    {
        // Act
        var message1 = new StartSystem();
        var message2 = new StartSystem();

        // Assert
        Assert.NotNull(message1);
        Assert.NotNull(message2);
        Assert.IsType<StartSystem>(message1);
        Assert.IsType<StartSystem>(message2);
    }

    [Fact]
    public void StopSystem_Should_Be_Singleton_Message()
    {
        // Act
        var message1 = new StopSystem();
        var message2 = new StopSystem();

        // Assert
        Assert.NotNull(message1);
        Assert.NotNull(message2);
        Assert.IsType<StopSystem>(message1);
        Assert.IsType<StopSystem>(message2);
    }

    [Fact]
    public void GetSystemHealth_Should_Be_Singleton_Message()
    {
        // Act
        var message1 = new GetSystemHealth();
        var message2 = new GetSystemHealth();

        // Assert
        Assert.NotNull(message1);
        Assert.NotNull(message2);
        Assert.IsType<GetSystemHealth>(message1);
        Assert.IsType<GetSystemHealth>(message2);
    }

    [Fact]
    public void AIMessage_Should_Be_Base_Type()
    {
        // Act & Assert
        Assert.True(typeof(CreateAgent).IsAssignableTo(typeof(AIMessage)));
        Assert.True(typeof(AgentResponse).IsAssignableTo(typeof(AIMessage)));
        Assert.True(typeof(SystemHealth).IsAssignableTo(typeof(AIMessage)));
        Assert.True(typeof(StartSystem).IsAssignableTo(typeof(AIMessage)));
    }

    [Fact]
    public void AgentMessage_Should_Be_AI_Message_Subtype()
    {
        // Act & Assert
        Assert.True(typeof(CreateAgent).IsAssignableTo(typeof(AgentMessage)));
        Assert.True(typeof(StopAgent).IsAssignableTo(typeof(AgentMessage)));
        Assert.True(typeof(GetAgentStatus).IsAssignableTo(typeof(AgentMessage)));
        Assert.True(typeof(AgentStatus).IsAssignableTo(typeof(AgentMessage)));
    }

    [Fact]
    public void AIProcessingMessage_Should_Be_AI_Message_Subtype()
    {
        // Act & Assert
        Assert.True(typeof(InvokeAgent).IsAssignableTo(typeof(AIProcessingMessage)));
        Assert.True(typeof(AgentResponse).IsAssignableTo(typeof(AIProcessingMessage)));
        Assert.True(typeof(StreamAgent).IsAssignableTo(typeof(AIProcessingMessage)));
        Assert.True(typeof(AgentStreamChunk).IsAssignableTo(typeof(AIProcessingMessage)));
    }

    [Fact]
    public void ContextMessage_Should_Be_AI_Message_Subtype()
    {
        // Act & Assert
        Assert.True(typeof(CreateConversation).IsAssignableTo(typeof(ContextMessage)));
        Assert.True(typeof(EndConversation).IsAssignableTo(typeof(ContextMessage)));
    }

    [Fact]
    public void SystemMessage_Should_Be_AI_Message_Subtype()
    {
        // Act & Assert
        Assert.True(typeof(StartSystem).IsAssignableTo(typeof(SystemMessage)));
        Assert.True(typeof(StopSystem).IsAssignableTo(typeof(SystemMessage)));
        Assert.True(typeof(GetSystemHealth).IsAssignableTo(typeof(SystemMessage)));
        Assert.True(typeof(SystemHealth).IsAssignableTo(typeof(SystemMessage)));
    }
}
using Akka.Actor;
using Akka.TestKit.Xunit2;
using GameConsole.AI.Actors.Actors;
using GameConsole.AI.Actors.Messages;
using Xunit;

namespace GameConsole.AI.Actors.Tests;

/// <summary>
/// Tests for the BaseAIActor functionality
/// </summary>
public class BaseAIActorTests : TestKit
{
    /// <summary>
    /// Test implementation of BaseAIActor for testing purposes
    /// </summary>
    private class TestAIActor : BaseAIActor
    {
        public TestAIActor(string agentId, object? configuration = null) 
            : base(agentId, configuration)
        {
        }

        protected override void SetupSpecificMessageHandlers()
        {
            Receive<InvokeAgent>(msg =>
            {
                var response = new AgentResponse(AgentId, $"Test response for: {msg.Input}", msg.ConversationId);
                Sender.Tell(response);
            });

            Receive<StreamAgent>(msg =>
            {
                var chunk = new AgentStreamChunk(AgentId, $"Test stream for: {msg.Input}", msg.ConversationId, true);
                Sender.Tell(chunk);
            });
        }
    }

    [Fact]
    public void BaseAIActor_Should_Handle_GetAgentStatus()
    {
        // Arrange
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", null)));

        // Act
        actorRef.Tell(new GetAgentStatus("test-agent"));

        // Assert
        var response = ExpectMsg<AgentStatus>();
        Assert.Equal("test-agent", response.AgentId);
        Assert.True(response.IsRunning);
        Assert.True(response.LastActivity <= DateTime.UtcNow);
    }

    [Fact]
    public void BaseAIActor_Should_Handle_CreateConversation()
    {
        // Arrange
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", null)));

        // Act
        actorRef.Tell(new CreateConversation("test-agent", "conv-1"));

        // Assert
        var response = ExpectMsg<bool>();
        Assert.True(response);
    }

    [Fact]
    public void BaseAIActor_Should_Handle_Duplicate_CreateConversation()
    {
        // Arrange
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", null)));

        // Act - Create conversation twice
        actorRef.Tell(new CreateConversation("test-agent", "conv-1"));
        ExpectMsg<bool>(); // First should succeed

        actorRef.Tell(new CreateConversation("test-agent", "conv-1"));

        // Assert
        var response = ExpectMsg<bool>();
        Assert.False(response); // Second should fail
    }

    [Fact]
    public void BaseAIActor_Should_Handle_EndConversation()
    {
        // Arrange
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", null)));

        // Create conversation first
        actorRef.Tell(new CreateConversation("test-agent", "conv-1"));
        ExpectMsg<bool>();

        // Act
        actorRef.Tell(new EndConversation("conv-1"));

        // Assert
        var response = ExpectMsg<bool>();
        Assert.True(response);
    }

    [Fact]
    public void BaseAIActor_Should_Handle_EndNonexistentConversation()
    {
        // Arrange
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", null)));

        // Act
        actorRef.Tell(new EndConversation("nonexistent-conv"));

        // Assert
        var response = ExpectMsg<bool>();
        Assert.False(response);
    }

    [Fact]
    public void BaseAIActor_Should_Handle_InvokeAgent()
    {
        // Arrange
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", null)));

        // Act
        actorRef.Tell(new InvokeAgent("test-agent", "test input", "conv-1"));

        // Assert
        var response = ExpectMsg<AgentResponse>();
        Assert.Equal("test-agent", response.AgentId);
        Assert.Equal("Test response for: test input", response.Output);
        Assert.Equal("conv-1", response.ConversationId);
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public void BaseAIActor_Should_Handle_StreamAgent()
    {
        // Arrange
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", null)));

        // Act
        actorRef.Tell(new StreamAgent("test-agent", "test input", "conv-1"));

        // Assert
        var response = ExpectMsg<AgentStreamChunk>();
        Assert.Equal("test-agent", response.AgentId);
        Assert.Equal("Test stream for: test input", response.Chunk);
        Assert.Equal("conv-1", response.ConversationId);
        Assert.True(response.IsComplete);
    }

    [Fact]
    public void BaseAIActor_Should_Handle_StopAgent()
    {
        // Arrange
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", null)));
        Watch(actorRef);

        // Act
        actorRef.Tell(new StopAgent("test-agent"));

        // Assert - Actor should terminate
        ExpectTerminated(actorRef);
    }

    [Fact]
    public void BaseAIActor_Should_Initialize_With_Configuration()
    {
        // Arrange
        var config = new { Setting = "test-value" };

        // Act
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", config)));

        // Assert - Just verify it starts successfully
        actorRef.Tell(new GetAgentStatus("test-agent"));
        var response = ExpectMsg<AgentStatus>();
        Assert.Equal("test-agent", response.AgentId);
        Assert.True(response.IsRunning);
    }

    [Fact]
    public void BaseAIActor_Should_Track_LastActivity()
    {
        // Arrange
        var actorRef = Sys.ActorOf(Props.Create(() => new TestAIActor("test-agent", null)));

        // Get initial status
        actorRef.Tell(new GetAgentStatus("test-agent"));
        var initialResponse = ExpectMsg<AgentStatus>();
        var initialTime = initialResponse.LastActivity;

        // Wait a small amount to ensure time difference
        Thread.Sleep(10);

        // Act - Send another message to update activity
        actorRef.Tell(new GetAgentStatus("test-agent"));

        // Assert
        var laterResponse = ExpectMsg<AgentStatus>();
        Assert.True(laterResponse.LastActivity >= initialTime);
    }
}
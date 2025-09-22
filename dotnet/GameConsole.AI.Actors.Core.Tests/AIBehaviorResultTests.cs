using GameConsole.AI.Actors.Core;
using Xunit;

namespace GameConsole.AI.Actors.Core.Tests;

public class AIBehaviorResultTests
{
    [Fact]
    public void Continue_CreatesContinueResult()
    {
        // Act
        var result = AIBehaviorResult.Continue();

        // Assert
        Assert.Equal(AIBehaviorAction.Continue, result.Action);
        Assert.Null(result.Response);
        Assert.Null(result.Target);
        Assert.Null(result.MessageToSend);
        Assert.Null(result.UpdatedState);
    }

    [Fact]
    public void Reply_CreatesReplyResult()
    {
        // Arrange
        var response = "test response";

        // Act
        var result = AIBehaviorResult.Reply(response);

        // Assert
        Assert.Equal(AIBehaviorAction.Reply, result.Action);
        Assert.Equal(response, result.Response);
    }

    [Fact]
    public void SendTo_CreatesSendMessageResult()
    {
        // Arrange
        var target = new MockActorRef("test-actor");
        var message = "test message";

        // Act
        var result = AIBehaviorResult.SendTo(target, message);

        // Assert
        Assert.Equal(AIBehaviorAction.SendMessage, result.Action);
        Assert.Equal(target, result.Target);
        Assert.Equal(message, result.MessageToSend);
    }

    [Fact]
    public void UpdateState_CreatesUpdateStateResult()
    {
        // Arrange
        var newState = new AIAgentState { AgentId = "test" };

        // Act
        var result = AIBehaviorResult.UpdateState(newState);

        // Assert
        Assert.Equal(AIBehaviorAction.UpdateState, result.Action);
        Assert.Equal(newState, result.UpdatedState);
    }
}

// Mock implementation for testing
public class MockActorRef : IActorRef
{
    public string Path { get; }
    public string Name { get; }
    public bool IsValid => true;

    public MockActorRef(string name)
    {
        Name = name;
        Path = $"/user/{name}";
    }

    public Task TellAsync(object message, IActorRef? sender = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<TResponse> AskAsync<TResponse>(object message, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(default(TResponse)!);
    }
}
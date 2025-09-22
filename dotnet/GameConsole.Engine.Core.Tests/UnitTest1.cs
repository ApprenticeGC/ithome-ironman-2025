using GameConsole.Core.Abstractions;
using GameConsole.Engine.Core;
using Xunit;

namespace GameConsole.Engine.Core.Tests;

// Test implementation of ActorBase for testing purposes
public class TestActor : ActorBase
{
    public override string ActorType => "TestActor";
    
    public List<IActorMessage> ReceivedMessages { get; } = new();
    
    public ActorMessageHandleResult NextHandleResult { get; set; } = ActorMessageHandleResult.Handled;
    
    public Exception? NextHandleException { get; set; }

    public TestActor(ActorId? actorId = null) : base(actorId)
    {
    }

    protected override Task<ActorMessageHandleResult> HandleMessageAsync(IActorMessage message, CancellationToken cancellationToken)
    {
        ReceivedMessages.Add(message);
        
        if (NextHandleException != null)
        {
            var exception = NextHandleException;
            NextHandleException = null; // Reset for next message
            throw exception;
        }
        
        return Task.FromResult(NextHandleResult);
    }
}

// Test message for testing purposes  
public class TestMessage : ActorMessage
{
    public string Content { get; }

    public TestMessage(string content) : base()
    {
        Content = content;
    }

    public TestMessage(string content, IActorMessage originalMessage) : base(originalMessage)
    {
        Content = content;
    }
}

public class ActorBaseTests : IAsyncDisposable
{
    private TestActor? _actor;

    public async ValueTask DisposeAsync()
    {
        if (_actor != null)
        {
            await _actor.DisposeAsync();
        }
    }

    [Fact]
    public async Task ActorBase_InitializeStartStop_ShouldFollowCorrectLifecycle()
    {
        // Arrange
        _actor = new TestActor();

        // Act & Assert - Initial state
        Assert.False(_actor.IsRunning);

        // Initialize
        await _actor.InitializeAsync();
        Assert.False(_actor.IsRunning);

        // Start
        await _actor.StartAsync();
        Assert.True(_actor.IsRunning);

        // Stop
        await _actor.StopAsync();
        Assert.False(_actor.IsRunning);
    }

    [Fact]
    public async Task ActorBase_SendMessage_WhenRunning_ShouldProcessMessage()
    {
        // Arrange
        _actor = new TestActor();
        await _actor.InitializeAsync();
        await _actor.StartAsync();

        var message = new TestMessage("test content");

        // Act
        await _actor.SendMessageAsync(message);
        
        // Wait a bit for message processing
        await Task.Delay(50);

        // Assert
        Assert.Contains(message, _actor.ReceivedMessages);
    }

    [Fact]
    public async Task ActorBase_SendMessage_WhenNotRunning_ShouldThrow()
    {
        // Arrange
        _actor = new TestActor();
        var message = new TestMessage("test content");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _actor.SendMessageAsync(message));
    }

    [Fact]
    public async Task ActorBase_SendRequestAsync_ShouldReceiveResponse()
    {
        // Arrange
        _actor = new TestActor();
        await _actor.InitializeAsync();
        await _actor.StartAsync();

        var request = new TestMessage("request");
        var response = new TestMessage("response", request);

        // Set up the actor to send back a response
        _actor.NextHandleResult = ActorMessageHandleResult.Handled;

        // Act - Start the request
        var requestTask = _actor.SendRequestAsync<TestMessage>(request, TimeSpan.FromSeconds(1));
        
        // Wait a bit for the request to be processed
        await Task.Delay(50);
        
        // Send the response manually (simulating another actor responding)
        await _actor.SendMessageAsync(response);
        
        // Wait for response
        var result = await requestTask;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("response", result.Content);
        Assert.Equal(request.MessageId, result.CorrelationId);
    }

    [Fact]
    public async Task ActorBase_SendRequestAsync_WithTimeout_ShouldReturnNull()
    {
        // Arrange
        _actor = new TestActor();
        await _actor.InitializeAsync();
        await _actor.StartAsync();

        var request = new TestMessage("request");

        // Act - Send request with very short timeout, don't send response
        var result = await _actor.SendRequestAsync<TestMessage>(request, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ActorBase_MessageProcessed_ShouldFireEvent()
    {
        // Arrange
        _actor = new TestActor();
        await _actor.InitializeAsync();
        await _actor.StartAsync();

        ActorMessageEventArgs? eventArgs = null;
        _actor.MessageProcessed += (sender, args) => eventArgs = args;

        var message = new TestMessage("test content");

        // Act
        await _actor.SendMessageAsync(message);
        
        // Wait for message processing
        await Task.Delay(50);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(message, eventArgs.Message);
        Assert.Equal(ActorMessageHandleResult.Handled, eventArgs.Result);
        Assert.Null(eventArgs.Exception);
        Assert.True(eventArgs.ProcessingTime >= TimeSpan.Zero);
    }

    [Fact]
    public async Task ActorBase_HandleException_ShouldFireEventWithException()
    {
        // Arrange
        _actor = new TestActor();
        await _actor.InitializeAsync();
        await _actor.StartAsync();

        var expectedException = new InvalidOperationException("Test exception");
        _actor.NextHandleException = expectedException;

        ActorMessageEventArgs? eventArgs = null;
        _actor.MessageProcessed += (sender, args) => eventArgs = args;

        var message = new TestMessage("test content");

        // Act
        await _actor.SendMessageAsync(message);
        
        // Wait for message processing
        await Task.Delay(50);

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal(message, eventArgs.Message);
        Assert.Equal(ActorMessageHandleResult.Failed, eventArgs.Result);
        Assert.Equal(expectedException, eventArgs.Exception);
    }

    [Fact]
    public async Task ActorBase_DoubleInitialize_ShouldThrow()
    {
        // Arrange
        _actor = new TestActor();
        await _actor.InitializeAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _actor.InitializeAsync());
    }

    [Fact]
    public async Task ActorBase_StartMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        _actor = new TestActor();
        await _actor.InitializeAsync();

        // Act & Assert - Should not throw
        await _actor.StartAsync();
        await _actor.StartAsync(); // Second start should be ignored
        
        Assert.True(_actor.IsRunning);
    }

    [Fact]
    public async Task ActorBase_StopWhenNotRunning_ShouldNotThrow()
    {
        // Arrange
        _actor = new TestActor();

        // Act & Assert - Should not throw
        await _actor.StopAsync();
        
        Assert.False(_actor.IsRunning);
    }
}
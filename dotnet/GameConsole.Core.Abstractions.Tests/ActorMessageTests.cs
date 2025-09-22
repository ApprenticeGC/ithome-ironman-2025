using GameConsole.Core.Abstractions;
using Xunit;

namespace GameConsole.Core.Abstractions.Tests;

public class ActorMessageTests
{
    private class TestMessage : ActorMessage
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

        public TestMessage(string content, Guid correlationId) : base(correlationId)
        {
            Content = content;
        }
    }

    [Fact]
    public void ActorMessage_Constructor_ShouldSetBasicProperties()
    {
        // Act
        var message = new TestMessage("test");

        // Assert
        Assert.NotEqual(Guid.Empty, message.MessageId);
        Assert.Null(message.CorrelationId);
        Assert.True(message.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(message.Timestamp >= DateTimeOffset.UtcNow.AddSeconds(-5)); // Allow some time for test execution
        Assert.Equal("test", message.Content);
    }

    [Fact]
    public void ActorMessage_Constructor_WithOriginalMessage_ShouldSetCorrelationId()
    {
        // Arrange
        var originalMessage = new TestMessage("original");

        // Act
        var responseMessage = new TestMessage("response", originalMessage);

        // Assert
        Assert.NotEqual(Guid.Empty, responseMessage.MessageId);
        Assert.Equal(originalMessage.MessageId, responseMessage.CorrelationId);
        Assert.NotEqual(originalMessage.MessageId, responseMessage.MessageId);
        Assert.Equal("response", responseMessage.Content);
    }

    [Fact]
    public void ActorMessage_Constructor_WithCorrelationId_ShouldSetCorrelationId()
    {
        // Arrange
        var correlationId = Guid.NewGuid();

        // Act
        var message = new TestMessage("test", correlationId);

        // Assert
        Assert.NotEqual(Guid.Empty, message.MessageId);
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.NotEqual(correlationId, message.MessageId);
        Assert.Equal("test", message.Content);
    }

    [Fact]
    public void ActorMessage_UniqueIds_ShouldBeGeneratedForEachMessage()
    {
        // Act
        var message1 = new TestMessage("test1");
        var message2 = new TestMessage("test2");

        // Assert
        Assert.NotEqual(message1.MessageId, message2.MessageId);
    }

    [Fact]
    public void ActorMessage_Timestamp_ShouldBeReasonablyCurrent()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var message = new TestMessage("test");

        // Arrange
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(message.Timestamp >= beforeCreation);
        Assert.True(message.Timestamp <= afterCreation);
    }
}
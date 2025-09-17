using TestLib.CQRS;

namespace TestLib.Tests.CQRS;

/// <summary>
/// Tests for CQRS Event and EventBus functionality.
/// </summary>
public class EventTests
{
    [Fact]
    public void Event_Should_Have_Unique_Id()
    {
        // Arrange & Act
        var event1 = new TestEvent { Message = "Event1" };
        var event2 = new TestEvent { Message = "Event2" };

        // Assert
        Assert.NotEqual(event1.Id, event2.Id);
        Assert.NotEqual(Guid.Empty, event1.Id);
        Assert.NotEqual(Guid.Empty, event2.Id);
    }

    [Fact]
    public void Event_Should_Have_Timestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        
        // Act
        var testEvent = new TestEvent { Message = "Test" };
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(testEvent.Timestamp >= beforeCreation);
        Assert.True(testEvent.Timestamp <= afterCreation);
    }

    [Fact]
    public async Task EventBus_Should_Publish_Event_With_No_Subscribers()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var testEvent = new TestEvent { Message = "Test Message" };

        // Act & Assert - should not throw
        await eventBus.PublishAsync(testEvent);
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public async Task EventBus_Should_Notify_Subscriber()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var receivedEvents = new List<TestEvent>();
        
        eventBus.Subscribe<TestEvent>(async (@event, ct) => 
        {
            receivedEvents.Add(@event);
            await Task.CompletedTask;
        });

        var testEvent = new TestEvent { Message = "Test Message" };

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        Assert.Single(receivedEvents);
        Assert.Equal("Test Message", receivedEvents.First().Message);
    }

    [Fact]
    public async Task EventBus_Should_Notify_Multiple_Subscribers()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var receivedEvents1 = new List<TestEvent>();
        var receivedEvents2 = new List<TestEvent>();
        
        eventBus.Subscribe<TestEvent>(async (@event, ct) => 
        {
            receivedEvents1.Add(@event);
            await Task.CompletedTask;
        });
        
        eventBus.Subscribe<TestEvent>(async (@event, ct) => 
        {
            receivedEvents2.Add(@event);
            await Task.CompletedTask;
        });

        var testEvent = new TestEvent { Message = "Test Message" };

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        Assert.Single(receivedEvents1);
        Assert.Single(receivedEvents2);
        Assert.Equal("Test Message", receivedEvents1.First().Message);
        Assert.Equal("Test Message", receivedEvents2.First().Message);
    }

    [Fact]
    public void EventBus_Should_Track_Handler_Count()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        
        // Act & Assert - No handlers initially
        Assert.Equal(0, eventBus.GetHandlerCount<TestEvent>());
        
        // Add handler
        eventBus.Subscribe<TestEvent>(async (@event, ct) => await Task.CompletedTask);
        Assert.Equal(1, eventBus.GetHandlerCount<TestEvent>());
        
        // Add another handler
        eventBus.Subscribe<TestEvent>(async (@event, ct) => await Task.CompletedTask);
        Assert.Equal(2, eventBus.GetHandlerCount<TestEvent>());
    }

    [Fact]
    public void EventBus_Should_Clear_All_Handlers()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        eventBus.Subscribe<TestEvent>(async (@event, ct) => await Task.CompletedTask);
        
        // Act
        eventBus.Clear();

        // Assert
        Assert.Equal(0, eventBus.GetHandlerCount<TestEvent>());
    }

    [Fact]
    public async Task EventBus_Should_Support_Cancellation()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var cts = new CancellationTokenSource();
        var testEvent = new TestEvent { Message = "Test" };

        // Act & Assert - should not throw when cancellation token is passed
        await eventBus.PublishAsync(testEvent, cts.Token);
        Assert.True(true); // If we get here, no exception was thrown
    }

    [Fact]
    public void EventBus_Should_Handle_Unsubscribe()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        Func<TestEvent, CancellationToken, Task> handler = async (@event, ct) => await Task.CompletedTask;
        
        eventBus.Subscribe<TestEvent>(handler);
        Assert.Equal(1, eventBus.GetHandlerCount<TestEvent>());

        // Act
        eventBus.Unsubscribe<TestEvent>(handler);

        // Assert
        Assert.Equal(0, eventBus.GetHandlerCount<TestEvent>());
    }
}
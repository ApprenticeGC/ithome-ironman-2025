using TestLib.CQRS;

namespace TestLib.Tests.CQRS;

/// <summary>
/// Integration tests demonstrating the complete CQRS flow.
/// </summary>
public class CQRSIntegrationTests
{
    [Fact]
    public async Task Complete_CQRS_Flow_Should_Work_Together()
    {
        // Arrange
        var commandHandler = new TestCommandHandler();
        var queryHandler = new TestQueryHandler();
        var eventBus = new InMemoryEventBus();
        var receivedEvents = new List<TestEvent>();

        // Subscribe to events
        eventBus.Subscribe<TestEvent>(async (@event, ct) =>
        {
            receivedEvents.Add(@event);
            await Task.CompletedTask;
        });

        // Act
        // 1. Execute a command
        var command = new TestCommand { Data = "Integration Test", InitiatedBy = "TestUser" };
        await commandHandler.HandleAsync(command);

        // 2. Execute a query
        var query = new TestQuery { Input = "Integration Query", InitiatedBy = "TestUser" };
        var queryResult = await queryHandler.HandleAsync(query);

        // 3. Publish an event
        var testEvent = new TestEvent { Message = "Integration Event Occurred" };
        await eventBus.PublishAsync(testEvent);

        // Assert
        // Command was handled
        Assert.Single(commandHandler.HandledCommands);
        Assert.Equal("Integration Test", commandHandler.HandledCommands.First().Data);
        Assert.Equal("TestUser", commandHandler.HandledCommands.First().InitiatedBy);

        // Query returned correct result
        Assert.Equal("Result for: Integration Query", queryResult);

        // Event was published and received
        Assert.Single(receivedEvents);
        Assert.Equal("Integration Event Occurred", receivedEvents.First().Message);
    }

    [Fact]
    public async Task CQRS_Should_Support_Multiple_Operations_Concurrently()
    {
        // Arrange
        var commandHandler = new TestCommandHandler();
        var queryHandler = new TestQueryHandler();
        var eventBus = new InMemoryEventBus();
        var receivedEvents = new List<TestEvent>();

        eventBus.Subscribe<TestEvent>(async (@event, ct) =>
        {
            receivedEvents.Add(@event);
            await Task.Delay(1, ct); // Small delay to simulate work
        });

        // Act - Run operations concurrently
        var tasks = new List<Task>();

        // Execute multiple commands
        for (int i = 0; i < 5; i++)
        {
            var command = new TestCommand { Data = $"Command {i}" };
            tasks.Add(commandHandler.HandleAsync(command));
        }

        // Execute multiple queries
        for (int i = 0; i < 5; i++)
        {
            var query = new TestQuery { Input = $"Query {i}" };
            tasks.Add(queryHandler.HandleAsync(query));
        }

        // Publish multiple events
        for (int i = 0; i < 5; i++)
        {
            var testEvent = new TestEvent { Message = $"Event {i}" };
            tasks.Add(eventBus.PublishAsync(testEvent));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, commandHandler.HandledCommands.Count);
        Assert.Equal(5, receivedEvents.Count);

        // Verify all commands were handled
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains(commandHandler.HandledCommands, c => c.Data == $"Command {i}");
        }

        // Verify all events were received
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains(receivedEvents, e => e.Message == $"Event {i}");
        }
    }

    [Fact]
    public async Task CQRS_Performance_Benchmark_Integration_Should_Work()
    {
        // Arrange
        var benchmarks = new CQRSBenchmarks();
        var commandHandler = new TestCommandHandler();
        var queryHandler = new TestQueryHandler();
        var eventBus = new InMemoryEventBus();
        
        // Add event subscriber for benchmark
        var eventCounter = 0;
        eventBus.Subscribe<TestEvent>(async (@event, ct) =>
        {
            Interlocked.Increment(ref eventCounter);
            await Task.CompletedTask;
        });

        var command = new TestCommand { Data = "Benchmark Test" };
        var query = new TestQuery { Input = "Benchmark Test" };
        var testEvent = new TestEvent { Message = "Benchmark Test" };

        // Act - Run all benchmark types
        var commandBenchmark = await benchmarks.BenchmarkCommandAsync(command, commandHandler, 100);
        var queryBenchmark = await benchmarks.BenchmarkQueryAsync(query, queryHandler, 100);
        var eventBenchmark = await benchmarks.BenchmarkEventAsync(testEvent, eventBus, 100);

        // Assert
        Assert.Equal("Command", commandBenchmark.OperationType);
        Assert.Equal("Query", queryBenchmark.OperationType);
        Assert.Equal("Event", eventBenchmark.OperationType);

        Assert.Equal(100, commandBenchmark.Iterations);
        Assert.Equal(100, queryBenchmark.Iterations);
        Assert.Equal(100, eventBenchmark.Iterations);

        // All should have measurable performance
        Assert.True(commandBenchmark.OperationsPerSecond > 0);
        Assert.True(queryBenchmark.OperationsPerSecond > 0);
        Assert.True(eventBenchmark.OperationsPerSecond > 0);

        // Verify operations actually executed
        Assert.Equal(100, commandHandler.HandledCommands.Count);
        Assert.Equal(100, eventCounter);
    }

    [Fact]
    public void CQRS_Components_Should_Have_Proper_Interface_Compliance()
    {
        // Arrange & Act
        var command = new TestCommand();
        var query = new TestQuery();
        var testEvent = new TestEvent();
        var commandHandler = new TestCommandHandler();
        var queryHandler = new TestQueryHandler();
        var eventBus = new InMemoryEventBus();

        // Assert - Type checking and interface compliance
        Assert.IsAssignableFrom<ICommand>(command);
        Assert.IsAssignableFrom<IQuery<string>>(query);
        Assert.IsAssignableFrom<IEvent>(testEvent);
        Assert.IsAssignableFrom<ICommandHandler<TestCommand>>(commandHandler);
        Assert.IsAssignableFrom<IQueryHandler<TestQuery, string>>(queryHandler);
        Assert.IsAssignableFrom<IEventBus>(eventBus);

        // Verify base class functionality
        Assert.NotEqual(Guid.Empty, command.Id);
        Assert.NotEqual(Guid.Empty, query.Id);
        Assert.NotEqual(Guid.Empty, testEvent.Id);

        Assert.True(command.Timestamp <= DateTime.UtcNow);
        Assert.True(query.Timestamp <= DateTime.UtcNow);
        Assert.True(testEvent.Timestamp <= DateTime.UtcNow);
    }
}
using TestLib.CQRS;

namespace TestLib.Tests.CQRS;

/// <summary>
/// Tests for CQRS performance benchmarking functionality.
/// </summary>
public class BenchmarkTests
{
    [Fact]
    public async Task BenchmarkCommandAsync_Should_Execute_And_Return_Results()
    {
        // Arrange
        var benchmarks = new CQRSBenchmarks();
        var command = new TestCommand { Data = "Benchmark Test" };
        var handler = new TestCommandHandler();
        var iterations = 10;

        // Act
        var result = await benchmarks.BenchmarkCommandAsync(command, handler, iterations);

        // Assert
        Assert.Equal("Command", result.OperationType);
        Assert.Equal(iterations, result.Iterations);
        Assert.True(result.TotalElapsedMs >= 0);
        Assert.True(result.AverageElapsedMs >= 0);
        Assert.True(result.OperationsPerSecond > 0);
        Assert.True(result.EndTime >= result.StartTime);
        Assert.Equal(iterations, handler.HandledCommands.Count);
    }

    [Fact]
    public async Task BenchmarkQueryAsync_Should_Execute_And_Return_Results()
    {
        // Arrange
        var benchmarks = new CQRSBenchmarks();
        var query = new TestQuery { Input = "Benchmark Test" };
        var handler = new TestQueryHandler();
        var iterations = 10;

        // Act
        var result = await benchmarks.BenchmarkQueryAsync(query, handler, iterations);

        // Assert
        Assert.Equal("Query", result.OperationType);
        Assert.Equal(iterations, result.Iterations);
        Assert.True(result.TotalElapsedMs >= 0);
        Assert.True(result.AverageElapsedMs >= 0);
        Assert.True(result.OperationsPerSecond > 0);
        Assert.True(result.EndTime >= result.StartTime);
    }

    [Fact]
    public async Task BenchmarkEventAsync_Should_Execute_And_Return_Results()
    {
        // Arrange
        var benchmarks = new CQRSBenchmarks();
        var testEvent = new TestEvent { Message = "Benchmark Test" };
        var eventBus = new InMemoryEventBus();
        var iterations = 10;

        // Add a subscriber to ensure event processing
        var receivedEvents = new List<TestEvent>();
        eventBus.Subscribe<TestEvent>(async (@event, ct) =>
        {
            receivedEvents.Add(@event);
            await Task.CompletedTask;
        });

        // Act
        var result = await benchmarks.BenchmarkEventAsync(testEvent, eventBus, iterations);

        // Assert
        Assert.Equal("Event", result.OperationType);
        Assert.Equal(iterations, result.Iterations);
        Assert.True(result.TotalElapsedMs >= 0);
        Assert.True(result.AverageElapsedMs >= 0);
        Assert.True(result.OperationsPerSecond > 0);
        Assert.True(result.EndTime >= result.StartTime);
        Assert.Equal(iterations, receivedEvents.Count);
    }

    [Fact]
    public void BenchmarkResult_ToString_Should_Format_Correctly()
    {
        // Arrange
        var result = new BenchmarkResult
        {
            OperationType = "Command",
            Iterations = 100,
            TotalElapsedMs = 50,
            AverageElapsedMs = 0.5,
            OperationsPerSecond = 2000,
            StartTime = DateTime.UtcNow.AddSeconds(-1),
            EndTime = DateTime.UtcNow
        };

        // Act
        var formatted = result.ToString();

        // Assert
        Assert.Contains("Command Benchmark", formatted);
        Assert.Contains("100 iterations", formatted);
        Assert.Contains("50ms total", formatted);
        Assert.Contains("0.50ms avg", formatted);
        Assert.Contains("2000 ops/sec", formatted);
    }

    [Fact]
    public async Task Benchmark_Should_Handle_High_Iterations()
    {
        // Arrange
        var benchmarks = new CQRSBenchmarks();
        var command = new TestCommand { Data = "High Volume Test" };
        var handler = new TestCommandHandler();
        var iterations = 1000;

        // Act
        var result = await benchmarks.BenchmarkCommandAsync(command, handler, iterations);

        // Assert
        Assert.Equal(iterations, result.Iterations);
        Assert.Equal(iterations, handler.HandledCommands.Count);
        Assert.True(result.OperationsPerSecond > 100); // Should be quite fast
    }

    [Fact]
    public async Task Benchmark_Should_Support_Cancellation()
    {
        // Arrange
        var benchmarks = new CQRSBenchmarks();
        var command = new TestCommand { Data = "Cancellation Test" };
        var handler = new TestCommandHandler();
        var cts = new CancellationTokenSource();

        // Act & Assert - should not throw when cancellation token is passed
        var result = await benchmarks.BenchmarkCommandAsync(command, handler, 10, cts.Token);
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData(1, "Single iteration should work")]
    [InlineData(10, "Small batch should work")]
    [InlineData(100, "Medium batch should work")]
    public async Task Benchmark_Should_Handle_Different_Iteration_Counts(int iterations, string description)
    {
        // Arrange
        var benchmarks = new CQRSBenchmarks();
        var query = new TestQuery { Input = description };
        var handler = new TestQueryHandler();

        // Act
        var result = await benchmarks.BenchmarkQueryAsync(query, handler, iterations);

        // Assert
        Assert.Equal(iterations, result.Iterations);
        Assert.True(result.TotalElapsedMs >= 0, description);
        Assert.True(result.OperationsPerSecond > 0, description);
    }
}
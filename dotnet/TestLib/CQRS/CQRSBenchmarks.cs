using System.Diagnostics;

namespace TestLib.CQRS;

/// <summary>
/// Performance benchmarking utilities for CQRS operations.
/// </summary>
public class CQRSBenchmarks
{
    /// <summary>
    /// Benchmarks command execution performance.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to benchmark.</typeparam>
    /// <param name="command">The command to execute.</param>
    /// <param name="handler">The command handler to use.</param>
    /// <param name="iterations">Number of iterations to run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Benchmark results for command execution.</returns>
    public async Task<BenchmarkResult> BenchmarkCommandAsync<TCommand>(
        TCommand command,
        ICommandHandler<TCommand> handler,
        int iterations = 1000,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < iterations; i++)
        {
            await handler.HandleAsync(command, cancellationToken);
        }

        stopwatch.Stop();
        var endTime = DateTime.UtcNow;

        return new BenchmarkResult
        {
            OperationType = "Command",
            Iterations = iterations,
            TotalElapsedMs = stopwatch.ElapsedMilliseconds,
            AverageElapsedMs = (double)stopwatch.ElapsedMilliseconds / iterations,
            OperationsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    /// <summary>
    /// Benchmarks query execution performance.
    /// </summary>
    /// <typeparam name="TQuery">The type of query to benchmark.</typeparam>
    /// <typeparam name="TResult">The type of result returned by the query.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="handler">The query handler to use.</param>
    /// <param name="iterations">Number of iterations to run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Benchmark results for query execution.</returns>
    public async Task<BenchmarkResult> BenchmarkQueryAsync<TQuery, TResult>(
        TQuery query,
        IQueryHandler<TQuery, TResult> handler,
        int iterations = 1000,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < iterations; i++)
        {
            await handler.HandleAsync(query, cancellationToken);
        }

        stopwatch.Stop();
        var endTime = DateTime.UtcNow;

        return new BenchmarkResult
        {
            OperationType = "Query",
            Iterations = iterations,
            TotalElapsedMs = stopwatch.ElapsedMilliseconds,
            AverageElapsedMs = (double)stopwatch.ElapsedMilliseconds / iterations,
            OperationsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    /// <summary>
    /// Benchmarks event publishing performance.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to benchmark.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <param name="eventBus">The event bus to use.</param>
    /// <param name="iterations">Number of iterations to run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Benchmark results for event publishing.</returns>
    public async Task<BenchmarkResult> BenchmarkEventAsync<TEvent>(
        TEvent @event,
        IEventBus eventBus,
        int iterations = 1000,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        for (int i = 0; i < iterations; i++)
        {
            await eventBus.PublishAsync(@event, cancellationToken);
        }

        stopwatch.Stop();
        var endTime = DateTime.UtcNow;

        return new BenchmarkResult
        {
            OperationType = "Event",
            Iterations = iterations,
            TotalElapsedMs = stopwatch.ElapsedMilliseconds,
            AverageElapsedMs = (double)stopwatch.ElapsedMilliseconds / iterations,
            OperationsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds,
            StartTime = startTime,
            EndTime = endTime
        };
    }
}

/// <summary>
/// Result of a CQRS performance benchmark.
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// Type of operation benchmarked (Command, Query, or Event).
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Number of iterations performed.
    /// </summary>
    public int Iterations { get; set; }

    /// <summary>
    /// Total elapsed time in milliseconds.
    /// </summary>
    public long TotalElapsedMs { get; set; }

    /// <summary>
    /// Average elapsed time per operation in milliseconds.
    /// </summary>
    public double AverageElapsedMs { get; set; }

    /// <summary>
    /// Number of operations per second.
    /// </summary>
    public double OperationsPerSecond { get; set; }

    /// <summary>
    /// Benchmark start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Benchmark end time.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Returns a formatted string representation of the benchmark results.
    /// </summary>
    /// <returns>Formatted benchmark results.</returns>
    public override string ToString()
    {
        return $"{OperationType} Benchmark: {Iterations} iterations, " +
               $"{TotalElapsedMs}ms total, {AverageElapsedMs:F2}ms avg, " +
               $"{OperationsPerSecond:F0} ops/sec";
    }
}
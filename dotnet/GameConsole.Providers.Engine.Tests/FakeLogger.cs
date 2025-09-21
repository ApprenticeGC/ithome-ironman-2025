using Microsoft.Extensions.Logging;

namespace GameConsole.Providers.Engine.Tests;

/// <summary>
/// Simple fake logger for testing purposes.
/// </summary>
public class FakeLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, EventId EventId, string Message, Exception? Exception)> LogEntries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        LogEntries.Add((logLevel, eventId, message, exception));
    }
}
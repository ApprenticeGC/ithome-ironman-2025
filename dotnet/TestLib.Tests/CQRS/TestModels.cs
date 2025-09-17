using TestLib.CQRS;

namespace TestLib.Tests.CQRS;

// Test Command
public class TestCommand : Command
{
    public string Data { get; set; } = string.Empty;
}

public class TestCommandHandler : ICommandHandler<TestCommand>
{
    public List<TestCommand> HandledCommands { get; } = new();

    public Task HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
    {
        HandledCommands.Add(command);
        return Task.CompletedTask;
    }
}

// Test Query
public class TestQuery : Query<string>
{
    public string Input { get; set; } = string.Empty;
}

public class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<string> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"Result for: {query.Input}");
    }
}

// Test Event
public class TestEvent : Event
{
    public string Message { get; set; } = string.Empty;
}
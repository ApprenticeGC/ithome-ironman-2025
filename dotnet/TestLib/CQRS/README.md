# CQRS Implementation (RFC-015-03)

This document describes the Command Query Responsibility Segregation (CQRS) implementation for RFC-015-03.

## Overview

CQRS separates read and write operations using different models:
- **Commands**: Write operations that modify state
- **Queries**: Read operations that return data without modifying state
- **Events**: Notifications about things that happened in the system
- **Event Bus**: Publishes and subscribes to events for decoupled communication

## Core Components

### Interfaces
- `ICommand` - Marker interface for commands
- `IQuery<TResult>` - Interface for queries that return a result
- `ICommandHandler<TCommand>` - Handles command execution
- `IQueryHandler<TQuery, TResult>` - Handles query execution
- `IEvent` - Interface for events
- `IEventBus` - Event publishing and subscription

### Base Classes
- `Command` - Base class with Id, Timestamp, and InitiatedBy properties
- `Query<TResult>` - Base class for queries with tracking properties
- `Event` - Base class for events with Id and Timestamp

### Infrastructure
- `InMemoryEventBus` - Thread-safe in-memory event bus implementation
- `CQRSBenchmarks` - Performance benchmarking utilities

## Usage Example

```csharp
// Define your domain models
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Create commands for write operations
public class CreateUserCommand : Command
{
    public string Name { get; set; }
    public string Email { get; set; }
}

// Create queries for read operations
public class GetUserQuery : Query<User?>
{
    public Guid UserId { get; set; }
}

// Create events for notifications
public class UserCreatedEvent : Event
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// Implement command handlers
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly Dictionary<Guid, User> _users;
    private readonly IEventBus _eventBus;

    public CreateUserCommandHandler(Dictionary<Guid, User> users, IEventBus eventBus)
    {
        _users = users;
        _eventBus = eventBus;
    }

    public async Task HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Email = command.Email
        };

        _users[user.Id] = user;

        // Publish event
        await _eventBus.PublishAsync(new UserCreatedEvent
        {
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email
        }, cancellationToken);
    }
}

// Implement query handlers
public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User?>
{
    private readonly Dictionary<Guid, User> _users;

    public GetUserQueryHandler(Dictionary<Guid, User> users)
    {
        _users = users;
    }

    public Task<User?> HandleAsync(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(query.UserId, out var user);
        return Task.FromResult(user);
    }
}
```

## Setup and Execution

```csharp
// Setup
var users = new Dictionary<Guid, User>();
var eventBus = new InMemoryEventBus();

// Subscribe to events
eventBus.Subscribe<UserCreatedEvent>(async (@event, ct) =>
{
    Console.WriteLine($"User created: {@event.Name}");
});

// Create handlers
var createHandler = new CreateUserCommandHandler(users, eventBus);
var getUserHandler = new GetUserQueryHandler(users);

// Execute command
var createCommand = new CreateUserCommand { Name = "John", Email = "john@example.com" };
await createHandler.HandleAsync(createCommand);

// Execute query
var getUserQuery = new GetUserQuery { UserId = someUserId };
var user = await getUserHandler.HandleAsync(getUserQuery);
```

## Performance Benchmarking

The system includes built-in performance benchmarking:

```csharp
var benchmarks = new CQRSBenchmarks();

// Benchmark commands
var commandBenchmark = await benchmarks.BenchmarkCommandAsync(command, handler, iterations: 1000);
Console.WriteLine($"Command Performance: {commandBenchmark}");

// Benchmark queries
var queryBenchmark = await benchmarks.BenchmarkQueryAsync(query, handler, iterations: 1000);
Console.WriteLine($"Query Performance: {queryBenchmark}");

// Benchmark events
var eventBenchmark = await benchmarks.BenchmarkEventAsync(@event, eventBus, iterations: 1000);
Console.WriteLine($"Event Performance: {eventBenchmark}");
```

## Features

- **Async/Await Support**: All operations support async execution with cancellation tokens
- **Thread Safety**: Event bus is thread-safe for concurrent operations
- **Performance Monitoring**: Built-in benchmarking utilities
- **Event-Driven Architecture**: Decoupled communication through events
- **Type Safety**: Generic interfaces ensure compile-time type checking
- **Tracking**: Commands and queries include Id, timestamp, and initiator tracking

## Test Coverage

The implementation includes comprehensive test coverage:
- Unit tests for all core components
- Integration tests demonstrating full CQRS flow
- Performance benchmark validation
- Concurrent operation testing
- Example usage tests

Total: 37+ tests covering all CQRS functionality.

## Architecture Integration

This CQRS implementation follows the repository's 4-tier architecture:
- **Tier 1**: Stable contracts (interfaces)
- **Tier 2**: Mechanical proxies (handlers)
- **Tier 3**: Business logic (command/query handlers)
- **Tier 4**: Pluggable providers (event bus implementations)
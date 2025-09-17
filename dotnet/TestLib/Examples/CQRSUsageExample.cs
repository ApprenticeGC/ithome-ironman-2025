using TestLib.CQRS;

namespace TestLib.Examples;

/// <summary>
/// Example demonstrating the CQRS system usage.
/// This shows a simple user management scenario using CQRS patterns.
/// </summary>
public class CQRSUsageExample
{
    // Example Domain Models
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Commands (Write Operations)
    public class CreateUserCommand : Command
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateUserCommand : Command
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // Queries (Read Operations)
    public class GetUserQuery : Query<User?>
    {
        public Guid UserId { get; set; }
    }

    public class GetAllUsersQuery : Query<List<User>>
    {
    }

    // Events
    public class UserCreatedEvent : Event
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UserUpdatedEvent : Event
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // Command Handlers (Business Logic for Write Operations)
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
                Email = command.Email,
                CreatedAt = DateTime.UtcNow
            };

            _users[user.Id] = user;

            // Publish event to notify other parts of the system
            var userCreatedEvent = new UserCreatedEvent
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email
            };

            await _eventBus.PublishAsync(userCreatedEvent, cancellationToken);
        }
    }

    public class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
    {
        private readonly Dictionary<Guid, User> _users;
        private readonly IEventBus _eventBus;

        public UpdateUserCommandHandler(Dictionary<Guid, User> users, IEventBus eventBus)
        {
            _users = users;
            _eventBus = eventBus;
        }

        public async Task HandleAsync(UpdateUserCommand command, CancellationToken cancellationToken = default)
        {
            if (_users.TryGetValue(command.UserId, out var user))
            {
                user.Name = command.Name;
                user.Email = command.Email;

                var userUpdatedEvent = new UserUpdatedEvent
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email
                };

                await _eventBus.PublishAsync(userUpdatedEvent, cancellationToken);
            }
        }
    }

    // Query Handlers (Business Logic for Read Operations)
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

    public class GetAllUsersQueryHandler : IQueryHandler<GetAllUsersQuery, List<User>>
    {
        private readonly Dictionary<Guid, User> _users;

        public GetAllUsersQueryHandler(Dictionary<Guid, User> users)
        {
            _users = users;
        }

        public Task<List<User>> HandleAsync(GetAllUsersQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.Values.ToList());
        }
    }

    /// <summary>
    /// Demonstrates the complete CQRS flow with commands, queries, and events.
    /// </summary>
    public static async Task DemonstrateUsage()
    {
        // Setup
        var users = new Dictionary<Guid, User>();
        var eventBus = new InMemoryEventBus();
        var benchmarks = new CQRSBenchmarks();

        // Event handlers for logging
        eventBus.Subscribe<UserCreatedEvent>(async (@event, ct) =>
        {
            Console.WriteLine($"Event: User created - {@event.Name} ({@event.Email}) at {@event.Timestamp}");
            await Task.CompletedTask;
        });

        eventBus.Subscribe<UserUpdatedEvent>(async (@event, ct) =>
        {
            Console.WriteLine($"Event: User updated - {@event.Name} ({@event.Email}) at {@event.Timestamp}");
            await Task.CompletedTask;
        });

        // Create handlers
        var createUserHandler = new CreateUserCommandHandler(users, eventBus);
        var updateUserHandler = new UpdateUserCommandHandler(users, eventBus);
        var getUserHandler = new GetUserQueryHandler(users);
        var getAllUsersHandler = new GetAllUsersQueryHandler(users);

        Console.WriteLine("=== CQRS Usage Example ===");

        // 1. Create users using commands
        Console.WriteLine("\n1. Creating users...");
        var createCommand1 = new CreateUserCommand 
        { 
            Name = "Alice Johnson", 
            Email = "alice@example.com",
            InitiatedBy = "System"
        };
        
        var createCommand2 = new CreateUserCommand 
        { 
            Name = "Bob Smith", 
            Email = "bob@example.com",
            InitiatedBy = "System"
        };

        await createUserHandler.HandleAsync(createCommand1);
        await createUserHandler.HandleAsync(createCommand2);

        // 2. Query users
        Console.WriteLine("\n2. Querying all users...");
        var getAllQuery = new GetAllUsersQuery { InitiatedBy = "System" };
        var allUsers = await getAllUsersHandler.HandleAsync(getAllQuery);
        
        foreach (var user in allUsers)
        {
            Console.WriteLine($"User: {user.Name} ({user.Email}) - Created: {user.CreatedAt}");
        }

        // 3. Query specific user
        Console.WriteLine("\n3. Querying specific user...");
        var firstUserId = allUsers.First().Id;
        var getUserQuery = new GetUserQuery { UserId = firstUserId, InitiatedBy = "System" };
        var specificUser = await getUserHandler.HandleAsync(getUserQuery);
        
        if (specificUser != null)
        {
            Console.WriteLine($"Found user: {specificUser.Name} ({specificUser.Email})");
        }

        // 4. Update user
        Console.WriteLine("\n4. Updating user...");
        var updateCommand = new UpdateUserCommand
        {
            UserId = firstUserId,
            Name = "Alice Johnson-Updated",
            Email = "alice.updated@example.com",
            InitiatedBy = "System"
        };
        
        await updateUserHandler.HandleAsync(updateCommand);

        // 5. Query updated user
        Console.WriteLine("\n5. Querying updated user...");
        var updatedUser = await getUserHandler.HandleAsync(getUserQuery);
        if (updatedUser != null)
        {
            Console.WriteLine($"Updated user: {updatedUser.Name} ({updatedUser.Email})");
        }

        // 6. Performance benchmarking
        Console.WriteLine("\n6. Performance benchmarking...");
        var benchmarkCommand = new CreateUserCommand { Name = "Benchmark User", Email = "bench@example.com" };
        var commandBenchmark = await benchmarks.BenchmarkCommandAsync(benchmarkCommand, createUserHandler, 100);
        Console.WriteLine($"Command Benchmark: {commandBenchmark}");

        var benchmarkQuery = new GetAllUsersQuery();
        var queryBenchmark = await benchmarks.BenchmarkQueryAsync(benchmarkQuery, getAllUsersHandler, 100);
        Console.WriteLine($"Query Benchmark: {queryBenchmark}");

        var benchmarkEvent = new UserCreatedEvent { UserId = Guid.NewGuid(), Name = "Event User", Email = "event@example.com" };
        var eventBenchmark = await benchmarks.BenchmarkEventAsync(benchmarkEvent, eventBus, 100);
        Console.WriteLine($"Event Benchmark: {eventBenchmark}");

        Console.WriteLine("\n=== CQRS Example Complete ===");
    }
}
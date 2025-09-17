using TestLib.Examples;
using TestLib.CQRS;

namespace TestLib.Tests.Examples;

/// <summary>
/// Tests for the CQRS usage example to ensure it works correctly.
/// </summary>
public class CQRSUsageExampleTests
{
    [Fact]
    public async Task CQRSUsageExample_Should_Execute_Without_Errors()
    {
        // Act & Assert - should not throw any exceptions
        await CQRSUsageExample.DemonstrateUsage();
        
        // If we get here, the example ran successfully
        Assert.True(true, "CQRS usage example executed successfully");
    }

    [Fact]
    public async Task CQRSUsageExample_Components_Should_Work_Independently()
    {
        // Arrange
        var users = new Dictionary<Guid, CQRSUsageExample.User>();
        var eventBus = new InMemoryEventBus();
        
        var createHandler = new CQRSUsageExample.CreateUserCommandHandler(users, eventBus);
        var getUserHandler = new CQRSUsageExample.GetUserQueryHandler(users);
        var getAllHandler = new CQRSUsageExample.GetAllUsersQueryHandler(users);

        // Act
        var createCommand = new CQRSUsageExample.CreateUserCommand 
        { 
            Name = "Test User", 
            Email = "test@example.com" 
        };
        
        await createHandler.HandleAsync(createCommand);

        var getAllQuery = new CQRSUsageExample.GetAllUsersQuery();
        var allUsers = await getAllHandler.HandleAsync(getAllQuery);

        // Assert
        Assert.Single(allUsers);
        Assert.Equal("Test User", allUsers.First().Name);
        Assert.Equal("test@example.com", allUsers.First().Email);
    }

    [Fact]
    public async Task CQRSUsageExample_Events_Should_Be_Published()
    {
        // Arrange
        var users = new Dictionary<Guid, CQRSUsageExample.User>();
        var eventBus = new InMemoryEventBus();
        var receivedEvents = new List<CQRSUsageExample.UserCreatedEvent>();

        eventBus.Subscribe<CQRSUsageExample.UserCreatedEvent>(async (@event, ct) =>
        {
            receivedEvents.Add(@event);
            await Task.CompletedTask;
        });

        var createHandler = new CQRSUsageExample.CreateUserCommandHandler(users, eventBus);

        // Act
        var createCommand = new CQRSUsageExample.CreateUserCommand 
        { 
            Name = "Event Test User", 
            Email = "event@example.com" 
        };
        
        await createHandler.HandleAsync(createCommand);

        // Assert
        Assert.Single(receivedEvents);
        Assert.Equal("Event Test User", receivedEvents.First().Name);
        Assert.Equal("event@example.com", receivedEvents.First().Email);
    }

    [Fact]
    public async Task CQRSUsageExample_Update_Should_Work()
    {
        // Arrange
        var users = new Dictionary<Guid, CQRSUsageExample.User>();
        var eventBus = new InMemoryEventBus();
        var updatedEvents = new List<CQRSUsageExample.UserUpdatedEvent>();

        eventBus.Subscribe<CQRSUsageExample.UserUpdatedEvent>(async (@event, ct) =>
        {
            updatedEvents.Add(@event);
            await Task.CompletedTask;
        });

        var createHandler = new CQRSUsageExample.CreateUserCommandHandler(users, eventBus);
        var updateHandler = new CQRSUsageExample.UpdateUserCommandHandler(users, eventBus);
        var getUserHandler = new CQRSUsageExample.GetUserQueryHandler(users);

        // Create a user first
        var createCommand = new CQRSUsageExample.CreateUserCommand 
        { 
            Name = "Original User", 
            Email = "original@example.com" 
        };
        await createHandler.HandleAsync(createCommand);

        var userId = users.Values.First().Id;

        // Act - Update the user
        var updateCommand = new CQRSUsageExample.UpdateUserCommand
        {
            UserId = userId,
            Name = "Updated User",
            Email = "updated@example.com"
        };
        await updateHandler.HandleAsync(updateCommand);

        // Query the updated user
        var getUserQuery = new CQRSUsageExample.GetUserQuery { UserId = userId };
        var updatedUser = await getUserHandler.HandleAsync(getUserQuery);

        // Assert
        Assert.NotNull(updatedUser);
        Assert.Equal("Updated User", updatedUser.Name);
        Assert.Equal("updated@example.com", updatedUser.Email);
        Assert.Single(updatedEvents);
        Assert.Equal("Updated User", updatedEvents.First().Name);
    }
}
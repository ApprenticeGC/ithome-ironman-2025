using TestLib.CQRS;

namespace TestLib.Tests.CQRS;

/// <summary>
/// Tests for CQRS Command functionality.
/// </summary>
public class CommandTests
{
    [Fact]
    public void Command_Should_Have_Unique_Id()
    {
        // Arrange & Act
        var command1 = new TestCommand { Data = "Test1" };
        var command2 = new TestCommand { Data = "Test2" };

        // Assert
        Assert.NotEqual(command1.Id, command2.Id);
        Assert.NotEqual(Guid.Empty, command1.Id);
        Assert.NotEqual(Guid.Empty, command2.Id);
    }

    [Fact]
    public void Command_Should_Have_Timestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        
        // Act
        var command = new TestCommand { Data = "Test" };
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(command.Timestamp >= beforeCreation);
        Assert.True(command.Timestamp <= afterCreation);
    }

    [Fact]
    public void Command_Should_Allow_InitiatedBy_Property()
    {
        // Arrange & Act
        var command = new TestCommand 
        { 
            Data = "Test",
            InitiatedBy = "TestUser"
        };

        // Assert
        Assert.Equal("TestUser", command.InitiatedBy);
    }

    [Fact]
    public async Task CommandHandler_Should_Handle_Command()
    {
        // Arrange
        var handler = new TestCommandHandler();
        var command = new TestCommand { Data = "Test Data" };

        // Act
        await handler.HandleAsync(command);

        // Assert
        Assert.Single(handler.HandledCommands);
        Assert.Equal("Test Data", handler.HandledCommands.First().Data);
    }

    [Fact]
    public async Task CommandHandler_Should_Handle_Multiple_Commands()
    {
        // Arrange
        var handler = new TestCommandHandler();
        var command1 = new TestCommand { Data = "Test1" };
        var command2 = new TestCommand { Data = "Test2" };

        // Act
        await handler.HandleAsync(command1);
        await handler.HandleAsync(command2);

        // Assert
        Assert.Equal(2, handler.HandledCommands.Count);
        Assert.Contains(handler.HandledCommands, c => c.Data == "Test1");
        Assert.Contains(handler.HandledCommands, c => c.Data == "Test2");
    }
}
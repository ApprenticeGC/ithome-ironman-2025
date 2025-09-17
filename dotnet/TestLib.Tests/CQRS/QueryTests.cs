using TestLib.CQRS;

namespace TestLib.Tests.CQRS;

/// <summary>
/// Tests for CQRS Query functionality.
/// </summary>
public class QueryTests
{
    [Fact]
    public void Query_Should_Have_Unique_Id()
    {
        // Arrange & Act
        var query1 = new TestQuery { Input = "Test1" };
        var query2 = new TestQuery { Input = "Test2" };

        // Assert
        Assert.NotEqual(query1.Id, query2.Id);
        Assert.NotEqual(Guid.Empty, query1.Id);
        Assert.NotEqual(Guid.Empty, query2.Id);
    }

    [Fact]
    public void Query_Should_Have_Timestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;
        
        // Act
        var query = new TestQuery { Input = "Test" };
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(query.Timestamp >= beforeCreation);
        Assert.True(query.Timestamp <= afterCreation);
    }

    [Fact]
    public void Query_Should_Allow_InitiatedBy_Property()
    {
        // Arrange & Act
        var query = new TestQuery 
        { 
            Input = "Test",
            InitiatedBy = "TestUser"
        };

        // Assert
        Assert.Equal("TestUser", query.InitiatedBy);
    }

    [Fact]
    public async Task QueryHandler_Should_Handle_Query_And_Return_Result()
    {
        // Arrange
        var handler = new TestQueryHandler();
        var query = new TestQuery { Input = "Test Input" };

        // Act
        var result = await handler.HandleAsync(query);

        // Assert
        Assert.Equal("Result for: Test Input", result);
    }

    [Fact]
    public async Task QueryHandler_Should_Handle_Multiple_Queries()
    {
        // Arrange
        var handler = new TestQueryHandler();
        var query1 = new TestQuery { Input = "Input1" };
        var query2 = new TestQuery { Input = "Input2" };

        // Act
        var result1 = await handler.HandleAsync(query1);
        var result2 = await handler.HandleAsync(query2);

        // Assert
        Assert.Equal("Result for: Input1", result1);
        Assert.Equal("Result for: Input2", result2);
    }

    [Fact]
    public async Task QueryHandler_Should_Support_Cancellation()
    {
        // Arrange
        var handler = new TestQueryHandler();
        var query = new TestQuery { Input = "Test" };
        var cts = new CancellationTokenSource();

        // Act & Assert - should not throw when cancellation token is passed
        var result = await handler.HandleAsync(query, cts.Token);
        Assert.NotNull(result);
    }
}
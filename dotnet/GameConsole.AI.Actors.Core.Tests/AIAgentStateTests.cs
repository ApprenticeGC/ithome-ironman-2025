using GameConsole.AI.Actors.Core;
using Xunit;

namespace GameConsole.AI.Actors.Core.Tests;

public class AIAgentStateTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var state = new AIAgentState();

        // Assert
        Assert.Empty(state.AgentId);
        Assert.Empty(state.AgentType);
        Assert.Empty(state.Properties);
        Assert.Equal(AIAgentStatus.Idle, state.Status);
        Assert.True(state.CreatedAt <= DateTime.UtcNow);
        Assert.True(state.LastUpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task SetProperty_AddsPropertyAndUpdatesTimestamp()
    {
        // Arrange
        var state = new AIAgentState();
        var beforeUpdate = state.LastUpdatedAt;
        
        // Wait a small amount to ensure timestamp difference
        await Task.Delay(10);

        // Act
        state.SetProperty("test", "value");

        // Assert
        Assert.True(state.Properties.ContainsKey("test"));
        Assert.Equal("value", state.Properties["test"]);
        Assert.True(state.LastUpdatedAt > beforeUpdate);
    }

    [Fact]
    public void GetProperty_ReturnsCorrectValue()
    {
        // Arrange
        var state = new AIAgentState();
        state.SetProperty("stringValue", "test");
        state.SetProperty("intValue", 42);

        // Act & Assert
        Assert.Equal("test", state.GetProperty<string>("stringValue"));
        Assert.Equal(42, state.GetProperty<int>("intValue"));
        Assert.Null(state.GetProperty<string>("nonExistent"));
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new AIAgentState
        {
            AgentId = "test-agent",
            AgentType = "TestAgent",
            Status = AIAgentStatus.Processing
        };
        original.SetProperty("key", "value");

        // Act
        var clone = original.Clone();

        // Assert
        Assert.Equal(original.AgentId, clone.AgentId);
        Assert.Equal(original.AgentType, clone.AgentType);
        Assert.Equal(original.Status, clone.Status);
        Assert.Equal("value", clone.GetProperty<string>("key"));
        
        // Verify independence
        clone.SetProperty("key", "newValue");
        Assert.Equal("value", original.GetProperty<string>("key"));
        Assert.Equal("newValue", clone.GetProperty<string>("key"));
    }
}
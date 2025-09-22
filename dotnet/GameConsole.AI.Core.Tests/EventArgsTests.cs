using GameConsole.AI.Core;
using Xunit;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Tests for AI agent event arguments and data structures.
/// </summary>
public class EventArgsTests
{
    [Fact]
    public void AIAgentStatusChangedEventArgs_Should_Initialize_Correctly()
    {
        // Arrange
        var previousStatus = AIAgentStatus.Ready;
        var currentStatus = AIAgentStatus.Processing;
        var reason = "Starting request processing";
        var beforeTimestamp = DateTimeOffset.UtcNow;

        // Act
        var eventArgs = new AIAgentStatusChangedEventArgs(previousStatus, currentStatus, reason);

        // Assert
        Assert.Equal(previousStatus, eventArgs.PreviousStatus);
        Assert.Equal(currentStatus, eventArgs.CurrentStatus);
        Assert.Equal(reason, eventArgs.Reason);
        Assert.True(eventArgs.Timestamp >= beforeTimestamp);
        Assert.True(eventArgs.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void AIAgentStatusChangedEventArgs_Should_Handle_Null_Reason()
    {
        // Arrange
        var previousStatus = AIAgentStatus.Ready;
        var currentStatus = AIAgentStatus.Processing;

        // Act
        var eventArgs = new AIAgentStatusChangedEventArgs(previousStatus, currentStatus);

        // Assert
        Assert.Equal(previousStatus, eventArgs.PreviousStatus);
        Assert.Equal(currentStatus, eventArgs.CurrentStatus);
        Assert.Null(eventArgs.Reason);
    }

    [Fact]
    public void AIAgentRegistryStatistics_Should_Initialize_With_Defaults()
    {
        // Arrange & Act
        var statistics = new AIAgentRegistryStatistics();

        // Assert
        Assert.Equal(0, statistics.TotalAgents);
        Assert.NotNull(statistics.AgentsByStatus);
        Assert.Empty(statistics.AgentsByStatus);
        Assert.Equal(TimeSpan.Zero, statistics.RegistryUptime);
        Assert.True(statistics.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void AIAgentHealthStatus_Should_Initialize_Correctly()
    {
        // Arrange
        var agentId = "test-agent-001";
        var isHealthy = true;
        var details = "Agent is functioning normally";
        var beforeTimestamp = DateTimeOffset.UtcNow;

        // Act
        var healthStatus = new AIAgentHealthStatus(agentId, isHealthy, details);

        // Assert
        Assert.Equal(agentId, healthStatus.AgentId);
        Assert.Equal(isHealthy, healthStatus.IsHealthy);
        Assert.Equal(details, healthStatus.Details);
        Assert.True(healthStatus.Timestamp >= beforeTimestamp);
        Assert.True(healthStatus.Timestamp <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void AIAgentHealthStatus_Should_Handle_Null_Details()
    {
        // Arrange
        var agentId = "test-agent-002";
        var isHealthy = false;

        // Act
        var healthStatus = new AIAgentHealthStatus(agentId, isHealthy);

        // Assert
        Assert.Equal(agentId, healthStatus.AgentId);
        Assert.Equal(isHealthy, healthStatus.IsHealthy);
        Assert.Null(healthStatus.Details);
    }
}
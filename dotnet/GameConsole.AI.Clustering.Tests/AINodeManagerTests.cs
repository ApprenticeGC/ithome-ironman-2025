using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Clustering.Tests;

/// <summary>
/// Tests for AINodeManager functionality including individual node management and local agent handling.
/// </summary>
public class AINodeManagerTests
{
    private readonly ILogger<AINodeManager> _logger;

    public AINodeManagerTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AINodeManager>();
    }

    [Fact]
    public async Task JoinClusterAsync_WithValidParameters_Should_Initialize_Node_Successfully()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);
        await nodeManager.InitializeAsync();
        await nodeManager.StartAsync();

        var nodeId = "test-node-1";
        var capabilities = new[] { "ai.dialogue", "ai.analysis" };

        // Act
        var result = await nodeManager.JoinClusterAsync(nodeId, capabilities);

        // Assert
        Assert.True(result);

        // Cleanup
        await nodeManager.LeaveClusterAsync();
        await nodeManager.StopAsync();
        await nodeManager.DisposeAsync();
    }

    [Fact]
    public async Task CreateAgentAsync_Should_Create_Local_Agent_Successfully()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);
        await nodeManager.InitializeAsync();
        await nodeManager.StartAsync();

        var nodeId = "test-node-1";
        var nodeCapabilities = new[] { "ai.dialogue" };
        await nodeManager.JoinClusterAsync(nodeId, nodeCapabilities);

        var agentId = "test-agent-1";
        var agentCapabilities = new[] { "ai.dialogue.assistant" };

        // Act
        var result = await nodeManager.CreateAgentAsync(agentId, agentCapabilities);

        // Assert
        Assert.True(result);

        // Cleanup
        await nodeManager.RemoveAgentAsync(agentId);
        await nodeManager.LeaveClusterAsync();
        await nodeManager.StopAsync();
        await nodeManager.DisposeAsync();
    }

    [Fact]
    public async Task RemoveAgentAsync_Should_Remove_Existing_Agent_Successfully()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);
        await nodeManager.InitializeAsync();
        await nodeManager.StartAsync();

        var nodeId = "test-node-1";
        var nodeCapabilities = new[] { "ai.dialogue" };
        await nodeManager.JoinClusterAsync(nodeId, nodeCapabilities);

        var agentId = "test-agent-1";
        var agentCapabilities = new[] { "ai.dialogue.assistant" };
        await nodeManager.CreateAgentAsync(agentId, agentCapabilities);

        // Act
        var result = await nodeManager.RemoveAgentAsync(agentId);

        // Assert
        Assert.True(result);

        // Cleanup
        await nodeManager.LeaveClusterAsync();
        await nodeManager.StopAsync();
        await nodeManager.DisposeAsync();
    }

    [Fact]
    public async Task RemoveAgentAsync_WithNonExistentAgent_Should_Return_False()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);
        await nodeManager.InitializeAsync();
        await nodeManager.StartAsync();

        var nodeId = "test-node-1";
        var nodeCapabilities = new[] { "ai.dialogue" };
        await nodeManager.JoinClusterAsync(nodeId, nodeCapabilities);

        var nonExistentAgentId = "non-existent-agent";

        // Act
        var result = await nodeManager.RemoveAgentAsync(nonExistentAgentId);

        // Assert
        Assert.False(result);

        // Cleanup
        await nodeManager.LeaveClusterAsync();
        await nodeManager.StopAsync();
        await nodeManager.DisposeAsync();
    }

    [Fact]
    public async Task GetResourceMetrics_Should_Return_Valid_Metrics()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);
        await nodeManager.InitializeAsync();
        await nodeManager.StartAsync();

        var nodeId = "test-node-1";
        var nodeCapabilities = new[] { "ai.dialogue" };
        await nodeManager.JoinClusterAsync(nodeId, nodeCapabilities);

        // Act
        var metrics = nodeManager.GetResourceMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(nodeId, metrics.NodeId);
        Assert.True(metrics.CpuUsage >= 0 && metrics.CpuUsage <= 100);
        Assert.True(metrics.MemoryUsage >= 0);
        Assert.True(metrics.ActiveAgents >= 0);
        Assert.True(metrics.MessageThroughput >= 0);
        Assert.True(metrics.LastUpdated > DateTime.MinValue);

        // Cleanup
        await nodeManager.LeaveClusterAsync();
        await nodeManager.StopAsync();
        await nodeManager.DisposeAsync();
    }

    [Fact]
    public async Task RouteMessageAsync_WithValidCapabilities_Should_Process_Message()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);
        await nodeManager.InitializeAsync();
        await nodeManager.StartAsync();

        var nodeId = "test-node-1";
        var nodeCapabilities = new[] { "ai.dialogue" };
        await nodeManager.JoinClusterAsync(nodeId, nodeCapabilities);

        var message = "Test message for local processing";
        var requiredCapabilities = new[] { "ai.dialogue" };

        // Act
        var response = await nodeManager.RouteMessageAsync(message, requiredCapabilities);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);

        // Cleanup
        await nodeManager.LeaveClusterAsync();
        await nodeManager.StopAsync();
        await nodeManager.DisposeAsync();
    }

    [Fact]
    public async Task RouteMessageAsync_WithUnavailableCapabilities_Should_Indicate_Unavailable()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);
        await nodeManager.InitializeAsync();
        await nodeManager.StartAsync();

        var nodeId = "test-node-1";
        var nodeCapabilities = new[] { "ai.dialogue" };
        await nodeManager.JoinClusterAsync(nodeId, nodeCapabilities);

        var message = "Test message requiring unavailable capability";
        var requiredCapabilities = new[] { "ai.image.generation" }; // Not available

        // Act
        var response = await nodeManager.RouteMessageAsync(message, requiredCapabilities);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("does not have required capabilities", response);

        // Cleanup
        await nodeManager.LeaveClusterAsync();
        await nodeManager.StopAsync();
        await nodeManager.DisposeAsync();
    }

    [Fact]
    public async Task GetClusterStatusAsync_Should_Return_Local_Node_Status()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);
        await nodeManager.InitializeAsync();
        await nodeManager.StartAsync();

        var nodeId = "test-node-1";
        var nodeCapabilities = new[] { "ai.dialogue", "ai.analysis" };
        await nodeManager.JoinClusterAsync(nodeId, nodeCapabilities);

        // Act
        var status = await nodeManager.GetClusterStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.Equal(nodeId, status.LeaderNodeId);
        Assert.Equal(1, status.ActiveNodes); // Only this node
        Assert.Equal(nodeCapabilities, status.AvailableCapabilities);
        Assert.Equal(ClusterState.Up, status.State);

        // Cleanup
        await nodeManager.LeaveClusterAsync();
        await nodeManager.StopAsync();
        await nodeManager.DisposeAsync();
    }

    [Fact]
    public async Task Multiple_Agents_Should_Be_Managed_Correctly()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);
        await nodeManager.InitializeAsync();
        await nodeManager.StartAsync();

        var nodeId = "test-node-1";
        var nodeCapabilities = new[] { "ai.dialogue", "ai.analysis" };
        await nodeManager.JoinClusterAsync(nodeId, nodeCapabilities);

        var agent1Id = "test-agent-1";
        var agent2Id = "test-agent-2";
        var agentCapabilities = new[] { "ai.assistant" };

        // Act - Create multiple agents
        var result1 = await nodeManager.CreateAgentAsync(agent1Id, agentCapabilities);
        var result2 = await nodeManager.CreateAgentAsync(agent2Id, agentCapabilities);

        // Assert - Both created successfully
        Assert.True(result1);
        Assert.True(result2);

        // Act - Remove agents
        var remove1 = await nodeManager.RemoveAgentAsync(agent1Id);
        var remove2 = await nodeManager.RemoveAgentAsync(agent2Id);

        // Assert - Both removed successfully
        Assert.True(remove1);
        Assert.True(remove2);

        // Cleanup
        await nodeManager.LeaveClusterAsync();
        await nodeManager.StopAsync();
        await nodeManager.DisposeAsync();
    }

    [Fact]
    public async Task Node_Lifecycle_Should_Handle_Graceful_Shutdown()
    {
        // Arrange
        var nodeManager = new AINodeManager(_logger);

        // Act & Assert - Initialize
        await nodeManager.InitializeAsync();
        Assert.False(nodeManager.IsRunning);

        // Act & Assert - Start
        await nodeManager.StartAsync();
        Assert.True(nodeManager.IsRunning);

        // Act & Assert - Join cluster and create agents
        var joinResult = await nodeManager.JoinClusterAsync("test-node", new[] { "ai.test" });
        Assert.True(joinResult);

        var agentResult = await nodeManager.CreateAgentAsync("test-agent", new[] { "ai.test" });
        Assert.True(agentResult);

        // Act & Assert - Stop (should gracefully shutdown agents)
        await nodeManager.StopAsync();
        Assert.False(nodeManager.IsRunning);

        // Act & Assert - Dispose
        await nodeManager.DisposeAsync();
        Assert.False(nodeManager.IsRunning);
    }
}
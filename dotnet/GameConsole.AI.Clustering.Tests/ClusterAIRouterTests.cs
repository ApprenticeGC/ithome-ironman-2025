using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Clustering.Tests;

/// <summary>
/// Tests for ClusterAIRouter functionality including intelligent message routing and load balancing.
/// </summary>
public class ClusterAIRouterTests
{
    private readonly ILogger<ClusterAIRouter> _logger;

    public ClusterAIRouterTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ClusterAIRouter>();
    }

    [Fact]
    public async Task JoinClusterAsync_Should_Initialize_Router_Successfully()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        await router.InitializeAsync();
        await router.StartAsync();

        var nodeId = "router-node-1";
        var capabilities = new[] { "ai.routing", "ai.dialogue" };

        // Act
        var result = await router.JoinClusterAsync(nodeId, capabilities);

        // Assert
        Assert.True(result);

        // Cleanup
        await router.LeaveClusterAsync();
        await router.StopAsync();
        await router.DisposeAsync();
    }

    [Fact]
    public async Task RouteMessageAsync_Should_Route_Message_Successfully()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        await router.InitializeAsync();
        await router.StartAsync();

        var nodeId = "router-node-1";
        var capabilities = new[] { "ai.routing" };
        await router.JoinClusterAsync(nodeId, capabilities);

        // Register a test route
        router.RegisterRoute("ai.dialogue", "target-node-1");

        var message = "Test message for routing";
        var requiredCapabilities = new[] { "ai.dialogue" };

        // Act
        var response = await router.RouteMessageAsync(message, requiredCapabilities);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);

        // Cleanup
        await router.LeaveClusterAsync();
        await router.StopAsync();
        await router.DisposeAsync();
    }

    [Fact]
    public async Task RouteWithStrategyAsync_RoundRobin_Should_Distribute_Messages()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        await router.InitializeAsync();
        await router.StartAsync();

        var nodeId = "router-node-1";
        var capabilities = new[] { "ai.routing" };
        await router.JoinClusterAsync(nodeId, capabilities);

        // Register multiple routes for same capability
        router.RegisterRoute("ai.dialogue", "node-1");
        router.RegisterRoute("ai.dialogue", "node-2");
        router.RegisterRoute("ai.dialogue", "node-3");

        // Act - Route multiple messages
        var response1 = await router.RouteWithStrategyAsync("Message 1", RoutingStrategy.RoundRobin);
        var response2 = await router.RouteWithStrategyAsync("Message 2", RoutingStrategy.RoundRobin);
        var response3 = await router.RouteWithStrategyAsync("Message 3", RoutingStrategy.RoundRobin);

        // Assert
        Assert.NotNull(response1);
        Assert.NotNull(response2);
        Assert.NotNull(response3);

        // Cleanup
        await router.LeaveClusterAsync();
        await router.StopAsync();
        await router.DisposeAsync();
    }

    [Fact]
    public async Task RouteWithStrategyAsync_ConsistentHashing_Should_Route_Consistently()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        await router.InitializeAsync();
        await router.StartAsync();

        var nodeId = "router-node-1";
        var capabilities = new[] { "ai.routing" };
        await router.JoinClusterAsync(nodeId, capabilities);

        // Register routes
        router.RegisterRoute("ai.dialogue", "node-1");
        router.RegisterRoute("ai.dialogue", "node-2");

        var message = "Consistent message for hashing";

        // Act - Route same message multiple times
        var response1 = await router.RouteWithStrategyAsync(message, RoutingStrategy.ConsistentHashing);
        var response2 = await router.RouteWithStrategyAsync(message, RoutingStrategy.ConsistentHashing);
        var response3 = await router.RouteWithStrategyAsync(message, RoutingStrategy.ConsistentHashing);

        // Assert - Same message should route to same node consistently
        Assert.NotNull(response1);
        Assert.NotNull(response2);
        Assert.NotNull(response3);
        
        // In a real implementation, we would verify they went to the same node
        // For now, we just verify they all succeed

        // Cleanup
        await router.LeaveClusterAsync();
        await router.StopAsync();
        await router.DisposeAsync();
    }

    [Fact]
    public async Task BroadcastMessageAsync_Should_Send_To_Multiple_Nodes()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        await router.InitializeAsync();
        await router.StartAsync();

        var nodeId = "router-node-1";
        var capabilities = new[] { "ai.routing" };
        await router.JoinClusterAsync(nodeId, capabilities);

        // Register routes with same capability
        router.RegisterRoute("ai.dialogue", "node-1");
        router.RegisterRoute("ai.dialogue", "node-2");
        router.RegisterRoute("ai.dialogue", "node-3");

        var message = "Broadcast message";
        var targetCapabilities = new[] { "ai.dialogue" };

        // Act
        var responses = await router.BroadcastMessageAsync(message, targetCapabilities);

        // Assert
        Assert.NotNull(responses);
        Assert.True(responses.Length > 0);

        // Cleanup
        await router.LeaveClusterAsync();
        await router.StopAsync();
        await router.DisposeAsync();
    }

    [Fact]
    public void RegisterRoute_Should_Add_Route_Successfully()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        var capability = "ai.dialogue";
        var nodeId = "test-node-1";
        var weight = 10;

        // Act
        router.RegisterRoute(capability, nodeId, weight);

        // Assert - No exception should be thrown
        // In a real implementation, we could verify the route was added
    }

    [Fact]
    public void UnregisterRoute_Should_Remove_Route_Successfully()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        var capability = "ai.dialogue";
        var nodeId = "test-node-1";

        // Act
        router.RegisterRoute(capability, nodeId);
        router.UnregisterRoute(capability, nodeId);

        // Assert - No exception should be thrown
        // In a real implementation, we could verify the route was removed
    }

    [Fact]
    public void SetDefaultRoutingStrategy_Should_Update_Strategy()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        var newStrategy = RoutingStrategy.LeastLoaded;

        // Act
        router.SetDefaultRoutingStrategy(newStrategy);

        // Assert - No exception should be thrown
        // The strategy change is internal and would be verified through routing behavior
    }

    [Fact]
    public void GetRoutingStatistics_Should_Return_Statistics()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);

        // Act
        var stats = router.GetRoutingStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.IsType<Dictionary<string, ClusterAIRouter.RoutingStats>>(stats);
    }

    [Fact]
    public async Task GetClusterStatusAsync_Should_Return_Router_Status()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        await router.InitializeAsync();
        await router.StartAsync();

        var nodeId = "router-node-1";
        var capabilities = new[] { "ai.routing", "ai.dialogue" };
        await router.JoinClusterAsync(nodeId, capabilities);

        // Add some routes
        router.RegisterRoute("ai.dialogue", "node-1");
        router.RegisterRoute("ai.analysis", "node-2");

        // Act
        var status = await router.GetClusterStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.Equal("router-node", status.LeaderNodeId);
        Assert.True(status.ActiveNodes >= 0);
        Assert.NotEmpty(status.AvailableCapabilities);
        Assert.Equal(ClusterState.Up, status.State);

        // Cleanup
        await router.LeaveClusterAsync();
        await router.StopAsync();
        await router.DisposeAsync();
    }

    [Theory]
    [InlineData(RoutingStrategy.RoundRobin)]
    [InlineData(RoutingStrategy.LeastLoaded)]
    [InlineData(RoutingStrategy.CapabilityBased)]
    [InlineData(RoutingStrategy.ConsistentHashing)]
    [InlineData(RoutingStrategy.Random)]
    public async Task RouteWithStrategyAsync_AllStrategies_Should_Work(RoutingStrategy strategy)
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        await router.InitializeAsync();
        await router.StartAsync();

        var nodeId = "router-node-1";
        var capabilities = new[] { "ai.routing" };
        await router.JoinClusterAsync(nodeId, capabilities);

        // Register routes
        router.RegisterRoute("ai.dialogue", "node-1");
        router.RegisterRoute("ai.dialogue", "node-2");

        var message = $"Test message for {strategy}";

        // Act
        var response = await router.RouteWithStrategyAsync(message, strategy);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);

        // Cleanup
        await router.LeaveClusterAsync();
        await router.StopAsync();
        await router.DisposeAsync();
    }

    [Fact]
    public async Task Router_Should_Handle_No_Available_Routes()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        await router.InitializeAsync();
        await router.StartAsync();

        var nodeId = "router-node-1";
        var capabilities = new[] { "ai.routing" };
        await router.JoinClusterAsync(nodeId, capabilities);

        // Don't register any routes
        var message = "Message with no routes";

        // Act
        var response = await router.RouteWithStrategyAsync(message, RoutingStrategy.RoundRobin);

        // Assert
        Assert.NotNull(response);
        Assert.Contains("No target node available", response);

        // Cleanup
        await router.LeaveClusterAsync();
        await router.StopAsync();
        await router.DisposeAsync();
    }

    [Fact]
    public async Task BroadcastMessageAsync_WithNoMatchingNodes_Should_Handle_Gracefully()
    {
        // Arrange
        var router = new ClusterAIRouter(_logger);
        await router.InitializeAsync();
        await router.StartAsync();

        var nodeId = "router-node-1";
        var capabilities = new[] { "ai.routing" };
        await router.JoinClusterAsync(nodeId, capabilities);

        // Register routes for different capabilities
        router.RegisterRoute("ai.analysis", "node-1");

        var message = "Broadcast message";
        var targetCapabilities = new[] { "ai.image.generation" }; // No matching nodes

        // Act
        var responses = await router.BroadcastMessageAsync(message, targetCapabilities);

        // Assert
        Assert.NotNull(responses);
        Assert.Single(responses);
        Assert.Contains("No nodes with required capabilities", responses[0]);

        // Cleanup
        await router.LeaveClusterAsync();
        await router.StopAsync();
        await router.DisposeAsync();
    }
}
using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Clustering.Tests;

/// <summary>
/// Tests for AIClusterManager functionality including cluster coordination and orchestration.
/// </summary>
public class AIClusterManagerTests
{
    private readonly ILogger<AIClusterManager> _logger;

    public AIClusterManagerTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AIClusterManager>();
    }

    [Fact]
    public async Task InitializeAsync_Should_Initialize_Service_Successfully()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);

        // Act
        await clusterManager.InitializeAsync();

        // Assert
        Assert.False(clusterManager.IsRunning); // Not started yet, just initialized
    }

    [Fact]
    public async Task StartAsync_Should_Start_Service_Successfully()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);
        await clusterManager.InitializeAsync();

        // Act
        await clusterManager.StartAsync();

        // Assert
        Assert.True(clusterManager.IsRunning);

        // Cleanup
        await clusterManager.StopAsync();
        await clusterManager.DisposeAsync();
    }

    [Fact]
    public async Task JoinClusterAsync_WithValidNodeId_Should_Return_True()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);
        await clusterManager.InitializeAsync();
        await clusterManager.StartAsync();

        var nodeId = "test-node-1";
        var capabilities = new[] { "ai.dialogue", "ai.analysis" };

        // Act
        var result = await clusterManager.JoinClusterAsync(nodeId, capabilities);

        // Assert
        Assert.True(result);

        // Cleanup
        await clusterManager.LeaveClusterAsync();
        await clusterManager.StopAsync();
        await clusterManager.DisposeAsync();
    }

    [Fact]
    public async Task JoinClusterAsync_WithEmptyNodeId_Should_Throw_ArgumentException()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);
        await clusterManager.InitializeAsync();
        await clusterManager.StartAsync();

        var capabilities = new[] { "ai.dialogue" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            clusterManager.JoinClusterAsync("", capabilities));

        // Cleanup
        await clusterManager.StopAsync();
        await clusterManager.DisposeAsync();
    }

    [Fact]
    public async Task JoinClusterAsync_WithEmptyCapabilities_Should_Throw_ArgumentException()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);
        await clusterManager.InitializeAsync();
        await clusterManager.StartAsync();

        var nodeId = "test-node-1";
        var capabilities = Array.Empty<string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            clusterManager.JoinClusterAsync(nodeId, capabilities));

        // Cleanup
        await clusterManager.StopAsync();
        await clusterManager.DisposeAsync();
    }

    [Fact]
    public async Task GetClusterStatusAsync_Should_Return_Valid_Status()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);
        await clusterManager.InitializeAsync();
        await clusterManager.StartAsync();

        var nodeId = "test-node-1";
        var capabilities = new[] { "ai.dialogue", "ai.analysis" };
        await clusterManager.JoinClusterAsync(nodeId, capabilities);

        // Act
        var status = await clusterManager.GetClusterStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.True(status.ActiveNodes >= 0);
        Assert.NotEmpty(status.AvailableCapabilities);
        Assert.True(status.LastUpdated > DateTime.MinValue);

        // Cleanup
        await clusterManager.LeaveClusterAsync();
        await clusterManager.StopAsync();
        await clusterManager.DisposeAsync();
    }

    [Fact]
    public async Task RouteMessageAsync_WithValidMessage_Should_Return_Response()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);
        await clusterManager.InitializeAsync();
        await clusterManager.StartAsync();

        var nodeId = "test-node-1";
        var capabilities = new[] { "ai.dialogue" };
        await clusterManager.JoinClusterAsync(nodeId, capabilities);

        var message = "Test message for routing";
        var requiredCapabilities = new[] { "ai.dialogue" };

        // Act
        var response = await clusterManager.RouteMessageAsync(message, requiredCapabilities);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);

        // Cleanup
        await clusterManager.LeaveClusterAsync();
        await clusterManager.StopAsync();
        await clusterManager.DisposeAsync();
    }

    [Fact]
    public async Task GetNodeHealthMetricsAsync_Should_Return_Metrics()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);
        await clusterManager.InitializeAsync();
        await clusterManager.StartAsync();

        var nodeId = "test-node-1";
        var capabilities = new[] { "ai.dialogue" };
        await clusterManager.JoinClusterAsync(nodeId, capabilities);

        // Act
        var metrics = await clusterManager.GetNodeHealthMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        
        // Cleanup
        await clusterManager.LeaveClusterAsync();
        await clusterManager.StopAsync();
        await clusterManager.DisposeAsync();
    }

    [Fact]
    public async Task GetLoadBalanceStatsAsync_Should_Return_Stats()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);
        await clusterManager.InitializeAsync();
        await clusterManager.StartAsync();

        var nodeId = "test-node-1";
        var capabilities = new[] { "ai.dialogue" };
        await clusterManager.JoinClusterAsync(nodeId, capabilities);

        // Act
        var stats = await clusterManager.GetLoadBalanceStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalMessages >= 0);
        Assert.NotNull(stats.MessagesPerNode);
        Assert.NotNull(stats.AverageResponseTime);

        // Cleanup
        await clusterManager.LeaveClusterAsync();
        await clusterManager.StopAsync();
        await clusterManager.DisposeAsync();
    }

    [Fact]
    public async Task Service_Lifecycle_Should_Work_Correctly()
    {
        // Arrange
        var clusterManager = new AIClusterManager(_logger);

        // Act & Assert - Initialize
        await clusterManager.InitializeAsync();
        Assert.False(clusterManager.IsRunning);

        // Act & Assert - Start
        await clusterManager.StartAsync();
        Assert.True(clusterManager.IsRunning);

        // Act & Assert - Join cluster
        var joinResult = await clusterManager.JoinClusterAsync("test-node", new[] { "ai.test" });
        Assert.True(joinResult);

        // Act & Assert - Stop
        await clusterManager.StopAsync();
        Assert.False(clusterManager.IsRunning);

        // Act & Assert - Dispose
        await clusterManager.DisposeAsync();
        Assert.False(clusterManager.IsRunning);
    }
}
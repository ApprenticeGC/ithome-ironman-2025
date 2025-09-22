using GameConsole.AI.Clustering;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Clustering.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task AIClusteringServices_ShouldWorkTogether()
    {
        // Arrange
        var loggerFactory = new LoggerFactory();
        var clusterManager = new AIClusterManager(loggerFactory.CreateLogger<AIClusterManager>());
        var nodeManager = new AINodeManager(loggerFactory.CreateLogger<AINodeManager>());
        var router = new ClusterAIRouter(loggerFactory.CreateLogger<ClusterAIRouter>());
        var monitor = new AIClusterMonitor(loggerFactory.CreateLogger<AIClusterMonitor>());

        // Act & Assert - Initialize all services
        await clusterManager.InitializeAsync();
        await nodeManager.InitializeAsync();
        await router.InitializeAsync();
        await monitor.InitializeAsync();

        // Start all services
        await clusterManager.StartAsync();
        await nodeManager.StartAsync();
        await router.StartAsync();
        await monitor.StartAsync();

        // Verify all services are running
        Assert.True(clusterManager.IsRunning);
        Assert.True(nodeManager.IsRunning);
        Assert.True(router.IsRunning);
        Assert.True(monitor.IsRunning);

        // Test cluster formation
        await clusterManager.FormClusterAsync();
        Assert.Equal(GameConsole.Core.Abstractions.ClusterStatus.Up, clusterManager.Status);

        // Test node health monitoring
        Assert.Equal(GameConsole.Core.Abstractions.NodeHealth.Healthy, nodeManager.Health);
        await nodeManager.UpdateMetricsAsync(50.0, 60.0, 5);

        // Test routing functionality
        var availableNodes = await router.GetAvailableNodesAsync("text-processing", 3);
        Assert.NotEmpty(availableNodes);

        var routedNode = await router.RouteMessageAsync("test-msg", "text-processing", 2);
        Assert.NotNull(routedNode);
        Assert.NotEmpty(routedNode);

        // Test monitoring
        await monitor.StartNodeMonitoringAsync("akka://test-node@127.0.0.1:2554");
        var metrics = await monitor.GetPerformanceMetricsAsync();
        Assert.NotNull(metrics);

        // Clean shutdown
        await clusterManager.StopAsync();
        await nodeManager.StopAsync();
        await router.StopAsync();
        await monitor.StopAsync();

        // Verify all services stopped
        Assert.False(clusterManager.IsRunning);
        Assert.False(nodeManager.IsRunning);
        Assert.False(router.IsRunning);
        Assert.False(monitor.IsRunning);

        // Dispose all services
        await clusterManager.DisposeAsync();
        await nodeManager.DisposeAsync();
        await router.DisposeAsync();
        await monitor.DisposeAsync();
    }
}
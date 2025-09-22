using GameConsole.AI.Clustering.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Clustering.Tests;

/// <summary>
/// Tests for AIClusterMonitor functionality including health tracking and failure detection.
/// </summary>
public class AIClusterMonitorTests
{
    private readonly ILogger<AIClusterMonitor> _logger;

    public AIClusterMonitorTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AIClusterMonitor>();
    }

    [Fact]
    public async Task JoinClusterAsync_Should_Initialize_Monitor_Successfully()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        await monitor.InitializeAsync();
        await monitor.StartAsync();

        var nodeId = "monitor-node-1";
        var capabilities = new[] { "ai.monitoring" };

        // Act
        var result = await monitor.JoinClusterAsync(nodeId, capabilities);

        // Assert
        Assert.True(result);

        // Cleanup
        await monitor.LeaveClusterAsync();
        await monitor.StopAsync();
        await monitor.DisposeAsync();
    }

    [Fact]
    public void RegisterNode_Should_Add_Node_For_Monitoring()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        var nodeId = "test-node-1";
        var capabilities = new[] { "ai.dialogue", "ai.analysis" };
        var nodeJoined = false;

        monitor.NodeJoined += (sender, e) =>
        {
            nodeJoined = true;
            Assert.Equal(nodeId, e.NodeId);
            Assert.Equal(capabilities, e.Capabilities);
        };

        // Act
        monitor.RegisterNode(nodeId, capabilities);

        // Assert
        Assert.True(nodeJoined);
    }

    [Fact]
    public void UnregisterNode_Should_Remove_Node_From_Monitoring()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        var nodeId = "test-node-1";
        var capabilities = new[] { "ai.dialogue" };
        var reason = "Test removal";
        var nodeLeft = false;

        monitor.RegisterNode(nodeId, capabilities);

        monitor.NodeLeft += (sender, e) =>
        {
            nodeLeft = true;
            Assert.Equal(nodeId, e.NodeId);
            Assert.Equal(reason, e.Reason);
        };

        // Act
        monitor.UnregisterNode(nodeId, reason);

        // Assert
        Assert.True(nodeLeft);
    }

    [Fact]
    public void UpdateNodeHealth_Should_Update_Metrics_And_Status()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        var nodeId = "test-node-1";
        var capabilities = new[] { "ai.dialogue" };
        monitor.RegisterNode(nodeId, capabilities);

        var healthChanged = false;
        monitor.NodeHealthChanged += (sender, e) =>
        {
            healthChanged = true;
            Assert.Equal(nodeId, e.NodeId);
        };

        // Act - Update with unhealthy metrics
        monitor.UpdateNodeHealth(nodeId, cpuUsage: 95.0, memoryUsage: 95.0, activeMessages: 100);

        // Assert
        Assert.True(healthChanged);
    }

    [Fact]
    public async Task GetNodeHealthMetricsAsync_Should_Return_All_Node_Metrics()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        await monitor.InitializeAsync();
        await monitor.StartAsync();

        var nodeId = "monitor-node-1";
        var capabilities = new[] { "ai.monitoring" };
        await monitor.JoinClusterAsync(nodeId, capabilities);

        // Register test nodes
        monitor.RegisterNode("node-1", new[] { "ai.dialogue" });
        monitor.RegisterNode("node-2", new[] { "ai.analysis" });

        // Update health for nodes
        monitor.UpdateNodeHealth("node-1", 50.0, 60.0, 10);
        monitor.UpdateNodeHealth("node-2", 30.0, 40.0, 5);

        // Act
        var metrics = await monitor.GetNodeHealthMetricsAsync();

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.Length >= 2);

        var node1Metrics = metrics.FirstOrDefault(m => m.NodeId == "node-1");
        var node2Metrics = metrics.FirstOrDefault(m => m.NodeId == "node-2");

        Assert.NotNull(node1Metrics);
        Assert.NotNull(node2Metrics);
        Assert.Equal(50.0, node1Metrics.CpuUsage);
        Assert.Equal(30.0, node2Metrics.CpuUsage);

        // Cleanup
        await monitor.LeaveClusterAsync();
        await monitor.StopAsync();
        await monitor.DisposeAsync();
    }

    [Fact]
    public async Task GetLoadBalanceStatsAsync_Should_Return_Load_Statistics()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        await monitor.InitializeAsync();
        await monitor.StartAsync();

        var nodeId = "monitor-node-1";
        var capabilities = new[] { "ai.monitoring" };
        await monitor.JoinClusterAsync(nodeId, capabilities);

        // Register and update nodes
        monitor.RegisterNode("node-1", new[] { "ai.dialogue" });
        monitor.RegisterNode("node-2", new[] { "ai.analysis" });
        monitor.UpdateNodeHealth("node-1", 50.0, 60.0, 10);
        monitor.UpdateNodeHealth("node-2", 30.0, 40.0, 5);

        // Act
        var stats = await monitor.GetLoadBalanceStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalMessages >= 0);
        Assert.NotNull(stats.MessagesPerNode);
        Assert.NotNull(stats.AverageResponseTime);
        Assert.True(stats.LoadDistributionEfficiency >= 0 && stats.LoadDistributionEfficiency <= 1);

        // Cleanup
        await monitor.LeaveClusterAsync();
        await monitor.StopAsync();
        await monitor.DisposeAsync();
    }

    [Fact]
    public void GetHealthyNodesWithCapabilities_Should_Return_Matching_Healthy_Nodes()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);

        // Register nodes with different capabilities and health states
        monitor.RegisterNode("healthy-node-1", new[] { "ai.dialogue", "ai.analysis" });
        monitor.RegisterNode("healthy-node-2", new[] { "ai.dialogue", "ai.generation" });
        monitor.RegisterNode("unhealthy-node", new[] { "ai.dialogue" });

        // Update health - make one unhealthy
        monitor.UpdateNodeHealth("healthy-node-1", 20.0, 30.0, 5);
        monitor.UpdateNodeHealth("healthy-node-2", 25.0, 35.0, 3);
        monitor.UpdateNodeHealth("unhealthy-node", 95.0, 95.0, 1000); // Should be unhealthy

        var requiredCapabilities = new[] { "ai.dialogue" };

        // Act
        var healthyNodes = monitor.GetHealthyNodesWithCapabilities(requiredCapabilities);

        // Assert
        Assert.NotNull(healthyNodes);
        Assert.Contains("healthy-node-1", healthyNodes);
        Assert.Contains("healthy-node-2", healthyNodes);
        Assert.DoesNotContain("unhealthy-node", healthyNodes);
    }

    [Fact]
    public void GetClusterHealthSummary_Should_Return_Comprehensive_Summary()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);

        // Register nodes with different health states
        monitor.RegisterNode("healthy-node", new[] { "ai.dialogue" });
        monitor.RegisterNode("degraded-node", new[] { "ai.analysis" });
        monitor.RegisterNode("unhealthy-node", new[] { "ai.generation" });

        // Update health to create different statuses
        monitor.UpdateNodeHealth("healthy-node", 20.0, 30.0, 5);      // Healthy
        monitor.UpdateNodeHealth("degraded-node", 80.0, 80.0, 100);   // Degraded  
        monitor.UpdateNodeHealth("unhealthy-node", 95.0, 95.0, 1000); // Unhealthy

        // Act
        var summary = monitor.GetClusterHealthSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(3, summary.TotalNodes);
        Assert.Equal(1, summary.HealthyNodes);
        Assert.Equal(1, summary.DegradedNodes);
        Assert.Equal(1, summary.UnhealthyNodes);
        Assert.Equal(0, summary.UnreachableNodes);
        Assert.True(summary.AverageCpuUsage > 0);
        Assert.True(summary.AverageMemoryUsage > 0);
        Assert.True(summary.TotalActiveMessages > 0);
        Assert.True(summary.LastUpdated > DateTime.MinValue);
    }

    [Fact]
    public void GetNodeHealthHistory_Should_Return_Event_History()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        var nodeId = "test-node";
        var capabilities = new[] { "ai.dialogue" };

        monitor.RegisterNode(nodeId, capabilities);

        // Trigger health status changes to generate history
        monitor.UpdateNodeHealth(nodeId, 30.0, 40.0, 10);  // Healthy
        monitor.UpdateNodeHealth(nodeId, 80.0, 80.0, 100); // Degraded
        monitor.UpdateNodeHealth(nodeId, 95.0, 95.0, 1000); // Unhealthy

        // Act
        var history = monitor.GetNodeHealthHistory(nodeId);

        // Assert
        Assert.NotNull(history);
        Assert.True(history.Count > 0);

        // Verify history contains status change events
        Assert.Contains(history, h => h.EventType == "StatusChange");
    }

    [Fact]
    public async Task GetClusterStatusAsync_Should_Include_All_Registered_Nodes()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        await monitor.InitializeAsync();
        await monitor.StartAsync();

        var nodeId = "monitor-node-1";
        var capabilities = new[] { "ai.monitoring" };
        await monitor.JoinClusterAsync(nodeId, capabilities);

        // Register multiple nodes
        monitor.RegisterNode("node-1", new[] { "ai.dialogue", "ai.analysis" });
        monitor.RegisterNode("node-2", new[] { "ai.generation" });
        monitor.UpdateNodeHealth("node-1", 20.0, 30.0, 5);
        monitor.UpdateNodeHealth("node-2", 25.0, 35.0, 3);

        // Act
        var status = await monitor.GetClusterStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.True(status.ActiveNodes >= 2);
        Assert.Contains("ai.dialogue", status.AvailableCapabilities);
        Assert.Contains("ai.analysis", status.AvailableCapabilities);
        Assert.Contains("ai.generation", status.AvailableCapabilities);
        Assert.Equal(ClusterState.Up, status.State);

        // Cleanup
        await monitor.LeaveClusterAsync();
        await monitor.StopAsync();
        await monitor.DisposeAsync();
    }

    [Fact]
    public async Task Monitor_Should_Handle_Node_Timeout_Scenarios()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        await monitor.InitializeAsync();
        await monitor.StartAsync();

        var nodeId = "monitor-node-1";
        var capabilities = new[] { "ai.monitoring" };
        await monitor.JoinClusterAsync(nodeId, capabilities);

        // Register a node but don't update its health (simulating timeout)
        monitor.RegisterNode("timeout-node", new[] { "ai.dialogue" });

        // Act - Wait briefly and check (in real scenario, would wait for timeout period)
        var healthyNodes = monitor.GetHealthyNodesWithCapabilities(new[] { "ai.dialogue" });

        // Assert - Node should still be considered available initially
        Assert.Contains("timeout-node", healthyNodes);

        // Cleanup
        await monitor.LeaveClusterAsync();
        await monitor.StopAsync();
        await monitor.DisposeAsync();
    }

    [Fact]
    public async Task Service_Lifecycle_Should_Work_With_Event_Handlers()
    {
        // Arrange
        var monitor = new AIClusterMonitor(_logger);
        var eventsReceived = 0;

        monitor.NodeJoined += (s, e) => eventsReceived++;
        monitor.NodeLeft += (s, e) => eventsReceived++;
        monitor.NodeHealthChanged += (s, e) => eventsReceived++;

        // Act & Assert
        await monitor.InitializeAsync();
        await monitor.StartAsync();

        await monitor.JoinClusterAsync("test-node", new[] { "ai.test" });
        
        monitor.RegisterNode("test-node-2", new[] { "ai.test" });
        monitor.UpdateNodeHealth("test-node-2", 95.0, 95.0, 1000); // Should trigger health change
        monitor.UnregisterNode("test-node-2");

        Assert.True(eventsReceived >= 2); // At least join and health change events

        await monitor.StopAsync();
        await monitor.DisposeAsync();
    }
}
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using GameConsole.AI.Clustering.Models;
using GameConsole.AI.Clustering.Services;

namespace GameConsole.AI.Clustering.Tests;

/// <summary>
/// Unit tests for AIClusterManager service.
/// </summary>
public class AIClusterManagerTests : IDisposable
{
    private readonly Mock<ILogger<AIClusterManager>> _mockLogger;
    private readonly AIClusterManager _clusterManager;

    public AIClusterManagerTests()
    {
        _mockLogger = new Mock<ILogger<AIClusterManager>>();
        _clusterManager = new AIClusterManager(_mockLogger.Object);
    }

    [Fact]
    public async Task InitializeAsync_Should_Complete_Successfully()
    {
        // Act
        await _clusterManager.InitializeAsync();

        // Assert
        // No exceptions should be thrown
    }

    [Fact]
    public async Task StartAsync_Should_Set_IsRunning_To_True()
    {
        // Arrange
        await _clusterManager.InitializeAsync();

        // Act
        await _clusterManager.StartAsync();

        // Assert
        Assert.True(_clusterManager.IsRunning);
    }

    [Fact]
    public async Task StopAsync_Should_Set_IsRunning_To_False()
    {
        // Arrange
        await _clusterManager.InitializeAsync();
        await _clusterManager.StartAsync();

        // Act
        await _clusterManager.StopAsync();

        // Assert
        Assert.False(_clusterManager.IsRunning);
    }

    [Fact]
    public async Task InitializeClusterAsync_Should_Set_Configuration()
    {
        // Arrange
        var config = new ClusterConfiguration
        {
            ClusterName = "test-cluster",
            SeedNodes = new[] { "akka.tcp://test-cluster@127.0.0.1:8080" },
            MinimumNodes = 1,
            MaximumNodes = 10,
            AutoScaling = new AutoScalingConfiguration { Enabled = true }
        };

        // Act
        await _clusterManager.InitializeClusterAsync(config);

        // Assert
        Assert.Equal("test-cluster", _clusterManager.Configuration.ClusterName);
        Assert.Single(_clusterManager.Configuration.SeedNodes);
    }

    [Fact]
    public async Task GetClusterNodesAsync_Initially_Should_Return_Empty_List()
    {
        // Act
        var nodes = await _clusterManager.GetClusterNodesAsync();

        // Assert
        Assert.Empty(nodes);
    }

    [Fact]
    public async Task AddNodeAsync_Should_Add_Node_And_Raise_Event()
    {
        // Arrange
        var node = CreateTestNode("node1");
        ClusterNode? eventNode = null;
        _clusterManager.NodeJoined += (sender, n) => eventNode = n;

        // Act
        await _clusterManager.AddNodeAsync(node);

        // Assert
        var nodes = await _clusterManager.GetClusterNodesAsync();
        Assert.Single(nodes);
        Assert.Equal("node1", nodes[0].NodeId);
        Assert.NotNull(eventNode);
        Assert.Equal("node1", eventNode.NodeId);
    }

    [Fact]
    public async Task RemoveNodeAsync_Should_Remove_Node_And_Raise_Event()
    {
        // Arrange
        var node = CreateTestNode("node1");
        await _clusterManager.AddNodeAsync(node);
        
        string? eventNodeId = null;
        _clusterManager.NodeLeft += (sender, nodeId) => eventNodeId = nodeId;

        // Act
        await _clusterManager.RemoveNodeAsync("node1");

        // Assert
        var nodes = await _clusterManager.GetClusterNodesAsync();
        Assert.Empty(nodes);
        Assert.Equal("node1", eventNodeId);
    }

    [Fact]
    public async Task UpdateConfigurationAsync_Should_Update_Config_And_Raise_Event()
    {
        // Arrange
        var originalConfig = new ClusterConfiguration
        {
            ClusterName = "original-cluster",
            SeedNodes = new[] { "akka.tcp://original-cluster@127.0.0.1:8080" },
            MinimumNodes = 1,
            MaximumNodes = 5,
            AutoScaling = new AutoScalingConfiguration { Enabled = false }
        };
        
        var newConfig = new ClusterConfiguration
        {
            ClusterName = "updated-cluster",
            SeedNodes = new[] { "akka.tcp://updated-cluster@127.0.0.1:8080" },
            MinimumNodes = 2,
            MaximumNodes = 10,
            AutoScaling = new AutoScalingConfiguration { Enabled = true }
        };

        await _clusterManager.InitializeClusterAsync(originalConfig);
        
        ClusterConfiguration? eventConfig = null;
        _clusterManager.ConfigurationChanged += (sender, config) => eventConfig = config;

        // Act
        await _clusterManager.UpdateConfigurationAsync(newConfig);

        // Assert
        Assert.Equal("updated-cluster", _clusterManager.Configuration.ClusterName);
        Assert.Equal(2, _clusterManager.Configuration.MinimumNodes);
        Assert.NotNull(eventConfig);
        Assert.Equal("updated-cluster", eventConfig.ClusterName);
    }

    [Fact]
    public async Task FormClusterAsync_Should_Throw_When_Not_Initialized()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _clusterManager.FormClusterAsync());
    }

    [Fact]
    public async Task Service_Lifecycle_Should_Work_Correctly()
    {
        // Test full service lifecycle
        
        // Initialize
        Assert.False(_clusterManager.IsRunning);
        await _clusterManager.InitializeAsync();
        Assert.False(_clusterManager.IsRunning);

        // Start
        await _clusterManager.StartAsync();
        Assert.True(_clusterManager.IsRunning);

        // Stop
        await _clusterManager.StopAsync();
        Assert.False(_clusterManager.IsRunning);
    }

    public void Dispose()
    {
        _clusterManager?.DisposeAsync().AsTask().Wait();
    }

    private static ClusterNode CreateTestNode(string nodeId)
    {
        return new ClusterNode
        {
            NodeId = nodeId,
            Address = "127.0.0.1",
            Port = 8080,
            Capabilities = new[]
            {
                new AgentCapability
                {
                    CapabilityId = "test-capability",
                    AgentType = "test-agent",
                    SupportedOperations = new[] { "test-operation" },
                    Performance = new CapabilityPerformance
                    {
                        ExpectedLatencyMs = 100,
                        MaxThroughputPerSecond = 10
                    },
                    Resources = new ResourceRequirements
                    {
                        MinCpuCores = 1.0,
                        MinMemoryMb = 512
                    }
                }
            },
            Health = NodeHealth.Healthy
        };
    }
}
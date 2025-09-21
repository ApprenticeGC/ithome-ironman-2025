using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using GameConsole.AI.Clustering.Models;
using GameConsole.AI.Clustering.Services;

namespace GameConsole.AI.Clustering.Tests;

/// <summary>
/// Unit tests for AINodeManager service.
/// </summary>
public class AINodeManagerTests : IDisposable
{
    private readonly Mock<ILogger<AINodeManager>> _mockLogger;
    private readonly AINodeManager _nodeManager;

    public AINodeManagerTests()
    {
        _mockLogger = new Mock<ILogger<AINodeManager>>();
        _nodeManager = new AINodeManager(_mockLogger.Object);
    }

    [Fact]
    public async Task InitializeAsync_Should_Complete_Successfully()
    {
        // Act
        await _nodeManager.InitializeAsync();

        // Assert
        // No exceptions should be thrown
    }

    [Fact]
    public async Task StartAsync_Should_Set_IsRunning_To_True()
    {
        // Arrange
        await _nodeManager.InitializeAsync();

        // Act
        await _nodeManager.StartAsync();

        // Assert
        Assert.True(_nodeManager.IsRunning);
    }

    [Fact]
    public async Task StopAsync_Should_Set_IsRunning_To_False()
    {
        // Arrange
        await _nodeManager.InitializeAsync();
        await _nodeManager.StartAsync();

        // Act
        await _nodeManager.StopAsync();

        // Assert
        Assert.False(_nodeManager.IsRunning);
        Assert.False(_nodeManager.IsClusterMember);
    }

    [Fact]
    public async Task InitializeNodeAsync_Should_Set_Current_Node()
    {
        // Arrange
        var capabilities = CreateTestCapabilities();

        // Act
        await _nodeManager.InitializeNodeAsync("test-node", "127.0.0.1", 8080, capabilities);

        // Assert
        Assert.NotNull(_nodeManager.CurrentNode);
        Assert.Equal("test-node", _nodeManager.CurrentNode.NodeId);
        Assert.Equal("127.0.0.1", _nodeManager.CurrentNode.Address);
        Assert.Equal(8080, _nodeManager.CurrentNode.Port);
        Assert.True(_nodeManager.IsClusterMember);
    }

    [Fact]
    public async Task UpdateCapabilitiesAsync_Should_Update_Node_Capabilities()
    {
        // Arrange
        var initialCapabilities = CreateTestCapabilities();
        await _nodeManager.InitializeNodeAsync("test-node", "127.0.0.1", 8080, initialCapabilities);

        var newCapabilities = new[]
        {
            new AgentCapability
            {
                CapabilityId = "updated-capability",
                AgentType = "updated-agent",
                SupportedOperations = new[] { "updated-operation" },
                Performance = new CapabilityPerformance
                {
                    ExpectedLatencyMs = 50,
                    MaxThroughputPerSecond = 20
                },
                Resources = new ResourceRequirements
                {
                    MinCpuCores = 2.0,
                    MinMemoryMb = 1024
                }
            }
        };

        IReadOnlyList<AgentCapability>? eventCapabilities = null;
        _nodeManager.CapabilitiesChanged += (sender, caps) => eventCapabilities = caps;

        // Act
        await _nodeManager.UpdateCapabilitiesAsync(newCapabilities);

        // Assert
        Assert.Single(_nodeManager.CurrentNode.Capabilities);
        Assert.Equal("updated-capability", _nodeManager.CurrentNode.Capabilities[0].CapabilityId);
        Assert.NotNull(eventCapabilities);
        Assert.Single(eventCapabilities);
        Assert.Equal("updated-capability", eventCapabilities[0].CapabilityId);
    }

    [Fact]
    public async Task UpdateCapabilitiesAsync_Should_Throw_When_Node_Not_Initialized()
    {
        // Arrange
        var capabilities = CreateTestCapabilities();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _nodeManager.UpdateCapabilitiesAsync(capabilities));
    }

    [Fact]
    public async Task GetHealthStatusAsync_Should_Return_Unknown_When_Not_Initialized()
    {
        // Act
        var health = await _nodeManager.GetHealthStatusAsync();

        // Assert
        Assert.Equal(NodeHealth.Unknown, health);
    }

    [Fact]
    public async Task GetHealthStatusAsync_Should_Return_Healthy_When_Running()
    {
        // Arrange
        var capabilities = CreateTestCapabilities();
        await _nodeManager.InitializeNodeAsync("test-node", "127.0.0.1", 8080, capabilities);
        await _nodeManager.InitializeAsync();
        await _nodeManager.StartAsync();

        // Act
        var health = await _nodeManager.GetHealthStatusAsync();

        // Assert
        Assert.Equal(NodeHealth.Healthy, health);
    }

    [Fact]
    public async Task GetHealthStatusAsync_Should_Return_Offline_When_Not_Running()
    {
        // Arrange
        var capabilities = CreateTestCapabilities();
        await _nodeManager.InitializeNodeAsync("test-node", "127.0.0.1", 8080, capabilities);
        await _nodeManager.InitializeAsync();

        // Act
        var health = await _nodeManager.GetHealthStatusAsync();

        // Assert
        Assert.Equal(NodeHealth.Offline, health);
    }

    [Fact]
    public async Task PerformHealthCheckAsync_Should_Return_Health_Check_Result()
    {
        // Arrange
        var capabilities = CreateTestCapabilities();
        await _nodeManager.InitializeNodeAsync("test-node", "127.0.0.1", 8080, capabilities);
        await _nodeManager.InitializeAsync();
        await _nodeManager.StartAsync();

        // Act
        var result = await _nodeManager.PerformHealthCheckAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(NodeHealth.Healthy, result.Status);
        Assert.Empty(result.Issues);
        Assert.Contains("cpu_utilization", result.Metrics.Keys);
        Assert.Contains("memory_utilization", result.Metrics.Keys);
    }

    [Fact]
    public async Task PerformHealthCheckAsync_Should_Report_Issues_When_Not_Running()
    {
        // Arrange
        var capabilities = CreateTestCapabilities();
        await _nodeManager.InitializeNodeAsync("test-node", "127.0.0.1", 8080, capabilities);
        await _nodeManager.InitializeAsync();

        // Act
        var result = await _nodeManager.PerformHealthCheckAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(NodeHealth.Offline, result.Status);
        Assert.Contains("Node is not running", result.Issues);
    }

    [Fact]
    public async Task GetResourceUtilizationAsync_Should_Return_Utilization_Metrics()
    {
        // Arrange
        var capabilities = CreateTestCapabilities();
        await _nodeManager.InitializeNodeAsync("test-node", "127.0.0.1", 8080, capabilities);

        // Act
        var utilization = await _nodeManager.GetResourceUtilizationAsync();

        // Assert
        Assert.NotNull(utilization);
        Assert.True(utilization.CapturedAt <= DateTime.UtcNow);
        Assert.Equal(0, utilization.ActiveAgentInstances);
    }

    [Fact]
    public async Task PrepareShutdownAsync_Should_Mark_Node_As_Offline()
    {
        // Arrange
        var capabilities = CreateTestCapabilities();
        await _nodeManager.InitializeNodeAsync("test-node", "127.0.0.1", 8080, capabilities);
        await _nodeManager.InitializeAsync();
        await _nodeManager.StartAsync();

        NodeHealth? eventHealth = null;
        _nodeManager.HealthStatusChanged += (sender, health) => eventHealth = health;

        // Act
        await _nodeManager.PrepareShutdownAsync();

        // Assert
        Assert.False(_nodeManager.IsClusterMember);
        Assert.Equal(NodeHealth.Offline, _nodeManager.CurrentNode.Health);
        Assert.Equal(NodeHealth.Offline, eventHealth);
    }

    [Fact]
    public async Task Service_Lifecycle_Should_Work_Correctly()
    {
        // Test full service lifecycle
        
        // Initialize
        Assert.False(_nodeManager.IsRunning);
        await _nodeManager.InitializeAsync();
        Assert.False(_nodeManager.IsRunning);

        // Start
        await _nodeManager.StartAsync();
        Assert.True(_nodeManager.IsRunning);

        // Stop
        await _nodeManager.StopAsync();
        Assert.False(_nodeManager.IsRunning);
    }

    [Fact]
    public async Task Health_Status_Should_Change_Over_Time()
    {
        // Arrange
        var capabilities = CreateTestCapabilities();
        await _nodeManager.InitializeNodeAsync("test-node", "127.0.0.1", 8080, capabilities);
        await _nodeManager.InitializeAsync();

        var healthChanges = new List<NodeHealth>();
        _nodeManager.HealthStatusChanged += (sender, health) => healthChanges.Add(health);

        // Act
        await _nodeManager.StartAsync();
        await Task.Delay(100); // Allow time for health check timer
        await _nodeManager.PrepareShutdownAsync();

        // Assert
        Assert.Contains(NodeHealth.Offline, healthChanges);
    }

    public void Dispose()
    {
        _nodeManager?.DisposeAsync().AsTask().Wait();
    }

    private static IReadOnlyList<AgentCapability> CreateTestCapabilities()
    {
        return new[]
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
        };
    }
}
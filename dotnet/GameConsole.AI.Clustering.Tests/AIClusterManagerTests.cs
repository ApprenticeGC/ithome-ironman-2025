using GameConsole.AI.Clustering;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.AI.Clustering.Tests;

public class AIClusterManagerTests
{
    private readonly ILogger<AIClusterManager> _logger;
    private readonly AIClusterManager _clusterManager;

    public AIClusterManagerTests()
    {
        _logger = new LoggerFactory().CreateLogger<AIClusterManager>();
        _clusterManager = new AIClusterManager(_logger);
    }

    [Fact]
    public async Task InitializeAsync_ShouldCompleteSuccessfully()
    {
        // Act
        await _clusterManager.InitializeAsync();

        // Assert
        // No exceptions should be thrown
    }

    [Fact]
    public async Task StartAsync_ShouldSetIsRunningToTrue()
    {
        // Arrange
        await _clusterManager.InitializeAsync();

        // Act
        await _clusterManager.StartAsync();

        // Assert
        Assert.True(_clusterManager.IsRunning);
    }

    [Fact]
    public async Task FormClusterAsync_ShouldChangeStatusToUp()
    {
        // Arrange
        await _clusterManager.InitializeAsync();
        await _clusterManager.StartAsync();

        // Act
        await _clusterManager.FormClusterAsync();

        // Assert
        Assert.Equal(GameConsole.Core.Abstractions.ClusterStatus.Up, _clusterManager.Status);
    }

    [Fact]
    public async Task StopAsync_ShouldSetIsRunningToFalse()
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
    public async Task JoinClusterAsync_WithSeedNodes_ShouldChangeStatusToUp()
    {
        // Arrange
        await _clusterManager.InitializeAsync();
        await _clusterManager.StartAsync();
        var seedNodes = new[] { "akka://ai-cluster@127.0.0.1:2552" };

        // Act
        await _clusterManager.JoinClusterAsync(seedNodes);

        // Assert
        Assert.Equal(GameConsole.Core.Abstractions.ClusterStatus.Up, _clusterManager.Status);
    }

    [Fact]
    public async Task MembershipChanged_ShouldBeRaisedWhenClusterFormed()
    {
        // Arrange
        await _clusterManager.InitializeAsync();
        await _clusterManager.StartAsync();
        
        var eventRaised = false;
        _clusterManager.MembershipChanged += (sender, args) =>
        {
            eventRaised = true;
        };

        // Act
        await _clusterManager.FormClusterAsync();

        // Assert
        Assert.True(eventRaised);
    }
}
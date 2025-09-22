using Microsoft.Extensions.Logging;
using GameConsole.AI.Services;
using GameConsole.Engine.Core;
using Xunit;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Tests for the ActorClusterManager implementation.
/// </summary>
public class ActorClusterManagerTests
{
    private readonly ILogger<ActorClusterManager> _logger;
    private readonly ILogger<TestActor> _actorLogger;

    public ActorClusterManagerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ActorClusterManager>();
        _actorLogger = loggerFactory.CreateLogger<TestActor>();
    }

    [Fact]
    public async Task ClusterManager_InitializeStartStop_LifecycleWorksCorrectly()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);

        // Act & Assert
        Assert.False(manager.IsRunning);

        await manager.InitializeAsync();
        await manager.StartAsync();
        Assert.True(manager.IsRunning);

        await manager.StopAsync();
        Assert.False(manager.IsRunning);

        // Cleanup
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_CreateDissolveCluster_ManagesClustersCorrectly()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var config = new ClusterConfiguration
        {
            MaxActorCount = 10,
            FormationStrategy = ClusterFormationStrategy.Manual,
            LoadBalancingEnabled = true
        };

        // Act - Create cluster
        await manager.CreateClusterAsync("test-cluster-1", config);

        // Assert
        Assert.Contains("test-cluster-1", manager.ActiveClusters);
        
        var retrievedConfig = await manager.GetClusterConfigurationAsync("test-cluster-1");
        Assert.NotNull(retrievedConfig);
        Assert.Equal(10, retrievedConfig.MaxActorCount);
        Assert.True(retrievedConfig.LoadBalancingEnabled);

        // Act - Dissolve cluster
        await manager.DissolveClusterAsync("test-cluster-1");

        // Assert
        Assert.DoesNotContain("test-cluster-1", manager.ActiveClusters);

        // Cleanup
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_RegisterUnregisterActor_ManagesActorsCorrectly()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var actor = new TestActor("test-actor-1", _actorLogger);

        // Act - Register actor
        await manager.RegisterActorAsync(actor);

        // Assert - Actor is registered but not in any cluster
        var clusterId = await manager.GetActorClusterAsync("test-actor-1");
        Assert.Null(clusterId);

        // Act - Unregister actor
        await manager.UnregisterActorAsync("test-actor-1");

        // Assert - Actor is no longer tracked
        clusterId = await manager.GetActorClusterAsync("test-actor-1");
        Assert.Null(clusterId);

        // Cleanup
        await actor.DisposeAsync();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_AddRemoveActorFromCluster_ManagesClusterMembership()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var config = new ClusterConfiguration { MaxActorCount = 5 };
        await manager.CreateClusterAsync("membership-cluster", config);

        var actor1 = new TestActor("actor-1", _actorLogger);
        var actor2 = new TestActor("actor-2", _actorLogger);

        await manager.RegisterActorAsync(actor1);
        await manager.RegisterActorAsync(actor2);

        // Act - Add actors to cluster
        await manager.AddActorToClusterAsync("actor-1", "membership-cluster");
        await manager.AddActorToClusterAsync("actor-2", "membership-cluster");

        // Assert
        Assert.Equal("membership-cluster", await manager.GetActorClusterAsync("actor-1"));
        Assert.Equal("membership-cluster", await manager.GetActorClusterAsync("actor-2"));

        var clusterActors = await manager.GetClusterActorsAsync("membership-cluster");
        Assert.Equal(2, clusterActors.Count());
        Assert.Contains("actor-1", clusterActors);
        Assert.Contains("actor-2", clusterActors);

        // Act - Remove actor from cluster
        await manager.RemoveActorFromClusterAsync("actor-1");

        // Assert
        Assert.Null(await manager.GetActorClusterAsync("actor-1"));
        Assert.Equal("membership-cluster", await manager.GetActorClusterAsync("actor-2"));

        clusterActors = await manager.GetClusterActorsAsync("membership-cluster");
        Assert.Single(clusterActors);
        Assert.Contains("actor-2", clusterActors);

        // Cleanup
        await actor1.DisposeAsync();
        await actor2.DisposeAsync();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_RouteMessageToCluster_RoutesMessageToActor()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var config = new ClusterConfiguration();
        await manager.CreateClusterAsync("routing-cluster", config);

        var actor = new TestActor("routing-actor", _actorLogger);
        await actor.InitializeAsync();
        await actor.StartAsync();

        await manager.RegisterActorAsync(actor);
        await manager.AddActorToClusterAsync("routing-actor", "routing-cluster");

        var testMessage = new TestMessage("Hello from cluster manager");

        // Act
        await manager.RouteMessageToClusterAsync("routing-cluster", testMessage);

        // Wait for message processing
        await Task.Delay(100);

        // Assert
        Assert.Single(actor.ProcessedMessages);
        Assert.Equal("Hello from cluster manager", ((TestMessage)actor.ProcessedMessages[0]).Content);

        // Cleanup
        await actor.DisposeAsync();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_BroadcastToCluster_SendsMessageToAllActors()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var config = new ClusterConfiguration();
        await manager.CreateClusterAsync("broadcast-cluster", config);

        var actor1 = new TestActor("broadcast-actor-1", _actorLogger);
        var actor2 = new TestActor("broadcast-actor-2", _actorLogger);
        
        await actor1.InitializeAsync();
        await actor1.StartAsync();
        await actor2.InitializeAsync();
        await actor2.StartAsync();

        await manager.RegisterActorAsync(actor1);
        await manager.RegisterActorAsync(actor2);
        await manager.AddActorToClusterAsync("broadcast-actor-1", "broadcast-cluster");
        await manager.AddActorToClusterAsync("broadcast-actor-2", "broadcast-cluster");

        var broadcastMessage = new TestMessage("Broadcast message");

        // Act
        await manager.BroadcastToClusterAsync("broadcast-cluster", broadcastMessage);

        // Wait for message processing
        await Task.Delay(100);

        // Assert
        Assert.Single(actor1.ProcessedMessages);
        Assert.Single(actor2.ProcessedMessages);
        Assert.Equal("Broadcast message", ((TestMessage)actor1.ProcessedMessages[0]).Content);
        Assert.Equal("Broadcast message", ((TestMessage)actor2.ProcessedMessages[0]).Content);

        // Cleanup
        await actor1.DisposeAsync();
        await actor2.DisposeAsync();
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_GetClusterMetrics_ReturnsValidMetrics()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var config = new ClusterConfiguration();
        await manager.CreateClusterAsync("metrics-cluster", config);

        // Act
        var metrics = await manager.GetClusterMetricsAsync("metrics-cluster");

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.ActiveActorCount);
        Assert.Equal(0, metrics.FailedActorCount);
        Assert.InRange(metrics.HealthScore, 0.0f, 1.0f);
        Assert.True(metrics.LastUpdated <= DateTimeOffset.UtcNow);

        // Cleanup
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_PerformHealthCheck_ReturnsHealthReport()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var config = new ClusterConfiguration();
        await manager.CreateClusterAsync("health-cluster-1", config);
        await manager.CreateClusterAsync("health-cluster-2", config);

        // Act
        var healthReport = await manager.PerformHealthCheckAsync();

        // Assert
        Assert.Equal(2, healthReport.Count);
        Assert.True(healthReport.ContainsKey("health-cluster-1"));
        Assert.True(healthReport.ContainsKey("health-cluster-2"));

        foreach (var metrics in healthReport.Values)
        {
            Assert.InRange(metrics.HealthScore, 0.0f, 1.0f);
        }

        // Cleanup
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_UpdateClusterConfiguration_UpdatesConfig()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var initialConfig = new ClusterConfiguration
        {
            MaxActorCount = 10,
            LoadBalancingEnabled = true
        };

        await manager.CreateClusterAsync("config-cluster", initialConfig);

        // Act
        var newConfig = new ClusterConfiguration
        {
            MaxActorCount = 20,
            LoadBalancingEnabled = false,
            AutoRebalancingEnabled = true
        };

        await manager.UpdateClusterConfigurationAsync("config-cluster", newConfig);

        // Assert
        var retrievedConfig = await manager.GetClusterConfigurationAsync("config-cluster");
        Assert.NotNull(retrievedConfig);
        Assert.Equal(20, retrievedConfig.MaxActorCount);
        Assert.False(retrievedConfig.LoadBalancingEnabled);
        Assert.True(retrievedConfig.AutoRebalancingEnabled);

        // Cleanup
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_ClusterStateChangedEvent_RaisedCorrectly()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var stateChanges = new List<ClusterEventArgs>();
        manager.ClusterStateChanged += (sender, args) => stateChanges.Add(args);

        var config = new ClusterConfiguration();

        // Act
        await manager.CreateClusterAsync("event-cluster", config);
        await manager.DissolveClusterAsync("event-cluster");

        // Assert
        Assert.Equal(2, stateChanges.Count);
        
        Assert.Equal("event-cluster", stateChanges[0].ClusterId);
        Assert.Equal(ClusterState.Active, stateChanges[0].State);

        Assert.Equal("event-cluster", stateChanges[1].ClusterId);
        Assert.Equal(ClusterState.Dissolved, stateChanges[1].State);

        // Cleanup
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task ClusterManager_RebalanceClusters_CompletesWithoutError()
    {
        // Arrange
        var manager = new ActorClusterManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var config = new ClusterConfiguration { LoadBalancingEnabled = true };
        await manager.CreateClusterAsync("rebalance-cluster", config);

        // Act & Assert - Should complete without throwing
        await manager.RebalanceClustersAsync();

        // Cleanup
        await manager.DisposeAsync();
    }
}
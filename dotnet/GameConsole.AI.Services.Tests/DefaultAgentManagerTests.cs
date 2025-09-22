using GameConsole.AI.Services;
using GameConsole.Core.Abstractions;
using GameConsole.Engine.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Tests for the DefaultAgentManager service (Tier 2 proxy service).
/// </summary>
public class DefaultAgentManagerTests
{
    private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    [Fact]
    public async Task AgentManager_CanInitializeStartAndStop()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DefaultAgentManager>();
        var manager = new DefaultAgentManager(logger);

        // Act & Assert
        Assert.False(manager.IsRunning);

        await manager.InitializeAsync();
        Assert.False(manager.IsRunning);

        await manager.StartAsync();
        Assert.True(manager.IsRunning);

        await manager.StopAsync();
        Assert.False(manager.IsRunning);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task AgentManager_RequiresActorSystemForOperations()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DefaultAgentManager>();
        var manager = new DefaultAgentManager(logger);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            manager.CreateClusterAsync("test-cluster"));
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            manager.SpawnAgentAsync("test-cluster", "worker"));
        
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            manager.GetClusterStatusAsync());
    }

    [Fact]
    public async Task AgentManager_CanSetActorSystemAndPerformOperations()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DefaultAgentManager>();
        var manager = new DefaultAgentManager(logger);
        var actorSystem = new BasicActorSystem("test-system", _loggerFactory);

        await manager.SetActorSystemAsync(actorSystem);
        await manager.InitializeAsync();
        await manager.StartAsync();

        // Act & Assert
        // Create cluster
        await manager.CreateClusterAsync("manager-cluster", LoadBalancingStrategy.RoundRobin);
        
        var status = await manager.GetClusterStatusAsync();
        Assert.Contains("manager-cluster", status.Keys);
        Assert.Equal(ClusterHealth.Healthy, status["manager-cluster"]);

        // Spawn agent
        var agentId = await manager.SpawnAgentAsync("manager-cluster", "worker", "test-worker");
        Assert.Equal("test-worker", agentId);

        // Submit task
        var task = "Manager test task";
        var (result, executingAgentId) = await manager.SubmitTaskAsync("manager-cluster", task);
        
        Assert.NotNull(result);
        Assert.Equal("test-worker", executingAgentId);
        Assert.Contains(task, result.ToString());

        // Get metrics
        var systemMetrics = await manager.GetMetricsAsync();
        Assert.Contains("system_name", systemMetrics.Keys);
        Assert.Contains("total_clusters", systemMetrics.Keys);
        Assert.Contains("total_agents", systemMetrics.Keys);

        var clusterMetrics = await manager.GetMetricsAsync("manager-cluster");
        Assert.Contains("cluster_id", clusterMetrics.Keys);
        Assert.Contains("cluster_health", clusterMetrics.Keys);
        Assert.Contains("agent_count", clusterMetrics.Keys);

        // Cleanup
        await manager.TerminateAgentAsync("test-worker");
        await manager.DestroyClusterAsync("manager-cluster");
        await manager.DisposeAsync();
    }

    [Fact]
    public async Task AgentManager_RaisesEventsForOperations()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DefaultAgentManager>();
        var manager = new DefaultAgentManager(logger);
        var actorSystem = new BasicActorSystem("event-system", _loggerFactory);

        var configurationChanges = 0;
        var operationCompletions = new List<string>();
        var topologyChanges = new List<string>();

        manager.ConfigurationChanged += (sender, args) => configurationChanges++;
        manager.OperationCompleted += (sender, args) => operationCompletions.Add(args.AgentId);
        manager.TopologyChanged += (sender, args) => topologyChanges.Add(args.ClusterId);

        await manager.SetActorSystemAsync(actorSystem);
        await manager.StartAsync();

        // Act
        var newConfig = new AgentManagementConfiguration
        {
            MaxAgentsPerCluster = 50,
            DefaultLoadBalancingStrategy = LoadBalancingStrategy.LoadBased
        };
        await manager.UpdateConfigurationAsync(newConfig);

        await manager.CreateClusterAsync("event-cluster");
        var agentId = await manager.SpawnAgentAsync("event-cluster", "event-worker");
        await manager.TerminateAgentAsync(agentId);
        await manager.DestroyClusterAsync("event-cluster");

        // Assert
        Assert.Equal(1, configurationChanges);
        Assert.Contains(agentId, operationCompletions);
        Assert.Contains("event-cluster", topologyChanges);

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task AgentManager_PerformsHealthChecksAndRebalancing()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DefaultAgentManager>();
        var manager = new DefaultAgentManager(logger);
        var actorSystem = new BasicActorSystem("health-system", _loggerFactory);

        await manager.SetActorSystemAsync(actorSystem);
        await manager.StartAsync();

        await manager.CreateClusterAsync("health-cluster-1");
        await manager.CreateClusterAsync("health-cluster-2");
        
        await manager.SpawnAgentAsync("health-cluster-1", "worker");
        await manager.SpawnAgentAsync("health-cluster-2", "worker");

        // Act & Assert - These should not throw exceptions
        await manager.PerformHealthCheckAsync();
        await manager.RebalanceAsync();

        var status = await manager.GetClusterStatusAsync();
        Assert.Equal(2, status.Count);
        Assert.All(status.Values, health => Assert.Equal(ClusterHealth.Healthy, health));

        await manager.DisposeAsync();
    }

    [Fact]
    public async Task AgentManager_HandlesGracefulShutdown()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DefaultAgentManager>();
        var manager = new DefaultAgentManager(logger);
        var actorSystem = new BasicActorSystem("graceful-system", _loggerFactory);

        await manager.SetActorSystemAsync(actorSystem);
        await manager.StartAsync();

        await manager.CreateClusterAsync("graceful-cluster");
        var agentId = await manager.SpawnAgentAsync("graceful-cluster", "graceful-worker");

        // Act - Test graceful operations
        await manager.TerminateAgentAsync(agentId, graceful: true);
        await manager.DestroyClusterAsync("graceful-cluster", graceful: true);

        // Assert
        var status = await manager.GetClusterStatusAsync();
        Assert.Empty(status);

        await manager.DisposeAsync();
    }

    [Fact]
    public void AgentManager_ConfigurationPropertiesWork()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DefaultAgentManager>();
        var manager = new DefaultAgentManager(logger);

        // Act & Assert - Test default configuration
        var config = manager.Configuration;
        Assert.Equal(100, config.MaxAgentsPerCluster);
        Assert.Equal(LoadBalancingStrategy.RoundRobin, config.DefaultLoadBalancingStrategy);
        Assert.Equal(30000, config.AgentOperationTimeoutMs);
        Assert.Equal(10000, config.HealthCheckIntervalMs);
        Assert.True(config.AutoRestartFailedAgents);
    }

    [Fact]
    public async Task AgentManager_HandlesTaskDistributionWithAgentTypeFiltering()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<DefaultAgentManager>();
        var manager = new DefaultAgentManager(logger);
        var actorSystem = new BasicActorSystem("filter-system", _loggerFactory);

        await manager.SetActorSystemAsync(actorSystem);
        await manager.StartAsync();

        await manager.CreateClusterAsync("filter-cluster");
        
        var workerId = await manager.SpawnAgentAsync("filter-cluster", "worker", "worker-001");
        var processorId = await manager.SpawnAgentAsync("filter-cluster", "processor", "proc-001");

        // Act & Assert - Submit task to specific agent type
        var (result, agentId) = await manager.SubmitTaskAsync("filter-cluster", "Worker task", "worker");
        Assert.Equal("worker-001", agentId);
        Assert.NotNull(result);

        var (result2, agentId2) = await manager.SubmitTaskAsync("filter-cluster", "Processor task", "processor");
        Assert.Equal("proc-001", agentId2);
        Assert.NotNull(result2);

        // Submit task without type filter (should go to any available agent)
        var (result3, agentId3) = await manager.SubmitTaskAsync("filter-cluster", "Any task");
        Assert.True(agentId3 == "worker-001" || agentId3 == "proc-001");
        Assert.NotNull(result3);

        await manager.DisposeAsync();
    }
}
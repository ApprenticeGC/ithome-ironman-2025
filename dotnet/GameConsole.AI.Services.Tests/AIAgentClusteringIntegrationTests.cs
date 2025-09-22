using GameConsole.AI.Services;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Integration tests for AI Agent Actor Clustering functionality.
/// Tests the complete flow from agent creation through clustering and task distribution.
/// </summary>
public class AIAgentClusteringIntegrationTests
{
    private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

    [Fact]
    public async Task BasicAgent_CanInitializeStartAndStop()
    {
        // Arrange
        var agentId = "test-agent-001";
        var agentType = "worker";
        var logger = _loggerFactory.CreateLogger<BasicAIAgent>();
        var agent = new BasicAIAgent(agentId, agentType, logger);

        // Act & Assert
        Assert.Equal(AgentState.Idle, agent.State);
        Assert.Equal(agentId, agent.AgentId);
        Assert.Equal(agentType, agent.AgentType);

        await agent.InitializeAsync();
        Assert.Equal(AgentState.Idle, agent.State);

        await agent.StartAsync();
        Assert.Equal(AgentState.Processing, agent.State);

        await agent.StopAsync();
        Assert.Equal(AgentState.Idle, agent.State);

        await agent.DisposeAsync();
        Assert.Equal(AgentState.Disposed, agent.State);
    }

    [Fact]
    public async Task BasicAgent_CanProcessTasks()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<BasicAIAgent>();
        var agent = new BasicAIAgent("test-agent-002", "processor", logger);
        
        await agent.InitializeAsync();
        await agent.StartAsync();

        // Act
        var task = "Test task for processing";
        var result = await agent.ProcessTaskAsync(task);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("test-agent-002", result.ToString());
        Assert.Contains(task, result.ToString());

        await agent.DisposeAsync();
    }

    [Fact]
    public async Task BasicAgent_RaisesStateChangedEvents()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<BasicAIAgent>();
        var agent = new BasicAIAgent("test-agent-003", "evented", logger);
        var stateChanges = new List<AgentState>();

        agent.StateChanged += (sender, args) => stateChanges.Add(args.State);

        // Act
        await agent.InitializeAsync();
        await agent.StartAsync();
        await agent.PauseAsync();
        await agent.ResumeAsync();
        await agent.StopAsync();
        await agent.DisposeAsync();

        // Assert
        Assert.Contains(AgentState.Idle, stateChanges);
        Assert.Contains(AgentState.Processing, stateChanges);
        Assert.Contains(AgentState.Paused, stateChanges);
        Assert.Contains(AgentState.Disposed, stateChanges);
    }

    [Fact]
    public async Task BasicAgentCluster_CanManageAgents()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<BasicAgentCluster>();
        var cluster = new BasicAgentCluster("test-cluster-001", logger);
        
        await cluster.InitializeAsync();
        await cluster.StartAsync();

        var agent1 = new BasicAIAgent("agent-001", "worker", _loggerFactory.CreateLogger<BasicAIAgent>());
        var agent2 = new BasicAIAgent("agent-002", "processor", _loggerFactory.CreateLogger<BasicAIAgent>());

        // Act & Assert
        Assert.Equal(ClusterHealth.Healthy, cluster.Health);
        Assert.Equal(0, await cluster.GetAgentCountAsync());

        // Add agents
        await cluster.AddAgentAsync(agent1);
        await cluster.AddAgentAsync(agent2);

        Assert.Equal(2, await cluster.GetAgentCountAsync());
        
        var agentIds = await cluster.GetAgentIdsAsync();
        Assert.Contains("agent-001", agentIds);
        Assert.Contains("agent-002", agentIds);

        // Test agent by type
        var workers = await cluster.GetAgentsByTypeAsync("worker");
        Assert.Single(workers);
        Assert.Contains("agent-001", workers);

        // Remove agent
        await cluster.RemoveAgentAsync("agent-001");
        Assert.Equal(1, await cluster.GetAgentCountAsync());

        await cluster.DisposeAsync();
    }

    [Fact]
    public async Task BasicAgentCluster_CanDistributeTasks()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<BasicAgentCluster>();
        var cluster = new BasicAgentCluster("task-cluster", logger);
        
        await cluster.InitializeAsync();
        await cluster.StartAsync();

        var agent = new BasicAIAgent("task-agent", "worker", _loggerFactory.CreateLogger<BasicAIAgent>());
        await cluster.AddAgentAsync(agent);

        // Act
        var task = "Test task distribution";
        var (result, agentId) = await cluster.DistributeTaskAsync(task);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("task-agent", agentId);
        Assert.Contains(task, result.ToString());

        await cluster.DisposeAsync();
    }

    [Fact]
    public async Task BasicAgentCluster_RaisesClusterEvents()
    {
        // Arrange
        var logger = _loggerFactory.CreateLogger<BasicAgentCluster>();
        var cluster = new BasicAgentCluster("event-cluster", logger);
        
        var joinedAgents = new List<string>();
        var leftAgents = new List<string>();
        var healthChanges = new List<ClusterHealth>();

        cluster.AgentJoined += (sender, args) => joinedAgents.Add(args.AgentId);
        cluster.AgentLeft += (sender, args) => leftAgents.Add(args.AgentId);
        cluster.HealthChanged += (sender, args) => healthChanges.Add(args.Health);

        await cluster.InitializeAsync();
        await cluster.StartAsync();

        // Act
        var agent = new BasicAIAgent("event-agent", "worker", _loggerFactory.CreateLogger<BasicAIAgent>());
        await cluster.AddAgentAsync(agent);
        await cluster.RemoveAgentAsync("event-agent");

        // Assert
        Assert.Contains("event-agent", joinedAgents);
        Assert.Contains("event-agent", leftAgents);

        await cluster.DisposeAsync();
    }

    [Fact]
    public async Task BasicActorSystem_CanManageClustersAndAgents()
    {
        // Arrange
        var actorSystem = new BasicActorSystem("test-system", _loggerFactory);
        
        await actorSystem.InitializeAsync();
        await actorSystem.StartAsync();

        // Act & Assert - Create cluster
        var cluster = await actorSystem.CreateClusterAsync("test-cluster");
        Assert.NotNull(cluster);
        Assert.Equal("test-cluster", cluster.ClusterId);

        var clusterIds = await actorSystem.GetClusterIdsAsync();
        Assert.Contains("test-cluster", clusterIds);

        // Create and add agents
        var agent1 = await actorSystem.CreateAgentAsync("worker", "worker-001");
        var agent2 = await actorSystem.CreateAgentAsync("processor", "proc-001");

        Assert.Equal("worker-001", agent1.AgentId);
        Assert.Equal("worker", agent1.AgentType);

        // Add agents to cluster
        await actorSystem.AddAgentToClusterAsync("test-cluster", agent1);
        await actorSystem.AddAgentToClusterAsync("test-cluster", agent2);

        // Verify agents are in the system
        var allAgentIds = await actorSystem.GetAllAgentIdsAsync();
        Assert.Contains("worker-001", allAgentIds);
        Assert.Contains("proc-001", allAgentIds);

        // Find agents by type
        var workers = await actorSystem.FindAgentsByTypeAsync("worker");
        Assert.Contains("worker-001", workers);

        // Get specific agent
        var retrievedAgent = await actorSystem.GetAgentAsync("worker-001");
        Assert.NotNull(retrievedAgent);
        Assert.Equal("worker-001", retrievedAgent.AgentId);

        // Cleanup
        await actorSystem.DestroyClusterAsync("test-cluster");
        await actorSystem.DisposeAsync();
    }

    [Fact]
    public async Task BasicActorSystem_ImplementsCapabilityProvider()
    {
        // Arrange
        var actorSystem = new BasicActorSystem("capability-system", _loggerFactory);

        // Act & Assert
        var capabilities = await actorSystem.GetCapabilitiesAsync();
        Assert.DoesNotContain(typeof(IAIAgent), capabilities); // Actor system creates agents but doesn't provide them as capabilities
        Assert.DoesNotContain(typeof(IAgentCluster), capabilities); // Actor system creates clusters but doesn't provide them as capabilities  
        Assert.Contains(typeof(IActorSystem), capabilities);

        Assert.True(await actorSystem.HasCapabilityAsync<IActorSystem>());
        Assert.False(await actorSystem.HasCapabilityAsync<IAIAgent>());

        var actorSystemCapability = await actorSystem.GetCapabilityAsync<IActorSystem>();
        Assert.Same(actorSystem, actorSystemCapability);

        await actorSystem.DisposeAsync();
    }

    [Fact]
    public async Task ActorSystem_CanChangeMode()
    {
        // Arrange
        var actorSystem = new BasicActorSystem("mode-system", _loggerFactory);
        var modeChanges = new List<ActorSystemMode>();

        actorSystem.ModeChanged += (sender, args) => modeChanges.Add(args.Mode);

        // Act & Assert
        Assert.Equal(ActorSystemMode.SingleNode, actorSystem.Mode);

        await actorSystem.SetModeAsync(ActorSystemMode.Clustered);
        Assert.Equal(ActorSystemMode.Clustered, actorSystem.Mode);
        Assert.Contains(ActorSystemMode.Clustered, modeChanges);

        await actorSystem.SetModeAsync(ActorSystemMode.Hybrid);
        Assert.Equal(ActorSystemMode.Hybrid, actorSystem.Mode);
        Assert.Contains(ActorSystemMode.Hybrid, modeChanges);

        await actorSystem.DisposeAsync();
    }

    [Fact]
    public async Task CompleteWorkflow_CreateSystemAddAgentsDistributeTasks()
    {
        // Arrange
        var actorSystem = new BasicActorSystem("workflow-system", _loggerFactory);
        await actorSystem.InitializeAsync();
        await actorSystem.StartAsync();

        // Act - Create cluster and agents
        var cluster = await actorSystem.CreateClusterAsync("workflow-cluster");
        
        var agent1 = await actorSystem.CreateAgentAsync("calculator", "calc-001");
        var agent2 = await actorSystem.CreateAgentAsync("calculator", "calc-002");
        
        await actorSystem.AddAgentToClusterAsync("workflow-cluster", agent1);
        await actorSystem.AddAgentToClusterAsync("workflow-cluster", agent2);

        // Distribute multiple tasks
        var task1Result = await cluster.DistributeTaskAsync("2 + 2");
        var task2Result = await cluster.DistributeTaskAsync("5 * 3");
        var task3Result = await cluster.DistributeTaskAsync("10 - 4");

        // Assert
        Assert.NotNull(task1Result.result);
        Assert.NotNull(task2Result.result);
        Assert.NotNull(task3Result.result);

        // Verify tasks were distributed (could be to either agent)
        Assert.True(task1Result.agentId == "calc-001" || task1Result.agentId == "calc-002");
        Assert.True(task2Result.agentId == "calc-001" || task2Result.agentId == "calc-002");
        Assert.True(task3Result.agentId == "calc-001" || task3Result.agentId == "calc-002");

        // Verify cluster health
        Assert.Equal(ClusterHealth.Healthy, cluster.Health);
        Assert.Equal(2, await cluster.GetAgentCountAsync());

        // Cleanup
        await actorSystem.DisposeAsync();
    }
}
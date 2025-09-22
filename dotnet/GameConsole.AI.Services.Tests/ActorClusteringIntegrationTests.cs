using Microsoft.Extensions.Logging;
using GameConsole.AI.Services;
using GameConsole.Engine.Core;
using Xunit;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Example implementation of an AI agent for demonstrating the clustering system.
/// This would typically be in a separate example or demo project.
/// </summary>
public class GameAIAgent : BaseAIAgent
{
    public GameAIAgent(string actorId, ILogger logger, AIAgentCapability capability = AIAgentCapability.Reactive) 
        : base(actorId, logger, capability)
    {
    }

    protected override Task OnExecuteDecisionAsync(AIDecision decision, CancellationToken cancellationToken = default)
    {
        // Simulate AI agent executing game actions
        _logger.LogInformation("Game AI Agent {ActorId} executing action: {Action}", ActorId, decision.Action);
        return Task.CompletedTask;
    }

    protected override async Task OnCollaborateAsync(string task, ICollection<string> participantIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Game AI Agent {ActorId} collaborating on task '{Task}' with agents: {Participants}", 
            ActorId, task, string.Join(", ", participantIds));
        
        // Simulate collaboration
        await Task.Delay(50, cancellationToken);
    }

    protected override Task OnShareKnowledgeAsync(IDictionary<string, object> knowledge, ICollection<string>? targetAgentIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Game AI Agent {ActorId} sharing knowledge: {Knowledge}", 
            ActorId, string.Join(", ", knowledge.Keys));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Integration test that demonstrates the complete AI Agent Actor Clustering system working together.
/// This shows how the system would be used in a real game scenario.
/// </summary>
public class ActorClusteringIntegrationTests
{
    private readonly ILogger<ActorClusterManager> _managerLogger;
    private readonly ILogger<GameAIAgent> _agentLogger;

    public ActorClusteringIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _managerLogger = loggerFactory.CreateLogger<ActorClusterManager>();
        _agentLogger = loggerFactory.CreateLogger<GameAIAgent>();
    }

    [Fact]
    public async Task CompleteActorClusteringSystem_GameScenario_WorksEndToEnd()
    {
        // Arrange - Create cluster manager
        var clusterManager = new ActorClusterManager(_managerLogger);
        await clusterManager.InitializeAsync();
        await clusterManager.StartAsync();

        // Create cluster configurations for different game scenarios
        var combatClusterConfig = new ClusterConfiguration
        {
            MaxActorCount = 5,
            FormationStrategy = ClusterFormationStrategy.Functional,
            LoadBalancingEnabled = true,
            AutoRebalancingEnabled = false
        };

        var explorationClusterConfig = new ClusterConfiguration
        {
            MaxActorCount = 3,
            FormationStrategy = ClusterFormationStrategy.Geographic,
            LoadBalancingEnabled = true,
            AutoRebalancingEnabled = true
        };

        // Create clusters
        await clusterManager.CreateClusterAsync("combat-cluster", combatClusterConfig);
        await clusterManager.CreateClusterAsync("exploration-cluster", explorationClusterConfig);

        // Create AI agents with different capabilities
        var combatAgent1 = new GameAIAgent("combat-agent-1", _agentLogger, AIAgentCapability.Proactive);
        var combatAgent2 = new GameAIAgent("combat-agent-2", _agentLogger, AIAgentCapability.Social);
        var explorationAgent1 = new GameAIAgent("exploration-agent-1", _agentLogger, AIAgentCapability.Autonomous);

        // Initialize and start agents
        var agents = new[] { combatAgent1, combatAgent2, explorationAgent1 };
        foreach (var agent in agents)
        {
            await agent.InitializeAsync();
            await agent.StartAsync();
            await clusterManager.RegisterActorAsync(agent);
        }

        // Add goals to agents
        await combatAgent1.AddGoalAsync("engage-enemy", 10);
        await combatAgent1.AddGoalAsync("protect-allies", 15);
        await combatAgent2.AddGoalAsync("support-combat", 8);
        await explorationAgent1.AddGoalAsync("explore-area", 12);
        await explorationAgent1.AddGoalAsync("gather-resources", 7);

        // Assign agents to clusters
        await clusterManager.AddActorToClusterAsync("combat-agent-1", "combat-cluster");
        await clusterManager.AddActorToClusterAsync("combat-agent-2", "combat-cluster");
        await clusterManager.AddActorToClusterAsync("exploration-agent-1", "exploration-cluster");

        // Simulate game scenario - Combat situation
        var combatContext = new AIContext();
        combatContext.EnvironmentState["enemy-count"] = 2;
        combatContext.EnvironmentState["ally-health"] = 0.8f;
        combatContext.AvailableActions.Add("attack");
        combatContext.AvailableActions.Add("defend");
        combatContext.AvailableActions.Add("heal-ally");

        // Combat agents make decisions
        var combatDecision1 = await combatAgent1.MakeDecisionAsync(combatContext);
        var combatDecision2 = await combatAgent2.MakeDecisionAsync(combatContext);

        await combatAgent1.ExecuteDecisionAsync(combatDecision1);
        await combatAgent2.ExecuteDecisionAsync(combatDecision2);

        // Provide feedback for learning
        await combatAgent1.ProvideFeedbackAsync(combatDecision1, "success", 1.0f);
        await combatAgent2.ProvideFeedbackAsync(combatDecision2, "partial-success", 0.7f);

        // Simulate collaboration between combat agents
        await combatAgent1.CollaborateAsync("coordinate-attack", new[] { "combat-agent-2" });

        // Exploration scenario
        var explorationContext = new AIContext();
        explorationContext.EnvironmentState["unexplored-areas"] = 3;
        explorationContext.EnvironmentState["resources-nearby"] = true;
        explorationContext.AvailableActions.Add("explore-north");
        explorationContext.AvailableActions.Add("gather-resources");
        explorationContext.AvailableActions.Add("return-to-base");

        var explorationDecision = await explorationAgent1.MakeDecisionAsync(explorationContext);
        await explorationAgent1.ExecuteDecisionAsync(explorationDecision);

        // Share knowledge between agents
        var sharedKnowledge = new Dictionary<string, object>
        {
            ["enemy-positions"] = new[] { "north", "east" },
            ["resource-locations"] = new[] { "west-cave", "south-forest" },
            ["safe-paths"] = new[] { "central-route" }
        };
        await combatAgent1.ShareKnowledgeAsync(sharedKnowledge);

        // Test cluster management operations
        var combatActors = await clusterManager.GetClusterActorsAsync("combat-cluster");
        var explorationActors = await clusterManager.GetClusterActorsAsync("exploration-cluster");
        
        Assert.Equal(2, combatActors.Count());
        Assert.Single(explorationActors);

        // Test message routing and broadcasting
        var broadcastMessage = new TestMessage("Alert: Enemy reinforcements incoming!");
        await clusterManager.BroadcastToClusterAsync("combat-cluster", broadcastMessage);

        // Wait for message processing
        await Task.Delay(200);

        // Test cluster metrics and health
        var combatMetrics = await clusterManager.GetClusterMetricsAsync("combat-cluster");
        var healthReport = await clusterManager.PerformHealthCheckAsync();

        Assert.Equal(2, combatMetrics.ActiveActorCount);
        Assert.Equal(2, healthReport.Count);
        Assert.True(healthReport.All(kvp => kvp.Value.HealthScore >= 0.0f && kvp.Value.HealthScore <= 1.0f));

        // Test rebalancing
        await clusterManager.RebalanceClustersAsync();

        // Get final metrics from agents
        var agent1Metrics = await combatAgent1.GetMetricsAsync();
        var agent2Metrics = await combatAgent2.GetMetricsAsync();
        var agent3Metrics = await explorationAgent1.GetMetricsAsync();

        // Verify agent metrics include AI-specific data
        Assert.Contains("GoalCount", agent1Metrics.Keys);
        Assert.Contains("DecisionStrategy", agent1Metrics.Keys);
        Assert.Contains("Capability", agent1Metrics.Keys);
        Assert.Contains("KnowledgeBaseSize", agent1Metrics.Keys);

        // Verify agents have gained knowledge through execution
        var agent1Knowledge = await combatAgent1.GetKnowledgeBaseAsync();
        Assert.NotEmpty(agent1Knowledge);

        // Cleanup - Stop all agents and cluster manager
        foreach (var agent in agents)
        {
            await agent.StopAsync();
            await agent.DisposeAsync();
        }

        await clusterManager.StopAsync();
        await clusterManager.DisposeAsync();

        // Assert final state
        Assert.Empty(clusterManager.ActiveClusters);
    }
}
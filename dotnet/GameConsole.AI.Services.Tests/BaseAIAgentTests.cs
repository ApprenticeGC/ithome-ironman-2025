using Microsoft.Extensions.Logging;
using GameConsole.AI.Services;
using GameConsole.Engine.Core;
using Xunit;

namespace GameConsole.AI.Services.Tests;

/// <summary>
/// Test implementation of BaseAIAgent for testing purposes.
/// </summary>
public class TestAIAgent : BaseAIAgent
{
    public TestAIAgent(string actorId, ILogger logger, AIAgentCapability capability = AIAgentCapability.Reactive) 
        : base(actorId, logger, capability)
    {
    }

    public List<AIDecision> ExecutedDecisions { get; } = new List<AIDecision>();
    public List<string> ReceivedFeedback { get; } = new List<string>();

    protected override Task OnExecuteDecisionAsync(AIDecision decision, CancellationToken cancellationToken = default)
    {
        ExecutedDecisions.Add(decision);
        return Task.CompletedTask;
    }

    protected override Task OnProvideFeedbackAsync(AIDecision decision, string outcome, float? reward, CancellationToken cancellationToken = default)
    {
        ReceivedFeedback.Add($"{decision.Action}:{outcome}:{reward}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Tests for the BaseAIAgent implementation.
/// </summary>
public class BaseAIAgentTests
{
    private readonly ILogger<TestAIAgent> _logger;

    public BaseAIAgentTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<TestAIAgent>();
    }

    [Fact]
    public async Task AIAgent_InitialState_HasCorrectDefaults()
    {
        // Arrange & Act
        var agent = new TestAIAgent("ai-agent-1", _logger, AIAgentCapability.Proactive);

        // Assert
        Assert.Equal("ai-agent-1", agent.ActorId);
        Assert.Equal(AIDecisionStrategy.RuleBased, agent.DecisionStrategy);
        Assert.Equal(AIAgentCapability.Proactive, agent.Capability);
        Assert.Empty(agent.Goals);

        // Cleanup
        await agent.DisposeAsync();
    }

    [Fact]
    public async Task AIAgent_AddRemoveGoals_ManagesGoalsCorrectly()
    {
        // Arrange
        var agent = new TestAIAgent("ai-agent-2", _logger);

        // Act - Add goals
        await agent.AddGoalAsync("collect-resources", 10);
        await agent.AddGoalAsync("defend-base", 20);
        await agent.AddGoalAsync("explore-map", 5);

        // Assert - Goals are ordered by priority
        Assert.Equal(3, agent.Goals.Count);
        var goalsArray = agent.Goals.ToArray();
        Assert.Equal("defend-base", goalsArray[0]); // Priority 20
        Assert.Equal("collect-resources", goalsArray[1]); // Priority 10
        Assert.Equal("explore-map", goalsArray[2]); // Priority 5

        // Act - Remove goal
        await agent.RemoveGoalAsync("collect-resources");

        // Assert
        Assert.Equal(2, agent.Goals.Count);
        Assert.DoesNotContain("collect-resources", agent.Goals);

        // Cleanup
        await agent.DisposeAsync();
    }

    [Fact]
    public async Task AIAgent_SetDecisionStrategy_UpdatesStrategy()
    {
        // Arrange
        var agent = new TestAIAgent("ai-agent-3", _logger);

        // Act
        await agent.SetDecisionStrategyAsync(AIDecisionStrategy.MachineLearning);

        // Assert
        Assert.Equal(AIDecisionStrategy.MachineLearning, agent.DecisionStrategy);

        // Cleanup
        await agent.DisposeAsync();
    }

    [Fact]
    public async Task AIAgent_MakeDecision_ReturnsValidDecision()
    {
        // Arrange
        var agent = new TestAIAgent("ai-agent-4", _logger);
        var context = new AIContext();
        context.AvailableActions.Add("move-north");
        context.AvailableActions.Add("attack-enemy");
        context.EnvironmentState["enemy-nearby"] = true;

        // Act
        var decision = await agent.MakeDecisionAsync(context);

        // Assert
        Assert.NotNull(decision);
        Assert.NotEmpty(decision.Action);
        Assert.InRange(decision.Confidence, 0.0f, 1.0f);

        // Cleanup
        await agent.DisposeAsync();
    }

    [Fact]
    public async Task AIAgent_ExecuteDecision_CallsOnExecuteDecision()
    {
        // Arrange
        var agent = new TestAIAgent("ai-agent-5", _logger);
        var decision = new AIDecision("test-action", 0.8f);

        // Act
        await agent.ExecuteDecisionAsync(decision);

        // Assert
        Assert.Single(agent.ExecutedDecisions);
        Assert.Equal("test-action", agent.ExecutedDecisions[0].Action);

        // Cleanup
        await agent.DisposeAsync();
    }

    [Fact]
    public async Task AIAgent_ProvideFeedback_CallsOnProvideFeedback()
    {
        // Arrange
        var agent = new TestAIAgent("ai-agent-6", _logger);
        var decision = new AIDecision("test-action", 0.7f);

        // Act
        await agent.ProvideFeedbackAsync(decision, "success", 1.0f);

        // Assert
        Assert.Single(agent.ReceivedFeedback);
        Assert.Equal("test-action:success:1", agent.ReceivedFeedback[0]);

        // Cleanup
        await agent.DisposeAsync();
    }

    [Fact]
    public async Task AIAgent_DecisionMadeEvent_RaisedCorrectly()
    {
        // Arrange
        var agent = new TestAIAgent("ai-agent-7", _logger);
        var decisionEvents = new List<AIDecisionEventArgs>();
        
        agent.DecisionMade += (sender, args) => decisionEvents.Add(args);
        
        var context = new AIContext();
        context.AvailableActions.Add("wait");

        // Act
        await agent.MakeDecisionAsync(context);

        // Assert
        Assert.Single(decisionEvents);
        Assert.Equal("ai-agent-7", decisionEvents[0].AgentId);
        Assert.Same(context, decisionEvents[0].Context);
        Assert.NotNull(decisionEvents[0].Decision);

        // Cleanup
        await agent.DisposeAsync();
    }

    [Fact]
    public async Task AIAgent_GetMetrics_IncludesAISpecificMetrics()
    {
        // Arrange
        var agent = new TestAIAgent("ai-agent-8", _logger, AIAgentCapability.Learning);
        await agent.AddGoalAsync("test-goal", 5);

        // Act
        var metrics = await agent.GetMetricsAsync();

        // Assert
        Assert.Equal("ai-agent-8", metrics["ActorId"]);
        Assert.Equal("Learning", metrics["Capability"]);
        Assert.Equal("RuleBased", metrics["DecisionStrategy"]);
        Assert.Equal(1, metrics["GoalCount"]);
        Assert.Equal(0, metrics["KnowledgeBaseSize"]);
        
        var activeGoals = (List<string>)metrics["ActiveGoals"];
        Assert.Single(activeGoals);
        Assert.Equal("test-goal", activeGoals[0]);

        // Cleanup
        await agent.DisposeAsync();
    }

    [Fact]
    public async Task AIAgent_GetKnowledgeBase_ReturnsKnowledgeData()
    {
        // Arrange
        var agent = new TestAIAgent("ai-agent-9", _logger);
        await agent.InitializeAsync();
        await agent.StartAsync();

        // Execute a decision to populate knowledge base
        var decision = new AIDecision("learn", 0.9f);
        await agent.ExecuteDecisionAsync(decision);

        // Act
        var knowledge = await agent.GetKnowledgeBaseAsync();

        // Assert
        Assert.NotEmpty(knowledge);
        
        // Should have execution knowledge
        var executionKeys = knowledge.Keys.Where(k => k.StartsWith("execution_")).ToList();
        Assert.NotEmpty(executionKeys);

        // Cleanup
        await agent.DisposeAsync();
    }

    [Theory]
    [InlineData(AIDecisionStrategy.RuleBased)]
    [InlineData(AIDecisionStrategy.BehaviorTree)]
    [InlineData(AIDecisionStrategy.StateMachine)]
    [InlineData(AIDecisionStrategy.MachineLearning)]
    [InlineData(AIDecisionStrategy.Hybrid)]
    public async Task AIAgent_MakeDecisionWithDifferentStrategies_ReturnsValidDecisions(AIDecisionStrategy strategy)
    {
        // Arrange
        var agent = new TestAIAgent("ai-agent-strategy-test", _logger);
        await agent.SetDecisionStrategyAsync(strategy);
        
        var context = new AIContext();
        context.AvailableActions.Add("test-action");

        // Act
        var decision = await agent.MakeDecisionAsync(context);

        // Assert
        Assert.NotNull(decision);
        Assert.NotEmpty(decision.Action);
        Assert.InRange(decision.Confidence, 0.0f, 1.0f);
        Assert.NotNull(decision.Reasoning);

        // Cleanup
        await agent.DisposeAsync();
    }
}
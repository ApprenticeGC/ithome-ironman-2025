using GameConsole.Engine.Core;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Services;

/// <summary>
/// Base implementation for AI agents that extends actor capabilities with artificial intelligence.
/// Provides decision-making, learning, and collaboration capabilities for clustered AI systems.
/// </summary>
public abstract class BaseAIAgent : BaseActor, IAIAgent
{
    private readonly Dictionary<string, int> _goals;
    private readonly Dictionary<string, object> _knowledgeBase;
    private AIDecisionStrategy _decisionStrategy;
    private AIContext _currentContext;

    /// <summary>
    /// Initializes a new instance of the BaseAIAgent class.
    /// </summary>
    /// <param name="actorId">Unique identifier for this AI agent.</param>
    /// <param name="logger">Logger instance for this AI agent.</param>
    /// <param name="capability">The capability level of this AI agent.</param>
    protected BaseAIAgent(string actorId, ILogger logger, AIAgentCapability capability = AIAgentCapability.Reactive)
        : base(actorId, logger)
    {
        _goals = new Dictionary<string, int>();
        _knowledgeBase = new Dictionary<string, object>();
        _decisionStrategy = AIDecisionStrategy.RuleBased;
        _currentContext = new AIContext();
        Capability = capability;
    }

    #region IAIAgent Implementation

    public event EventHandler<AIDecisionEventArgs>? DecisionMade;

    public AIDecisionStrategy DecisionStrategy => _decisionStrategy;

    public AIAgentCapability Capability { get; }

    public IReadOnlyCollection<string> Goals
    {
        get
        {
            lock (_goals)
            {
                return _goals.Keys.OrderByDescending(g => _goals[g]).ToList().AsReadOnly();
            }
        }
    }

    public virtual Task SetDecisionStrategyAsync(AIDecisionStrategy strategy, CancellationToken cancellationToken = default)
    {
        _decisionStrategy = strategy;
        _logger.LogInformation("AI agent {ActorId} changed decision strategy to {Strategy}", ActorId, strategy);
        return Task.CompletedTask;
    }

    public virtual Task AddGoalAsync(string goal, int priority = 0, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(goal)) throw new ArgumentException("Goal cannot be null or empty", nameof(goal));

        lock (_goals)
        {
            _goals[goal] = priority;
        }

        _logger.LogInformation("AI agent {ActorId} added goal '{Goal}' with priority {Priority}", ActorId, goal, priority);
        return Task.CompletedTask;
    }

    public virtual Task RemoveGoalAsync(string goal, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(goal)) throw new ArgumentException("Goal cannot be null or empty", nameof(goal));

        lock (_goals)
        {
            if (_goals.Remove(goal))
            {
                _logger.LogInformation("AI agent {ActorId} removed goal '{Goal}'", ActorId, goal);
            }
        }

        return Task.CompletedTask;
    }

    public virtual Task UpdateContextAsync(AIContext context, CancellationToken cancellationToken = default)
    {
        _currentContext = context ?? throw new ArgumentNullException(nameof(context));
        _logger.LogDebug("AI agent {ActorId} updated context with {StateCount} environment states and {ActionCount} actions", 
            ActorId, context.EnvironmentState.Count, context.AvailableActions.Count);
        return Task.CompletedTask;
    }

    public virtual async Task<AIDecision> MakeDecisionAsync(AIContext context, CancellationToken cancellationToken = default)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));

        await UpdateContextAsync(context, cancellationToken);

        var decision = _decisionStrategy switch
        {
            AIDecisionStrategy.RuleBased => await MakeRuleBasedDecisionAsync(context, cancellationToken),
            AIDecisionStrategy.BehaviorTree => await MakeBehaviorTreeDecisionAsync(context, cancellationToken),
            AIDecisionStrategy.StateMachine => await MakeStateMachineDecisionAsync(context, cancellationToken),
            AIDecisionStrategy.MachineLearning => await MakeMachineLearningDecisionAsync(context, cancellationToken),
            AIDecisionStrategy.Hybrid => await MakeHybridDecisionAsync(context, cancellationToken),
            _ => await MakeDefaultDecisionAsync(context, cancellationToken)
        };

        DecisionMade?.Invoke(this, new AIDecisionEventArgs(ActorId, context, decision));
        _logger.LogInformation("AI agent {ActorId} made decision: {Action} (confidence: {Confidence:P})", 
            ActorId, decision.Action, decision.Confidence);

        return decision;
    }

    public virtual async Task ExecuteDecisionAsync(AIDecision decision, CancellationToken cancellationToken = default)
    {
        if (decision == null) throw new ArgumentNullException(nameof(decision));

        _logger.LogInformation("AI agent {ActorId} executing decision: {Action}", ActorId, decision.Action);

        try
        {
            await OnExecuteDecisionAsync(decision, cancellationToken);
            
            // Update knowledge base with execution experience
            var executionKey = $"execution_{decision.Action}_{DateTimeOffset.UtcNow:yyyyMMdd}";
            lock (_knowledgeBase)
            {
                _knowledgeBase[executionKey] = new
                {
                    Action = decision.Action,
                    Confidence = decision.Confidence,
                    Parameters = decision.Parameters,
                    Timestamp = DateTimeOffset.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI agent {ActorId} failed to execute decision: {Action}", ActorId, decision.Action);
            throw;
        }
    }

    public virtual Task ProvideFeedbackAsync(AIDecision decision, string outcome, float? reward = null, CancellationToken cancellationToken = default)
    {
        if (decision == null) throw new ArgumentNullException(nameof(decision));
        if (string.IsNullOrWhiteSpace(outcome)) throw new ArgumentException("Outcome cannot be null or empty", nameof(outcome));

        _logger.LogInformation("AI agent {ActorId} received feedback for decision {Action}: {Outcome} (reward: {Reward})", 
            ActorId, decision.Action, outcome, reward);

        // Update knowledge base with feedback
        var feedbackKey = $"feedback_{decision.Action}_{DateTimeOffset.UtcNow.Ticks}";
        lock (_knowledgeBase)
        {
            _knowledgeBase[feedbackKey] = new
            {
                Decision = decision,
                Outcome = outcome,
                Reward = reward,
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        return OnProvideFeedbackAsync(decision, outcome, reward, cancellationToken);
    }

    public virtual async Task CollaborateAsync(string task, ICollection<string> participantIds, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(task)) throw new ArgumentException("Task cannot be null or empty", nameof(task));
        if (participantIds == null) throw new ArgumentNullException(nameof(participantIds));

        _logger.LogInformation("AI agent {ActorId} starting collaboration on task '{Task}' with {ParticipantCount} participants", 
            ActorId, task, participantIds.Count);

        await OnCollaborateAsync(task, participantIds, cancellationToken);
    }

    public virtual Task ShareKnowledgeAsync(IDictionary<string, object> knowledge, ICollection<string>? targetAgentIds = null, CancellationToken cancellationToken = default)
    {
        if (knowledge == null) throw new ArgumentNullException(nameof(knowledge));

        var targets = targetAgentIds != null ? string.Join(", ", targetAgentIds) : "all cluster members";
        _logger.LogInformation("AI agent {ActorId} sharing {KnowledgeCount} knowledge items with {Targets}", 
            ActorId, knowledge.Count, targets);

        return OnShareKnowledgeAsync(knowledge, targetAgentIds, cancellationToken);
    }

    public virtual Task<IDictionary<string, object>> GetKnowledgeBaseAsync(CancellationToken cancellationToken = default)
    {
        lock (_knowledgeBase)
        {
            return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>(_knowledgeBase));
        }
    }

    #endregion

    #region Protected Virtual Methods for Decision Strategies

    /// <summary>
    /// Makes a rule-based decision using predefined rules and logic.
    /// </summary>
    /// <param name="context">The current context for decision making.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the AI decision.</returns>
    protected virtual Task<AIDecision> MakeRuleBasedDecisionAsync(AIContext context, CancellationToken cancellationToken = default)
    {
        // Default rule-based implementation: choose first available action with highest priority goal
        var action = context.AvailableActions.FirstOrDefault() ?? "wait";
        var confidence = context.AvailableActions.Any() ? 0.8f : 0.3f;
        
        return Task.FromResult(new AIDecision(action, confidence, reasoning: "Rule-based decision using first available action"));
    }

    /// <summary>
    /// Makes a decision using a behavior tree approach.
    /// </summary>
    /// <param name="context">The current context for decision making.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the AI decision.</returns>
    protected virtual Task<AIDecision> MakeBehaviorTreeDecisionAsync(AIContext context, CancellationToken cancellationToken = default)
    {
        // Default behavior tree implementation
        var action = "evaluate";
        var confidence = 0.7f;
        
        return Task.FromResult(new AIDecision(action, confidence, reasoning: "Behavior tree decision"));
    }

    /// <summary>
    /// Makes a decision using a state machine approach.
    /// </summary>
    /// <param name="context">The current context for decision making.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the AI decision.</returns>
    protected virtual Task<AIDecision> MakeStateMachineDecisionAsync(AIContext context, CancellationToken cancellationToken = default)
    {
        // Default state machine implementation
        var action = "transition";
        var confidence = 0.75f;
        
        return Task.FromResult(new AIDecision(action, confidence, reasoning: "State machine decision"));
    }

    /// <summary>
    /// Makes a decision using machine learning algorithms.
    /// </summary>
    /// <param name="context">The current context for decision making.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the AI decision.</returns>
    protected virtual Task<AIDecision> MakeMachineLearningDecisionAsync(AIContext context, CancellationToken cancellationToken = default)
    {
        // Default ML implementation (placeholder)
        var action = "predict";
        var confidence = 0.6f;
        
        return Task.FromResult(new AIDecision(action, confidence, reasoning: "Machine learning decision"));
    }

    /// <summary>
    /// Makes a decision using a hybrid approach combining multiple strategies.
    /// </summary>
    /// <param name="context">The current context for decision making.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the AI decision.</returns>
    protected virtual Task<AIDecision> MakeHybridDecisionAsync(AIContext context, CancellationToken cancellationToken = default)
    {
        // Default hybrid implementation
        var action = "optimize";
        var confidence = 0.85f;
        
        return Task.FromResult(new AIDecision(action, confidence, reasoning: "Hybrid decision combining multiple strategies"));
    }

    /// <summary>
    /// Makes a default decision when no specific strategy is matched.
    /// </summary>
    /// <param name="context">The current context for decision making.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the AI decision.</returns>
    protected virtual Task<AIDecision> MakeDefaultDecisionAsync(AIContext context, CancellationToken cancellationToken = default)
    {
        var action = "default";
        var confidence = 0.5f;
        
        return Task.FromResult(new AIDecision(action, confidence, reasoning: "Default decision"));
    }

    #endregion

    #region Protected Virtual Methods for Extension Points

    /// <summary>
    /// Called when a decision needs to be executed. Override to provide custom execution logic.
    /// </summary>
    /// <param name="decision">The decision to execute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async execution operation.</returns>
    protected virtual Task OnExecuteDecisionAsync(AIDecision decision, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when feedback is provided about a decision. Override to implement learning logic.
    /// </summary>
    /// <param name="decision">The decision that was executed.</param>
    /// <param name="outcome">The outcome of the decision.</param>
    /// <param name="reward">Optional reward value for reinforcement learning.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async feedback processing operation.</returns>
    protected virtual Task OnProvideFeedbackAsync(AIDecision decision, string outcome, float? reward, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when collaboration is requested. Override to provide custom collaboration logic.
    /// </summary>
    /// <param name="task">The collaborative task to perform.</param>
    /// <param name="participantIds">IDs of other agents to collaborate with.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async collaboration operation.</returns>
    protected virtual Task OnCollaborateAsync(string task, ICollection<string> participantIds, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when knowledge sharing is requested. Override to provide custom knowledge sharing logic.
    /// </summary>
    /// <param name="knowledge">The knowledge to share.</param>
    /// <param name="targetAgentIds">IDs of agents to share knowledge with, or null for all cluster members.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async knowledge sharing operation.</returns>
    protected virtual Task OnShareKnowledgeAsync(IDictionary<string, object> knowledge, ICollection<string>? targetAgentIds, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Override Base Actor Methods

    public override async Task<IDictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var baseMetrics = await base.GetMetricsAsync(cancellationToken);
        
        lock (_goals)
        {
            baseMetrics["GoalCount"] = _goals.Count;
            baseMetrics["ActiveGoals"] = Goals.ToList();
        }
        
        lock (_knowledgeBase)
        {
            baseMetrics["KnowledgeBaseSize"] = _knowledgeBase.Count;
        }
        
        baseMetrics["DecisionStrategy"] = _decisionStrategy.ToString();
        baseMetrics["Capability"] = Capability.ToString();
        
        return baseMetrics;
    }

    #endregion
}
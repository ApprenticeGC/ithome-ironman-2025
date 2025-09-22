using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// AI decision-making strategies for agents.
/// </summary>
public enum AIDecisionStrategy
{
    /// <summary>
    /// Simple rule-based decision making.
    /// </summary>
    RuleBased,
    
    /// <summary>
    /// Behavior tree-based decision making.
    /// </summary>
    BehaviorTree,
    
    /// <summary>
    /// State machine-based decision making.
    /// </summary>
    StateMachine,
    
    /// <summary>
    /// Machine learning-based decision making.
    /// </summary>
    MachineLearning,
    
    /// <summary>
    /// Hybrid approach combining multiple strategies.
    /// </summary>
    Hybrid
}

/// <summary>
/// AI agent capability levels.
/// </summary>
public enum AIAgentCapability
{
    /// <summary>
    /// Basic reactive agent that responds to immediate stimuli.
    /// </summary>
    Reactive,
    
    /// <summary>
    /// Proactive agent that can plan and initiate actions.
    /// </summary>
    Proactive,
    
    /// <summary>
    /// Social agent that can collaborate with other agents.
    /// </summary>
    Social,
    
    /// <summary>
    /// Learning agent that can adapt its behavior over time.
    /// </summary>
    Learning,
    
    /// <summary>
    /// Autonomous agent with full decision-making capabilities.
    /// </summary>
    Autonomous
}

/// <summary>
/// Represents contextual information for AI decision making.
/// </summary>
public class AIContext
{
    /// <summary>
    /// The current environment state.
    /// </summary>
    public IDictionary<string, object> EnvironmentState { get; }
    
    /// <summary>
    /// Available actions for the agent.
    /// </summary>
    public ICollection<string> AvailableActions { get; }
    
    /// <summary>
    /// Current goals for the agent.
    /// </summary>
    public ICollection<string> Goals { get; }
    
    /// <summary>
    /// Historical context and memory.
    /// </summary>
    public IDictionary<string, object> Memory { get; }

    /// <summary>
    /// Initializes a new instance of the AIContext class.
    /// </summary>
    public AIContext()
    {
        EnvironmentState = new Dictionary<string, object>();
        AvailableActions = new List<string>();
        Goals = new List<string>();
        Memory = new Dictionary<string, object>();
    }
}

/// <summary>
/// Represents an AI decision made by an agent.
/// </summary>
public class AIDecision
{
    /// <summary>
    /// The chosen action to take.
    /// </summary>
    public string Action { get; }
    
    /// <summary>
    /// Parameters for the chosen action.
    /// </summary>
    public IDictionary<string, object> Parameters { get; }
    
    /// <summary>
    /// Confidence level in this decision (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; }
    
    /// <summary>
    /// Reasoning or explanation for this decision.
    /// </summary>
    public string? Reasoning { get; }

    /// <summary>
    /// Initializes a new instance of the AIDecision class.
    /// </summary>
    /// <param name="action">The chosen action to take.</param>
    /// <param name="confidence">Confidence level in this decision.</param>
    /// <param name="parameters">Parameters for the chosen action.</param>
    /// <param name="reasoning">Optional reasoning for this decision.</param>
    public AIDecision(string action, float confidence, IDictionary<string, object>? parameters = null, string? reasoning = null)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Confidence = Math.Clamp(confidence, 0.0f, 1.0f);
        Parameters = parameters ?? new Dictionary<string, object>();
        Reasoning = reasoning;
    }
}

/// <summary>
/// Arguments for AI decision events.
/// </summary>
public class AIDecisionEventArgs : EventArgs
{
    /// <summary>
    /// The AI agent ID that made the decision.
    /// </summary>
    public string AgentId { get; }
    
    /// <summary>
    /// The context used for decision making.
    /// </summary>
    public AIContext Context { get; }
    
    /// <summary>
    /// The decision that was made.
    /// </summary>
    public AIDecision Decision { get; }

    /// <summary>
    /// Initializes a new instance of the AIDecisionEventArgs class.
    /// </summary>
    /// <param name="agentId">The AI agent ID that made the decision.</param>
    /// <param name="context">The context used for decision making.</param>
    /// <param name="decision">The decision that was made.</param>
    public AIDecisionEventArgs(string agentId, AIContext context, AIDecision decision)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Decision = decision ?? throw new ArgumentNullException(nameof(decision));
    }
}

/// <summary>
/// Tier 2: AI Agent interface that extends actor capabilities with artificial intelligence.
/// Provides decision-making, learning, and goal-oriented behavior for clustered AI agents
/// that can collaborate and adapt within the game environment.
/// </summary>
public interface IAIAgent : IActor
{
    /// <summary>
    /// Event raised when the AI agent makes a decision.
    /// </summary>
    event EventHandler<AIDecisionEventArgs>? DecisionMade;

    /// <summary>
    /// Gets the AI decision strategy used by this agent.
    /// </summary>
    AIDecisionStrategy DecisionStrategy { get; }
    
    /// <summary>
    /// Gets the capability level of this AI agent.
    /// </summary>
    AIAgentCapability Capability { get; }
    
    /// <summary>
    /// Gets the current goals of this AI agent.
    /// </summary>
    IReadOnlyCollection<string> Goals { get; }

    /// <summary>
    /// Sets the decision strategy for this AI agent.
    /// </summary>
    /// <param name="strategy">The decision strategy to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetDecisionStrategyAsync(AIDecisionStrategy strategy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a goal for this AI agent to pursue.
    /// </summary>
    /// <param name="goal">The goal to add.</param>
    /// <param name="priority">Priority level of the goal (higher values = higher priority).</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddGoalAsync(string goal, int priority = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a goal from this AI agent.
    /// </summary>
    /// <param name="goal">The goal to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RemoveGoalAsync(string goal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the environment context for decision making.
    /// </summary>
    /// <param name="context">The current environment context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateContextAsync(AIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a decision based on the current context and goals.
    /// </summary>
    /// <param name="context">The current context for decision making.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the AI decision.</returns>
    Task<AIDecision> MakeDecisionAsync(AIContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a decision by performing the chosen action.
    /// </summary>
    /// <param name="decision">The decision to execute.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async execution operation.</returns>
    Task ExecuteDecisionAsync(AIDecision decision, CancellationToken cancellationToken = default);

    /// <summary>
    /// Provides feedback to the AI agent about the outcome of a decision.
    /// </summary>
    /// <param name="decision">The decision that was executed.</param>
    /// <param name="outcome">The outcome of the decision (success, failure, etc.).</param>
    /// <param name="reward">Optional reward value for reinforcement learning.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async feedback operation.</returns>
    Task ProvideFeedbackAsync(AIDecision decision, string outcome, float? reward = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Collaborates with other AI agents in the same cluster.
    /// </summary>
    /// <param name="task">The collaborative task to perform.</param>
    /// <param name="participantIds">IDs of other agents to collaborate with.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async collaboration operation.</returns>
    Task CollaborateAsync(string task, ICollection<string> participantIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Shares knowledge or experience with other AI agents in the cluster.
    /// </summary>
    /// <param name="knowledge">The knowledge to share.</param>
    /// <param name="targetAgentIds">IDs of agents to share knowledge with, or null for all cluster members.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async knowledge sharing operation.</returns>
    Task ShareKnowledgeAsync(IDictionary<string, object> knowledge, ICollection<string>? targetAgentIds = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current knowledge base of this AI agent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the knowledge base.</returns>
    Task<IDictionary<string, object>> GetKnowledgeBaseAsync(CancellationToken cancellationToken = default);
}
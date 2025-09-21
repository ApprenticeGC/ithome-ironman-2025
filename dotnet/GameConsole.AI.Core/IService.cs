using GameConsole.AI.Core.Models;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Services;

/// <summary>
/// Core AI orchestration service interface for managing AI agents and their lifecycle.
/// Provides agent discovery, context management, and high-level AI operations.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService, ICapabilityProvider
{
    #region Agent Discovery and Management

    /// <summary>
    /// Gets all available AI agents in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the list of available agent identifiers.</returns>
    Task<IReadOnlyList<string>> GetAvailableAgentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the agent's metadata, or null if not found.</returns>
    Task<AIAgentMetadata?> GetAgentInfoAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for agents that match specific criteria or capabilities.
    /// </summary>
    /// <param name="criteria">The search criteria for finding suitable agents.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the list of matching agent identifiers.</returns>
    Task<IReadOnlyList<string>> SearchAgentsAsync(AgentSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a new AI agent with the service.
    /// </summary>
    /// <param name="agent">The agent instance to register.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent was successfully registered.</returns>
    Task<bool> RegisterAgentAsync(IAIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregisters an AI agent from the service.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to unregister.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the agent was successfully unregistered.</returns>
    Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default);

    #endregion

    #region Agent Execution

    /// <summary>
    /// Executes a request using a specific agent and returns the result.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to use.</param>
    /// <param name="input">The input data or prompt for the agent.</param>
    /// <param name="options">Optional execution parameters and configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the agent's response.</returns>
    Task<string> InvokeAgentAsync(string agentId, string input, AgentExecutionOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a request using a specific agent and streams the results.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent to use.</param>
    /// <param name="input">The input data or prompt for the agent.</param>
    /// <param name="options">Optional execution parameters and configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>An async enumerable that yields response chunks as they are generated.</returns>
    IAsyncEnumerable<string> StreamAgentAsync(string agentId, string input, AgentExecutionOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically selects and invokes the most appropriate agent for a given input.
    /// </summary>
    /// <param name="input">The input data or prompt.</param>
    /// <param name="criteria">Optional criteria for agent selection.</param>
    /// <param name="options">Optional execution parameters and configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the selected agent's response and metadata.</returns>
    Task<AgentResponse> InvokeAutoAsync(string input, AgentSearchCriteria? criteria = null, AgentExecutionOptions? options = null, CancellationToken cancellationToken = default);

    #endregion

    #region Context Management

    /// <summary>
    /// Creates a new execution context for agent operations.
    /// </summary>
    /// <param name="securityLevel">The security level for the context.</param>
    /// <param name="resourceLimits">Optional resource limits for the context.</param>
    /// <param name="configuration">Optional configuration settings for the context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the newly created context.</returns>
    Task<IAIContext> CreateContextAsync(
        GameConsole.AI.Core.SecurityLevel securityLevel = GameConsole.AI.Core.SecurityLevel.Basic,
        IReadOnlyList<GameConsole.AI.Core.ResourceRequirement>? resourceLimits = null,
        IReadOnlyDictionary<string, object>? configuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a conversation session that maintains context across multiple interactions.
    /// </summary>
    /// <param name="agentId">The agent to use for the conversation.</param>
    /// <param name="options">Optional conversation configuration.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the conversation identifier.</returns>
    Task<string> CreateConversationAsync(string agentId, ConversationOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ends a conversation session and releases associated resources.
    /// </summary>
    /// <param name="conversationId">The unique identifier of the conversation to end.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns true if the conversation was successfully ended.</returns>
    Task<bool> EndConversationAsync(string conversationId, CancellationToken cancellationToken = default);

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// Gets performance metrics for a specific agent.
    /// </summary>
    /// <param name="agentId">The unique identifier of the agent.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the agent's performance metrics.</returns>
    Task<IReadOnlyList<GameConsole.AI.Core.PerformanceMetric>> GetAgentMetricsAsync(string agentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets system-wide performance metrics for all AI operations.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the system performance metrics.</returns>
    Task<IReadOnlyList<GameConsole.AI.Core.PerformanceMetric>> GetSystemMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current resource utilization across all agents and contexts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that returns the current resource utilization metrics.</returns>
    Task<ResourceUtilization> GetResourceUtilizationAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Configuration

    /// <summary>
    /// Initializes the AI service with a specific profile configuration.
    /// </summary>
    /// <param name="profile">The AI profile configuration to use.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async initialization operation.</returns>
    Task InitializeAsync(AIProfile profile, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the service configuration at runtime.
    /// </summary>
    /// <param name="configuration">The new configuration settings.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async configuration update operation.</returns>
    Task UpdateConfigurationAsync(IReadOnlyDictionary<string, object> configuration, CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Represents search criteria for finding suitable AI agents.
/// </summary>
public class AgentSearchCriteria
{
    /// <summary>
    /// Gets or sets the required capabilities that agents must possess.
    /// </summary>
    public IReadOnlyList<string> RequiredCapabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the preferred capabilities that would enhance the agent's suitability.
    /// </summary>
    public IReadOnlyList<string> PreferredCapabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the tags or categories that agents should match.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the maximum resource requirements for suitable agents.
    /// </summary>
    public IReadOnlyList<GameConsole.AI.Core.ResourceRequirement> MaxResourceRequirements { get; set; } = Array.Empty<GameConsole.AI.Core.ResourceRequirement>();

    /// <summary>
    /// Gets or sets the minimum version requirement for agents.
    /// </summary>
    public Version? MinimumVersion { get; set; }

    /// <summary>
    /// Gets or sets the supported input types that agents must handle.
    /// </summary>
    public IReadOnlyList<string> SupportedInputTypes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets additional search parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents the response from an agent invocation, including metadata about the execution.
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// Gets the identifier of the agent that generated the response.
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Gets the actual response content from the agent.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the execution time for the request.
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }

    /// <summary>
    /// Gets any performance metrics collected during execution.
    /// </summary>
    public IReadOnlyList<GameConsole.AI.Core.PerformanceMetric> Metrics { get; init; } = Array.Empty<GameConsole.AI.Core.PerformanceMetric>();

    /// <summary>
    /// Gets additional metadata about the execution.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Configuration options for conversation sessions.
/// </summary>
public class ConversationOptions
{
    /// <summary>
    /// Gets or sets the maximum number of messages to retain in conversation history.
    /// </summary>
    public int MaxHistorySize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the timeout for conversation sessions.
    /// </summary>
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets additional configuration parameters for the conversation.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Represents current resource utilization across the AI system.
/// </summary>
public class ResourceUtilization
{
    /// <summary>
    /// Gets the current resource usage by type.
    /// </summary>
    public IReadOnlyDictionary<GameConsole.AI.Core.ResourceType, double> Usage { get; init; } = new Dictionary<GameConsole.AI.Core.ResourceType, double>();

    /// <summary>
    /// Gets the resource limits by type.
    /// </summary>
    public IReadOnlyDictionary<GameConsole.AI.Core.ResourceType, double> Limits { get; init; } = new Dictionary<GameConsole.AI.Core.ResourceType, double>();

    /// <summary>
    /// Gets the number of active agents by status.
    /// </summary>
    public IReadOnlyDictionary<GameConsole.AI.Core.AgentStatus, int> AgentCounts { get; init; } = new Dictionary<GameConsole.AI.Core.AgentStatus, int>();

    /// <summary>
    /// Gets the number of active contexts.
    /// </summary>
    public int ActiveContexts { get; init; }

    /// <summary>
    /// Gets the timestamp when these metrics were collected.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// AI profile configuration for different operational modes.
/// </summary>
public class AIProfile
{
    /// <summary>
    /// Gets or sets the name of the profile.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the default security level for agents in this profile.
    /// </summary>
    public GameConsole.AI.Core.SecurityLevel DefaultSecurityLevel { get; set; } = GameConsole.AI.Core.SecurityLevel.Basic;

    /// <summary>
    /// Gets or sets the default resource limits for this profile.
    /// </summary>
    public IReadOnlyList<GameConsole.AI.Core.ResourceRequirement> DefaultResourceLimits { get; set; } = Array.Empty<GameConsole.AI.Core.ResourceRequirement>();

    /// <summary>
    /// Gets or sets profile-specific configuration settings.
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of agents that should be automatically loaded for this profile.
    /// </summary>
    public IReadOnlyList<string> AutoLoadAgents { get; set; } = Array.Empty<string>();
}
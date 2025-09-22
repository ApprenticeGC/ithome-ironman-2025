namespace GameConsole.AI.Discovery;

/// <summary>
/// Interface for matching agents to task requirements.
/// Provides scoring and ranking capabilities for agent selection.
/// </summary>
public interface ICapabilityMatcher
{
    /// <summary>
    /// Finds agents that match the specified task requirements.
    /// </summary>
    /// <param name="requirements">Task requirements to match against.</param>
    /// <param name="availableAgents">Collection of available agents to consider.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task returning a collection of agent matches scored and ranked by suitability.</returns>
    Task<IEnumerable<AgentMatch>> FindMatchingAgentsAsync(
        TaskRequirements requirements,
        IEnumerable<AgentMetadata> availableAgents,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates a compatibility score between an agent and task requirements.
    /// </summary>
    /// <param name="agent">Agent metadata to score.</param>
    /// <param name="requirements">Task requirements to score against.</param>
    /// <returns>Compatibility score (0.0 = no match, 1.0 = perfect match).</returns>
    double CalculateCompatibilityScore(AgentMetadata agent, TaskRequirements requirements);

    /// <summary>
    /// Checks if an agent meets the minimum requirements for a task.
    /// </summary>
    /// <param name="agent">Agent metadata to check.</param>
    /// <param name="requirements">Task requirements to check against.</param>
    /// <returns>True if the agent meets minimum requirements, false otherwise.</returns>
    bool MeetsMinimumRequirements(AgentMetadata agent, TaskRequirements requirements);
}

/// <summary>
/// Represents task requirements for agent matching.
/// </summary>
public class TaskRequirements
{
    /// <summary>
    /// Required capabilities that the agent must provide.
    /// </summary>
    public IReadOnlyList<Type> RequiredCapabilities { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Preferred capabilities that are beneficial but not required.
    /// </summary>
    public IReadOnlyList<Type> PreferredCapabilities { get; init; } = Array.Empty<Type>();

    /// <summary>
    /// Required tags that the agent must have.
    /// </summary>
    public IReadOnlyList<string> RequiredTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Preferred tags that are beneficial but not required.
    /// </summary>
    public IReadOnlyList<string> PreferredTags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Minimum priority level required.
    /// </summary>
    public int MinimumPriority { get; init; } = 0;

    /// <summary>
    /// Maximum resource requirements that can be accommodated.
    /// </summary>
    public AgentResourceRequirements MaxResourceLimits { get; init; } = new AgentResourceRequirements
    {
        MinMemoryBytes = 0,
        MaxMemoryBytes = long.MaxValue,
        RequiredCpuCores = 0,
        RequiresGpu = false,
        NetworkAccess = NetworkAccessLevel.Internet,
        InitializationTimeoutMs = 60000
    };

    /// <summary>
    /// Whether to only consider available agents.
    /// </summary>
    public bool OnlyAvailableAgents { get; init; } = true;
}

/// <summary>
/// Represents a match between an agent and task requirements.
/// </summary>
public class AgentMatch
{
    /// <summary>
    /// The matched agent metadata.
    /// </summary>
    public required AgentMetadata Agent { get; init; }

    /// <summary>
    /// Compatibility score (0.0 to 1.0).
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// Detailed scoring breakdown for analysis.
    /// </summary>
    public MatchScoreBreakdown ScoreBreakdown { get; init; } = new();

    /// <summary>
    /// Whether the agent meets all minimum requirements.
    /// </summary>
    public bool MeetsMinimumRequirements { get; init; }
}

/// <summary>
/// Detailed breakdown of matching scores for transparency.
/// </summary>
public class MatchScoreBreakdown
{
    /// <summary>
    /// Score for required capabilities (0.0 to 1.0).
    /// </summary>
    public double RequiredCapabilitiesScore { get; init; }

    /// <summary>
    /// Score for preferred capabilities (0.0 to 1.0).
    /// </summary>
    public double PreferredCapabilitiesScore { get; init; }

    /// <summary>
    /// Score for required tags (0.0 to 1.0).
    /// </summary>
    public double RequiredTagsScore { get; init; }

    /// <summary>
    /// Score for preferred tags (0.0 to 1.0).
    /// </summary>
    public double PreferredTagsScore { get; init; }

    /// <summary>
    /// Score for priority matching (0.0 to 1.0).
    /// </summary>
    public double PriorityScore { get; init; }

    /// <summary>
    /// Score for resource compatibility (0.0 to 1.0).
    /// </summary>
    public double ResourceScore { get; init; }

    /// <summary>
    /// Score for availability (0.0 to 1.0).
    /// </summary>
    public double AvailabilityScore { get; init; }
}
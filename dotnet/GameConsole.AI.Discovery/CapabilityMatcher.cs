using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Discovery;

/// <summary>
/// Implementation of capability matcher using weighted scoring algorithm.
/// Provides intelligent agent selection based on multiple criteria.
/// </summary>
public class CapabilityMatcher : ICapabilityMatcher
{
    private readonly ILogger<CapabilityMatcher> _logger;
    
    // Weights for different scoring components (should sum to 1.0)
    private const double RequiredCapabilitiesWeight = 0.30;
    private const double PreferredCapabilitiesWeight = 0.20;
    private const double RequiredTagsWeight = 0.15;
    private const double PreferredTagsWeight = 0.10;
    private const double PriorityWeight = 0.10;
    private const double ResourceWeight = 0.10;
    private const double AvailabilityWeight = 0.05;

    public CapabilityMatcher(ILogger<CapabilityMatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentMatch>> FindMatchingAgentsAsync(
        TaskRequirements requirements,
        IEnumerable<AgentMetadata> availableAgents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(availableAgents);

        _logger.LogDebug("Finding matching agents for task requirements");

        var matches = new List<AgentMatch>();

        await Task.Run(() =>
        {
            foreach (var agent in availableAgents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip unavailable agents if required
                if (requirements.OnlyAvailableAgents && !agent.IsAvailable)
                    continue;

                var meetsMinimum = MeetsMinimumRequirements(agent, requirements);
                var score = CalculateCompatibilityScore(agent, requirements);
                var breakdown = CalculateScoreBreakdown(agent, requirements);

                var match = new AgentMatch
                {
                    Agent = agent,
                    Score = score,
                    ScoreBreakdown = breakdown,
                    MeetsMinimumRequirements = meetsMinimum
                };

                matches.Add(match);
                _logger.LogTrace("Agent {AgentId} scored {Score:F3}", agent.Id, score);
            }
        }, cancellationToken);

        // Sort by score (descending) and then by priority (descending)
        var sortedMatches = matches
            .OrderByDescending(m => m.Score)
            .ThenByDescending(m => m.Agent.Priority)
            .ToList();

        _logger.LogInformation("Found {Count} agent matches, top score: {TopScore:F3}", 
            sortedMatches.Count, 
            sortedMatches.FirstOrDefault()?.Score ?? 0);

        return sortedMatches;
    }

    /// <inheritdoc />
    public double CalculateCompatibilityScore(AgentMetadata agent, TaskRequirements requirements)
    {
        var breakdown = CalculateScoreBreakdown(agent, requirements);
        
        var totalScore = 
            breakdown.RequiredCapabilitiesScore * RequiredCapabilitiesWeight +
            breakdown.PreferredCapabilitiesScore * PreferredCapabilitiesWeight +
            breakdown.RequiredTagsScore * RequiredTagsWeight +
            breakdown.PreferredTagsScore * PreferredTagsWeight +
            breakdown.PriorityScore * PriorityWeight +
            breakdown.ResourceScore * ResourceWeight +
            breakdown.AvailabilityScore * AvailabilityWeight;

        return Math.Max(0.0, Math.Min(1.0, totalScore));
    }

    /// <inheritdoc />
    public bool MeetsMinimumRequirements(AgentMetadata agent, TaskRequirements requirements)
    {
        // Check required capabilities
        foreach (var requiredCapability in requirements.RequiredCapabilities)
        {
            if (!agent.Capabilities.Contains(requiredCapability) && 
                !agent.AgentType.IsAssignableTo(requiredCapability))
            {
                return false;
            }
        }

        // Check required tags
        foreach (var requiredTag in requirements.RequiredTags)
        {
            if (!agent.Tags.Contains(requiredTag))
            {
                return false;
            }
        }

        // Check minimum priority
        if (agent.Priority < requirements.MinimumPriority)
        {
            return false;
        }

        // Check resource constraints
        if (!IsWithinResourceLimits(agent.ResourceRequirements, requirements.MaxResourceLimits))
        {
            return false;
        }

        // Check availability if required
        if (requirements.OnlyAvailableAgents && !agent.IsAvailable)
        {
            return false;
        }

        return true;
    }

    private MatchScoreBreakdown CalculateScoreBreakdown(AgentMetadata agent, TaskRequirements requirements)
    {
        return new MatchScoreBreakdown
        {
            RequiredCapabilitiesScore = CalculateCapabilitiesScore(agent, requirements.RequiredCapabilities),
            PreferredCapabilitiesScore = CalculateCapabilitiesScore(agent, requirements.PreferredCapabilities),
            RequiredTagsScore = CalculateTagsScore(agent, requirements.RequiredTags),
            PreferredTagsScore = CalculateTagsScore(agent, requirements.PreferredTags),
            PriorityScore = CalculatePriorityScore(agent, requirements),
            ResourceScore = CalculateResourceScore(agent, requirements),
            AvailabilityScore = agent.IsAvailable ? 1.0 : 0.0
        };
    }

    private double CalculateCapabilitiesScore(AgentMetadata agent, IReadOnlyList<Type> requiredCapabilities)
    {
        if (requiredCapabilities.Count == 0)
            return 1.0;

        var matchedCapabilities = 0;
        foreach (var capability in requiredCapabilities)
        {
            if (agent.Capabilities.Contains(capability) || agent.AgentType.IsAssignableTo(capability))
            {
                matchedCapabilities++;
            }
        }

        return (double)matchedCapabilities / requiredCapabilities.Count;
    }

    private double CalculateTagsScore(AgentMetadata agent, IReadOnlyList<string> requiredTags)
    {
        if (requiredTags.Count == 0)
            return 1.0;

        var matchedTags = requiredTags.Count(tag => agent.Tags.Contains(tag));
        return (double)matchedTags / requiredTags.Count;
    }

    private double CalculatePriorityScore(AgentMetadata agent, TaskRequirements requirements)
    {
        if (agent.Priority < requirements.MinimumPriority)
            return 0.0;

        // Higher priority agents get higher scores, but with diminishing returns
        var excessPriority = agent.Priority - requirements.MinimumPriority;
        return Math.Min(1.0, 0.5 + (excessPriority * 0.1));
    }

    private double CalculateResourceScore(AgentMetadata agent, TaskRequirements requirements)
    {
        if (!IsWithinResourceLimits(agent.ResourceRequirements, requirements.MaxResourceLimits))
            return 0.0;

        // Agents with lower resource requirements get slightly higher scores
        var memoryRatio = (double)agent.ResourceRequirements.MinMemoryBytes / requirements.MaxResourceLimits.MaxMemoryBytes;
        var memoryScore = Math.Max(0.0, 1.0 - memoryRatio);

        return memoryScore;
    }

    private bool IsWithinResourceLimits(AgentResourceRequirements agentRequirements, AgentResourceRequirements maxLimits)
    {
        return agentRequirements.MinMemoryBytes <= maxLimits.MaxMemoryBytes &&
               agentRequirements.RequiredCpuCores <= maxLimits.RequiredCpuCores &&
               (!agentRequirements.RequiresGpu || maxLimits.RequiresGpu) &&
               agentRequirements.NetworkAccess <= maxLimits.NetworkAccess &&
               agentRequirements.InitializationTimeoutMs <= maxLimits.InitializationTimeoutMs;
    }
}
namespace GameConsole.AI.Core;

/// <summary>
/// Defines the capabilities and skills of an AI agent.
/// </summary>
public interface IAIAgentCapabilities
{
    /// <summary>
    /// Gets the types of requests this agent can handle.
    /// </summary>
    IEnumerable<Type> SupportedRequestTypes { get; }

    /// <summary>
    /// Gets the agent's skill domains (e.g., "natural_language", "image_processing", "reasoning").
    /// </summary>
    IEnumerable<string> SkillDomains { get; }

    /// <summary>
    /// Gets the agent's processing capabilities (e.g., "text_generation", "classification", "analysis").
    /// </summary>
    IEnumerable<string> ProcessingCapabilities { get; }

    /// <summary>
    /// Gets the maximum number of concurrent requests this agent can handle.
    /// </summary>
    int MaxConcurrentRequests { get; }

    /// <summary>
    /// Gets the estimated processing time range in milliseconds for typical requests.
    /// </summary>
    (int MinMs, int MaxMs) EstimatedProcessingTime { get; }

    /// <summary>
    /// Gets additional metadata about the agent's capabilities.
    /// </summary>
    IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Checks if the agent has a specific capability.
    /// </summary>
    /// <param name="capability">The capability to check for.</param>
    /// <returns>True if the agent has the capability, false otherwise.</returns>
    bool HasCapability(string capability);

    /// <summary>
    /// Gets the confidence level (0-100) for handling a specific request type.
    /// </summary>
    /// <param name="requestType">The request type to evaluate.</param>
    /// <returns>Confidence level from 0 (cannot handle) to 100 (expert level).</returns>
    int GetConfidenceLevel(Type requestType);
}
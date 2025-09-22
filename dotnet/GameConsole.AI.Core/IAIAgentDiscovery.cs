using System.Reflection;
using GameConsole.Core.Abstractions;

namespace GameConsole.AI.Core;

/// <summary>
/// Represents information about a discovered AI agent.
/// </summary>
public class DiscoveredAIAgent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveredAIAgent"/> class.
    /// </summary>
    /// <param name="agentType">The type of the AI agent.</param>
    /// <param name="attribute">The AI agent attribute metadata.</param>
    public DiscoveredAIAgent(Type agentType, AIAgentAttribute attribute)
    {
        AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
    }

    /// <summary>
    /// Gets the type of the AI agent.
    /// </summary>
    public Type AgentType { get; }

    /// <summary>
    /// Gets the AI agent attribute metadata.
    /// </summary>
    public AIAgentAttribute Attribute { get; }

    /// <summary>
    /// Gets the unique identifier for the AI agent.
    /// </summary>
    public string Id => Attribute.Id;

    /// <summary>
    /// Gets the name of the AI agent.
    /// </summary>
    public string Name => Attribute.Name;

    /// <summary>
    /// Gets the capabilities of the AI agent.
    /// </summary>
    public AIAgentCapability Capabilities => Attribute.Capabilities;
}

/// <summary>
/// Service for discovering AI agents in assemblies.
/// Implements RFC-007-02: AI Agent Discovery Service.
/// </summary>
public interface IAIAgentDiscovery : IService
{
    /// <summary>
    /// Discovers all AI agents in the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to scan for AI agents.</param>
    /// <returns>A collection of discovered AI agents.</returns>
    IEnumerable<DiscoveredAIAgent> DiscoverAgents(Assembly assembly);

    /// <summary>
    /// Discovers all AI agents in the specified assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for AI agents.</param>
    /// <returns>A collection of discovered AI agents.</returns>
    IEnumerable<DiscoveredAIAgent> DiscoverAgents(IEnumerable<Assembly> assemblies);

    /// <summary>
    /// Discovers all AI agents in the current application domain.
    /// </summary>
    /// <returns>A collection of discovered AI agents.</returns>
    IEnumerable<DiscoveredAIAgent> DiscoverAllAgents();

    /// <summary>
    /// Validates that the specified type is a valid AI agent implementation.
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <returns>True if the type is a valid AI agent, false otherwise.</returns>
    bool ValidateAgentType(Type type);

    /// <summary>
    /// Gets detailed validation information for the specified type.
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <returns>A validation result with details about any issues found.</returns>
    AIAgentValidationResult ValidateAgentTypeDetailed(Type type);
}

/// <summary>
/// Represents the result of validating an AI agent type.
/// </summary>
public class AIAgentValidationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether the AI agent type is valid.</param>
    /// <param name="errors">The validation errors, if any.</param>
    public AIAgentValidationResult(bool isValid, IEnumerable<string>? errors = null)
    {
        IsValid = isValid;
        Errors = errors?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Gets a value indicating whether the AI agent type is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation errors, if any.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static AIAgentValidationResult Success() => new(true);

    /// <summary>
    /// Creates a failed validation result with the specified errors.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static AIAgentValidationResult Failure(params string[] errors) => new(false, errors);
}
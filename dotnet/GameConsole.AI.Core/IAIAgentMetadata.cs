using GameConsole.Plugins.Core;

namespace GameConsole.AI.Core;

/// <summary>
/// Represents metadata specific to AI agents, extending plugin metadata with AI-specific information.
/// </summary>
public interface IAIAgentMetadata : IPluginMetadata
{
    /// <summary>
    /// Gets the type of AI agent (e.g., "conversational", "decision-making", "automation").
    /// </summary>
    string AgentType { get; }

    /// <summary>
    /// Gets the capabilities that this AI agent provides.
    /// </summary>
    IReadOnlyList<string> Capabilities { get; }

    /// <summary>
    /// Gets the resource requirements for this AI agent.
    /// </summary>
    IAIAgentResourceRequirements ResourceRequirements { get; }

    /// <summary>
    /// Gets the supported communication protocols for this AI agent.
    /// </summary>
    IReadOnlyList<string> SupportedProtocols { get; }

    /// <summary>
    /// Gets a value indicating whether this agent can learn and adapt during runtime.
    /// </summary>
    bool SupportsLearning { get; }
}
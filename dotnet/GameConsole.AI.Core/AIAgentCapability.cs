namespace GameConsole.AI.Core;

/// <summary>
/// Defines the capabilities that an AI agent can possess.
/// Used for categorizing and querying AI agents by their functionality.
/// </summary>
[Flags]
public enum AIAgentCapability
{
    /// <summary>
    /// No specific capabilities defined.
    /// </summary>
    None = 0,

    /// <summary>
    /// Navigation and pathfinding capabilities.
    /// </summary>
    PathFinding = 1 << 0,

    /// <summary>
    /// Decision making and behavior tree capabilities.
    /// </summary>
    DecisionMaking = 1 << 1,

    /// <summary>
    /// Character animation and state management capabilities.
    /// </summary>
    Animation = 1 << 2,

    /// <summary>
    /// Dialogue and conversation system capabilities.
    /// </summary>
    Dialogue = 1 << 3,

    /// <summary>
    /// Combat and tactical AI capabilities.
    /// </summary>
    Combat = 1 << 4,

    /// <summary>
    /// Environmental interaction capabilities.
    /// </summary>
    EnvironmentInteraction = 1 << 5
}
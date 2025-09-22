namespace GameConsole.Core.Abstractions;

/// <summary>
/// Base interface for AI agent capabilities.
/// Capabilities define specific functions or services that an AI agent can provide.
/// </summary>
public interface IAIAgentCapability
{
    /// <summary>
    /// Gets the name of this capability.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of this capability implementation.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets a description of what this capability provides.
    /// </summary>
    string Description { get; }
}
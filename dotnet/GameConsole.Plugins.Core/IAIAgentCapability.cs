namespace GameConsole.Plugins.Core;

/// <summary>
/// Marker interface for AI agent capabilities.
/// Implementing this interface indicates that a type represents a specific AI capability
/// that can be provided by an AI agent.
/// </summary>
public interface IAIAgentCapability
{
    /// <summary>
    /// Gets the unique name identifier for this capability.
    /// This should be a stable identifier that doesn't change across versions.
    /// </summary>
    string CapabilityName { get; }

    /// <summary>
    /// Gets a human-readable description of what this capability provides.
    /// </summary>
    string Description { get; }
}
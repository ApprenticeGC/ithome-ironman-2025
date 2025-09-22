namespace GameConsole.AI.Core;

/// <summary>
/// Defines resource requirements for an AI agent.
/// </summary>
public interface IAIAgentResourceRequirements
{
    /// <summary>
    /// Gets the minimum amount of memory required in MB.
    /// </summary>
    int MinMemoryMB { get; }

    /// <summary>
    /// Gets the recommended amount of memory in MB.
    /// </summary>
    int RecommendedMemoryMB { get; }

    /// <summary>
    /// Gets a value indicating whether GPU acceleration is required.
    /// </summary>
    bool RequiresGPU { get; }

    /// <summary>
    /// Gets a value indicating whether network connectivity is required.
    /// </summary>
    bool RequiresNetwork { get; }

    /// <summary>
    /// Gets the maximum concurrent instances supported.
    /// </summary>
    int MaxConcurrentInstances { get; }
}
namespace GameConsole.AI.Discovery;

/// <summary>
/// Represents the resource requirements for an AI agent.
/// </summary>
public class AgentResourceRequirements
{
    /// <summary>
    /// Minimum required memory in bytes.
    /// </summary>
    public long MinMemoryBytes { get; init; } = 0;

    /// <summary>
    /// Maximum memory the agent might use in bytes.
    /// </summary>
    public long MaxMemoryBytes { get; init; } = long.MaxValue;

    /// <summary>
    /// Required CPU cores (0 = any available).
    /// </summary>
    public int RequiredCpuCores { get; init; } = 0;

    /// <summary>
    /// Whether GPU acceleration is required.
    /// </summary>
    public bool RequiresGpu { get; init; } = false;

    /// <summary>
    /// Network access requirements.
    /// </summary>
    public NetworkAccessLevel NetworkAccess { get; init; } = NetworkAccessLevel.None;

    /// <summary>
    /// Initialization timeout in milliseconds.
    /// </summary>
    public int InitializationTimeoutMs { get; init; } = 30000;
}

/// <summary>
/// Levels of network access required by an agent.
/// </summary>
public enum NetworkAccessLevel
{
    /// <summary>
    /// No network access required.
    /// </summary>
    None = 0,

    /// <summary>
    /// Local network access only.
    /// </summary>
    Local = 1,

    /// <summary>
    /// Internet access required.
    /// </summary>
    Internet = 2
}
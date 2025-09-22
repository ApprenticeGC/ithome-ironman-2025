namespace GameConsole.AI.Core;

/// <summary>
/// Represents the current status and health of an AI agent.
/// </summary>
public interface IAIAgentStatus
{
    /// <summary>
    /// Gets a value indicating whether the agent is healthy and operational.
    /// </summary>
    bool IsHealthy { get; }

    /// <summary>
    /// Gets the current state of the agent (e.g., "idle", "processing", "training", "error").
    /// </summary>
    string State { get; }

    /// <summary>
    /// Gets the number of requests processed since startup.
    /// </summary>
    long RequestsProcessed { get; }

    /// <summary>
    /// Gets the average response time in milliseconds.
    /// </summary>
    double AverageResponseTimeMs { get; }

    /// <summary>
    /// Gets the current memory usage in MB.
    /// </summary>
    double MemoryUsageMB { get; }

    /// <summary>
    /// Gets the CPU usage percentage.
    /// </summary>
    double CpuUsagePercent { get; }

    /// <summary>
    /// Gets the timestamp of the last successful operation.
    /// </summary>
    DateTimeOffset LastActivity { get; }

    /// <summary>
    /// Gets additional status information and metrics.
    /// </summary>
    IReadOnlyDictionary<string, object> AdditionalMetrics { get; }
}
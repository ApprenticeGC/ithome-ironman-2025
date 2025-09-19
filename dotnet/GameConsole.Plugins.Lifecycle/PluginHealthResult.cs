using GameConsole.Plugins.Core;

namespace GameConsole.Plugins.Lifecycle;

/// <summary>
/// Represents health check result for a plugin.
/// </summary>
public class PluginHealthResult
{
    /// <summary>
    /// Gets the plugin that was health checked.
    /// </summary>
    public required IPlugin Plugin { get; init; }

    /// <summary>
    /// Gets whether the plugin is healthy.
    /// </summary>
    public required bool IsHealthy { get; init; }

    /// <summary>
    /// Gets the timestamp when the health check was performed.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the response time for the health check operation.
    /// </summary>
    public required TimeSpan ResponseTime { get; init; }

    /// <summary>
    /// Gets any error that occurred during health check.
    /// </summary>
    public Exception? Error { get; init; }

    /// <summary>
    /// Gets additional health check data.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}
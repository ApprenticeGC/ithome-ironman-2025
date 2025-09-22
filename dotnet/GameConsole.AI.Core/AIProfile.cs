using System;

namespace GameConsole.AI.Core;

/// <summary>
/// Configuration profile for AI agent behavior and resource allocation.
/// </summary>
public class AIProfile
{
    /// <summary>
    /// The task kind this profile is optimized for.
    /// </summary>
    public TaskKind TaskKind { get; set; }

    /// <summary>
    /// Base URL for the AI backend service.
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434/v1";

    /// <summary>
    /// Model name to use for AI operations.
    /// </summary>
    public string Model { get; set; } = "llama3.1:8b";

    /// <summary>
    /// Maximum number of tokens for responses.
    /// </summary>
    public int MaxTokens { get; set; } = 256;

    /// <summary>
    /// Temperature setting for response generation (0.0 to 1.0).
    /// </summary>
    public float Temperature { get; set; } = 0.6f;

    /// <summary>
    /// Maximum number of parallel agent operations.
    /// </summary>
    public int MaxParallel { get; set; } = 2;

    /// <summary>
    /// Timeout for AI operations.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Array of enabled tool names for this profile.
    /// </summary>
    public string[] EnabledTools { get; set; } = new string[0];

    /// <summary>
    /// Whether to use a remote gateway for AI operations.
    /// </summary>
    public bool UseRemoteGateway { get; set; } = false;

    /// <summary>
    /// Gateway host when using remote gateway.
    /// </summary>
    public string? GatewayHost { get; set; }

    /// <summary>
    /// Gateway port when using remote gateway.
    /// </summary>
    public int GatewayPort { get; set; } = 4053;
}

/// <summary>
/// Enumeration of task kinds that determine AI profile behavior.
/// </summary>
public enum TaskKind
{
    /// <summary>
    /// Long context, rich tools, higher latency OK.
    /// </summary>
    EditorAuthoring,

    /// <summary>
    /// Asset QA, import validation, batch processing.
    /// </summary>
    EditorAnalysis,

    /// <summary>
    /// Tight budget, low latency, PCG steering.
    /// </summary>
    RuntimeDirector,

    /// <summary>
    /// Player help, lore queries, safe subset only.
    /// </summary>
    RuntimeCodex
}